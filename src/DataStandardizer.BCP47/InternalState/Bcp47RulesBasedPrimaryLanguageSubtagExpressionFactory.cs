using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DataStandardizer.Core;
using DataStandardizer.ISO639;

namespace DataStandardizer.BCP47.InternalState
{
    internal class Bcp47RulesBasedPrimaryLanguageSubtagExpressionFactory : IBcp47ExpressionFactory
    {
        private static readonly RegexOptions ExpressionOptions;
        internal static readonly string pattern;

        static Bcp47RulesBasedPrimaryLanguageSubtagExpressionFactory()
        {
            ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER
            ExpressionOptions |= RegexOptions.Compiled;
#endif

            pattern = ComposePattern();
        }

        public Regex Create()
        {
            return new Regex($"^{pattern}$", ExpressionOptions);
        }

        public string GetPattern()
        {
            return pattern;
        }

        private static string ComposePattern()
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
            for (var secondCharacterIndex = (byte)'a'; secondCharacterIndex <= (byte)'t'; secondCharacterIndex++)
            for (var thirdCharacterIndex = (byte)'a'; thirdCharacterIndex <= (byte)'z'; thirdCharacterIndex++)
            {
                iso639Part2ReservedNames.Add($"q{(char)secondCharacterIndex}{(char)thirdCharacterIndex}");
            }

            // Include subtags that may be registered (ref. RFC 5646 §2.2.1¶5)
            const string registeredSubtags = "[a-zA-Z0-9]{5,8}";

            // Compose pattern.
            var iso639Names = iso639Part1Names.Union(iso639Part2Names).Union(iso639Part2ReservedNames).Union(iso639Part3Names).Union(iso639Part5Names);
            return string.Concat("(?:", string.Join("|", iso639Names.Select(Regex.Escape)), "|", registeredSubtags, ")");
        }
    }
}