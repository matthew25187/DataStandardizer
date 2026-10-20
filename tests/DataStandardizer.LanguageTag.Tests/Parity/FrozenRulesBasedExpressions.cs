using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;

namespace DataStandardizer.LanguageTag.Tests.Parity
{
    /// <summary>
    /// The regular expressions that validated language tags by the default BCP 47 rules before the parser replaced them (#221).
    /// </summary>
    /// <remarks>
    /// Frozen copy of the patterns the <c>Bcp47RulesBased*ExpressionFactory</c> classes composed at commit 983790f, with the options and
    /// <c>^...\z</c> anchoring of <c>Bcp47ExpressionCache</c>, the ASCII check of <c>Bcp47TagExpression</c>, and the duplicate checks in
    /// <c>Bcp47LanguageTag.IsWellFormed(Match)</c>. Do not change it to follow the package: it is the reference the parser is compared with.
    /// </remarks>
    internal static class FrozenRulesBasedExpressions
    {
        internal const string PrimaryLanguageGroupName = "PrimaryLanguage";
        internal const string ExtendedLanguageGroupName = "ExtendedLanguage";
        internal const string ScriptGroupName = "Script";
        internal const string RegionGroupName = "Region";
        internal const string VariantGroupName = "Variant";
        internal const string ExtensionGroupName = "Extension";
        internal const string PrivateUseGroupName = "PrivateUse";

        private const RegexOptions ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled;

        private const string LongPrimaryLanguagePattern = "[a-zA-Z]{4,8}";
        private const string ExtendedLanguagePattern = "[a-zA-Z]{3}";
        private const string VariantPattern = "(?:(?=[a-zA-Z])[a-zA-Z0-9]{5,8}|(?=[0-9])[a-zA-Z0-9]{4,8})";
        private const string ExtensionPattern = "(?!x)[0-9a-zA-Z](?:-[0-9a-zA-Z]{2,8})+";
        private const string PrivateUsePattern = "x(?:-[0-9a-zA-Z]{1,8})+";
        private const ushort ChannelIslandsCode = 830;

        private static readonly string[] GrandfatheredLanguageTags =
        {
            "en-GB-oed", "i-ami", "i-bnn", "i-default", "i-enochian", "i-hak", "i-klingon", "i-lux", "i-mingo", "i-navajo", "i-pwn", "i-tao", "i-tay", "i-tsu",
            "sgn-BE-FR", "sgn-BE-NL", "sgn-CH-DE",
            "art-lojban", "cel-gaulish", "no-bok", "no-nyn", "zh-guoyu", "zh-hakka", "zh-min", "zh-min-nan", "zh-xiang"
        };

        static FrozenRulesBasedExpressions()
        {
            var shortPrimaryLanguagePattern = ComposeShortPrimaryLanguagePattern();
            var scriptPattern = ComposeScriptPattern();
            var regionPattern = ComposeRegionPattern();

            PrimaryLanguage = Create(string.Concat("(?:", shortPrimaryLanguagePattern, "|", LongPrimaryLanguagePattern, ")"));
            ExtendedLanguage = Create(ExtendedLanguagePattern);
            Script = Create(scriptPattern);
            Region = Create(regionPattern);
            Variant = Create(VariantPattern);
            Extension = Create(ExtensionPattern);
            PrivateUse = Create(PrivateUsePattern);

            var extendedLanguageSubtagGroupPattern = $"(?:(?:-(?<{ExtendedLanguageGroupName}>{ExtendedLanguagePattern})){{1,3}})";
            var primaryLanguageSubtagGroupPattern = string.Concat(
                "(?:(?<", PrimaryLanguageGroupName, ">", shortPrimaryLanguagePattern, ")", extendedLanguageSubtagGroupPattern, "?",
                "|(?<", PrimaryLanguageGroupName, ">", LongPrimaryLanguagePattern, "))");
            var scriptSubtagGroupPattern = $"(?:-(?<{ScriptGroupName}>{scriptPattern}))";
            var regionSubtagGroupPattern = $"(?:-(?<{RegionGroupName}>{regionPattern}))";
            var variantSubtagGroupPattern = $"(?:(?:-(?<{VariantGroupName}>{VariantPattern}))+)";
            var extensionSubtagGroupPattern = $"(?:(?:-(?<{ExtensionGroupName}>{ExtensionPattern}))+)";
            var privateUseSubtagGroupPattern = $"(?:-(?<{PrivateUseGroupName}>{PrivateUsePattern}))";
            var grandfatheredLanguageTagPattern = string.Join("|", GrandfatheredLanguageTags.Select(Regex.Escape));
            LanguageTag = Create(string.Concat("(?:", grandfatheredLanguageTagPattern, "|(?<", PrivateUseGroupName, ">", PrivateUsePattern, ")|", primaryLanguageSubtagGroupPattern,
                scriptSubtagGroupPattern, "?", regionSubtagGroupPattern, "?", variantSubtagGroupPattern, "?", extensionSubtagGroupPattern, "?", privateUseSubtagGroupPattern, "?)"));
        }

        internal static Regex PrimaryLanguage { get; }
        internal static Regex ExtendedLanguage { get; }
        internal static Regex Script { get; }
        internal static Regex Region { get; }
        internal static Regex Variant { get; }
        internal static Regex Extension { get; }
        internal static Regex PrivateUse { get; }
        internal static Regex LanguageTag { get; }

