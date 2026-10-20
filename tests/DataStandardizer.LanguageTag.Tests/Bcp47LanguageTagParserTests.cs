// The matchTimeout overloads are obsolete but stay tested until v2.0 removes them (#256).
#pragma warning disable CS0618

using System;
using System.Diagnostics.CodeAnalysis;
using DataStandardizer.LanguageTag.InternalState;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    /// <summary>
    /// Tests of validation by the default BCP 47 rules that are specific to the parser that replaced the regular expressions (#221).
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Bcp47LanguageTagParserTests
    {
        #region Test: Validation_WithTimeout

        public static TheoryData<TimeSpan> InvalidMatchTimeouts => new TheoryData<TimeSpan>
        {
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(-2),
            TimeSpan.FromMilliseconds(int.MaxValue)
        };

        [Theory]
        [MemberData(nameof(InvalidMatchTimeouts))]
        public void Create_WithLanguageTagAndInvalidTimeout_ThrowsArgumentOutOfRangeException(TimeSpan testTimeout)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create("en", testTimeout);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>("the timeout is validated as Regex validates it").WithParameterName("matchTimeout");
        }

        [Theory]
        [MemberData(nameof(InvalidMatchTimeouts))]
        public void TryCreate_WithLanguageTagAndInvalidTimeout_ThrowsArgumentOutOfRangeException(TimeSpan testTimeout)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.TryCreate("en", testTimeout, out _);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>("the timeout is validated as Regex validates it").WithParameterName("matchTimeout");
        }

        [Fact]
        public void Create_WithLanguageTagAndLongestTimeout_ReturnsBcp47LanguageTag()
        {
            // act
            var languageTag = Bcp47LanguageTag.Create("en", TimeSpan.FromMilliseconds(int.MaxValue - 1));

            // assert
            ((string)languageTag).Should().Be("en");
        }

        [Fact]
        public void TryCreate_WithLanguageTagAndTinyTimeout_ReturnsTrue()
        {
            // act
            Bcp47LanguageTag languageTag;
            var testResult = Bcp47LanguageTag.TryCreate("en-Latn-US-1994-u-co-phonebk", TimeSpan.FromTicks(1), out languageTag);

            // assert
            testResult.Should().BeTrue("the default rules are checked by a parser, which cannot time out");
            ((string)languageTag).Should().Be("en-Latn-US-1994-u-co-phonebk");
        }

        #endregion

        #region Test: Validation_WithNull

        [Fact]
        public void CheckSubtag_WithNull_ThrowsArgumentNullException()
        {
            // assert
            ((Action)(() => Bcp47LanguageTag.CheckPrimaryLanguageSubtag(null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckExtendedLanguageSubtag(null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckScriptSubtag(null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckRegionSubtag(null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckVariantSubtag(null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckExtensionSubtag(null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckPrivateUseSubtag(null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.IsWellFormedLanguageTagString(null))).Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region Test: Bcp47CharSpan

        public static TheoryData<string, int, int, string> CharSpanComparisons => new TheoryData<string, int, int, string>
        {
            { "en-US", 0, 2, "en" },
            { "en-US", 0, 2, "EN" },
            { "en-US", 0, 2, "eng" },
            { "en-US", 0, 3, "en" },
            { "en-US", 3, 2, "us" },
            { "en-US", 3, 2, "UT" },
            { "en-US", 3, 2, "U" },
            { "en-US", 2, 0, "" },
            { "en-US", 2, 0, "a" },
            { "zh-yue-HK", 3, 3, "YUE" },
            { "zh-yue-HK", 3, 3, "yuf" },
            { "zh-yue-HK", 3, 3, "yu" },
            { "zh-yue-HK", 3, 6, "yue-hk" },
            { "a1-Z9", 0, 5, "A1-z9" },
            { "a1-Z9", 0, 5, "A1_z9" }
        };

        [Theory]
        [MemberData(nameof(CharSpanComparisons))]
        public void CharSpan_CompareTo_AgreesWithReadOnlySpan(string text, int start, int length, string other)
        {
            // act
            var testResult = new Bcp47CharSpan(text).Slice(start, length).CompareTo(new Bcp47CharSpan(other), StringComparison.OrdinalIgnoreCase);
            var expectedResult = text.AsSpan(start, length).CompareTo(other.AsSpan(), StringComparison.OrdinalIgnoreCase);

            // assert
            Math.Sign(testResult).Should().Be(Math.Sign(expectedResult), "the shim stands in for ReadOnlySpan<char> on netstandard1.0");
        }

        [Fact]
        public void CharSpan_Members_AgreeWithReadOnlySpan()
        {
            // arrange
            const string text = "zh-yue-HK";
            var shim = new Bcp47CharSpan(text);

            // assert
            shim.Length.Should().Be(text.AsSpan().Length);
            shim.IsEmpty.Should().BeFalse();
            shim.Slice(3).ToString().Should().Be(text.AsSpan().Slice(3).ToString());
            shim.Slice(3, 3).ToString().Should().Be(text.AsSpan().Slice(3, 3).ToString());
            shim.Slice(3, 3)[2].Should().Be(text.AsSpan().Slice(3, 3)[2]);
            shim.Slice(9).IsEmpty.Should().BeTrue();
            default(Bcp47CharSpan).ToString().Should().BeEmpty();
            ((Action)(() => { var _ = shim.Slice(3, 3)[3]; })).Should().Throw<IndexOutOfRangeException>();
            ((Action)(() => shim.Slice(10))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => shim.Slice(3, 7))).Should().Throw<ArgumentOutOfRangeException>();
        }

        #endregion
    }
}
