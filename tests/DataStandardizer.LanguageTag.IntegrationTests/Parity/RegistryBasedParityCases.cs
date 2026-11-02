using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DataStandardizer.LanguageTag.InternalState;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    /// <summary>
    /// The test cases of <see cref="Bcp47LanguageTagRegistryParityTests"/> for one Subtag Registry: every input of the corpus, with what the frozen
    /// regular expression of <see cref="FrozenRegistryBasedExpression"/> gives for it against that registry.
    /// </summary>
    /// <remarks>
    /// This is the test case factory of the parity tests, so it may loop and branch: it runs the reference implementation, not the code under test.
    /// The one intended difference is #253: the expression accepted an empty subtag in a position for which the registry has no records, or the
    /// empty string when it has no grandfathered records, and the parser does not. An empty subtag is never valid, whatever records the registry
    /// has, so the expected result for such an input is that it is not a language tag.
    /// </remarks>
    internal sealed class RegistryBasedParityCases
    {
        private static readonly string[] TrimmedRegistryKeptSubtags = { "en", "zh", "de", "sl", "sr", "yue", "cmn", "Latn", "Hant", "Cyrl", "US", "CH", "DE", "419" };

        private static readonly string[] SubtagGroupNames =
        {
            FrozenRulesBasedExpressions.PrimaryLanguageGroupName, FrozenRulesBasedExpressions.ExtendedLanguageGroupName, FrozenRulesBasedExpressions.ScriptGroupName,
            FrozenRulesBasedExpressions.RegionGroupName, FrozenRulesBasedExpressions.VariantGroupName, FrozenRulesBasedExpressions.ExtensionGroupName,
            FrozenRulesBasedExpressions.PrivateUseGroupName
        };

        private static readonly Lazy<RegistryBasedParityCases> SharedIanaCases =
            new Lazy<RegistryBasedParityCases>(() => new RegistryBasedParityCases(IanaSubtagRegistryResource.CreateSubtagRegistry()));

        private static readonly Lazy<RegistryBasedParityCases> SharedTrimmedCases =
            new Lazy<RegistryBasedParityCases>(() => new RegistryBasedParityCases(CreateTrimmedSubtagRegistry()));

        private RegistryBasedParityCases(SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            Registry = subtagRegistry;
            Frozen = new FrozenRegistryBasedExpression(subtagRegistry);

            var expectations = ParityCorpus.Inputs
                .AsParallel()
                .AsOrdered()
                .Select(Expect)
                .ToList();

            Validity = expectations.Select(expectation => new DifferentialCase<LanguageTagValidity>(expectation.Input, expectation.Validity)).ToList();
            Decomposition = expectations
                .Where(expectation => expectation.MatchesSubtags)
                .Select(expectation => new DifferentialCase<SubtagTexts>(expectation.Input, expectation.Subtags))
                .ToList();
            Properties = expectations
                .Where(expectation => expectation.Validity.TryCreate)
                .Select(expectation => new DifferentialCase<SubtagTexts>(expectation.Input, expectation.Subtags))
                .ToList();
        }

        /// <summary>
        /// The cases against a copy of the IANA registry fixture.
        /// </summary>
        internal static RegistryBasedParityCases Iana => SharedIanaCases.Value;

        /// <summary>
        /// The cases against a registry with no grandfathered or variant records, from <see cref="CreateTrimmedSubtagRegistry"/>.
        /// </summary>
        internal static RegistryBasedParityCases Trimmed => SharedTrimmedCases.Value;

        internal SubtagRegistry.SubtagRegistry Registry { get; }

        internal FrozenRegistryBasedExpression Frozen { get; }

        /// <summary>
        /// Every input, with whether it is a language tag against the registry.
        /// </summary>
        internal IReadOnlyList<DifferentialCase<LanguageTagValidity>> Validity { get; }

        /// <summary>
        /// Every input that has the syntax of a language tag against the registry, with the subtags the frozen expression matched in it.
        /// </summary>
        internal IReadOnlyList<DifferentialCase<SubtagTexts>> Decomposition { get; }

        /// <summary>
        /// Every input that is a valid language tag against the registry, with the subtags the frozen expression matched in it.
        /// </summary>
        internal IReadOnlyList<DifferentialCase<SubtagTexts>> Properties { get; }

        /// <summary>
        /// A copy of the IANA registry fixture with a third of its language, extlang, script and region records, every range record, and no
        /// grandfathered or variant records.
        /// </summary>
        internal static SubtagRegistry.SubtagRegistry CreateTrimmedSubtagRegistry()
        {
            var records = Regex.Split(IanaSubtagRegistryResource.ReadResourceText().Replace("\r\n", "\n"), "\n%%\n");
            var keptRecords = records.Where((record, index) =>
            {
                if (index == 0) return true; // File-Date

                var type = GetField(record, "Type");
                if (type == "grandfathered" || type == "variant") return false;

                var subtag = GetField(record, "Subtag");
                return index % 3 == 0 || (subtag != null && (subtag.Contains("..") || TrimmedRegistryKeptSubtags.Contains(subtag)));
            });

            return SubtagRegistry.SubtagRegistry.CreateFromContent(string.Join("\n%%\n", keptRecords));
        }

        /// <summary>
        /// Whether the frozen expression matched the input through an empty alternation: the empty string, or an empty capture of a subtag group (#253).
        /// </summary>
        internal static bool HasEmptySubtag(string input, Match match)
        {
            if (!match.Success) return false;

            if (input.Length == 0) return true;

            foreach (var groupName in SubtagGroupNames)
            {
                foreach (Capture capture in match.Groups[groupName].Captures)
                {
                    if (capture.Length == 0) return true;
                }
            }

            return false;
        }

        private static string GetField(string record, string fieldName)
        {
            var prefix = fieldName + ": ";
            return record.Split('\n').Where(line => line.StartsWith(prefix, StringComparison.Ordinal)).Select(line => line.Substring(prefix.Length)).FirstOrDefault();
        }

        private (string Input, bool MatchesSubtags, LanguageTagValidity Validity, SubtagTexts Subtags) Expect(string input)
        {
            var match = Frozen.Match(input);
            var matchesSubtags = match.Success && !HasEmptySubtag(input, match);
            var isValid = matchesSubtags && FrozenRulesBasedExpressions.IsWellFormed(match);
            var status = !matchesSubtags ? Bcp47ParseStatus.Invalid : isValid ? Bcp47ParseStatus.Accepted : Bcp47ParseStatus.RepeatedSubtag;

            return (input, matchesSubtags, new LanguageTagValidity(isValid, isValid, status), FrozenSubtags.Of(match));
        }
    }
}
