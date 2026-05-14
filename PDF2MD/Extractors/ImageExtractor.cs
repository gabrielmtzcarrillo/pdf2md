using FileSignatures;
using FileSignatures.Formats;
using ImageMagick;
using Pdf2Md.Formats;
using Pdf2Md.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Pdf2Md.Extractors;

/// <summary>Extracts images from PDF pages and saves them to disk.</summary>
public static class ImageExtractor
{
    private static readonly FileFormatInspector ImageFormatInspector =
        new(FileFormatLocator.GetFormats().OfType<Image>().Concat(new[] { new Jpeg2000Format() }));

    /// <summary>
    /// Extracts all images from the given PDF document and saves them to
    /// <paramref name="outputDirectory"/>. Returns a list of <see cref="PageImage"/>
    /// records that describe each saved image.
    /// </summary>
    public static ImageExtractionResult ExtractImages(
        PdfDocument document,
        string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var images = new List<PageImage>();
        var warnings = new List<ImageExtractionWarning>();

        foreach (var page in document.GetPages())
        {
            var imageIndex = 1;
            foreach (var pdfImage in page.GetImages())
            {
                var pngBytes = pdfImage.TryGetPng(out var extractedPngBytes) && extractedPngBytes is { Length: > 0 }
                    ? extractedPngBytes
                    : null;
                var outcome = TryExtractImage(outputDirectory, page.Number, imageIndex, pngBytes, pdfImage.RawBytes.ToArray());
                if (outcome.Image is not null)
                    images.Add(outcome.Image);
                if (outcome.Warning is not null)
                    warnings.Add(outcome.Warning);

                imageIndex++;
            }
        }

        return new ImageExtractionResult(images, warnings);
    }

    /// <summary>
    /// Counts how many images are on each page without saving to disk.
    /// Returns a dictionary keyed by 1-based page number.
    /// </summary>
    public static IReadOnlyDictionary<int, int> CountImagesPerPage(PdfDocument document)
    {
        var counts = new Dictionary<int, int>();
        foreach (var page in document.GetPages())
        {
            counts[page.Number] = page.GetImages().Count();
        }
        return counts;
    }

    // ── helpers ────────────────────────────────────────────────────────────

    internal static ImageExtractionOutcome TryExtractImage(
        string outputDirectory,
        int pageNumber,
        int imageIndex,
        byte[]? pngBytes,
        byte[] rawBytes)
    {
        try
        {
            byte[] bytes;
            string extension;

            if (pngBytes is { Length: > 0 })
            {
                bytes = pngBytes;
                extension = "png";
            }
            else
            {
                bytes = rawBytes;
                extension = DetermineExtension(bytes);

                if (extension == "jp2")
                {
                    bytes = ConvertJpeg2000ToPng(bytes);
                    extension = "png";
                }
            }

            var fileName = BuildImageFileName(pageNumber, imageIndex, extension);
            var filePath = Path.Combine(outputDirectory, fileName);

            File.WriteAllBytes(filePath, bytes);
            return new ImageExtractionOutcome(
                new PageImage(pageNumber, imageIndex, bytes, extension),
                null);
        }
        catch (Exception ex)
        {
            var message = string.IsNullOrWhiteSpace(ex.Message)
                ? ex.GetType().Name
                : $"{ex.GetType().Name}: {ex.Message}";

            return new ImageExtractionOutcome(
                null,
                new ImageExtractionWarning(pageNumber, imageIndex, message));
        }
    }

    private static string DetermineExtension(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var format = ImageFormatInspector.DetermineFileFormat(stream);
        return format?.Extension.TrimStart('.') ?? "bin";
    }

    private static string BuildImageFileName(int pageNumber, int imageIndex, string extension) =>
        $"page{pageNumber}_img{imageIndex}.{extension}";

    private static byte[] ConvertJpeg2000ToPng(byte[] bytes)
    {
        using var image = new MagickImage(bytes, MagickFormat.Jp2);
        return image.ToByteArray(MagickFormat.Png);
    }
}

internal sealed record ImageExtractionOutcome(PageImage? Image, ImageExtractionWarning? Warning);
