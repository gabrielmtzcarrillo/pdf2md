using Pdf2Md.Extractors;

namespace PDF2MD.Tests;

public class ImageExtractorTests
{
    private static readonly byte[] ValidPngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO2M6wAAAABJRU5ErkJggg==");

    [Fact]
    public void TryExtractImage_ReturnsWarningWhenWriteFails()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"pdf2md-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var blockingFile = Path.Combine(tempRoot, "blocked");
        File.WriteAllText(blockingFile, "not a directory");

        try
        {
            var outcome = ImageExtractor.TryExtractImage(
                blockingFile,
                pageNumber: 3,
                imageIndex: 2,
                pngBytes: ValidPngBytes,
                rawBytes: Array.Empty<byte>());

            Assert.Null(outcome.Image);
            Assert.NotNull(outcome.Warning);
            var warning = outcome.Warning!;
            Assert.Contains("Skipped image 2 on page 3", warning.ToDisplayMessage());
            Assert.Contains("DirectoryNotFoundException", warning.Message);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void TryExtractImage_SavesPngBytesWithExpectedFileName()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"pdf2md-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var outcome = ImageExtractor.TryExtractImage(
                tempRoot,
                pageNumber: 4,
                imageIndex: 1,
                pngBytes: ValidPngBytes,
                rawBytes: Array.Empty<byte>());

            Assert.NotNull(outcome.Image);
            var image = outcome.Image!;
            Assert.Null(outcome.Warning);
            Assert.Equal("png", image.Extension);
            Assert.Equal(ValidPngBytes, image.Bytes);
            Assert.True(File.Exists(Path.Combine(tempRoot, "page4_img1.png")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
