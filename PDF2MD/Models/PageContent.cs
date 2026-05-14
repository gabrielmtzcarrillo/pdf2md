namespace Pdf2Md.Models;

/// <summary>Represents a block of text on a PDF page (paragraph or heading).</summary>
public sealed record TextBlock(string Text, double FontSize, bool IsHeading, int HeadingLevel);

/// <summary>Represents an image extracted from a PDF page.</summary>
public sealed record PageImage(int PageNumber, int ImageIndex, byte[] Bytes, string Extension);

/// <summary>Represents the extracted content of a single PDF page.</summary>
public sealed record PageContent(
    int PageNumber,
    IReadOnlyList<TextBlock> TextBlocks,
    IReadOnlyList<PageImage> Images);
