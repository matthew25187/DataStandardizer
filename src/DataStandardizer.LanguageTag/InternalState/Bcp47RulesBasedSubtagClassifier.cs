using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using static DataStandardizer.LanguageTag.Bcp47Constants;
#if NETSTANDARD1_0
using CharSpan = DataStandardizer.LanguageTag.InternalState.Bcp47CharSpan;
#else
using CharSpan = System.ReadOnlySpan<char>;
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Classifies subtags by the default BCP 47 rules: the ISO code lists, the reserved ranges and the syntax of RFC 5646.
    /// </summary>
    internal sealed class Bcp47RulesBasedSubtagClassifier : Bcp47SubtagClassifier
    {
        private const ushort ChannelIslandsCode = 830;

        internal static readonly Bcp47RulesBasedSubtagClassifier Instance = new Bcp47RulesBasedSubtagClassifier();

        private static readonly Bcp47SubtagSet GrandfatheredLanguageTagSet = new Bcp47SubtagSet(GrandfatheredLanguageTags);

        /// <summary>
        /// The two- and three-letter Primary Language subtags, which alone may be followed by Extended Language subtags (ref. RFC 5646 §2.1, §2.2.2).
        /// </summary>
        private static readonly Bcp47SubtagSet ShortPrimaryLanguageSubtagSet = new Bcp47SubtagSet(ComposeShortPrimaryLanguageSubtags());

        private static readonly Bcp47SubtagSet ScriptSubtagSet = new Bcp47SubtagSet(ComposeScriptSubtags());

        private static readonly Bcp47SubtagSet RegionSubtagSet = new Bcp47SubtagSet(ComposeRegionSubtags());

        private Bcp47RulesBasedSubtagClassifier()
        {
        }

        internal override bool IsGrandfathered(CharSpan languageTag)
        {
            return GrandfatheredLanguageTagSet.Contains(languageTag);
        }

        internal override bool IsPrimaryLanguage(CharSpan subtag, out bool allowsExtendedLanguage)
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

        internal override bool IsExtendedLanguage(CharSpan subtag)
        {
            // ref. RFC 5646 §2.2.2
            return subtag.Length == 3 && Bcp47LanguageTagParser.IsAsciiLetters(subtag);
        }

        internal override bool IsScript(CharSpan subtag)
        {
            return subtag.Length == 4 && ScriptSubtagSet.Contains(subtag);
        }

        internal override bool IsRegion(CharSpan subtag)
        {
            return (subtag.Length == 2 || subtag.Length == 3) && RegionSubtagSet.Contains(subtag);
        }

        internal override bool IsVariant(CharSpan subtag)
        {
            // Five to eight characters starting with a letter, or four to eight starting with a digit (ref. RFC 5646 §2.2.5).
            if (subtag.Length < 4 || subtag.Length > 8 || !Bcp47LanguageTagParser.IsAsciiLettersOrDigits(subtag)) return false;

            return subtag.Length > 4 || Bcp47LanguageTagParser.IsAsciiDigit(subtag[0]);
        }

        private static IEnumerable<string> ComposeShortPrimaryLanguageSubtags()
        {
            // Include alpha-2 codes from ISO 639-1:2002 (ref. RFC 5646 §2.2.1¶1)
            var iso639Part1Names = StringEnum.GetNames<Iso639Part1Language>();

            // Include alpha-3 codes from ISO 639-2:1998 (ref. RFC 5646 §2.2.1¶2A)
            var iso639Part2Names = StringEnum.GetNames<Iso639Part2TLanguage>();

            // Include alpha-3 codes from ISO 639-3:2007 (ref. RFC 5646 §2.2.1¶2B)
            var iso639Part3Names = StringEnum.GetNames<Iso639Part3Language>();

            // Include alpha-3 codes from ISO 639-5:2008 (ref. RFC 5646 §2.2.1¶2C)
            var iso639Part5Names = StringEnum.GetNames<Iso639Part5LanguageFamily>();

            // Include alpha-3 codes reserved for local use (ref. RFC 5646 §2.2.1¶3)
            var iso639Part2ReservedNames = new List<string>();
            for (var secondCharacter = 'a'; secondCharacter <= 't'; secondCharacter++)
            for (var thirdCharacter = 'a'; thirdCharacter <= 'z'; thirdCharacter++)
            {
                iso639Part2ReservedNames.Add(string.Concat("q", secondCharacter.ToString(), thirdCharacter.ToString()));
            }

            return iso639Part1Names.Concat(iso639Part2Names).Concat(iso639Part2ReservedNames).Concat(iso639Part3Names).Concat(iso639Part5Names);
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
