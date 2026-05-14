using System.Reflection;

namespace Pdf2Md;

internal static class ApplicationVersion
{
    private const string CliVersionMetadataKey = "CliVersion";

    public static string GetDisplayVersion(Assembly assembly)
    {
        var metadataVersion = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, CliVersionMetadataKey, StringComparison.Ordinal))
            ?.Value;

        if (!string.IsNullOrWhiteSpace(metadataVersion))
            return metadataVersion;

        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }
}
