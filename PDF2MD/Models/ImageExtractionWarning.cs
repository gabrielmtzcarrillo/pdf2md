namespace Pdf2Md.Models;

public sealed record ImageExtractionWarning(int PageNumber, int ImageIndex, string Message)
{
    public string ToDisplayMessage() =>
        $"Skipped image {ImageIndex} on page {PageNumber}: {Message}";
}
