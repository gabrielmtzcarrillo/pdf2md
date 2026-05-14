namespace Pdf2Md.Models;

public sealed record LoadedPdfDocument(
    string DefaultTitle,
    IReadOnlyList<PageContent> Pages,
    IReadOnlyDictionary<int, IReadOnlyList<PageImage>> ImagesByPage,
    IReadOnlyList<ImageExtractionWarning> Warnings);
