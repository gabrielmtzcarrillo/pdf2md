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
    public static IReadOnlyList<PageImage> ExtractImages(
        PdfDocument document,
        string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var results = new List<PageImage>();

        foreach (var page in document.GetPages())
        {
            int imageIndex = 0;

            foreach (var pdfImage in page.GetImages())
            {
                try
                {
                    byte[] bytes;
                    string extension;

                    if (pdfImage.TryGetPng(out var pngBytes) && pngBytes is { Length: > 0 })
                    {
                        bytes = pngBytes;
                        extension = "png";
                    }
                    else
                    {
                        // Fall back to whatever raw format the image is stored in.
                        bytes = pdfImage.RawBytes.ToArray();
                        extension = DetermineExtension(bytes);

                        if (extension == "jp2")
                        {
                            bytes = ConvertJpeg2000ToPng(bytes);
                            extension = "png";
                        }
                    }

                    var fileName = $"page{page.Number}_img{imageIndex + 1}.{extension}";
                    var filePath = Path.Combine(outputDirectory, fileName);

                    File.WriteAllBytes(filePath, bytes);
                    results.Add(new PageImage(page.Number, imageIndex + 1, bytes, extension));
                }
                catch
                {
                    // Skip images that cannot be decoded; continue with the rest.
                }

                imageIndex++;
            }
        }

        return results;
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

    private static string DetermineExtension(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var format = ImageFormatInspector.DetermineFileFormat(stream);
        return format?.Extension.TrimStart('.') ?? "bin";
    }

    private static byte[] ConvertJpeg2000ToPng(byte[] bytes)
    {
        using var image = new MagickImage(bytes, MagickFormat.Jp2);
        return image.ToByteArray(MagickFormat.Png);
    }
}
