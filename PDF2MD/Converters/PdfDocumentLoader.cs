using Pdf2Md.Extractors;
using Pdf2Md.Models;
using UglyToad.PdfPig;

namespace Pdf2Md.Converters;

internal static class PdfDocumentLoader
{
    public static LoadedPdfDocument Load(string pdfPath, string? imagesDirectory)
    {
        using var document = PdfDocument.Open(pdfPath);

        var extractionResult = imagesDirectory is not null
            ? ImageExtractor.ExtractImages(document, imagesDirectory)
            : ImageExtractionResult.Empty;

        var imagesByPage = extractionResult.Images
            .GroupBy(image => image.PageNumber)
            .ToDictionary(
                grouping => grouping.Key,
                grouping => (IReadOnlyList<PageImage>)grouping.ToList());

        return new LoadedPdfDocument(
            Path.GetFileNameWithoutExtension(pdfPath),
            PdfPageParser.Parse(document),
            imagesByPage,
            extractionResult.Warnings);
    }
}
