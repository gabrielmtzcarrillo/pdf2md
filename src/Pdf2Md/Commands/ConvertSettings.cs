using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pdf2Md.Commands;

/// <summary>CLI settings / options for the convert command.</summary>
public sealed class ConvertSettings : CommandSettings
{
    [CommandArgument(0, "<input>")]
    [Description("Path to the input PDF file.")]
    public string InputPath { get; init; } = string.Empty;

    [CommandOption("-o|--output")]
    [Description("Base output path (without extension). Defaults to the input file name.")]
    public string? OutputPath { get; init; }

    [CommandOption("-f|--format")]
    [Description("Output format: md, html, or both. Default is md.")]
    [DefaultValue("md")]
    public string Format { get; init; } = "md";

    [CommandOption("-e|--extract-images")]
    [Description("Extract images from the PDF and save them alongside the output.")]
    [DefaultValue(false)]
    public bool ExtractImages { get; init; }

    [CommandOption("-d|--images-dir")]
    [Description("Directory to save extracted images. Defaults to <output>_images.")]
    public string? ImagesDir { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(InputPath))
            return ValidationResult.Error("Input path is required.");

        if (!InputPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Error("Input file must be a PDF (.pdf) file.");

        var fmt = Format.ToLowerInvariant();
        if (fmt is not ("md" or "html" or "both"))
            return ValidationResult.Error("Format must be one of: md, html, both.");

        return ValidationResult.Success();
    }
}
