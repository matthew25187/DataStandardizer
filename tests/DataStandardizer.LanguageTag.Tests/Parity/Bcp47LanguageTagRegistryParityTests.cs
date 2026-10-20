using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataStandardizer.LanguageTag.InternalState;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests.Parity
{
    /// <summary>
    /// Differential test of the parser that validates language tags against a Subtag Registry against the regular expression it replaced (#223).
    /// </summary>
    /// <remarks>
    /// For every input in the corpus of <see cref="Bcp47LanguageTagParityTests"/>, the registry-based overloads must give the same result as
    /// <see cref="FrozenRegistryBasedExpression"/> and decompose the tag into the same subtags, against the IANA registry and against a trimmed
    /// registry that has no grandfathered or variant records. The one intended difference is #253: the expression accepted an empty subtag in a
    /// position for which the registry has no records, or the empty string when it has no grandfathered records, and the parser does not.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Collection(Bcp47LanguageTagParityCollection.Name)]
    public class Bcp47LanguageTagRegistryParityTests
    {
        private static readonly string[] TrimmedRegistryKeptSubtags = { "en", "zh", "de", "sl", "sr", "yue", "cmn", "Latn", "Hant", "Cyrl", "US", "CH", "DE", "419" };

        private static readonly string[] SubtagGroupNames =
        {
            FrozenRulesBasedExpressions.PrimaryLanguageGroupName, FrozenRulesBasedExpressions.ExtendedLanguageGroupName, FrozenRulesBasedExpressions.ScriptGroupName,
            FrozenRulesBasedExpressions.RegionGroupName, FrozenRulesBasedExpressions.VariantGroupName, FrozenRulesBasedExpressions.ExtensionGroupName,
            FrozenRulesBasedExpressions.PrivateUseGroupName
        };

        [Fact]
        public void Parser_WithIanaSubtagRegistryOverCorpus_AgreesWithFrozenRegularExpression()
        {
            AssertParity(Bcp47LanguageTagFixture.CreateIanaSubtagRegistry(), expectEmptySubtagMatches: false);
        }

        [Fact]
        public void Parser_WithTrimmedSubtagRegistryOverCorpus_AgreesWithFrozenRegularExpression()
        {
            AssertParity(CreateTrimmedSubtagRegistry(), expectEmptySubtagMatches: true);
        }

        private static void AssertParity(SubtagRegistry.SubtagRegistry subtagRegistry, bool expectEmptySubtagMatches)
        {
            // arrange
            var corpus = Bcp47LanguageTagParityTests.BuildCorpus();
            var frozen = new FrozenRegistryBasedExpression(subtagRegistry);
            var mismatches = new List<string>();
            var mismatchCount = 0;
            var emptySubtagMatchCount = 0;

            // act
            Parallel.ForEach(corpus, input =>
            {
                var match = frozen.Match(input);
                if (HasEmptySubtag(input, match))
                {
                    lock (mismatches) emptySubtagMatchCount++;
                }

                foreach (var mismatch in Compare(input, match, subtagRegistry))
                {
                    lock (mismatches)
                    {
                        if (mismatchCount++ < Bcp47LanguageTagParityTests.MaximumReportedMismatches) mismatches.Add(mismatch);
                    }
                }
            });

            // assert
            corpus.Count.Should().BeGreaterThan(100000, "the corpus covers every code list, the IANA registry, combinations and random strings");
            if (expectEmptySubtagMatches)
            {
                emptySubtagMatchCount.Should().BePositive("the corpus has inputs with an empty subtag that the frozen expression accepted against a registry with no records of some type (#253)");
            }
            else
            {
                emptySubtagMatchCount.Should().Be(0, "the frozen expression has no empty alternation for a registry with records of every type");
            }

            mismatchCount.Should().Be(0, "the parser must agree with the regular expression it replaced; the first mismatches were:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, mismatches));
        }

        private static IEnumerable<string> Compare(string input, Match match, SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            // #253: an empty subtag is never valid, whatever records the registry has.
            var matchesSubtags = match.Success && !HasEmptySubtag(input, match);
            var expectedWellFormed = matchesSubtags && FrozenRulesBasedExpressions.IsWellFormed(match);

            Bcp47LanguageTag languageTag;
            var actualTryCreate = Bcp47LanguageTag.TryCreate(input, subtagRegistry, out languageTag);
            if (actualTryCreate != expectedWellFormed) yield return $"TryCreate({Bcp47LanguageTagParityTests.Describe(input)}, registry): expected {expectedWellFormed}, parser returned {actualTryCreate}";

            var actualTryCreateWithTimeout = Bcp47LanguageTag.TryCreate(input, subtagRegistry, Regex.InfiniteMatchTimeout, out _);
            if (actualTryCreateWithTimeout != expectedWellFormed) yield return $"TryCreate({Bcp47LanguageTagParityTests.Describe(input)}, registry, timeout): expected {expectedWellFormed}, parser returned {actualTryCreateWithTimeout}";

            // The parser decomposes any input with the syntax of a language tag, including one that repeats a subtag.
            Bcp47LanguageTagRanges ranges;
            var status = Parse(input, subtagRegistry, out ranges);
            var expectedStatus = !matchesSubtags ? Bcp47ParseStatus.Invalid : expectedWellFormed ? Bcp47ParseStatus.WellFormed : Bcp47ParseStatus.RepeatedSubtag;
            if (status != expectedStatus) yield return $"Parse({Bcp47LanguageTagParityTests.Describe(input)}, registry): expected {expectedStatus}, parser returned {status}";

            if (!matchesSubtags) yield break;

            var expectedSubtags = Bcp47LanguageTagParityTests.DescribeSubtags(match);
            var actualSubtags = Bcp47LanguageTagParityTests.DescribeSubtags(input, ranges);
            if (actualSubtags != expectedSubtags) yield return $"Subtags of {Bcp47LanguageTagParityTests.Describe(input)}: expected {expectedSubtags}, parser returned {actualSubtags}";

            if (actualTryCreate && expectedWellFormed)
            {
                var propertySubtags = Bcp47LanguageTagParityTests.DescribeSubtags(languageTag);
                if (propertySubtags != expectedSubtags) yield return $"Properties of {Bcp47LanguageTagParityTests.Describe(input)}: expected {expectedSubtags}, returned {propertySubtags}";
            }
        }

        /// <summary>
        /// Parse outside the iterator, which cannot hold a span.
        /// </summary>
        private static Bcp47ParseStatus Parse(string input, SubtagRegistry.SubtagRegistry subtagRegistry, out Bcp47LanguageTagRanges ranges)
        {
            return Bcp47LanguageTagParser.Parse(input.AsSpan(), subtagRegistry.Classifier, out ranges);
        }

        /// <summary>
        /// Whether the frozen expression matched the input through an empty alternation: the empty string, or an empty capture of a subtag group (#253).
        /// </summary>
        private static bool HasEmptySubtag(string input, Match match)
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

        /// <summary>
        /// A copy of the IANA registry fixture with a third of its language, extlang, script and region records, every range record, and no
        /// grandfathered or variant records.
        /// </summary>
        private static SubtagRegistry.SubtagRegistry CreateTrimmedSubtagRegistry()
        {
            string content;
            using (var resourceStream = typeof(Bcp47LanguageTagRegistryParityTests).Assembly.GetManifestResourceStream("DataStandardizer.LanguageTag.Tests.Resources.IanaSubtagRegistry.txt"))
            {
                resourceStream.Should().NotBeNull("the IANA Subtag Registry fixture is an embedded resource");
                using (var reader = new StreamReader(resourceStream))
                {
                    content = reader.ReadToEnd();
                }
            }

            var records = Regex.Split(content.Replace("\r\n", "\n"), "\n%%\n");
            var keptRecords = records.Where((record, index) =>
            {
                if (index == 0) return true; // File-Date

                var type = GetField(record, "Type");
                if (type == "grandfathered" || type == "variant") return false;

                var subtag = GetField(record, "Subtag");
                return index % 3 == 0 || (subtag != null && (subtag.Contains("..") || TrimmedRegistryKeptSubtags.Contains(subtag)));
            });

            var subtagRegistry = SubtagRegistry.SubtagRegistry.CreateFromContent(string.Join("\n%%\n", keptRecords));
            subtagRegistry.Count.Should().BeInRange(1000, records.Length / 2, "the trimmed registry keeps about a third of the records");
            return subtagRegistry;
        }

        private static string GetField(string record, string fieldName)
        {
            var prefix = fieldName + ": ";
            return record.Split('\n').Where(line => line.StartsWith(prefix, StringComparison.Ordinal)).Select(line => line.Substring(prefix.Length)).FirstOrDefault();
        }
    }
}
