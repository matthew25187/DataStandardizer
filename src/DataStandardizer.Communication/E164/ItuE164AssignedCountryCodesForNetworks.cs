using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents the ITU E.164 assigned country codes for specific networks.
    /// </summary>
    /// <summary>
    /// International Network 1 (IN1) with the assigned country code 882.
    /// </summary>
    /// <summary>
    /// International Network 2 (IN2) with the assigned country code 883.
    /// </summary>
    /// <summary>
    /// Telecommunications for Disaster Relief (TDR) with the assigned country code 888.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ItuE164AssignedCountryCodesForNetworks : ushort
    {
        /// <summary>
        /// Global Mobile Satellite System (GMSS)
        /// </summary>
        GMSS = 881,

        /// <summary>
        /// International Networks
        /// </summary>
        IN1 = 882,

        /// <summary>
        /// International Networks
        /// </summary>
        IN2 = 883,
    }
}