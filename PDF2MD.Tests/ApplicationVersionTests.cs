using System.Reflection;
using Pdf2Md;
using Pdf2Md.Commands;

namespace PDF2MD.Tests;

public class ApplicationVersionTests
{
    [Fact]
    public void GetDisplayVersion_UsesCliVersionMetadata()
    {
        var assembly = typeof(ConvertCommand).Assembly;
        var expectedVersion = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "CliVersion")
            .Value;

        var actualVersion = ApplicationVersion.GetDisplayVersion(assembly);

        Assert.Equal(expectedVersion, actualVersion);
    }
}
