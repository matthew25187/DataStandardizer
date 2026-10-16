using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

public class AssemblySmokeTests
{
    [Fact]
    public void Load_LibraryAssembly_HasExpectedTitle()
    {
        // arrange
        var assemblyName = new AssemblyName("DataStandardizer.File.iCal");

        // act
        var assembly = Assembly.Load(assemblyName);
        var testResult = assembly.GetCustomAttribute<AssemblyTitleAttribute>();

        // assert
        testResult.Should().NotBeNull();
        testResult!.Title.Should().Be("Data Standardizer: File (iCal)");
    }
}
