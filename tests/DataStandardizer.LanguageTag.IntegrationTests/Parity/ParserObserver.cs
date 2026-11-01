using System;
using DataStandardizer.LanguageTag.InternalState;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    /// <summary>
    /// Calls the parser and the members of <see cref="Bcp47LanguageTag"/> for an input, and collects what they return, for a parity test to
    /// compare with what the frozen regular expressions gave.
    /// </summary>
    /// <remarks>
    /// Each method only calls the code under test and copies its results: it has no condition or loop.
    /// </remarks>
    internal static class ParserObserver
    {
        internal static SubtagCheckResults ObserveSubtagChecks(string input)
        {
            return new SubtagCheckResults(
                Bcp47LanguageTag.CheckPrimaryLanguageSubtag(input),
                Bcp47LanguageTag.CheckExtendedLanguageSubtag(input),
                Bcp47LanguageTag.CheckScriptSubtag(input),
                Bcp47LanguageTag.CheckRegionSubtag(input),
                Bcp47LanguageTag.CheckVariantSubtag(input),
                Bcp47LanguageTag.CheckExtensionSubtag(input),
                Bcp47LanguageTag.CheckPrivateUseSubtag(input));
        }

        internal static LanguageTagValidity ObserveValidity(string input)
        {
            return new LanguageTagValidity(
                Bcp47LanguageTag.IsWellFormedLanguageTagString(input),
                Bcp47LanguageTag.TryCreate(input, out _),
                Bcp47LanguageTagParser.Parse(input.AsSpan(), Bcp47RulesBasedSubtagClassifier.Instance, out _));
        }

        /// <summary>
        /// Whether an input is valid against a Subtag Registry. There is no registry-based well-formed check, so <see cref="LanguageTagValidity.IsWellFormed"/>
        /// is what <c>TryCreate</c> returns.
        /// </summary>
        internal static LanguageTagValidity ObserveValidity(string input, SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            var tryCreate = Bcp47LanguageTag.TryCreate(input, subtagRegistry, out _);
            return new LanguageTagValidity(tryCreate, tryCreate, Bcp47LanguageTagParser.Parse(input.AsSpan(), subtagRegistry.Classifier, out _));
        }

        internal static SubtagTexts ObserveDecomposition(string input)
        {
            Bcp47LanguageTagParser.Parse(input.AsSpan(), Bcp47RulesBasedSubtagClassifier.Instance, out var ranges);
            return DescribeRanges(input, ranges);
        }

        internal static SubtagTexts ObserveDecomposition(string input, SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            Bcp47LanguageTagParser.Parse(input.AsSpan(), subtagRegistry.Classifier, out var ranges);
            return DescribeRanges(input, ranges);
        }

        internal static SubtagTexts ObserveProperties(string input)
        {
            return DescribeProperties(Bcp47LanguageTag.Create(input));
        }

        internal static SubtagTexts ObserveProperties(string input, SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            return DescribeProperties(Bcp47LanguageTag.Create(input, subtagRegistry));
        }

        /// <summary>
        /// The text of each range: every subtag of a kind, which are always adjacent, without the leading hyphen; the empty string for a kind the tag doesn't have.
        /// </summary>
        private static SubtagTexts DescribeRanges(string input, Bcp47LanguageTagRanges ranges)
        {
            return new SubtagTexts(
                input.Substring(ranges.PrimaryLanguage.Start, ranges.PrimaryLanguage.Length),
                input.Substring(ranges.ExtendedLanguages.Start, ranges.ExtendedLanguages.Length),
                input.Substring(ranges.Script.Start, ranges.Script.Length),
                input.Substring(ranges.Region.Start, ranges.Region.Length),
                input.Substring(ranges.Variants.Start, ranges.Variants.Length),
                input.Substring(ranges.Extensions.Start, ranges.Extensions.Length),
                input.Substring(ranges.PrivateUse.Start, ranges.PrivateUse.Length));
        }

        /// <summary>
        /// The subtag properties of a language tag, with the subtags of each kind joined by hyphens; <see cref="string.Concat(object)"/> writes an absent subtag as the empty string.
        /// </summary>
        private static SubtagTexts DescribeProperties(Bcp47LanguageTag languageTag)
        {
            return new SubtagTexts(
                string.Concat(languageTag.PrimaryLanguageSubtag),
                string.Join("-", languageTag.ExtendedLanguageSubtags),
                string.Concat(languageTag.ScriptSubtag),
                string.Concat(languageTag.RegionSubtag),
                string.Join("-", languageTag.VariantSubtags),
                string.Join("-", languageTag.ExtensionSubtags),
                string.Concat((object)languageTag.PrivateUseSubtag));
        }
    }
}
