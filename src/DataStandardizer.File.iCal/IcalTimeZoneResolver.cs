using System;
using System.Globalization;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using DataStandardizer.Chronology;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The time zone resolvers of this package, a way to chain them, and conversions that use them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// RFC 5545 §3.2.19 requires a VTIMEZONE in the calendar for every <c>TZID</c> used, and §3.6.5 says that VTIMEZONE defines the time zone.
    /// <see cref="Default"/> is therefore <see cref="Calendar"/> alone, so that nothing can contradict a definition the calendar holds.
    /// </para>
    /// <para>
    /// A calendar that uses a <c>TZID</c> with no VTIMEZONE breaks that rule, and <see cref="IcalReader"/> rejects it unless
    /// <see cref="IcalOptions.AllowUndefinedTimeZones"/> is set.  To read such a calendar, use <c>Chain(Calendar, TzData)</c>: <see cref="TzData"/>
    /// then fills in only the time zones the calendar leaves undefined.  A caller who distrusts the calendar's definitions, for example because
    /// they are truncated, can use <c>Chain(TzData, Calendar)</c> instead, which departs from RFC 5545 by preferring the TZ Database.
    /// </para>
    /// <para>
    /// When nothing resolves a <c>TZID</c>, <see cref="Resolve"/> and <see cref="ToUtc"/> throw.  To treat such a time as floating instead, call
    /// <see cref="IIcalTimeZoneResolver.TryResolve"/> and handle the result.
    /// </para>
    /// </remarks>
    public static class IcalTimeZoneResolver
    {
        /// <summary>
        /// Gets the resolver that reads the calendar's own VTIMEZONE for the <c>TZID</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The UTC offsets come from the onsets of each observance, given by its <c>DTSTART</c>, <c>RRULE</c> and <c>RDATE</c>, and from its
        /// <c>TZOFFSETFROM</c> and <c>TZOFFSETTO</c>.  Any <c>TZID</c> the calendar defines resolves, including the Windows names, such as
        /// <c>Eastern Standard Time</c>, that Outlook writes.  Before the first onset, the <c>TZOFFSETFROM</c> of that onset is in use.
        /// </para>
        /// <para>
        /// The first VTIMEZONE whose <c>TZID</c> matches, compared ordinally, is used.  It is read when it is resolved, so changes made to it
        /// afterwards do not affect rules already returned.  Nothing resolves when no calendar is given.
        /// </para>
        /// </remarks>
        public static IIcalTimeZoneResolver Calendar { get; } = new CalendarResolver();

        /// <summary>
        /// Gets the resolver that takes time zones from the TZ Database, through <see cref="TzDataTimezone"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A <c>TZID</c> resolves when <see cref="TzDataTimezone.TryParse(string, out TzDataTimezone)"/> recognises it, such as
        /// <c>America/New_York</c>.  A <c>TZID</c> that starts with a solidus is globally unique (RFC 5545 §3.8.3.1), which vendors give by
        /// prefixing a path, as in <c>/mozilla.org/20050126_1/America/New_York</c>; each trailing part of such a path is tried in turn.
        /// </para>
        /// <para>
        /// This resolver ignores the calendar, so it is not in <see cref="Default"/>.  Chain it after <see cref="Calendar"/> to fill in the time
        /// zones that a non-compliant calendar leaves undefined.
        /// </para>
        /// </remarks>
        public static IIcalTimeZoneResolver TzData { get; } = new TzDataResolver();

        /// <summary>
        /// Gets the default resolver, which is <see cref="Calendar"/> alone, as RFC 5545 requires.
        /// </summary>
        public static IIcalTimeZoneResolver Default => Calendar;

        /// <summary>
        /// Combines resolvers into one that tries each in turn, so that the first to resolve a time zone wins.
        /// </summary>
        /// <param name="resolvers">The resolvers, in the order to try them.</param>
        /// <returns>The combined resolver.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resolvers"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="resolvers"/> is empty or contains <c>null</c>.</exception>
        public static IIcalTimeZoneResolver Chain(params IIcalTimeZoneResolver[] resolvers)
        {
            if (resolvers is null)
                throw new ArgumentNullException(nameof(resolvers));
            if (resolvers.Length == 0)
                throw new ArgumentException("At least one resolver must be given.", nameof(resolvers));

            var copy = new IIcalTimeZoneResolver[resolvers.Length];
            for (var index = 0; index < resolvers.Length; index++)
            {
                copy[index] = resolvers[index] ?? throw new ArgumentException("The resolvers cannot contain null.", nameof(resolvers));
            }

            return new ChainResolver(copy);
        }

        /// <summary>
        /// Resolves a time zone identifier, throwing if the resolver does not know it.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <param name="timeZoneId">The time zone identifier, as given by a <c>TZID</c> parameter.</param>
        /// <param name="calendar">The calendar the identifier appears in; or <c>null</c> if there is none.</param>
        /// <returns>The rules of the time zone.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resolver"/> or <paramref name="timeZoneId"/> is <c>null</c>.</exception>
        /// <exception cref="IcalFileException">The resolver does not know the time zone, or found a definition of it that is not valid.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static IcalTimeZoneRules Resolve(this IIcalTimeZoneResolver resolver, string timeZoneId, IcalCalendar? calendar)
#else
        public static IcalTimeZoneRules Resolve(this IIcalTimeZoneResolver resolver, string timeZoneId, IcalCalendar calendar)
#endif
        {
            if (resolver is null)
                throw new ArgumentNullException(nameof(resolver));
            if (timeZoneId is null)
                throw new ArgumentNullException(nameof(timeZoneId));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IcalTimeZoneRules? rules;
#else
            IcalTimeZoneRules rules;
#endif
            if (!resolver.TryResolve(timeZoneId, calendar, out rules) || rules is null)
                throw new IcalFileException(string.Format(CultureInfo.InvariantCulture,
                    "The time zone '{0}' could not be resolved.  RFC 5545 requires a VTIMEZONE for it in the calendar.", timeZoneId));

            return rules;
        }

        /// <summary>
        /// Converts a date-time to UTC, resolving its time zone if it is zoned.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <param name="value">The date-time, in UTC or in a named time zone.</param>
        /// <param name="calendar">The calendar the date-time appears in; or <c>null</c> if there is none.</param>
        /// <returns>The date-time in UTC.  A value already in UTC is returned as it is.</returns>
        /// <remarks>
        /// A zoned time is converted as RFC 5545 §3.3.5 requires: a time in a daylight saving gap is read with the offset in use before the gap,
        /// and a time in an overlap takes its first occurrence.  A leap second is converted as the second before it, and then moved on by a
        /// second.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is floating, so it has no time zone and no single instant.</exception>
        /// <exception cref="IcalFileException">The resolver does not know the time zone, or found a definition of it that is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The result falls outside the range of <see cref="IcalDateTime"/>.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static IcalDateTime ToUtc(this IIcalTimeZoneResolver resolver, IcalDateTime value, IcalCalendar? calendar)
#else
        public static IcalDateTime ToUtc(this IIcalTimeZoneResolver resolver, IcalDateTime value, IcalCalendar calendar)
#endif
        {
            if (resolver is null)
                throw new ArgumentNullException(nameof(resolver));

            switch (value.Kind)
            {
                case IcalDateTimeKind.Utc:
                    return value;
                case IcalDateTimeKind.Floating:
                    throw new ArgumentException("A floating date-time has no time zone, so it has no single instant in UTC.", nameof(value));
            }

            // A zoned value has a time zone identifier.
            var rules = resolver.Resolve(value.TimeZoneId ?? string.Empty, calendar);
            var time = value.Time;
            var isLeapSecond = time.Second == 60;
            var local = new DateTime(value.Date.ToDateTime().Ticks +
                                     (time.Hour * 3600 + time.Minute * 60 + (isLeapSecond ? 59 : time.Second)) * TimeSpan.TicksPerSecond,
                DateTimeKind.Unspecified);
            var utc = rules.ToUtc(local);
            if (isLeapSecond)
            {
                utc = IcalTimeZoneRules.AddClamped(utc, TimeSpan.TicksPerSecond, DateTimeKind.Utc);
            }

            return IcalDateTime.FromDateTime(utc);
        }

        private sealed class CalendarResolver : IIcalTimeZoneResolver
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            public bool TryResolve(string timeZoneId, IcalCalendar? calendar, [NotNullWhen(true)] out IcalTimeZoneRules? rules)
#else
            public bool TryResolve(string timeZoneId, IcalCalendar calendar, out IcalTimeZoneRules rules)
#endif
            {
                if (timeZoneId is null)
                    throw new ArgumentNullException(nameof(timeZoneId));

                rules = null;
                if (calendar is null) return false;

                foreach (var timeZone in calendar.TimeZones)
                {
                    if (!string.Equals(timeZone.TimeZoneId, timeZoneId, StringComparison.Ordinal)) continue;

                    rules = IcalCalendarTimeZoneRules.Create(timeZoneId, timeZone);
                    return true;
                }

                return false;
            }

            public override string ToString()
            {
                return "Calendar";
            }
        }

        private sealed class TzDataResolver : IIcalTimeZoneResolver
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            public bool TryResolve(string timeZoneId, IcalCalendar? calendar, [NotNullWhen(true)] out IcalTimeZoneRules? rules)
#else
            public bool TryResolve(string timeZoneId, IcalCalendar calendar, out IcalTimeZoneRules rules)
