using Pdf2Md.Converters;
using Pdf2Md.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text;

namespace Pdf2Md.Commands;

/// <summary>Main CLI command: converts a PDF to HTML and/or Markdown.</summary>
public sealed class ConvertCommand : AsyncCommand<ConvertSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        ConvertSettings settings,
        CancellationToken cancellationToken)
    {
        // ── validate input file ────────────────────────────────────────────
        if (!File.Exists(settings.InputPath))
        {
            AnsiConsole.MarkupLine($"[bold red]Error:[/] File not found: [yellow]{settings.InputPath}[/]");
            return 1;
        }

        var inputFullPath = Path.GetFullPath(settings.InputPath);
        var inputDir = Path.GetDirectoryName(inputFullPath) ?? ".";
        var inputName = Path.GetFileNameWithoutExtension(inputFullPath);

        var basePath = settings.OutputPath is not null
            ? Path.GetFullPath(settings.OutputPath)
            : Path.Combine(inputDir, inputName);

        var format = settings.Format.ToLowerInvariant();
        var imagesDir = ResolveImagesDirectory(settings, basePath);

        // ── render welcome header ──────────────────────────────────────────
        AnsiConsole.Write(
            new FigletText("pdf2md")
                .LeftJustified()
                .Color(Color.Blue));

        AnsiConsole.Write(new Rule("[blue]PDF Converter[/]").RuleStyle("grey").LeftJustified());
        AnsiConsole.MarkupLine($"  [grey]Input :[/]  [white]{inputFullPath}[/]");
        AnsiConsole.MarkupLine($"  [grey]Format:[/]  [white]{format}[/]");
        if (imagesDir is not null)
            AnsiConsole.MarkupLine($"  [grey]Images:[/]  [white]{imagesDir}[/]");
        AnsiConsole.WriteLine();

        // ── run conversion with progress ───────────────────────────────────
        var outputs = new List<string>();
        int errorCode = 0;

        await AnsiConsole.Progress()
            .AutoRefresh(true)
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var totalSteps = format is "both" ? 2 : 1;
                double stepSize = 100.0 / totalSteps;

                var task = ctx.AddTask("[green]Converting PDF[/]", maxValue: 100);

                try
                {
                    if (format is "md" or "both")
                    {
                        task.Description = "[green]Converting to Markdown[/]";
                        var converter = new PdfToMarkdownConverter();
                        var mdPath = $"{basePath}.md";
                        var mdImageRoot = imagesDir is not null
                            ? ImageReferencePath.GetReferenceRoot(imagesDir, mdPath)
                            : null;

                        var markdownOutputs = settings.SplitByTitle
                            ? converter.ConvertSplitByTitle(inputFullPath, imagesDir, mdImageRoot)
                            : new[]
                            {
                                new MarkdownDocumentPart(
                                    Path.GetFileNameWithoutExtension(mdPath),
                                    converter.Convert(inputFullPath, imagesDir, mdImageRoot))
                            };

                        for (int i = 0; i < markdownOutputs.Count; i++)
                        {
                            var outputPath = GetOutputPath(basePath, markdownOutputs.Count, i, markdownOutputs[i].Title, "md");
                            await File.WriteAllTextAsync(outputPath, markdownOutputs[i].Markdown, cancellationToken);
                            outputs.Add(outputPath);
                        }

                        task.Increment(stepSize);
                    }

                    if (format is "html" or "both")
                    {
                        task.Description = "[green]Converting to HTML[/]";
                        var converter = new PdfToHtmlConverter();
                        var htmlPath = $"{basePath}.html";
                        var htmlImageRoot = imagesDir is not null
                            ? ImageReferencePath.GetReferenceRoot(imagesDir, htmlPath)
                            : null;

                        var htmlOutputs = settings.SplitByTitle
                            ? converter.ConvertSplitByTitle(inputFullPath, imagesDir, htmlImageRoot)
                            : new[]
                            {
                                new HtmlDocumentPart(
                                    Path.GetFileNameWithoutExtension(htmlPath),
                                    converter.Convert(inputFullPath, imagesDir, htmlImageRoot))
                            };

                        for (int i = 0; i < htmlOutputs.Count; i++)
                        {
                            var outputPath = GetOutputPath(basePath, htmlOutputs.Count, i, htmlOutputs[i].Title, "html");
                            await File.WriteAllTextAsync(outputPath, htmlOutputs[i].Html, cancellationToken);
                            outputs.Add(outputPath);
                        }

                        task.Increment(stepSize);
                    }

                    task.Description = "[green]Done[/]";
                    task.Value = 100;
                }
                catch (Exception ex)
                {
                    task.Description = "[red]Failed[/]";
                    task.Value = 100;
                    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
                    errorCode = 1;
                }
            });

        if (errorCode != 0)
            return errorCode;

        // ── show results table ─────────────────────────────────────────────
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[green]Output files[/]").LeftJustified());

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[grey]File[/]")
            .AddColumn(new TableColumn("[grey]Size[/]").RightAligned());

        foreach (var path in outputs)
        {
            var size = new FileInfo(path).Length;
            table.AddRow(
                $"[white]{path}[/]",
                $"[green]{FormatBytes(size)}[/]");
        }

        if (imagesDir is not null && Directory.Exists(imagesDir))
        {
            var imageFiles = Directory.GetFiles(imagesDir);
            if (imageFiles.Length > 0)
            {
                var totalSize = imageFiles.Sum(f => new FileInfo(f).Length);
                table.AddRow(
                    $"[white]{imagesDir}[/] [grey]({imageFiles.Length} image(s))[/]",
                    $"[green]{FormatBytes(totalSize)}[/]");
            }
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[bold green]✔ Conversion complete![/]");

        return 0;
    }

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024):F1} MB"
        };
    }

    private static string ResolveImagesDirectory(
        ConvertSettings settings,
        string basePath)
    {
        if (!string.IsNullOrWhiteSpace(settings.ImagesDir))
            return Path.GetFullPath(settings.ImagesDir);

        var outputDirectory = Path.GetDirectoryName(basePath) ?? ".";
        return Path.Combine(outputDirectory, "images");
    }

    private static string GetOutputPath(
        string basePath,
        int partCount,
        int index,
        string title,
        string extension)
    {
        if (partCount == 1)
            return $"{basePath}.{extension}";

        var slug = Slugify(title);
        var suffix = string.IsNullOrWhiteSpace(slug)
            ? $"part{index + 1:D2}"
            : $"part{index + 1:D2}-{slug}";

        return $"{basePath}-{suffix}.{extension}";
    }

    private static string Slugify(string value)
    {
        var builder = new StringBuilder();
        bool lastWasDash = false;

        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastWasDash = false;
            }
            else if (builder.Length > 0 && !lastWasDash)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length <= 40 ? slug : slug[..40].TrimEnd('-');
    }
}
