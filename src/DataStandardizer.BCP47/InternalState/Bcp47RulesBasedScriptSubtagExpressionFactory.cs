using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DataStandardizer.ISO15924;

namespace DataStandardizer.BCP47.InternalState
{
    internal class Bcp47RulesBasedScriptSubtagExpressionFactory : IBcp47ExpressionFactory
    {
        private static readonly RegexOptions ExpressionOptions;
        internal static readonly string Pattern;

        static Bcp47RulesBasedScriptSubtagExpressionFactory()
        {
            ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
#if NETCOREAPP3_0_OR_GREATER||NET
            ExpressionOptions |= RegexOptions.Compiled;
#endif

            Pattern = ComposeScriptSubtagPattern();
        }

        public Regex Create()
        {
            return new Regex($"^{Pattern}$", ExpressionOptions);
        }

        public string GetPattern()
        {
            return Pattern;
        }

        private static string ComposeScriptSubtagPattern()
        {
            // Include alpha-4 codes from ISO 15924 (ref. RFC 5646 §2.2.3¶2)
            var iso15924Names = Enum.GetNames(typeof(Iso15924Script));

            // Include alpha-4 codes reserved for private use (ref. RFC 5646 §2.2.3¶3)
            var iso15924ReservedNames = new List<string>();
            for (var thirdCharacterIndex = (byte)'a'; thirdCharacterIndex <= (byte)'b'; thirdCharacterIndex++)
            for (var fourthCharacter = (byte)'a'; fourthCharacter <= (byte)'x'; fourthCharacter++)
            {
                iso15924ReservedNames.Add($"Qa{(char)thirdCharacterIndex}{(char)fourthCharacter}");
            }

            return string.Concat("(?:", string.Join("|", iso15924Names.Concat(iso15924ReservedNames).Select(Regex.Escape)), ")");
        }
    }
}