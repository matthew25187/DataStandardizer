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
        /// Determine whether a number is a UN M.49 code.
        /// </summary>
        internal static bool Contains(ushort code)
        {
            return AllCodes.Contains(code);
        }
    }
}
