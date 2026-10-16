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
    /// <see cref="System"/> resolves through the operating system's time zones, which include the Windows names that <see cref="TzData"/>
    /// lacks.  Their data varies from one machine to another, so place it last, as in <c>Chain(Calendar, TzData, System)</c>.
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
        /// Gets the resolver that takes time zones from the operating system, through <see cref="TimeZoneInfo.FindSystemTimeZoneById"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A <c>TZID</c> resolves when the system knows it.  From .NET 6 on, that includes both TZ Database names, such as
        /// <c>America/New_York</c>, and Windows names, such as <c>Eastern Standard Time</c>, on every operating system that has ICU; so this
        /// resolver can read a <c>TZID</c> that Outlook wrote without its VTIMEZONE, which <see cref="TzData"/> cannot.  .NET Framework knows
        /// only the Windows names.  Vendor prefixes are stripped as by <see cref="TzData"/>.  A <c>TZID</c> the system does not know, or that
        /// could name a file outside its time zone data, does not resolve, so that the next resolver of a chain is tried.
        /// </para>
        /// <para>
        /// The system's data is kept current by updates to the operating system, or to ICU, so it may hold rules newer than those of
        /// <see cref="TzData"/>, which are fixed when that package is built.  The results also agree with whatever else the application
        /// computes with <see cref="TimeZoneInfo"/>.
        /// </para>
        /// <para>
        /// However, the data depends on the machine.  As the <see cref="TimeZoneInfo"/> documentation warns, not every time zone is present in
        /// the Windows registry, time zones can be added to or removed from it, and it need not hold their history.  On Linux and macOS, and on
        /// Windows with ICU, which time zones are present and how much history they hold also varies from one machine to another.
        /// <see cref="TzData"/>, by contrast, always holds every time zone of the TZ Database release it was built from.
        /// </para>
        /// <para>
        /// This resolver is therefore not in <see cref="Default"/>, and should be placed last in a chain, after <see cref="TzData"/>, as in
        /// <c>Chain(Calendar, TzData, System)</c>, so that the same calendar gives the same times on every machine wherever it can.
        /// </para>
        /// <para>
        /// The transitions of a time zone are found by sampling its offset a day apart, so two changes of offset less than a day apart that
        /// cancel each other out are not seen, and finding the transitions over a long period takes time in proportion to its length.  A
        /// transition has no <see cref="IcalTimeZoneTransition.Name"/>, as <see cref="TimeZoneInfo"/> gives no customary abbreviation.
        /// </para>
        /// </remarks>
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        public static IIcalTimeZoneResolver System { get; } = new SystemResolver();
#endif

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
            return ToUtc(resolver.Resolve(value.TimeZoneId ?? string.Empty, calendar), value);
        }

        /// <summary>
        /// Converts the wall-clock time of a date-time to UTC with the rules of its time zone, whatever its kind.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The result falls outside the range of <see cref="IcalDateTime"/>.</exception>
        internal static IcalDateTime ToUtc(IcalTimeZoneRules rules, IcalDateTime value)
        {
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

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private sealed class SystemResolver : IIcalTimeZoneResolver
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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                TimeZoneInfo? timeZone;
#else
                TimeZoneInfo timeZone;
#endif
                if (!IcalSystemTimeZoneRules.TryFindTimeZone(timeZoneId, out timeZone) || timeZone is null) return false;

                rules = new IcalSystemTimeZoneRules(timeZoneId, timeZone);
                return true;
            }

            public override string ToString()
            {
                return "System";
            }
        }
#endif

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
