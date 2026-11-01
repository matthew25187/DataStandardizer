using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    /// <summary>
    /// The inputs that the parity tests compare the parser with the frozen regular expressions over: every code list, every subtag and tag of
    /// the IANA registry fixture, combinations of every kind of subtag, and random strings.
    /// </summary>
    /// <remarks>
    /// The corpus is the same on every run: the random strings come from a generator with a fixed seed.
    /// </remarks>
    internal static class ParityCorpus
    {
        /// <summary>
        /// The seed of the generator of random strings: the number of the RFC that defines language tags.
        /// </summary>
        private const int RandomSeed = 5646;

        private const int RandomStringCount = 20000;

        /// <summary>
        /// One more than the length of the longest random string.
        /// </summary>
        private const int RandomStringLengthLimit = 25;

        /// <summary>
        /// The alphabet of the random strings, small so that most of them are near misses.
        /// </summary>
        private const string RandomAlphabet = "aAbBeEnNsSuUxXzZ0149--\n\x212A\x0131";

        /// <summary>
        /// The examples of RFC 5646 Appendix A.
        /// </summary>
        internal static readonly string[] RfcAppendixAExamples =
        {
            "de", "fr", "ja", "i-enochian", "zh-Hant", "zh-Hans", "sr-Cyrl", "sr-Latn", "zh-cmn-Hans-CN", "cmn-Hans-CN", "zh-yue-HK", "yue-HK", "zh-Hans-CN",
            "sr-Latn-RS", "sl-rozaj", "sl-rozaj-biske", "sl-nedis", "de-CH-1901", "sl-IT-nedis", "hy-Latn-IT-arevela", "de-DE", "en-US", "es-419",
            "de-CH-x-phonebk", "az-Arab-x-AZE-derbend", "x-whatever", "qaa-Qaaa-QM-x-southern", "de-Qaaa", "sr-Latn-QM", "sr-Qaaa-RS", "en-US-u-islamcal",
            "zh-CN-a-myext-x-private", "en-a-myext-b-another", "de-419-DE", "a-DE", "ar-a-aaa-b-bbb-a-ccc", "en-GB-oed", "i-klingon", "zh-min-nan",
            "en-Latn-GB-boont-r-extended-sequence-x-private", "sgn-BE-FR", "art-lojban", "zh-guoyu"
        };

        private static readonly Lazy<IReadOnlyList<string>> SharedInputs = new Lazy<IReadOnlyList<string>>(Build);

        internal static IReadOnlyList<string> Inputs => SharedInputs.Value;

        private static IReadOnlyList<string> Build()
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
            var registryValues = ReadRegistryValues();
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
                corpus.Add(input.Replace('k', 'K'));
                corpus.Add(input.Replace('K', '\x212A'));
                corpus.Add(input.Replace('s', '\x017F'));
                corpus.Add(input.Replace('i', '\x0131'));
                corpus.Add(input.Replace('I', '\x0130'));
                corpus.Add(input.Replace('-', '\x2011'));
                corpus.Add(input + "\x00E9");
            }

            // Random strings over a small alphabet, so that most of them are near misses.
            var random = new Random(RandomSeed);
            for (var count = 0; count < RandomStringCount; count++)
            {
                var length = random.Next(0, RandomStringLengthLimit);
                var characters = new char[length];
                for (var index = 0; index < length; index++)
                {
                    characters[index] = RandomAlphabet[random.Next(RandomAlphabet.Length)];
                }

                corpus.Add(new string(characters));
            }

            return corpus.ToList();
        }

        private static string AlternateCase(string value)
        {
            return new string(value.Select((character, index) => index % 2 == 0 ? char.ToUpperInvariant(character) : char.ToLowerInvariant(character)).ToArray());
        }

        /// <summary>
        /// Every value of a Subtag or Tag field in the IANA registry fixture, read from the file without the registry reader.
        /// </summary>
        private static List<string> ReadRegistryValues()
        {
            var values = new List<string>();
            using (var reader = new StringReader(IanaSubtagRegistryResource.ReadResourceText()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("Subtag: ", StringComparison.Ordinal) || line.StartsWith("Tag: ", StringComparison.Ordinal))
                    {
                        values.Add(line.Substring(line.IndexOf(' ') + 1));
                    }
                }
            }

            return values;
        }
    }
}
