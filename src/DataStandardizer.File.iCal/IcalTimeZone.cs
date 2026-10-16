using System;
using System.Collections.Generic;
using DataStandardizer.Chronology;

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
        /// Builds a VTIMEZONE from the TZ Database, defining a time zone over a period.
        /// </summary>
        /// <param name="timezone">The TZ Database zone.  Its identifier becomes the <c>TZID</c>.</param>
        /// <param name="from">The start of the period, which is included.</param>
        /// <param name="to">The end of the period, which is excluded.</param>
        /// <returns>The VTIMEZONE, holding the <c>TZID</c> and the observances.</returns>
        /// <remarks>
        /// <para>
        /// RFC 5545 §3.2.19 requires a calendar to hold a VTIMEZONE for every <c>TZID</c> it uses.  Add the result to the calendar's
        /// <see cref="IcalComponent.Components"/> so that the calendar <see cref="IcalWriter"/> produces carries the definition, choosing a
        /// period that covers the calendar's times.
        /// </para>
        /// <para>
        /// The observances are built from the transitions the TZ Database gives over the period, so they are exact within it.  The first
        /// observance starts at <paramref name="from"/>, with the offset then in use as both its <c>TZOFFSETFROM</c> and its <c>TZOFFSETTO</c>.
        /// Each later one gathers the transitions that share a kind, a pair of offsets and a name: its <c>DTSTART</c> is the first of them,
        /// and <c>RDATE</c> properties list the rest, each in the local time before the transition.  <c>RRULE</c> is not used.  Before
        /// <paramref name="from"/>, the definition gives the offset in use at <paramref name="from"/>; from <paramref name="to"/> onwards, it
        /// gives the offset that follows the last transition in the period.
        /// </para>
        /// <para>
        /// Times are written to the whole second; a fraction of a second in <paramref name="from"/> is dropped.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="to"/> precedes <paramref name="from"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="timezone"/> is the default value, or its identifier is not that of a
        /// known time zone.</exception>
        public static IcalTimeZone FromTzData(TzDataTimezone timezone, DateTimeOffset from, DateTimeOffset to)
        {
            if (to < from)
                throw new ArgumentOutOfRangeException(nameof(to), to, "The end of the period must not precede its start.");

            var fromUtc = from.UtcDateTime;
            fromUtc = new DateTime(fromUtc.Ticks - fromUtc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
            var initial = timezone.GetOffsetInfo(fromUtc);

            // The identifier is not null, as GetOffsetInfo has rejected the default value.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? timeZoneId = timezone;
#else
            string timeZoneId = timezone;
#endif
            var timeZone = new IcalTimeZone { TimeZoneId = timeZoneId ?? string.Empty };
            var initialOffset = new IcalUtcOffset(initial.UtcOffset);
            timeZone.Components.Add(CreateObservance(IcalTzDataTimeZoneRules.GetKind(initial), IcalTzDataTimeZoneRules.GetName(initial),
                fromUtc + initial.UtcOffset, initialOffset, initialOffset));

            var observances = new Dictionary<ObservanceKey, IcalTimeZoneObservance>();
            var current = new ObservanceKey(IcalTzDataTimeZoneRules.GetKind(initial), initialOffset, initialOffset,
                IcalTzDataTimeZoneRules.GetName(initial));
            foreach (var tzDataTransition in timezone.GetTransitions(fromUtc, to.UtcDateTime))
            {
                if (tzDataTransition.InstantUtc <= fromUtc) continue;

                var transition = IcalTzDataTimeZoneRules.ToTransition(tzDataTransition);
                var key = new ObservanceKey(transition.Kind, transition.OffsetFrom, transition.OffsetTo, transition.Name);

                // A transition that changes only what an observance cannot express, such as the standard offset alone, is left out.
                if (transition.OffsetFrom == transition.OffsetTo && key.Kind == current.Kind &&
                    string.Equals(key.Name, current.Name, StringComparison.Ordinal))
                    continue;

                current = key;
                var onset = transition.InstantUtc + transition.OffsetFrom.Offset;
                if (observances.TryGetValue(key, out var observance))
                {
                    observance.Properties.Add(new IcalContentLine(IcalPropertyNames.RecurrenceDates,
                        ToLocalDateTime(onset).ToString(IcalValueFormat.Text)));
                }
                else
                {
                    observance = CreateObservance(transition.Kind, transition.Name, onset, transition.OffsetFrom, transition.OffsetTo);
                    observances.Add(key, observance);
                    timeZone.Components.Add(observance);
                }
            }

            return timeZone;
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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static IcalTimeZoneObservance CreateObservance(IcalTimeZoneObservanceKind kind, string? name, DateTime onset,
            IcalUtcOffset offsetFrom, IcalUtcOffset offsetTo)
#else
        private static IcalTimeZoneObservance CreateObservance(IcalTimeZoneObservanceKind kind, string name, DateTime onset,
            IcalUtcOffset offsetFrom, IcalUtcOffset offsetTo)
#endif
        {
            return new IcalTimeZoneObservance(kind)
            {
                DtStart = ToLocalDateTime(onset),
                OffsetFrom = offsetFrom,
                OffsetTo = offsetTo,
                TimeZoneName = name
            };
        }

        /// <summary>
        /// Gets the floating date-time of a local time, which RFC 5545 §3.6.5 requires for the <c>DTSTART</c> and <c>RDATE</c> of an observance.
        /// </summary>
        private static IcalDateTime ToLocalDateTime(DateTime local)
        {
            return IcalDateTime.FromDateTime(DateTime.SpecifyKind(local, DateTimeKind.Unspecified));
        }

        /// <summary>
        /// What the transitions an observance gathers share: a kind, a pair of offsets and a name.
        /// </summary>
        private readonly struct ObservanceKey : IEquatable<ObservanceKey>
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal ObservanceKey(IcalTimeZoneObservanceKind kind, IcalUtcOffset offsetFrom, IcalUtcOffset offsetTo, string? name)
#else
            internal ObservanceKey(IcalTimeZoneObservanceKind kind, IcalUtcOffset offsetFrom, IcalUtcOffset offsetTo, string name)
#endif
            {
                Kind = kind;
                OffsetFrom = offsetFrom;
                OffsetTo = offsetTo;
                Name = name;
            }

            internal IcalTimeZoneObservanceKind Kind { get; }

            internal IcalUtcOffset OffsetFrom { get; }

            internal IcalUtcOffset OffsetTo { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal string? Name { get; }
#else
            internal string Name { get; }
#endif

            public bool Equals(ObservanceKey other)
            {
                return Kind == other.Kind && OffsetFrom == other.OffsetFrom && OffsetTo == other.OffsetTo &&
                       string.Equals(Name, other.Name, StringComparison.Ordinal);
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            public override bool Equals(object? obj)
#else
            public override bool Equals(object obj)
#endif
            {
                return obj is ObservanceKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hashCode = ((int)Kind * 31 + OffsetFrom.GetHashCode()) * 31 + OffsetTo.GetHashCode();
                return Name is null ? hashCode : hashCode * 31 + StringComparer.Ordinal.GetHashCode(Name);
            }
        }
    }
}