        /// <summary>
        /// Whether the input has only ASCII characters; any other input was rejected before matching.
        /// </summary>
        internal static bool IsAscii(string input)
        {
            foreach (var character in input)
            {
                if (character > '\u007F')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Whether a match of <see cref="LanguageTag"/> is well-formed: no variant subtag or extension singleton repeats, case-insensitively.
        /// </summary>
        internal static bool IsWellFormed(Match languageTagMatch)
        {
            if (!languageTagMatch.Success) return false;

            var variantSubtags = languageTagMatch.Groups[VariantGroupName].Captures.Cast<Capture>().Select(capture => capture.Value).ToArray();
            for (var index = 1; index < variantSubtags.Length; index++)
            for (var earlierIndex = 0; earlierIndex < index; earlierIndex++)
            {
                if (string.Equals(variantSubtags[index], variantSubtags[earlierIndex], StringComparison.OrdinalIgnoreCase)) return false;
            }

            var extensionSubtags = languageTagMatch.Groups[ExtensionGroupName].Captures.Cast<Capture>().Select(capture => capture.Value).ToArray();
            for (var index = 1; index < extensionSubtags.Length; index++)
            for (var earlierIndex = 0; earlierIndex < index; earlierIndex++)
            {
                if (char.ToLowerInvariant(extensionSubtags[index][0]) == char.ToLowerInvariant(extensionSubtags[earlierIndex][0])) return false;
            }

            return true;
        }

        private static Regex Create(string pattern)
        {
            return new Regex($"^{pattern}\\z", ExpressionOptions);
        }

        private static string ComposeShortPrimaryLanguagePattern()
        {
            var iso639Part1Names = StringEnum.GetNames<Iso639Part1Language>();
            var iso639Part2Names = StringEnum.GetNames<Iso639Part2TLanguage>();
            var iso639Part3Names = StringEnum.GetNames<Iso639Part3Language>();
            var iso639Part5Names = StringEnum.GetNames<Iso639Part5LanguageFamily>();
            var iso639Part2ReservedNames = new List<string>();
            for (var secondCharacterIndex = (byte)'a'; secondCharacterIndex <= (byte)'t'; secondCharacterIndex++)
            for (var thirdCharacterIndex = (byte)'a'; thirdCharacterIndex <= (byte)'z'; thirdCharacterIndex++)
            {
                iso639Part2ReservedNames.Add($"q{(char)secondCharacterIndex}{(char)thirdCharacterIndex}");
            }

            var iso639Names = iso639Part1Names.Union(iso639Part2Names).Union(iso639Part2ReservedNames).Union(iso639Part3Names).Union(iso639Part5Names);
            return string.Concat("(?:", string.Join("|", iso639Names.Select(Regex.Escape)), ")");
        }

        private static string ComposeScriptPattern()
        {
            var iso15924Names = Enum.GetNames(typeof(Iso15924Script));
            var iso15924ReservedNames = new List<string>();
            for (var thirdCharacterIndex = (byte)'a'; thirdCharacterIndex <= (byte)'b'; thirdCharacterIndex++)
            for (var fourthCharacter = (byte)'a'; fourthCharacter <= (byte)'x'; fourthCharacter++)
            {
                iso15924ReservedNames.Add($"Qa{(char)thirdCharacterIndex}{(char)fourthCharacter}");
            }

            return string.Concat("(?:", string.Join("|", iso15924Names.Concat(iso15924ReservedNames).Select(Regex.Escape)), ")");
        }

        private static string ComposeRegionPattern()
        {
            var iso3166Part1Codes = Enum.GetNames(typeof(Iso3166Part1Alpha2Country));

            var iso3166QReservedCodes = new List<string>();
            for (var secondCharacterIndex = (byte)'M'; secondCharacterIndex <= (byte)'Z'; secondCharacterIndex++)
            {
                iso3166QReservedCodes.Add($"Q{(char)secondCharacterIndex}");
            }

            var iso3166XReservedCodes = new List<string>();
            for (var secondCharacterIndex = (byte)'A'; secondCharacterIndex <= (byte)'Z'; secondCharacterIndex++)
            {
                iso3166XReservedCodes.Add($"X{(char)secondCharacterIndex}");
            }

            var iso3166ReservedCodes = new List<string> { "AA", "ZZ" };
            iso3166ReservedCodes.InsertRange(1, iso3166XReservedCodes);
            iso3166ReservedCodes.InsertRange(1, iso3166QReservedCodes);

            var areas = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().ToArray();
            var globalCodes = areas.Select(code => code.GetGlobalCode()).Where(code => code.HasValue).Cast<ushort>();
            var regionCodes = areas.Select(code => code.GetRegionCode()).Where(code => code.HasValue).Cast<ushort>();
            var subRegionCodes = areas.Select(code => code.GetSubRegionCode()).Where(code => code.HasValue).Cast<ushort>();
            var intermediateRegionCodes = areas.Select(code => code.GetIntermediateRegionCode()).Where(code => code.HasValue).Cast<ushort>();
            var numericRegionSubtags = globalCodes.Union(regionCodes).Union(subRegionCodes).Union(intermediateRegionCodes).Where(code => code != ChannelIslandsCode).Select(code => $"{code:000}");

            return string.Concat("(?:", string.Join("|", iso3166Part1Codes.Concat(iso3166ReservedCodes).Concat(numericRegionSubtags).Select(Regex.Escape)), ")");
        }
    }
}
