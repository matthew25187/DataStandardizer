using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// An observance of a time zone (RFC 5545 §3.6.5): the STANDARD or DAYLIGHT component of a VTIMEZONE.
    /// </summary>
    public sealed class IcalTimeZoneObservance : IcalComponent
    {
        /// <summary>
        /// Creates an empty STANDARD or DAYLIGHT component.
        /// </summary>
        /// <param name="kind">Whether the observance is of standard time or daylight saving time.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not an <see cref="IcalTimeZoneObservanceKind"/>
        /// member.</exception>
        public IcalTimeZoneObservance(IcalTimeZoneObservanceKind kind) : base(GetName(kind))
        {
            Kind = kind;
        }

        /// <summary>
        /// Gets whether the observance is of standard time (STANDARD) or daylight saving time (DAYLIGHT).
        /// </summary>
        public IcalTimeZoneObservanceKind Kind { get; }

        private static string GetName(IcalTimeZoneObservanceKind kind)
        {
            switch (kind)
            {
                case IcalTimeZoneObservanceKind.Standard:
                    return IcalComponentNames.Standard;
                case IcalTimeZoneObservanceKind.Daylight:
                    return IcalComponentNames.Daylight;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "The observance kind is not valid.");
            }
        }

        /// <summary>
        /// Gets or sets the start of the observance (RFC 5545 §3.8.2.4, <c>DTSTART</c>), a DATE-TIME in local time; or <c>null</c> if there is
        /// none.
        /// </summary>
        /// <remarks>
        /// The <c>VALUE</c> and <c>TZID</c> parameters are read and written with the value.  The getter throws
        /// <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDateTime? DtStart
        {
            get { return GetDateTime(IcalPropertyNames.DtStart); }
            set { SetDateTime(IcalPropertyNames.DtStart, value); }
        }

        /// <summary>
        /// Gets or sets the UTC offset in use before the observance begins (RFC 5545 §3.8.3.3, <c>TZOFFSETFROM</c>); or <c>null</c> if
        /// there is none.
        /// </summary>
        /// <remarks>
        /// The getter throws <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalUtcOffset? OffsetFrom
        {
            get { return GetUtcOffset(IcalPropertyNames.TimeZoneOffsetFrom); }
            set { SetUtcOffset(IcalPropertyNames.TimeZoneOffsetFrom, value); }
        }

        /// <summary>
        /// Gets or sets the UTC offset in use during the observance (RFC 5545 §3.8.3.4, <c>TZOFFSETTO</c>); or <c>null</c> if there is
        /// none.
        /// </summary>
        /// <remarks>
        /// The getter throws <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalUtcOffset? OffsetTo
        {
            get { return GetUtcOffset(IcalPropertyNames.TimeZoneOffsetTo); }
            set { SetUtcOffset(IcalPropertyNames.TimeZoneOffsetTo, value); }
        }

        /// <summary>
        /// Gets or sets the recurrence rules (RFC 5545 §3.8.5.3, <c>RRULE</c>).
        /// </summary>
        /// <remarks>
        /// Setting a list replaces every <c>RRULE</c> property with one per rule; an empty list removes them.  The getter throws
        /// <see cref="IcalFileException"/> if a rule is not valid.
        /// </remarks>
        public IReadOnlyList<IcalRecurrenceRule> RecurrenceRules
        {
            get { return GetRecurrenceRules(IcalPropertyNames.RecurrenceRule); }
            set { SetRecurrenceRules(IcalPropertyNames.RecurrenceRule, value); }
        }

        /// <summary>
        /// Gets or sets the customary name of the time zone during the observance (RFC 5545 §3.8.3.2, <c>TZNAME</c>), such as <c>EST</c>;
        /// or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? TimeZoneName
#else
        public string TimeZoneName
#endif
        {
            get { return GetText(IcalPropertyNames.TimeZoneName); }
            set { SetText(IcalPropertyNames.TimeZoneName, value); }
        }
    }
}
