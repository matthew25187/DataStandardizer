using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using DataStandardizer.ISO3166;
using DataStandardizer.UNM49;

namespace DataStandardizer.BCP47.InternalState
{
    internal class Bcp47RulesBasedRegionSubtagExpressionFactory : IBcp47ExpressionFactory
    {
        private static readonly RegexOptions ExpressionOptions;
        internal static readonly string Pattern;

        static Bcp47RulesBasedRegionSubtagExpressionFactory()
        {
            ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            ExpressionOptions |= RegexOptions.Compiled;
#endif

            Pattern = ComposePattern();
        }

        public Regex Create()
        {
            return new Regex($"^{Pattern}$", ExpressionOptions);
        }

        public string GetPattern()
        {
            return Pattern;
        }

        private static string ComposePattern()
        {
            // Include alpha-2 codes from ISO 3166-1 (ref. RFC 5646 §2.2.4¶2)
            var iso3166Part1Codes = Enum.GetNames(typeof(Iso3166Part1Alpha2Country));

            // Include reserved subtags (ref. RFC 5646 §2.2.4¶3)
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

            // Include digit-3 region codes from UN M.49 (ref. RFC 5646 §2.2.4¶4A)
            var globalCodes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetGlobalCode()).Where(code => code.HasValue).Cast<ushort>();
            var regionCodes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetRegionCode()).Where(code => code.HasValue).Cast<ushort>();
            var subRegionCodes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetSubRegionCode()).Where(code => code.HasValue).Cast<ushort>();
            var intermediateRegionCodes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetIntermediateRegionCode()).Where(code => code.HasValue).Cast<ushort>();
            var numericRegionSubtags = globalCodes.Union(regionCodes).Union(subRegionCodes).Union(intermediateRegionCodes).Select(code => $"{code:000}");

            // Include Channel Islands code (ref. RFC 5646 §2.2.4¶4E)
            UnM49AreaByAlpha2CountryCode? channelIslandsSubtag = null;
            if (!Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<ushort>().Contains<ushort>(830))
            {
                channelIslandsSubtag = (UnM49AreaByAlpha2CountryCode?)Enum.ToObject(typeof(UnM49AreaByAlpha2CountryCode), 830);
            }

            return string.Concat("(?:", string.Join("|", iso3166Part1Codes.Concat(iso3166ReservedCodes).Concat(numericRegionSubtags).Select(Regex.Escape)), channelIslandsSubtag.HasValue ? $"|{(ushort)channelIslandsSubtag.Value}" : string.Empty, ")");
        }
    }
}