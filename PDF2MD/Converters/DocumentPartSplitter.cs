using Pdf2Md.Models;

namespace Pdf2Md.Converters;

internal static class DocumentPartSplitter
{
    public static IReadOnlyList<TPart> CreateParts<TPart>(
        string defaultTitle,
        IReadOnlyList<PageContent> pages,
        bool splitByTitle,
        Func<PageContent, string?> getSplitTitle,
        Func<string, IReadOnlyList<PageContent>, TPart> createPart)
    {
        if (!splitByTitle)
            return new[] { createPart(defaultTitle, pages) };

        var parts = new List<TPart>();
        var currentPages = new List<PageContent>();
        var currentTitle = defaultTitle;

        foreach (var page in pages)
        {
            var splitTitle = getSplitTitle(page);
            if (splitTitle is not null && currentPages.Count > 0)
            {
                parts.Add(createPart(currentTitle, currentPages));
                currentPages = new List<PageContent>();
            }

            if (currentPages.Count == 0 && splitTitle is not null)
                currentTitle = splitTitle;

            currentPages.Add(page);
        }

        if (currentPages.Count > 0)
            parts.Add(createPart(currentTitle, currentPages));

        return parts;
    }

    public static string? GetSplitTitle(PageContent page)
    {
        var titleBlock = page.TextBlocks.FirstOrDefault(block => block.IsHeading && block.HeadingLevel == 1);
        return string.IsNullOrWhiteSpace(titleBlock?.Text) ? null : titleBlock.Text;
    }
}
