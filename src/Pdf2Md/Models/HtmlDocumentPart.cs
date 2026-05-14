namespace Pdf2Md.Models;

/// <summary>Represents one generated HTML document when output is split into parts.</summary>
public sealed record HtmlDocumentPart(string Title, string Html);
