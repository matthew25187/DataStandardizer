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
