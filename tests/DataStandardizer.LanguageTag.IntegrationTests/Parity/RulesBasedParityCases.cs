using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DataStandardizer.LanguageTag.InternalState;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    /// <summary>
    /// The test cases of <see cref="Bcp47LanguageTagParityTests"/>: every input of the corpus, with what the frozen regular expressions of
    /// <see cref="FrozenRulesBasedExpressions"/> give for it.
    /// </summary>
    /// <remarks>
    /// This is the test case factory of the parity tests, so it may loop and branch: it runs the reference implementation, not the code under test.
    /// The cases are built once, on every core, the first time a test asks for them.
    /// </remarks>
    internal sealed class RulesBasedParityCases
    {
        private static readonly Lazy<RulesBasedParityCases> SharedCases = new Lazy<RulesBasedParityCases>(() => new RulesBasedParityCases());

        private static readonly (Regex Frozen, bool CanMatchSeparator)[] FrozenChecks =
        {
            WithSeparatorCheck(FrozenRulesBasedExpressions.PrimaryLanguage),
            WithSeparatorCheck(FrozenRulesBasedExpressions.ExtendedLanguage),
            WithSeparatorCheck(FrozenRulesBasedExpressions.Script),
            WithSeparatorCheck(FrozenRulesBasedExpressions.Region),
            WithSeparatorCheck(FrozenRulesBasedExpressions.Variant),
            WithSeparatorCheck(FrozenRulesBasedExpressions.Extension),
            WithSeparatorCheck(FrozenRulesBasedExpressions.PrivateUse)
        };

        private RulesBasedParityCases()
        {
            var expectations = ParityCorpus.Inputs
                .Where(input => !HasCorrectedPrivateUseScriptSubtag(input))
                .AsParallel()
                .AsOrdered()
                .Select(Expect)
                .ToList();

            SubtagChecks = expectations.Select(expectation => new DifferentialCase<SubtagCheckResults>(expectation.Input, expectation.SubtagChecks)).ToList();
            Validity = expectations.Select(expectation => new DifferentialCase<LanguageTagValidity>(expectation.Input, expectation.Validity)).ToList();
            Decomposition = expectations.Select(expectation => new DifferentialCase<SubtagTexts>(expectation.Input, expectation.Subtags)).ToList();
            Properties = expectations
                .Where(expectation => expectation.Validity.IsWellFormed)
                .Select(expectation => new DifferentialCase<SubtagTexts>(expectation.Input, expectation.Subtags))
                .ToList();
        }

        internal static RulesBasedParityCases Shared => SharedCases.Value;

        /// <summary>
        /// Every input, with whether each frozen subtag expression matches it.
        /// </summary>
        internal IReadOnlyList<DifferentialCase<SubtagCheckResults>> SubtagChecks { get; }

        /// <summary>
        /// Every input, with whether the frozen language tag expression matches it and whether the match repeats a subtag.
        /// </summary>
        internal IReadOnlyList<DifferentialCase<LanguageTagValidity>> Validity { get; }

        /// <summary>
        /// Every input, with the subtags the frozen language tag expression matched in it, including in a tag that repeats a subtag.
        /// </summary>
        internal IReadOnlyList<DifferentialCase<SubtagTexts>> Decomposition { get; }

        /// <summary>
        /// Every input that is a well-formed language tag, with the subtags the frozen language tag expression matched in it.
        /// </summary>
        internal IReadOnlyList<DifferentialCase<SubtagTexts>> Properties { get; }

        /// <summary>
        /// Determine whether an input has a subtag that the frozen regular expressions wrongly rejected as a Script subtag reserved for private use.
        /// </summary>
        /// <remarks>
        /// The frozen expressions left out <c>Qaay</c> and <c>Qaaz</c>, which RFC 5646 §2.2.3¶3 reserves for private use. The parser accepts them (#323),
        /// so the comparison skips inputs that have either one as a subtag. The unit tests of <c>Bcp47LanguageTag</c> cover them instead.
        /// </remarks>
        private static bool HasCorrectedPrivateUseScriptSubtag(string input)
        {
            return input.Split('-').Any(subtag => string.Equals(subtag, "Qaay", StringComparison.OrdinalIgnoreCase) || string.Equals(subtag, "Qaaz", StringComparison.OrdinalIgnoreCase));
        }

        private static (Regex, bool) WithSeparatorCheck(Regex frozen)
        {
            return (frozen, frozen.ToString().IndexOf('-') >= 0);
        }

        private static (string Input, SubtagCheckResults SubtagChecks, LanguageTagValidity Validity, SubtagTexts Subtags) Expect(string input)
        {
            var isAscii = FrozenRulesBasedExpressions.IsAscii(input);
            var hasSeparator = input.IndexOf('-') >= 0;

            // A pattern with no hyphen cannot match an input that has one. Skipping it saves running the alternations of every ISO code.
            var checks = FrozenChecks.Select(check => isAscii && (!hasSeparator || check.CanMatchSeparator) && check.Frozen.IsMatch(input)).ToArray();

            var match = isAscii ? FrozenRulesBasedExpressions.LanguageTag.Match(input) : Match.Empty;
            var isWellFormed = FrozenRulesBasedExpressions.IsWellFormed(match);
            var status = !match.Success ? Bcp47ParseStatus.Invalid : isWellFormed ? Bcp47ParseStatus.Accepted : Bcp47ParseStatus.RepeatedSubtag;

            return (input,
                new SubtagCheckResults(checks[0], checks[1], checks[2], checks[3], checks[4], checks[5], checks[6]),
                new LanguageTagValidity(isWellFormed, isWellFormed, status),
                FrozenSubtags.Of(match));
        }
    }

    /// <summary>
    /// The subtags that a match of a frozen language tag expression captured.
    /// </summary>
    internal static class FrozenSubtags
    {
        internal static SubtagTexts Of(Match match)
        {
            return new SubtagTexts(
                match.Groups[FrozenRulesBasedExpressions.PrimaryLanguageGroupName].Value,
                Join(match, FrozenRulesBasedExpressions.ExtendedLanguageGroupName),
                match.Groups[FrozenRulesBasedExpressions.ScriptGroupName].Value,
                match.Groups[FrozenRulesBasedExpressions.RegionGroupName].Value,
                Join(match, FrozenRulesBasedExpressions.VariantGroupName),
                Join(match, FrozenRulesBasedExpressions.ExtensionGroupName),
                match.Groups[FrozenRulesBasedExpressions.PrivateUseGroupName].Value);
        }

        private static string Join(Match match, string groupName)
        {
            return string.Join("-", match.Groups[groupName].Captures.Cast<Capture>().Select(capture => capture.Value));
        }
    }
}
