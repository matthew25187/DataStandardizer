using System.Reflection;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class AssemblySmokeTests
    {
        [Fact]
        public void GetCustomAttribute_LibraryAssemblyTitle_IsExpectedTitle()
        {
            // arrange
            var assembly = Assembly.Load(new AssemblyName("DataStandardizer.File.InternetMessage"));

            // act
            var testResult = assembly.GetCustomAttribute<AssemblyTitleAttribute>();

            // assert
            testResult.Should().NotBeNull();
            testResult.Title.Should().Be("Data Standardizer: File (Internet Message)");
        }
    }
}
