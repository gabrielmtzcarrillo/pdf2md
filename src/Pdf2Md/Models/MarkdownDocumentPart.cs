namespace Pdf2Md.Models;

/// <summary>Represents one generated Markdown document when output is split into parts.</summary>
public sealed record MarkdownDocumentPart(string Title, string Markdown);
