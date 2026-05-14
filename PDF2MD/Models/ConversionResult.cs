namespace Pdf2Md.Models;

public sealed record ConversionResult<TPart>(
    IReadOnlyList<TPart> Parts,
    IReadOnlyList<ImageExtractionWarning> Warnings);
