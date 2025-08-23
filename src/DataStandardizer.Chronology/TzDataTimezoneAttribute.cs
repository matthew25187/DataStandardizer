using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Metadata for a TZ Database timezone.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TzDataTimezoneAttribute : Attribute
    {
        public TzDataTimezoneAttribute(double latitude, double longitude, params string[] isoCountryCodes)
        {
            Latitude = latitude;
            Longitude = longitude;
            IsoCountryCodes = isoCountryCodes;
        }

        /// <summary>
        /// Gets or sets the timezone comment.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Comment { get; set; }
#else
        [CanBeNull]
        public string Comment { get; set; }
#endif
        /// <summary>
        /// Gets the ISO 3166 Part 1 Alpha-2 country codes for the countries covered by the timezone.
        /// </summary>
        public string[] IsoCountryCodes { get; }

        /// <summary>
        /// Gets the latitude of the principal location in the timezone.
        /// </summary>
        public double Latitude { get; }

        /// <summary>
        /// Gets the longitude of the principal location in the timezone.
        /// </summary>
        public double Longitude { get; }
    }
}
