using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using DataStandardizer.LanguageTag.InternalState;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests.Parity
{
    /// <summary>
    /// Runs the parity test on its own, after the tests that run in parallel.
    /// </summary>
    /// <remarks>
    /// The comparison uses every core. Run alongside other tests, it slowed down tests that depend on time, such as those with a short
    /// match timeout, enough to make them fail on a build agent.
    /// </remarks>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public class Bcp47LanguageTagParityCollection
    {
        internal const string Name = nameof(Bcp47LanguageTagParityCollection);
    }

    /// <summary>
    /// Differential test of the parser that validates language tags by the default BCP 47 rules against the regular expressions it replaced (#221).
    /// </summary>
    /// <remarks>
    /// For every input in the corpus, the parser must give the same result as <see cref="FrozenRulesBasedExpressions"/> for each <c>Check*</c>
    /// method and for the whole tag, and decompose it into the same subtags, including tags that repeat a subtag and so are not well-formed.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Collection(Bcp47LanguageTagParityCollection.Name)]
    public class Bcp47LanguageTagParityTests
    {
        private const int MaximumReportedMismatches = 50;
        private const string NoSubtag = "(none)";

        private static readonly string[] RfcAppendixAExamples =
        {
            "de", "fr", "ja", "i-enochian", "zh-Hant", "zh-Hans", "sr-Cyrl", "sr-Latn", "zh-cmn-Hans-CN", "cmn-Hans-CN", "zh-yue-HK", "yue-HK", "zh-Hans-CN",
            "sr-Latn-RS", "sl-rozaj", "sl-rozaj-biske", "sl-nedis", "de-CH-1901", "sl-IT-nedis", "hy-Latn-IT-arevela", "de-DE", "en-US", "es-419",
            "de-CH-x-phonebk", "az-Arab-x-AZE-derbend", "x-whatever", "qaa-Qaaa-QM-x-southern", "de-Qaaa", "sr-Latn-QM", "sr-Qaaa-RS", "en-US-u-islamcal",
            "zh-CN-a-myext-x-private", "en-a-myext-b-another", "de-419-DE", "a-DE", "ar-a-aaa-b-bbb-a-ccc", "en-GB-oed", "i-klingon", "zh-min-nan",
            "en-Latn-GB-boont-r-extended-sequence-x-private", "sgn-BE-FR", "art-lojban", "zh-guoyu"
        };

        private static readonly SubtagCheck[] SubtagChecks =
        {
            new SubtagCheck(nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), FrozenRulesBasedExpressions.PrimaryLanguage, Bcp47LanguageTag.CheckPrimaryLanguageSubtag),
            new SubtagCheck(nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), FrozenRulesBasedExpressions.ExtendedLanguage, Bcp47LanguageTag.CheckExtendedLanguageSubtag),
            new SubtagCheck(nameof(Bcp47LanguageTag.CheckScriptSubtag), FrozenRulesBasedExpressions.Script, Bcp47LanguageTag.CheckScriptSubtag),
            new SubtagCheck(nameof(Bcp47LanguageTag.CheckRegionSubtag), FrozenRulesBasedExpressions.Region, Bcp47LanguageTag.CheckRegionSubtag),
            new SubtagCheck(nameof(Bcp47LanguageTag.CheckVariantSubtag), FrozenRulesBasedExpressions.Variant, Bcp47LanguageTag.CheckVariantSubtag),
            new SubtagCheck(nameof(Bcp47LanguageTag.CheckExtensionSubtag), FrozenRulesBasedExpressions.Extension, Bcp47LanguageTag.CheckExtensionSubtag),
            new SubtagCheck(nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), FrozenRulesBasedExpressions.PrivateUse, Bcp47LanguageTag.CheckPrivateUseSubtag)
        };

        [Fact]
        public void Parser_OverCorpus_AgreesWithFrozenRegularExpressions()
        {
            // arrange
            var corpus = BuildCorpus();
            var mismatches = new List<string>();
            var mismatchCount = 0;

            // act
            Parallel.ForEach(corpus, input =>
            {
                foreach (var mismatch in Compare(input))
                {
                    lock (mismatches)
                    {
                        if (mismatchCount++ < MaximumReportedMismatches) mismatches.Add(mismatch);
                    }
                }
            });

            // assert
            corpus.Count.Should().BeGreaterThan(100000, "the corpus covers every code list, the IANA registry, combinations and random strings");
            mismatchCount.Should().Be(0, "the parser must agree with the regular expressions it replaced; the first mismatches were:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, mismatches));
        }

        private static IEnumerable<string> Compare(string input)
        {
            var isAscii = FrozenRulesBasedExpressions.IsAscii(input);
            var hasSeparator = input.IndexOf('-') >= 0;

            foreach (var check in SubtagChecks)
            {
                // A pattern with no hyphen cannot match an input that has one. Skipping it saves running the alternations of every ISO code.
                var expected = isAscii && (!hasSeparator || check.CanMatchSeparator) && check.Frozen.IsMatch(input);
                var actual = check.Parser(input);
                if (actual != expected) yield return $"{check.Name}({Describe(input)}): expected {expected}, parser returned {actual}";
            }

            var match = isAscii ? FrozenRulesBasedExpressions.LanguageTag.Match(input) : Match.Empty;
            var expectedWellFormed = FrozenRulesBasedExpressions.IsWellFormed(match);

            var actualWellFormed = Bcp47LanguageTag.IsWellFormedLanguageTagString(input);
            if (actualWellFormed != expectedWellFormed) yield return $"IsWellFormedLanguageTagString({Describe(input)}): expected {expectedWellFormed}, parser returned {actualWellFormed}";

            Bcp47LanguageTag languageTag;
            var actualTryCreate = Bcp47LanguageTag.TryCreate(input, out languageTag);
            if (actualTryCreate != expectedWellFormed) yield return $"TryCreate({Describe(input)}): expected {expectedWellFormed}, parser returned {actualTryCreate}";

            // The parser decomposes any input with the syntax of a language tag, including one that repeats a subtag.
            Bcp47LanguageTagRanges ranges;
            var status = Parse(input, out ranges);
            var expectedStatus = !match.Success ? Bcp47ParseStatus.Invalid : expectedWellFormed ? Bcp47ParseStatus.WellFormed : Bcp47ParseStatus.RepeatedSubtag;
            if (status != expectedStatus) yield return $"Parse({Describe(input)}): expected {expectedStatus}, parser returned {status}";

            var expectedSubtags = DescribeSubtags(match);
            var actualSubtags = DescribeSubtags(input, ranges);
            if (actualSubtags != expectedSubtags) yield return $"Subtags of {Describe(input)}: expected {expectedSubtags}, parser returned {actualSubtags}";

            if (actualTryCreate && expectedWellFormed)
            {
                var propertySubtags = DescribeSubtags(languageTag);
                if (propertySubtags != expectedSubtags) yield return $"Properties of {Describe(input)}: expected {expectedSubtags}, returned {propertySubtags}";
            }
        }

        /// <summary>
        /// Parse outside the iterator, which cannot hold a span.
        /// </summary>
        private static Bcp47ParseStatus Parse(string input, out Bcp47LanguageTagRanges ranges)
        {
            return Bcp47LanguageTagParser.Parse(input.AsSpan(), Bcp47RulesBasedSubtagClassifier.Instance, out ranges);
        }

        private static string DescribeSubtags(Match match)
        {
            Func<string, string> single = groupName => match.Groups[groupName].Success ? match.Groups[groupName].Value : NoSubtag;
            Func<string, string> many = groupName => string.Join(",", match.Groups[groupName].Captures.Cast<Capture>().Select(capture => capture.Value));

            return string.Join(" | ",
                match.Groups[FrozenRulesBasedExpressions.PrimaryLanguageGroupName].Value,
                many(FrozenRulesBasedExpressions.ExtendedLanguageGroupName),
                single(FrozenRulesBasedExpressions.ScriptGroupName),
                single(FrozenRulesBasedExpressions.RegionGroupName),
                many(FrozenRulesBasedExpressions.VariantGroupName),
                many(FrozenRulesBasedExpressions.ExtensionGroupName),
                single(FrozenRulesBasedExpressions.PrivateUseGroupName));
        }

        private static string DescribeSubtags(string input, Bcp47LanguageTagRanges ranges)
        {
            Func<Bcp47SubtagRange, string> single = range => range.IsEmpty ? NoSubtag : input.Substring(range.Start, range.Length);
            Func<Bcp47SubtagRange, string> many = range => range.IsEmpty ? string.Empty : string.Join(",", Bcp47LanguageTagParser.GetSubtags(input, range));

            return string.Join(" | ",
                ranges.PrimaryLanguage.IsEmpty ? string.Empty : input.Substring(ranges.PrimaryLanguage.Start, ranges.PrimaryLanguage.Length),
                many(ranges.ExtendedLanguages),
                single(ranges.Script),
                single(ranges.Region),
                many(ranges.Variants),
                ranges.Extensions.IsEmpty ? string.Empty : string.Join(",", Bcp47LanguageTagParser.GetExtensions(input, ranges.Extensions)),
                single(ranges.PrivateUse));
        }

        private static string DescribeSubtags(Bcp47LanguageTag languageTag)
        {
            var privateUseSubtag = languageTag.PrivateUseSubtag;

            return string.Join(" | ",
                languageTag.PrimaryLanguageSubtag,
                string.Join(",", languageTag.ExtendedLanguageSubtags),
                languageTag.ScriptSubtag ?? NoSubtag,
                languageTag.RegionSubtag ?? NoSubtag,
                string.Join(",", languageTag.VariantSubtags),
                string.Join(",", languageTag.ExtensionSubtags.Select(subtag => (string)subtag)),
                privateUseSubtag.HasValue ? (string)privateUseSubtag.Value : NoSubtag);
        }

        private static string Describe(string input)
        {
            var description = new StringBuilder("\"");
            foreach (var character in input)
            {
                if (character < ' ' || character > '~')
                {
                    description.Append("\\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
                }
                else
                {
                    description.Append(character);
                }
            }

            return description.Append('"').ToString();
        }

        #region Corpus

        private static List<string> BuildCorpus()
        {
            // All ISO enum names, and the reserved and numeric codes the rules accept.
            var primaryLanguageSubtags = new HashSet<string>(StringComparer.Ordinal);
            primaryLanguageSubtags.UnionWith(StringEnum.GetNames<Iso639Part1Language>());
            primaryLanguageSubtags.UnionWith(StringEnum.GetNames<Iso639Part2TLanguage>());
            primaryLanguageSubtags.UnionWith(StringEnum.GetNames<Iso639Part3Language>());
            primaryLanguageSubtags.UnionWith(StringEnum.GetNames<Iso639Part5LanguageFamily>());

            var otherSubtags = new HashSet<string>(StringComparer.Ordinal) { "830" };
            otherSubtags.UnionWith(Enum.GetNames(typeof(Iso15924Script)));
            otherSubtags.UnionWith(Enum.GetNames(typeof(Iso3166Part1Alpha2Country)));
            otherSubtags.UnionWith(Enum.GetValues(typeof(Iso3166Part1Alpha2Country)).Cast<Iso3166Part1Alpha2Country>().Select(code => ((ushort)code).ToString("000", CultureInfo.InvariantCulture)));
            foreach (var area in Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>())
            {
                otherSubtags.UnionWith(new[] { area.GetGlobalCode(), area.GetRegionCode(), area.GetSubRegionCode(), area.GetIntermediateRegionCode() }
                    .Where(code => code.HasValue).Select(code => code.GetValueOrDefault().ToString("000", CultureInfo.InvariantCulture)));
            }

            for (var first = 'a'; first <= 'z'; first++)
            for (var second = 'a'; second <= 'z'; second++)
            {
                otherSubtags.Add(string.Concat(first, second));
                otherSubtags.Add(string.Concat("q", first, second));
                otherSubtags.Add(string.Concat("Qa", first, second));
            }

            // Every Tag and Subtag in the IANA registry fixture.
            var registryValues = ReadRegistryValues().ToList();
            var registryTags = registryValues.Where(value => value.IndexOf('-') >= 0).ToList();
            otherSubtags.UnionWith(registryValues.Where(value => !primaryLanguageSubtags.Contains(value)));

            // Subtags of every length and character class.
            otherSubtags.UnionWith(new[]
            {
                "", "a", "x", "X", "1", "0", "ab", "a1", "12", "abc", "ab1", "123", "1234", "abcd", "123a", "a123", "abcde", "12345", "a1234", "1abcd", "abcdef",
                "abcdefgh", "1234abcd", "abcdefghi", "123456789", "-", "--", "a-", "-a", "a--b", "en_US", "en US", "en.US", "*", "x-", "x-a", "a-bb", "a-b",
                "u-co-phonebk", "u-ca-hebrew-tz-jeruslm", "t-a", "x-123456789", "x-abcdefgh", "u-abcdefghi"
            });

            // Each subtag alone, in its upper- and lower-case forms.
            var corpus = new HashSet<string>(StringComparer.Ordinal);
            foreach (var subtag in primaryLanguageSubtags.Concat(otherSubtags))
            {
                corpus.Add(subtag);
                corpus.Add(subtag.ToUpperInvariant());
                corpus.Add(subtag.ToLowerInvariant());
            }

            // Each subtag other than an ISO 639 code, and every eighth of those, in each position of a tag.
            foreach (var subtag in otherSubtags.Concat(primaryLanguageSubtags.Where((_, index) => index % 8 == 0)))
            {
                corpus.Add("en-" + subtag);
                corpus.Add("zh-yue-" + subtag);
                corpus.Add("en-Latn-" + subtag);
                corpus.Add("en-US-" + subtag);
                corpus.Add("en-1901-" + subtag);
                corpus.Add("en-a-bb-" + subtag);
                corpus.Add("abcde-" + subtag);
                corpus.Add(subtag + "-US");
                corpus.Add(subtag + "-yue");
                corpus.Add("x-" + subtag);
                corpus.Add(subtag + "-x-a");
            }

            // The RFC 5646 examples and the registry's whole tags, in several cases and combined with other subtags.
            foreach (var languageTag in RfcAppendixAExamples.Concat(registryTags))
            {
                foreach (var form in new[] { languageTag, languageTag.ToUpperInvariant(), languageTag.ToLowerInvariant(), AlternateCase(languageTag) })
                {
                    corpus.Add(form);
                    corpus.Add(form + "-x-a");
                    corpus.Add(form + "-a-bb");
                    corpus.Add(form + "-1901");
                    corpus.Add("x-" + form);
                }
            }

            // Combinations of every subtag kind, valid and invalid.
            var primaryLanguages = new[] { "en", "EN", "zh", "yue", "qaa", "abcd", "abcdefgh", "abcdefghi", "12345", "zz", "i", "x" };
            var extendedLanguages = new[] { "", "-yue", "-cmn-YUE", "-aaa-bbb-ccc", "-aaa-bbb-ccc-ddd" };
            var scripts = new[] { "", "-Latn", "-LATN", "-Qabx", "-Qaby", "-Abcd" };
            var regions = new[] { "", "-US", "-us", "-419", "-001", "-830", "-840", "-QM", "-XZ", "-ZZ", "-QL", "-1" };
            var variants = new[] { "", "-1901", "-rozaj", "-rozaj-biske", "-rozaj-ROZAJ", "-1901-1901", "-abcd", "-1abc", "-abcdefgh", "-abcdefghi" };
            var extensions = new[]
            {
                "", "-u-co-phonebk", "-a-bb-b-cc", "-a-bb-A-cc", "-a-b", "-a", "-x1-ab", "-1-abcdefgh", "-u-abcdefghi", "-t-en-US",
                "-a-bb-b-cc-c-dd", "-a-bb-b-cc-B-dd", "-9-bb-1-cc-9-dd"
            };
            var privateUses = new[] { "", "-x-a", "-X-private-use", "-x-abcdefghi", "-x", "-x-", "-x-a--b" };
            foreach (var primaryLanguage in primaryLanguages)
            foreach (var extendedLanguage in extendedLanguages)
            foreach (var script in scripts)
            foreach (var region in regions)
            foreach (var variant in variants.Take(6))
            {
                corpus.Add(primaryLanguage + extendedLanguage + script + region + variant);
            }

            foreach (var primaryLanguage in primaryLanguages.Take(6))
            foreach (var region in regions.Take(6))
            foreach (var variant in variants)
            foreach (var extension in extensions)
            foreach (var privateUse in privateUses)
            {
                corpus.Add(primaryLanguage + region + variant + extension + privateUse);
            }

            // Line feeds, control characters and non-ASCII case equivalents of ASCII letters.
            foreach (var input in new[]
                     {
                         "en", "en-US", "US", "i-klingon", "x-a", "it-CH", "en-x-k", "en-x-s", "sk", "es-ES", "u-co-phonebk", "1901",
                         "sl-rozaj-biske-1994", "de-CH-1901-u-co-phonebk", "kok-Knda-IN", "en-a-bbb-x-kkkk", "Latn", "Kore", "IT"
                     })
            {
                corpus.Add(input + "\n");
                corpus.Add(input + "\r\n");
                corpus.Add("\n" + input);
                corpus.Add(input + "\t");
                corpus.Add(input + "\0");
                corpus.Add(input.Replace('k', 'K'));
                corpus.Add(input.Replace('K', 'K'));
                corpus.Add(input.Replace('s', 'ſ'));
                corpus.Add(input.Replace('i', 'ı'));
                corpus.Add(input.Replace('I', 'İ'));
                corpus.Add(input.Replace('-', '‑'));
                corpus.Add(input + "é");
            }

            // Random strings over a small alphabet, so that most of them are near misses.
            const string alphabet = "aAbBeEnNsSuUxXzZ0149--\nKı";
            var random = new Random(5646);
            for (var count = 0; count < 20000; count++)
            {
                var length = random.Next(0, 25);
                var characters = new char[length];
                for (var index = 0; index < length; index++)
                {
                    characters[index] = alphabet[random.Next(alphabet.Length)];
                }

                corpus.Add(new string(characters));
            }

            return corpus.ToList();
        }

        private static string AlternateCase(string value)
        {
            return new string(value.Select((character, index) => index % 2 == 0 ? char.ToUpperInvariant(character) : char.ToLowerInvariant(character)).ToArray());
        }

        private static IEnumerable<string> ReadRegistryValues()
        {
            using (var resourceStream = typeof(Bcp47LanguageTagParityTests).Assembly.GetManifestResourceStream("DataStandardizer.LanguageTag.Tests.Resources.IanaSubtagRegistry.txt"))
            {
                resourceStream.Should().NotBeNull("the IANA Subtag Registry fixture is an embedded resource");
                using (var reader = new StreamReader(resourceStream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("Subtag: ", StringComparison.Ordinal) || line.StartsWith("Tag: ", StringComparison.Ordinal))
                        {
                            yield return line.Substring(line.IndexOf(' ') + 1);
                        }
                    }
                }
            }
        }

        #endregion

        private sealed class SubtagCheck
        {
            internal SubtagCheck(string name, Regex frozen, Func<string, bool> parser)
            {
                Name = name;
                Frozen = frozen;
                Parser = parser;
                CanMatchSeparator = frozen.ToString().IndexOf('-') >= 0;
            }

            internal string Name { get; }
            internal Regex Frozen { get; }
            internal Func<string, bool> Parser { get; }
            internal bool CanMatchSeparator { get; }
        }
    }
}
