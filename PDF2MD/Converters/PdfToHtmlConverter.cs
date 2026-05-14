using Pdf2Md.Models;

namespace Pdf2Md.Converters;

/// <summary>Converts a PDF file to an HTML document.</summary>
public sealed class PdfToHtmlConverter
{
    /// <summary>
    /// Converts the PDF at <paramref name="pdfPath"/> to an HTML string.
    /// If <paramref name="imagesDirectory"/> is provided, images are saved there and
    /// referenced as relative &lt;img&gt; tags in the output.
    /// </summary>
    public string Convert(
        string pdfPath,
        string? imagesDirectory = null,
        string? imageReferenceRoot = null)
    {
        return ConvertDetailed(
            PdfDocumentLoader.Load(pdfPath, imagesDirectory),
            imagesDirectory,
            imageReferenceRoot,
            splitByTitle: false).Parts[0].Html;
    }

    /// <summary>
    /// Converts the PDF into multiple HTML documents, splitting when a page begins
    /// with an H1/title-like heading.
    /// </summary>
    public IReadOnlyList<HtmlDocumentPart> ConvertSplitByTitle(
        string pdfPath,
        string? imagesDirectory = null,
        string? imageReferenceRoot = null)
    {
        return ConvertDetailed(
            PdfDocumentLoader.Load(pdfPath, imagesDirectory),
            imageReferenceRoot: imageReferenceRoot,
            imagesDirectory: imagesDirectory,
            splitByTitle: true).Parts;
    }

    internal ConversionResult<HtmlDocumentPart> ConvertDetailed(
        LoadedPdfDocument document,
        string? imagesDirectory,
        string? imageReferenceRoot,
        bool splitByTitle)
    {
        var parts = DocumentPartSplitter.CreateParts(
            document.DefaultTitle,
            document.Pages,
            splitByTitle,
            DocumentPartSplitter.GetSplitTitle,
            (title, pages) => new HtmlDocumentPart(
                title,
                RenderDocument(title, pages, document.ImagesByPage, imagesDirectory, imageReferenceRoot)));

        return new ConversionResult<HtmlDocumentPart>(parts, document.Warnings);
    }

    private static string RenderDocument(
        string title,
        IReadOnlyList<PageContent> pages,
        IReadOnlyDictionary<int, IReadOnlyList<PageImage>> imagesByPage,
        string? imagesDirectory,
        string? imageReferenceRoot)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.AppendLine($"  <title>{HtmlEncode(title)}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: Georgia, serif; max-width: 900px; margin: 2rem auto; padding: 0 1rem; line-height: 1.6; color: #222; }");
        sb.AppendLine("    h1, h2, h3 { color: #111; }");
        sb.AppendLine("    .page { border-top: 1px solid #ccc; padding-top: 1.5rem; margin-top: 2rem; }");
        sb.AppendLine("    .page:first-child { border-top: none; padding-top: 0; margin-top: 0; }");
        sb.AppendLine("    img { max-width: 100%; height: auto; display: block; margin: 1rem 0; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        foreach (var page in pages)
        {
            sb.AppendLine($"  <section class=\"page\" id=\"page-{page.PageNumber}\">");

            foreach (var block in page.TextBlocks)
            {
                if (string.IsNullOrWhiteSpace(block.Text))
                    continue;

                var encoded = HtmlEncode(block.Text);

                if (block.IsHeading)
                    sb.AppendLine($"    <h{block.HeadingLevel}>{encoded}</h{block.HeadingLevel}>");
                else
                    sb.AppendLine($"    <p>{encoded}</p>");
            }

            if (imagesByPage.TryGetValue(page.PageNumber, out var pageImages))
            {
                foreach (var img in pageImages)
                {
                    var fileName = $"page{img.PageNumber}_img{img.ImageIndex}.{img.Extension}";
                    var relPath = ImageReferencePath.GetImagePath(fileName, imagesDirectory, imageReferenceRoot);
                    sb.AppendLine($"    <img src=\"{relPath}\" alt=\"Image from page {img.PageNumber}\" />");
                }
            }

            sb.AppendLine("  </section>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string? GetSplitTitle(PageContent page)
    {
        var titleBlock = page.TextBlocks.FirstOrDefault(block => block.IsHeading && block.HeadingLevel == 1);
        return string.IsNullOrWhiteSpace(titleBlock?.Text) ? null : titleBlock.Text;
    }

    private static string HtmlEncode(string text) =>
        System.Net.WebUtility.HtmlEncode(text);
}
