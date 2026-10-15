using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Tests of which targets have jCal.  jCal needs System.Text.Json, which netstandard1.1 does not have, so the netstandard1.1 build of the
/// library must not hold the jCal types, while it still holds xCal.
/// </summary>
public class JcalPlatformTests
{
    [Fact]
    public void NetStandard11Build_HasNoJcalTypes()
    {
        // arrange
        var typeNames = ReadTypeNames(FindNetStandard11Build());

        // assert
        typeNames.Should().Contain(["XcalReader", "XcalWriter", "IcalValueFormat"]);
        typeNames.Should().NotContain(name => name.StartsWith("Jcal", StringComparison.Ordinal));
    }

    [Fact]
    public void CurrentBuild_HasJcalTypes()
    {
        // assert
        typeof(JcalReader).Assembly.Should().BeSameAs(typeof(XcalReader).Assembly);
        typeof(JcalWriter).IsPublic.Should().BeTrue();
    }

    /// <summary>
    /// Finds the netstandard1.1 build of the library, which the test project builds alongside the one it references.
    /// </summary>
    private static string FindNetStandard11Build()
    {
        // The test assembly is in tests/<project>/bin/<configuration>/<framework>.
        var outputDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        var configuration = outputDirectory.Parent!.Name;
        var repository = outputDirectory;
        while (repository != null && !System.IO.File.Exists(Path.Combine(repository.FullName, "DataStandardizer.sln")))
        {
            repository = repository.Parent;
        }

        repository.Should().NotBeNull("the tests run within the repository");
        var path = Path.Combine(repository!.FullName, "src", "DataStandardizer.File.iCal", "bin", configuration, "netstandard1.1",
            "DataStandardizer.File.iCal.dll");
        System.IO.File.Exists(path).Should().BeTrue("the netstandard1.1 build should be at {0}", path);
        return path;
    }

    private static List<string> ReadTypeNames(string path)
    {
        using var stream = System.IO.File.OpenRead(path);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();
        return metadata.TypeDefinitions
            .Select(handle => metadata.GetString(metadata.GetTypeDefinition(handle).Name))
            .ToList();
    }
}
