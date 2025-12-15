using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents the ITU E.164 assigned country codes for global services.
    /// </summary>
    /// <summary>
    /// International Freephone Service (IFS).
    /// </summary>
    /// <summary>
    /// International Shared Cost Service (ISCS).
    /// </summary>
    /// <summary>
    /// Shared Network Access Code (SNAC).
    /// </summary>
    /// <summary>
    /// Global Mobile Satellite System (GMSS).
    /// </summary>
    /// <summary>
    /// International Premium Rate Service (IPRS).
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ItuE164AssignedCountryCodesForGlobalServices : ushort
    {
        /// <summary>
        /// International Freephone Service
        /// </summary>
        IFS = 800,

        /// <summary>
        /// International Shared Cost Service
        /// </summary>
        ISCS = 808,

        /// <summary>
        /// Inmarsat SNAC
        /// </summary>
        SNAC = 870,

        /// <summary>
        /// International Premium Rate Service (IPRS)
        /// </summary>
        IPRS = 979,
    }
}