using Pdf2Md.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Pdf2Md.Extractors;

/// <summary>Extracts images from PDF pages and saves them to disk.</summary>
public static class ImageExtractor
{
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
                        extension = DetectExtension(bytes);
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

    private static string DetectExtension(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8)
            return "jpg";

        if (bytes.Length >= 8
            && bytes[0] == 0x89 && bytes[1] == 0x50
            && bytes[2] == 0x4E && bytes[3] == 0x47)
            return "png";

        if (bytes.Length >= 4
            && bytes[0] == 0x47 && bytes[1] == 0x49
            && bytes[2] == 0x46)
            return "gif";

        if (bytes.Length >= 4
            && bytes[0] == 0x42 && bytes[1] == 0x4D)
            return "bmp";

        return "bin";
    }
}