#endif
            {
                if (timeZoneId is null)
                    throw new ArgumentNullException(nameof(timeZoneId));

                rules = null;
                TzDataTimezone timezone;
                if (timeZoneId.Length == 0 || !IcalTzDataTimeZoneRules.TryFindTimezone(timeZoneId, out timezone)) return false;

                rules = new IcalTzDataTimeZoneRules(timeZoneId, timezone);
                return true;
            }

            public override string ToString()
            {
                return "TzData";
            }
        }

        private sealed class ChainResolver : IIcalTimeZoneResolver
        {
            private readonly IIcalTimeZoneResolver[] _resolvers;

            internal ChainResolver(IIcalTimeZoneResolver[] resolvers)
            {
                _resolvers = resolvers;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            public bool TryResolve(string timeZoneId, IcalCalendar? calendar, [NotNullWhen(true)] out IcalTimeZoneRules? rules)
#else
            public bool TryResolve(string timeZoneId, IcalCalendar calendar, out IcalTimeZoneRules rules)
#endif
            {
                if (timeZoneId is null)
                    throw new ArgumentNullException(nameof(timeZoneId));

                foreach (var resolver in _resolvers)
                {
                    if (resolver.TryResolve(timeZoneId, calendar, out rules) && rules != null) return true;
                }

                rules = null;
                return false;
            }

            public override string ToString()
            {
                return "Chain(" + string.Join(", ", (object[])_resolvers) + ")";
            }
        }
    }
}
