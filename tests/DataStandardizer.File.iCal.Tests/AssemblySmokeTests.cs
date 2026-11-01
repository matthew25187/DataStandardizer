using System.Reflection;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class AssemblySmokeTests
    {
        [Fact]
        public void GetCustomAttribute_LibraryAssemblyTitle_IsExpectedTitle()
        {
            // arrange
            var assembly = Assembly.Load(new AssemblyName("DataStandardizer.File.iCal"));

            // act
            var testResult = assembly.GetCustomAttribute<AssemblyTitleAttribute>();

            // assert
            testResult.Should().NotBeNull();
            testResult.Title.Should().Be("Data Standardizer: File (iCal)");
        }
    }
}
