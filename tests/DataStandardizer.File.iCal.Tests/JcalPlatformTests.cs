using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests that the build the tests run against has jCal.  The integration tests check that the netstandard1.1 build, which has no
    /// System.Text.Json, does not.
    /// </summary>
    public class JcalPlatformTests
    {
        [Fact]
        public void Assembly_JcalReaderInCurrentBuild_IsTheXcalAssembly()
        {
            // act
            var testResult = typeof(JcalReader).Assembly;

            // assert
            testResult.Should().BeSameAs(typeof(XcalReader).Assembly);
        }

        [Fact]
        public void IsPublic_JcalWriterInCurrentBuild_IsTrue()
        {
            // act
            var testResult = typeof(JcalWriter).IsPublic;

            // assert
            testResult.Should().BeTrue();
        }

    }
}
