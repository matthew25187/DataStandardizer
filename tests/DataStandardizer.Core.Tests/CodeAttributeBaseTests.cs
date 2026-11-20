using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.Core.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class CodeAttributeBaseTests
    {
        private const string FirstEnglishName = "First";
        private const string SecondEnglishName = "Second";
        private const string FirstFrenchName = "Premier";
        private const string SecondFrenchName = "Deuxi\x00E8me";
        private const string ReplacementName = "Replacement";

        [Fact]
        public void EnglishNames_AfterWritingToAReturnedArray_IsUnchanged()
        {
            // arrange
            var attribute = TestCodeAttribute.Create();

            // act
            attribute.EnglishNames[0] = ReplacementName;
            var testResult = attribute.EnglishNames;

            // assert
            testResult.Should().Equal(FirstEnglishName, SecondEnglishName);
        }

        [Fact]
        public void EnglishName_AfterWritingToAReturnedEnglishNamesArray_IsUnchanged()
        {
            // arrange
            var attribute = TestCodeAttribute.Create();

            // act
            attribute.EnglishNames[0] = ReplacementName;
            var testResult = attribute.EnglishName;

            // assert
            testResult.Should().Be(FirstEnglishName);
        }

        [Fact]
        public void FrenchNames_AfterWritingToAReturnedArray_IsUnchanged()
        {
            // arrange
            var attribute = TestCodeAttribute.Create();

            // act
            attribute.FrenchNames[0] = ReplacementName;
            var testResult = attribute.FrenchNames;

            // assert
            testResult.Should().Equal(FirstFrenchName, SecondFrenchName);
        }

        [Fact]
        public void FrenchName_AfterWritingToAReturnedFrenchNamesArray_IsUnchanged()
        {
            // arrange
            var attribute = TestCodeAttribute.Create();

            // act
            attribute.FrenchNames[0] = ReplacementName;
            var testResult = attribute.FrenchName;

            // assert
            testResult.Should().Be(FirstFrenchName);
        }

        [Fact]
        public void EnglishNames_WithNoNames_IsEmpty()
        {
            // arrange
            var attribute = new TestCodeAttribute();

            // act
            var testResult = attribute.EnglishNames;

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void FrenchNames_WithNoNames_IsEmpty()
        {
            // arrange
            var attribute = new TestCodeAttribute();

            // act
            var testResult = attribute.FrenchNames;

            // assert
            testResult.Should().BeEmpty();
        }

        private sealed class TestCodeAttribute : CodeAttributeBase
        {
            public TestCodeAttribute()
            {
            }

            private TestCodeAttribute(string[] englishNames, string[] frenchNames) : base(englishNames, frenchNames)
            {
            }

            public static TestCodeAttribute Create() =>
                new TestCodeAttribute(new[] { FirstEnglishName, SecondEnglishName }, new[] { FirstFrenchName, SecondFrenchName });
        }
    }
}
