using Pdf2Md.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Pdf2Md.Converters;

/// <summary>
/// Shared logic that parses PDF pages into <see cref="PageContent"/> objects
/// for use by both the Markdown and HTML converters.
/// </summary>
internal static class PdfPageParser
{
    private const double LineGroupingTolerance = 3.0;  // points

    /// <summary>Parses all pages in the document and returns their content.</summary>
    public static IReadOnlyList<PageContent> Parse(PdfDocument document)
    {
        var pages = new List<PageContent>();

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords(NearestNeighbourWordExtractor.Instance).ToList();

            if (words.Count == 0)
            {
                pages.Add(new PageContent(page.Number, Array.Empty<TextBlock>(), Array.Empty<PageImage>()));
                continue;
            }

            // Collect font sizes to determine the dominant body font size.
            var fontSizes = words
                .SelectMany(w => w.Letters)
                .Select(l => (double)l.PointSize)
                .Where(s => s > 0)
                .ToList();

            double bodyFontSize = fontSizes.Count > 0
                ? fontSizes
                    .GroupBy(s => Math.Round(s, 1))
                    .OrderByDescending(g => g.Count())
                    .First().Key
                : 10.0;

            // Sort words top-to-bottom (descending Y), then left-to-right (ascending X).
            words.Sort((a, b) =>
            {
                double yDiff = b.BoundingBox.Bottom - a.BoundingBox.Bottom;
                if (Math.Abs(yDiff) > LineGroupingTolerance)
                    return yDiff > 0 ? 1 : -1;
                return a.BoundingBox.Left.CompareTo(b.BoundingBox.Left);
            });

            var lines = GroupIntoLines(words);
            var textBlocks = GroupIntoBlocks(lines, bodyFontSize);

            pages.Add(new PageContent(page.Number, textBlocks, Array.Empty<PageImage>()));
        }

        return pages;
    }

    // ── private helpers ───────────────────────────────────────────────────

    private static List<(List<Word> Words, double Bottom, double Top)> GroupIntoLines(
        List<Word> words)
    {
        var lines = new List<(List<Word>, double, double)>();
        if (words.Count == 0) return lines;

        var currentWords = new List<Word> { words[0] };
        double currentBottom = words[0].BoundingBox.Bottom;
        double currentTop = words[0].BoundingBox.Top;

        for (int i = 1; i < words.Count; i++)
        {
            double wordBottom = words[i].BoundingBox.Bottom;
            if (Math.Abs(wordBottom - currentBottom) <= LineGroupingTolerance)
            {
                currentWords.Add(words[i]);
                currentTop = Math.Max(currentTop, words[i].BoundingBox.Top);
            }
            else
            {
                lines.Add((currentWords, currentBottom, currentTop));
                currentWords = new List<Word> { words[i] };
                currentBottom = wordBottom;
                currentTop = words[i].BoundingBox.Top;
            }
        }

        lines.Add((currentWords, currentBottom, currentTop));
        return lines;
    }

    private static List<TextBlock> GroupIntoBlocks(
        List<(List<Word> Words, double Bottom, double Top)> lines,
        double bodyFontSize)
    {
        var blocks = new List<TextBlock>();
        if (lines.Count == 0) return blocks;

        var currentLineTexts = new List<string>();
        double currentBlockFontSize = 0;
        double prevBottom = -1;
        double prevHeight = 0;

        foreach (var (lineWords, lineBottom, lineTop) in lines)
        {
            var lineText = string.Join(" ", lineWords.Select(w => w.Text)).Trim();
            if (string.IsNullOrEmpty(lineText)) continue;

            double lineHeight = lineTop - lineBottom;
            double lineFontSize = lineWords
                .SelectMany(w => w.Letters)
                .Select(l => (double)l.PointSize)
                .Where(s => s > 0)
                .DefaultIfEmpty(bodyFontSize)
                .Average();

            // A new paragraph starts when the gap between consecutive lines is
            // noticeably larger than the line height.
            bool newParagraph = prevBottom >= 0
                && (prevBottom - lineBottom) > Math.Max(prevHeight, lineHeight) * 1.4;

            if (newParagraph && currentLineTexts.Count > 0)
            {
                blocks.Add(BuildBlock(
                    currentLineTexts,
                    currentBlockFontSize > 0 ? currentBlockFontSize : bodyFontSize,
                    bodyFontSize));

                currentLineTexts = new List<string>();
                currentBlockFontSize = 0;
            }

            currentLineTexts.Add(lineText);
            currentBlockFontSize = Math.Max(currentBlockFontSize, lineFontSize);

            prevBottom = lineBottom;
            prevHeight = lineHeight;
        }

        if (currentLineTexts.Count > 0)
        {
            blocks.Add(BuildBlock(
                currentLineTexts,
                currentBlockFontSize > 0 ? currentBlockFontSize : bodyFontSize,
                bodyFontSize));
        }

        return blocks;
    }

    private static TextBlock BuildBlock(
        List<string> lines,
        double fontSize,
        double bodyFontSize)
    {
        var text = string.Join(" ", lines).Trim();
        double ratio = bodyFontSize > 0 ? fontSize / bodyFontSize : 1.0;

        bool isHeading = ratio >= 1.15;
        int level = isHeading
            ? ratio >= 1.8 ? 1
            : ratio >= 1.4 ? 2
            : 3
            : 0;

        return new TextBlock(text, fontSize, isHeading, level);
    }
}
