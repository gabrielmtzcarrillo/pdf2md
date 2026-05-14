namespace Pdf2Md.Converters;

internal static class ImageReferencePath
{
    public static string GetReferenceRoot(string imagesDirectory, string outputPath)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);
        var relativeRoot = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.GetFileName(imagesDirectory)
            : Path.GetRelativePath(outputDirectory, imagesDirectory);

        return NormalizeRoot(relativeRoot);
    }

    public static string GetImagePath(string fileName, string? imagesDirectory, string? referenceRoot)
    {
        var root = string.IsNullOrWhiteSpace(referenceRoot)
            ? imagesDirectory is not null ? Path.GetFileName(imagesDirectory) : "."
            : referenceRoot;

        var normalizedRoot = NormalizeRoot(root);
        return normalizedRoot == "."
            ? $"./{fileName}"
            : $"{normalizedRoot}/{fileName}";
    }

    private static string NormalizeRoot(string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
            return ".";

        var normalized = root.Replace('\\', '/').TrimEnd('/');
        if (normalized.Length == 0 || normalized == ".")
            return ".";

        return normalized.StartsWith("./", StringComparison.Ordinal)
            || normalized.StartsWith("../", StringComparison.Ordinal)
            || normalized.StartsWith("/", StringComparison.Ordinal)
            ? normalized
            : $"./{normalized}";
    }
}
