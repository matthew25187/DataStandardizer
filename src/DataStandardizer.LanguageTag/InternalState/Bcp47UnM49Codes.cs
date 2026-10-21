using System;
using System.Collections.Generic;
using DataStandardizer.Geography;

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// UN M.49 codes, read once from the attributes of <see cref="UnM49AreaByAlpha2CountryCode"/>.
    /// </summary>
    internal static class Bcp47UnM49Codes
    {
        /// <summary>
        /// Every code: the countries and areas, and the global, region, sub-region and intermediate region codes above them.
        /// </summary>
        private static readonly HashSet<ushort> AllCodes = new HashSet<ushort>(UnM49Extensions.GetM49Codes(typeof(UnM49AreaByAlpha2CountryCode)));

        /// <summary>
        /// The global, region, sub-region and intermediate region codes that group the countries and areas, but not the countries and areas themselves.
        /// </summary>
        private static readonly HashSet<ushort> SupranationalCodes = CreateSupranationalCodes();

        /// <summary>
        /// Determine whether a number is a UN M.49 code.
        /// </summary>
        internal static bool Contains(ushort code)
        {
            return AllCodes.Contains(code);
        }

        /// <summary>
        /// Determine whether a number is the UN M.49 code of a global, region, sub-region or intermediate region grouping.
        /// </summary>
        internal static bool ContainsSupranational(ushort code)
        {
            return SupranationalCodes.Contains(code);
        }

        private static HashSet<ushort> CreateSupranationalCodes()
        {
            var codes = new HashSet<ushort>();
            foreach (UnM49AreaByAlpha2CountryCode area in Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)))
            {
                AddIfHasValue(codes, area.GetGlobalCode());
                AddIfHasValue(codes, area.GetRegionCode());
                AddIfHasValue(codes, area.GetSubRegionCode());
                AddIfHasValue(codes, area.GetIntermediateRegionCode());
            }

            return codes;
        }

        private static void AddIfHasValue(HashSet<ushort> codes, ushort? code)
        {
            if (code.HasValue) codes.Add(code.Value);
        }
    }
}
