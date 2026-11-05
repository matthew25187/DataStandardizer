using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests that the build the tests run against has jCard.  The integration tests check that the netstandard1.1 build, which has no System.Text.Json, does not.
    /// </summary>
    public class JcardPlatformTests
    {
        [Fact]
        public void Assembly_JcardReaderInCurrentBuild_IsTheVcardReaderAssembly()
        {
            // act
            var testResult = typeof(JcardReader).Assembly;

            // assert
            testResult.Should().BeSameAs(typeof(VcardReader).Assembly);
        }

        [Fact]
        public void IsPublic_JcardWriterInCurrentBuild_IsTrue()
        {
            // act
            var testResult = typeof(JcardWriter).IsPublic;

            // assert
            testResult.Should().BeTrue();
        }
    }
}
