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
            ((Action)(() => Bcp47LanguageTag.CheckPrimaryLanguageSubtag((string)null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckExtendedLanguageSubtag((string)null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckScriptSubtag((string)null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckRegionSubtag((string)null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckVariantSubtag((string)null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckExtensionSubtag((string)null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.CheckPrivateUseSubtag((string)null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => Bcp47LanguageTag.IsWellFormedLanguageTagString((string)null))).Should().Throw<ArgumentNullException>();
        }

        #endregion
    }
}
