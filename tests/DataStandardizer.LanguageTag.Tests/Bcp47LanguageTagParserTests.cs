using System;
using System.Diagnostics.CodeAnalysis;
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
        #region Test: CheckSubtag_WithNull

        /// <summary>
        /// Each static check of the string overloads, called with <c>null</c>.
        /// </summary>
        public static TheoryData<string, Action> NullStringChecks => new TheoryData<string, Action>
        {
            { nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), () => Bcp47LanguageTag.CheckPrimaryLanguageSubtag((string)null) },
            { nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), () => Bcp47LanguageTag.CheckExtendedLanguageSubtag((string)null) },
            { nameof(Bcp47LanguageTag.CheckScriptSubtag), () => Bcp47LanguageTag.CheckScriptSubtag((string)null) },
            { nameof(Bcp47LanguageTag.CheckRegionSubtag), () => Bcp47LanguageTag.CheckRegionSubtag((string)null) },
            { nameof(Bcp47LanguageTag.CheckVariantSubtag), () => Bcp47LanguageTag.CheckVariantSubtag((string)null) },
            { nameof(Bcp47LanguageTag.CheckExtensionSubtag), () => Bcp47LanguageTag.CheckExtensionSubtag((string)null) },
            { nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), () => Bcp47LanguageTag.CheckPrivateUseSubtag((string)null) },
            { nameof(Bcp47LanguageTag.IsWellFormedLanguageTagString), () => Bcp47LanguageTag.IsWellFormedLanguageTagString((string)null) }
        };

        [Theory]
        [MemberData(nameof(NullStringChecks), DisableDiscoveryEnumeration = true)]
        public void CheckSubtag_WithNull_ThrowsArgumentNullException(string checkName, Action check)
        {
            // act
            Action testAction = check;

            // assert
            testAction.Should().Throw<ArgumentNullException>("{0} must reject null", checkName);
        }

        #endregion
    }
}
