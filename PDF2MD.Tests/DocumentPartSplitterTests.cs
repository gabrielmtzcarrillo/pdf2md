using Pdf2Md.Converters;
using Pdf2Md.Models;

namespace PDF2MD.Tests;

public class DocumentPartSplitterTests
{
    [Fact]
    public void CreateParts_SplitsOnSubsequentTitlePages()
    {
        var pages = new[]
        {
            CreatePage(1, "Introduction"),
            CreatePage(2, "Body text", isHeading: false, headingLevel: 0),
            CreatePage(3, "Appendix")
        };

        var parts = DocumentPartSplitter.CreateParts(
            "document",
            pages,
            splitByTitle: true,
            DocumentPartSplitter.GetSplitTitle,
            (title, partPages) => new TestDocumentPart(title, partPages.Select(page => page.PageNumber).ToArray()));

        Assert.Collection(
            parts,
            first =>
            {
                Assert.Equal("Introduction", first.Title);
                Assert.Equal(new[] { 1, 2 }, first.PageNumbers);
            },
            second =>
            {
                Assert.Equal("Appendix", second.Title);
                Assert.Equal(new[] { 3 }, second.PageNumbers);
            });
    }

    [Fact]
    public void CreateParts_LeavesDocumentIntactWhenSplittingDisabled()
    {
        var pages = new[]
        {
            CreatePage(1, "Introduction"),
            CreatePage(2, "Appendix")
        };

        var parts = DocumentPartSplitter.CreateParts(
            "document",
            pages,
            splitByTitle: false,
            DocumentPartSplitter.GetSplitTitle,
            (title, partPages) => new TestDocumentPart(title, partPages.Select(page => page.PageNumber).ToArray()));

        var part = Assert.Single(parts);
        Assert.Equal("document", part.Title);
        Assert.Equal(new[] { 1, 2 }, part.PageNumbers);
    }

    private static PageContent CreatePage(int pageNumber, string text, bool isHeading = true, int headingLevel = 1) =>
        new(
            pageNumber,
            new[] { new TextBlock(text, 18, isHeading, headingLevel) },
            Array.Empty<PageImage>());

    private sealed record TestDocumentPart(string Title, int[] PageNumbers);
}
