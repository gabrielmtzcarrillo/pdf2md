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
    public string Convert(
        string pdfPath,
        string? imagesDirectory = null,
        string? imageReferenceRoot = null)
    {
        return ConvertInternal(pdfPath, imagesDirectory, imageReferenceRoot, splitByTitle: false)[0].Markdown;
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
        return ConvertInternal(pdfPath, imagesDirectory, imageReferenceRoot, splitByTitle: true);
    }

    private static IReadOnlyList<MarkdownDocumentPart> ConvertInternal(
        string pdfPath,
        string? imagesDirectory,
        string? imageReferenceRoot,
        bool splitByTitle)
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

        var defaultTitle = Path.GetFileNameWithoutExtension(pdfPath);
        if (!splitByTitle)
        {
            return new[]
            {
                new MarkdownDocumentPart(
                    defaultTitle,
                    RenderDocument(pages, imagesByPage, imagesDirectory, imageReferenceRoot))
            };
        }

        var parts = new List<MarkdownDocumentPart>();
        var currentPages = new List<PageContent>();
        var currentTitle = defaultTitle;

        foreach (var page in pages)
        {
            var splitTitle = GetSplitTitle(page);
            if (splitTitle is not null && currentPages.Count > 0)
            {
                parts.Add(new MarkdownDocumentPart(
                    currentTitle,
                    RenderDocument(currentPages, imagesByPage, imagesDirectory, imageReferenceRoot)));
                currentPages = new List<PageContent>();
            }

            if (currentPages.Count == 0 && splitTitle is not null)
                currentTitle = splitTitle;

            currentPages.Add(page);
        }

        if (currentPages.Count > 0)
        {
            parts.Add(new MarkdownDocumentPart(
                currentTitle,
                RenderDocument(currentPages, imagesByPage, imagesDirectory, imageReferenceRoot)));
        }

        return parts;
    }

    private static string RenderDocument(
        IReadOnlyList<PageContent> pages,
        IReadOnlyDictionary<int, List<PageImage>> imagesByPage,
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

    private static string? GetSplitTitle(PageContent page)
    {
        var titleBlock = page.TextBlocks.FirstOrDefault(block => block.IsHeading && block.HeadingLevel == 1);
        return string.IsNullOrWhiteSpace(titleBlock?.Text) ? null : titleBlock.Text;
    }
}
