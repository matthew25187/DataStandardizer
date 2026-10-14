using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A time zone definition (RFC 5545 §3.6.5): the VTIMEZONE component.
    /// </summary>
    public sealed class IcalTimeZone : IcalComponent
    {
        /// <summary>
        /// Creates an empty VTIMEZONE component.
        /// </summary>
        public IcalTimeZone() : base(IcalComponentNames.TimeZone)
        {
        }

        /// <summary>
        /// Gets the observances (STANDARD and DAYLIGHT) of the time zone.
        /// </summary>
        public IEnumerable<IcalTimeZoneObservance> Observances
        {
            get { return GetComponents<IcalTimeZoneObservance>(); }
        }

        /// <summary>
        /// Gets or sets the date and time (RFC 5545 §3.8.7.3, <c>LAST-MODIFIED</c>), in UTC, at which the information was last revised; or
        /// <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The <c>VALUE</c> and <c>TZID</c> parameters are read and written with the value.  The getter throws
        /// <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDateTime? LastModified
        {
            get { return GetDateTime(IcalPropertyNames.LastModified); }
            set { SetDateTime(IcalPropertyNames.LastModified, value); }
        }

        /// <summary>
        /// Gets or sets the time zone identifier (RFC 5545 §3.8.3.1, <c>TZID</c>), which <c>TZID</c> parameters refer to; or <c>null</c> if
        /// there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? TimeZoneId
#else
        public string TimeZoneId
#endif
        {
            get { return GetText(IcalPropertyNames.TimeZoneId); }
            set { SetText(IcalPropertyNames.TimeZoneId, value); }
        }

        /// <summary>
        /// Gets or sets the location of a published definition of the time zone (RFC 5545 §3.8.3.5, <c>TZURL</c>); or <c>null</c> if there
        /// is none.
        /// </summary>
        /// <remarks>
        /// The getter throws <see cref="IcalFileException"/> if the value is not a valid absolute URI.  The setter takes an absolute URI.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Uri? TimeZoneUrl
#else
        public Uri TimeZoneUrl
#endif
        {
            get { return GetUri(IcalPropertyNames.TimeZoneUrl); }
            set { SetUri(IcalPropertyNames.TimeZoneUrl, value, false); }
        }
    }
}
