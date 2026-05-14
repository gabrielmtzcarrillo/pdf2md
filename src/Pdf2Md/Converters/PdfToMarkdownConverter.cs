using Pdf2Md.Extractors;
using Pdf2Md.Models;
using UglyToad.PdfPig;

namespace Pdf2Md.Converters;

/// <summary>Converts a PDF file to a Markdown document.</summary>
public sealed class PdfToMarkdownConverter
{
    /// <summary>
    /// Converts the PDF at <paramref name="pdfPath"/> to Markdown text.
    /// If <paramref name="imagesDirectory"/> is provided, images are saved there and
    /// referenced as relative paths in the output.
    /// </summary>
    public Task<string> ConvertAsync(string pdfPath, string? imagesDirectory = null)
    {
        using var document = PdfDocument.Open(pdfPath);

        // Extract images (if requested) before parsing text so we can reference them.
        IReadOnlyList<PageImage> allImages = imagesDirectory is not null
            ? ImageExtractor.ExtractImages(document, imagesDirectory)
            : Array.Empty<PageImage>();

        var pages = PdfPageParser.Parse(document);
        var imagesByPage = allImages
            .GroupBy(i => i.PageNumber)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < pages.Count; i++)
        {
            var page = pages[i];

            if (i > 0)
                sb.AppendLine();

            foreach (var block in page.TextBlocks)
            {
                if (string.IsNullOrWhiteSpace(block.Text))
                    continue;

                if (block.IsHeading)
                {
                    var hashes = new string('#', block.HeadingLevel);
                    sb.AppendLine($"{hashes} {block.Text}");
                }
                else
                {
                    sb.AppendLine(block.Text);
                }

                sb.AppendLine();
            }

            // Append image references for this page.
            if (imagesByPage.TryGetValue(page.PageNumber, out var pageImages))
            {
                foreach (var img in pageImages)
                {
                    var relPath = imagesDirectory is not null
                        ? Path.Combine(
                            Path.GetFileName(imagesDirectory),
                            $"page{img.PageNumber}_img{img.ImageIndex}.{img.Extension}")
                        : $"page{img.PageNumber}_img{img.ImageIndex}.{img.Extension}";

                    // Use forward slashes for cross-platform Markdown compatibility.
                    relPath = relPath.Replace('\\', '/');
                    sb.AppendLine($"![Image from page {img.PageNumber}]({relPath})");
                    sb.AppendLine();
                }
            }
        }

        return Task.FromResult(sb.ToString());
    }
}
