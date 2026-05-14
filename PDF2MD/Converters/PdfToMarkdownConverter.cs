using Pdf2Md.Models;

namespace Pdf2Md.Converters;

/// <summary>Converts a PDF file to a Markdown document.</summary>
public sealed class PdfToMarkdownConverter
{
    /// <summary>
    /// Converts the PDF at <paramref name="pdfPath"/> to Markdown text.
    /// If <paramref name="imagesDirectory"/> is provided, images are saved there and
    /// referenced as relative paths in the output.
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
            splitByTitle: false).Parts[0].Markdown;
    }

    /// <summary>
    /// Converts the PDF into multiple Markdown documents, splitting when a page contains
    /// an H1/title-like heading.
    /// </summary>
    public IReadOnlyList<MarkdownDocumentPart> ConvertSplitByTitle(
        string pdfPath,
        string? imagesDirectory = null,
        string? imageReferenceRoot = null)
    {
        return ConvertDetailed(
            PdfDocumentLoader.Load(pdfPath, imagesDirectory),
            imagesDirectory,
            imageReferenceRoot,
            splitByTitle: true).Parts;
    }

    internal ConversionResult<MarkdownDocumentPart> ConvertDetailed(
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
            (title, pages) => new MarkdownDocumentPart(
                title,
                RenderDocument(pages, document.ImagesByPage, imagesDirectory, imageReferenceRoot)));

        return new ConversionResult<MarkdownDocumentPart>(parts, document.Warnings);
    }

    private static string RenderDocument(
        IReadOnlyList<PageContent> pages,
        IReadOnlyDictionary<int, IReadOnlyList<PageImage>> imagesByPage,
        string? imagesDirectory,
        string? imageReferenceRoot)
    {
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

            if (imagesByPage.TryGetValue(page.PageNumber, out var pageImages))
            {
                foreach (var img in pageImages)
                {
                    var fileName = $"page{img.PageNumber}_img{img.ImageIndex}.{img.Extension}";
                    var relPath = ImageReferencePath.GetImagePath(fileName, imagesDirectory, imageReferenceRoot);
                    sb.AppendLine($"![Image from page {img.PageNumber}]({relPath})");
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
    }
}
