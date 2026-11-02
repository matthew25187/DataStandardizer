using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using static DataStandardizer.LanguageTag.Bcp47Constants;

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Classifies subtags for <see cref="LanguageTagConformance.IsoCodeLists"/>: the ISO code lists and the reserved ranges for the primary language, script and region, and the syntax of RFC 5646 for everything else.
    /// </summary>
    /// <remarks>
    /// This is an approximation of validity for a caller that cannot load a Subtag Registry, not RFC 5646 validity. It does not check variants, four- to eight-letter languages or extlang prefixes.
    /// </remarks>
    internal sealed class Bcp47IsoCodeListClassifier : Bcp47SubtagClassifier
    {
        private const ushort ChannelIslandsCode = 830;

        internal static readonly Bcp47IsoCodeListClassifier Instance = new Bcp47IsoCodeListClassifier();

        private static readonly Bcp47SubtagSet GrandfatheredLanguageTagSet = new Bcp47SubtagSet(GrandfatheredLanguageTags);

        /// <summary>
        /// The two- and three-letter Primary Language subtags, which alone may be followed by Extended Language subtags (ref. RFC 5646 §2.1, §2.2.2).
        /// </summary>
        private static readonly Bcp47SubtagSet ShortPrimaryLanguageSubtagSet = new Bcp47SubtagSet(ComposeShortPrimaryLanguageSubtags());

        private static readonly Bcp47SubtagSet ScriptSubtagSet = new Bcp47SubtagSet(ComposeScriptSubtags());

        private static readonly Bcp47SubtagSet RegionSubtagSet = new Bcp47SubtagSet(ComposeRegionSubtags());

        private Bcp47IsoCodeListClassifier()
        {
        }

        internal override bool IsGrandfathered(ReadOnlySpan<char> languageTag)
        {
            return GrandfatheredLanguageTagSet.Contains(languageTag);
        }

        internal override bool IsPrimaryLanguage(ReadOnlySpan<char> subtag, out bool allowsExtendedLanguage)
        {
            allowsExtendedLanguage = false;
            if (!Bcp47LanguageTagParser.IsAsciiLetters(subtag)) return false;

            if (subtag.Length <= 3)
            {
                allowsExtendedLanguage = ShortPrimaryLanguageSubtagSet.Contains(subtag);
                return allowsExtendedLanguage;
            }

            // Include subtags reserved for future standardisation (ref. RFC 5646 §2.2.1¶4) and subtags that may be registered (ref. RFC 5646 §2.2.1¶5).
            return subtag.Length <= 8;
        }

        internal override bool IsExtendedLanguage(ReadOnlySpan<char> subtag)
        {
            // ref. RFC 5646 §2.2.2
            return Bcp47SyntaxSubtagClassifier.Instance.IsExtendedLanguage(subtag);
        }

        internal override bool IsScript(ReadOnlySpan<char> subtag)
        {
            return subtag.Length == 4 && ScriptSubtagSet.Contains(subtag);
        }

        internal override bool IsRegion(ReadOnlySpan<char> subtag)
        {
            return (subtag.Length == 2 || subtag.Length == 3) && RegionSubtagSet.Contains(subtag);
        }

        internal override bool IsVariant(ReadOnlySpan<char> subtag)
        {
            // ref. RFC 5646 §2.2.5
            return Bcp47SyntaxSubtagClassifier.Instance.IsVariant(subtag);
        }

        private static IEnumerable<string> ComposeShortPrimaryLanguageSubtags()
        {
            return Iso639SubtagSets.Part1
                .Concat(Iso639SubtagSets.Part2TWithoutPart1Equivalent)
                .Concat(Iso639SubtagSets.LocalUse)
                .Concat(Iso639SubtagSets.Part3WithoutPart1Equivalent)
                .Concat(Iso639SubtagSets.Part5);
        }

        private static IEnumerable<string> ComposeScriptSubtags()
        {
            // Include alpha-4 codes from ISO 15924 (ref. RFC 5646 §2.2.3¶2)
            var iso15924Names = Enum.GetNames(typeof(Iso15924Script));

            // Include alpha-4 codes reserved for private use, Qaaa to Qaaz and then Qaba to Qabx (ref. RFC 5646 §2.2.3¶3)
            var iso15924ReservedNames = new List<string>();
            for (var thirdCharacter = 'a'; thirdCharacter <= 'b'; thirdCharacter++)
            for (var fourthCharacter = 'a'; fourthCharacter <= (thirdCharacter == 'a' ? 'z' : 'x'); fourthCharacter++)
            {
                iso15924ReservedNames.Add(string.Concat("Qa", thirdCharacter.ToString(), fourthCharacter.ToString()));
            }

            return iso15924Names.Concat(iso15924ReservedNames);
        }

        private static IEnumerable<string> ComposeRegionSubtags()
        {
            // Include alpha-2 codes from ISO 3166-1 (ref. RFC 5646 §2.2.4¶2)
            var iso3166Part1Codes = Enum.GetNames(typeof(Iso3166Part1Alpha2Country));

            // Include reserved subtags (ref. RFC 5646 §2.2.4¶3)
            var iso3166ReservedCodes = new List<string> { "AA", "ZZ" };
            for (var secondCharacter = 'M'; secondCharacter <= 'Z'; secondCharacter++)
            {
                iso3166ReservedCodes.Add(string.Concat("Q", secondCharacter.ToString()));
            }

            for (var secondCharacter = 'A'; secondCharacter <= 'Z'; secondCharacter++)
            {
                iso3166ReservedCodes.Add(string.Concat("X", secondCharacter.ToString()));
            }

            // Include digit-3 region codes from UN M.49 (ref. RFC 5646 §2.2.4¶4A)
            var areas = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().ToArray();
            var globalCodes = areas.Select(code => code.GetGlobalCode());
            var regionCodes = areas.Select(code => code.GetRegionCode());
            var subRegionCodes = areas.Select(code => code.GetSubRegionCode());
            var intermediateRegionCodes = areas.Select(code => code.GetIntermediateRegionCode());
            // Exclude the Channel Islands code, which may be registered but is not, whatever UN M.49 groups it under (ref. RFC 5646 §2.2.4¶4E-F)
            var numericRegionSubtags = globalCodes.Concat(regionCodes).Concat(subRegionCodes).Concat(intermediateRegionCodes)
                .Where(code => code.HasValue && code.Value != ChannelIslandsCode)
                .Select(code => code.GetValueOrDefault().ToString("000", CultureInfo.InvariantCulture));

            return iso3166Part1Codes.Concat(iso3166ReservedCodes).Concat(numericRegionSubtags);
        }
    }
}
