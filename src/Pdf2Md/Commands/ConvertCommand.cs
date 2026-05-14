using Pdf2Md.Converters;
using Spectre.Console;
using Spectre.Console.Cli;

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
        var shouldExtractImages = settings.ExtractImages || format is "html" or "both";
        var imagesDir = shouldExtractImages
            ? ResolveImagesDirectory(settings, format, basePath)
            : null;

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
                        var md = converter.Convert(inputFullPath, imagesDir, mdImageRoot);
                        await File.WriteAllTextAsync(mdPath, md, cancellationToken);
                        outputs.Add(mdPath);
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
                        var html = converter.Convert(inputFullPath, imagesDir, htmlImageRoot);
                        await File.WriteAllTextAsync(htmlPath, html, cancellationToken);
                        outputs.Add(htmlPath);
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
        string format,
        string basePath)
    {
        if (!string.IsNullOrWhiteSpace(settings.ImagesDir))
            return Path.GetFullPath(settings.ImagesDir);

        var outputDirectory = Path.GetDirectoryName(basePath) ?? ".";
        return format is "html" or "both"
            ? Path.Combine(outputDirectory, "images")
            : $"{basePath}_images";
    }
}
