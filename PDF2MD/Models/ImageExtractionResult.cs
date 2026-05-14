namespace Pdf2Md.Models;

public sealed record ImageExtractionResult(
    IReadOnlyList<PageImage> Images,
    IReadOnlyList<ImageExtractionWarning> Warnings)
{
    public static ImageExtractionResult Empty { get; } =
        new(Array.Empty<PageImage>(), Array.Empty<ImageExtractionWarning>());
}
