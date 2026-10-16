using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    public readonly partial struct IcalRecurrenceRule
    {
        private static readonly IcalOptions DefaultOptions = new IcalOptions();

        /// <summary>
        /// Gets the occurrences of the rule from a start date-time, in local wall-clock time.
        /// </summary>
        /// <param name="start">The start (DTSTART).  Each occurrence has its kind and time zone.</param>
        /// <param name="from">The earliest occurrence to give, inclusive; or <c>null</c> to start at <paramref name="start"/>.</param>
        /// <param name="to">The end of the occurrences to give, exclusive; or <c>null</c> for none, which needs a rule with COUNT or UNTIL.</param>
        /// <returns>The occurrences, in increasing order, generated as they are enumerated.</returns>
        /// <remarks>
        /// See <see cref="GetOccurrences(IcalDateTime, IcalDateTime?, IcalDateTime?, Func{IcalDateTime, IcalDateTime}, IcalOptions)"/> for how
        /// the rule is applied.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The rule breaks a constraint listed under <see cref="IsValid"/>.  Also thrown while
        /// enumerating, when more consecutive periods than <see cref="IcalOptions.MaxRecurrenceIterations"/> give no occurrence.</exception>
        /// <exception cref="ArgumentException">The rule has neither COUNT nor UNTIL and <paramref name="to"/> is <c>null</c>; or its UNTIL is
        /// in UTC and <paramref name="start"/> is not, which needs the overload that converts occurrences to UTC.</exception>
        public IEnumerable<IcalDateTime> GetOccurrences(IcalDateTime start, IcalDateTime? from, IcalDateTime? to)
        {
            return GetDateTimeOccurrences(start, from, to, null, DefaultOptions);
        }

        /// <summary>
        /// Gets the occurrences of the rule from a start date-time, in local wall-clock time.
        /// </summary>
        /// <param name="start">The start (DTSTART).  Each occurrence has its kind and time zone.</param>
        /// <param name="from">The earliest occurrence to give, inclusive; or <c>null</c> to start at <paramref name="start"/>.</param>
        /// <param name="to">The end of the occurrences to give, exclusive; or <c>null</c> for none, which needs a rule with COUNT or UNTIL.</param>
        /// <param name="options">The options, of which <see cref="IcalOptions.MaxRecurrenceIterations"/> applies.</param>
        /// <returns>The occurrences, in increasing order, generated as they are enumerated.</returns>
        /// <remarks>
        /// See <see cref="GetOccurrences(IcalDateTime, IcalDateTime?, IcalDateTime?, Func{IcalDateTime, IcalDateTime}, IcalOptions)"/> for how
        /// the rule is applied.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The rule breaks a constraint listed under <see cref="IsValid"/>.  Also thrown while
        /// enumerating, when more consecutive periods than <see cref="IcalOptions.MaxRecurrenceIterations"/> give no occurrence.</exception>
        /// <exception cref="ArgumentException">The rule has neither COUNT nor UNTIL and <paramref name="to"/> is <c>null</c>; its UNTIL is in
        /// UTC and <paramref name="start"/> is not, which needs the overload that converts occurrences to UTC; or
        /// <see cref="IcalOptions.MaxRecurrenceIterations"/> is less than 1.</exception>
        public IEnumerable<IcalDateTime> GetOccurrences(IcalDateTime start, IcalDateTime? from, IcalDateTime? to, IcalOptions options)
        {
            return GetDateTimeOccurrences(start, from, to, null, options);
        }

        /// <summary>
        /// Gets the occurrences of the rule from a start date-time, in local wall-clock time, with a conversion to UTC for a UNTIL in UTC.
        /// </summary>
        /// <param name="start">The start (DTSTART).  Each occurrence has its kind and time zone.</param>
        /// <param name="from">The earliest occurrence to give, inclusive; or <c>null</c> to start at <paramref name="start"/>.</param>
        /// <param name="to">The end of the occurrences to give, exclusive; or <c>null</c> for none, which needs a rule with COUNT or UNTIL.</param>
        /// <param name="toUtc">Converts an occurrence to UTC, to compare it with a UNTIL in UTC.  It is called only when UNTIL is in UTC and
        /// <paramref name="start"/> is not, and the wall-clock fields of what it returns are taken as UTC.  For a VTIMEZONE observance, whose
        /// DTSTART is floating, it subtracts the observance's TZOFFSETFROM; for a zoned start it resolves the time zone.</param>
        /// <param name="options">The options, of which <see cref="IcalOptions.MaxRecurrenceIterations"/> applies.</param>
        /// <returns>The occurrences, in increasing order, generated as they are enumerated.</returns>
        /// <remarks>
        /// <para>
        /// The rule is applied in local time throughout: occurrences are wall-clock values with the kind and time zone of
        /// <paramref name="start"/>, and resolving them to UTC, including any that fall in a daylight saving gap or overlap, is left to the
        /// caller.  <paramref name="from"/> and <paramref name="to"/> are compared with occurrences by their wall-clock fields, whatever their
        /// kind.
        /// </para>
        /// <para>
        /// The BYxxx rule parts are applied as the table in RFC 5545 §3.3.10 sets out, followed by BYSETPOS, WKST, COUNT and UNTIL.  Where no
        /// rule part gives the day or time, it is taken from <paramref name="start"/>.  An invalid date is skipped, not clamped, so
        /// <c>FREQ=MONTHLY;BYMONTHDAY=30</c> has no occurrence in February.  A second of 60 is never generated.
        /// </para>
        /// <para>
        /// Only the occurrences the rule generates are given.  RFC 5545 counts DTSTART as the first occurrence of a recurrence set, but if
        /// <paramref name="start"/> does not match the rule it is not given here, and adding it is left to the caller.  COUNT counts from
        /// <paramref name="start"/>, whatever <paramref name="from"/> is.
        /// </para>
        /// <para>
        /// UNTIL is compared by its wall-clock fields when it is floating, or when it and <paramref name="start"/> are both in UTC.  A DATE
        /// UNTIL includes the whole of that day.
        /// </para>
        /// <para>
        /// The occurrences end at 9999-12-31, the last date.  A rule that steps through more consecutive periods than
        /// <see cref="IcalOptions.MaxRecurrenceIterations"/> without an occurrence throws an <see cref="InvalidOperationException"/> while it
        /// is enumerated, so one that can never match does not run for a very long time.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="toUtc"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The rule breaks a constraint listed under <see cref="IsValid"/>.  Also thrown while
        /// enumerating, when more consecutive periods than <see cref="IcalOptions.MaxRecurrenceIterations"/> give no occurrence.</exception>
        /// <exception cref="ArgumentException">The rule has neither COUNT nor UNTIL and <paramref name="to"/> is <c>null</c>; or
        /// <see cref="IcalOptions.MaxRecurrenceIterations"/> is less than 1.</exception>
        public IEnumerable<IcalDateTime> GetOccurrences(IcalDateTime start, IcalDateTime? from, IcalDateTime? to,
            Func<IcalDateTime, IcalDateTime> toUtc, IcalOptions options)
        {
            if (toUtc is null)
                throw new ArgumentNullException(nameof(toUtc));

            return GetDateTimeOccurrences(start, from, to, toUtc, options);
        }

        /// <summary>
        /// Gets the occurrences of the rule from a start date, for an all-day DTSTART.
        /// </summary>
        /// <param name="start">The start (DTSTART).</param>
        /// <param name="from">The earliest occurrence to give, inclusive; or <c>null</c> to start at <paramref name="start"/>.</param>
        /// <param name="to">The end of the occurrences to give, exclusive; or <c>null</c> for none, which needs a rule with COUNT or UNTIL.</param>
        /// <returns>The occurrences, in increasing order, generated as they are enumerated.</returns>
        /// <remarks>
        /// See <see cref="GetOccurrences(IcalDate, IcalDate?, IcalDate?, IcalOptions)"/> for how the rule is applied.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The rule breaks a constraint listed under <see cref="IsValid"/>.  Also thrown while
        /// enumerating, when more consecutive periods than <see cref="IcalOptions.MaxRecurrenceIterations"/> give no occurrence.</exception>
        /// <exception cref="ArgumentException">The rule has neither COUNT nor UNTIL and <paramref name="to"/> is <c>null</c>; or it has a
        /// frequency below DAILY, or BYHOUR, BYMINUTE or BYSECOND, none of which a date can have.</exception>
        public IEnumerable<IcalDate> GetOccurrences(IcalDate start, IcalDate? from, IcalDate? to)
        {
            return GetOccurrences(start, from, to, DefaultOptions);
        }

        /// <summary>
        /// Gets the occurrences of the rule from a start date, for an all-day DTSTART.
        /// </summary>
        /// <param name="start">The start (DTSTART).</param>
        /// <param name="from">The earliest occurrence to give, inclusive; or <c>null</c> to start at <paramref name="start"/>.</param>
        /// <param name="to">The end of the occurrences to give, exclusive; or <c>null</c> for none, which needs a rule with COUNT or UNTIL.</param>
        /// <param name="options">The options, of which <see cref="IcalOptions.MaxRecurrenceIterations"/> applies.</param>
        /// <returns>The occurrences, in increasing order, generated as they are enumerated.</returns>
        /// <remarks>
        /// <para>
        /// The rule is applied as for a date-time start; see
        /// <see cref="GetOccurrences(IcalDateTime, IcalDateTime?, IcalDateTime?, Func{IcalDateTime, IcalDateTime}, IcalOptions)"/>.
        /// </para>
        /// <para>
        /// RFC 5545 requires UNTIL to be a DATE when DTSTART is.  A DATE-TIME UNTIL is compared by its date.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The rule breaks a constraint listed under <see cref="IsValid"/>.  Also thrown while
        /// enumerating, when more consecutive periods than <see cref="IcalOptions.MaxRecurrenceIterations"/> give no occurrence.</exception>
        /// <exception cref="ArgumentException">The rule has neither COUNT nor UNTIL and <paramref name="to"/> is <c>null</c>; it has a
        /// frequency below DAILY, or BYHOUR, BYMINUTE or BYSECOND, none of which a date can have; or
        /// <see cref="IcalOptions.MaxRecurrenceIterations"/> is less than 1.</exception>
        public IEnumerable<IcalDate> GetOccurrences(IcalDate start, IcalDate? from, IcalDate? to, IcalOptions options)
        {
            CheckOccurrenceArguments(options, to.HasValue);

            // ref. RFC 5545§3.3.10: BYSECOND, BYMINUTE and BYHOUR MUST NOT be given when DTSTART is a DATE.
            if (_parts.Frequency < IcalRecurrenceFrequency.Daily || _parts.BySecond != 0 || _parts.ByMinute != 0 || _parts.ByHour != 0)
                throw new ArgumentException("A rule with a frequency below DAILY, or with BYHOUR, BYMINUTE or BYSECOND, cannot start on a DATE.",
                    nameof(start));

            var untilStamp = long.MaxValue;
            if (_parts.UntilKind != UntilKindNone)
            {
                untilStamp = GetEndOfDayStamp(_parts.Until.Date);
            }

            var expander = new IcalRecurrenceExpander(in _parts, GetBySetPos(), GetStamp(start), from.HasValue ? GetStamp(from.Value) : long.MinValue,
                to.HasValue ? GetStamp(to.Value) : long.MaxValue, untilStamp, null, options.MaxRecurrenceIterations);
            return ToDates(expander.Expand());
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IEnumerable<IcalDateTime> GetDateTimeOccurrences(IcalDateTime start, IcalDateTime? from, IcalDateTime? to,
            Func<IcalDateTime, IcalDateTime>? toUtc, IcalOptions options)
#else
        private IEnumerable<IcalDateTime> GetDateTimeOccurrences(IcalDateTime start, IcalDateTime? from, IcalDateTime? to,
            Func<IcalDateTime, IcalDateTime> toUtc, IcalOptions options)
#endif
        {
            CheckOccurrenceArguments(options, to.HasValue);

            var untilStamp = long.MaxValue;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Func<long, long>? stampToUtc = null;
#else
            Func<long, long> stampToUtc = null;
#endif
            if (_parts.UntilKind == UntilKindDate)
            {
                untilStamp = GetEndOfDayStamp(_parts.Until.Date);
            }
            else if (_parts.UntilKind == UntilKindDateTime)
            {
                untilStamp = GetStamp(_parts.Until);
                if (_parts.Until.Kind == IcalDateTimeKind.Utc && start.Kind != IcalDateTimeKind.Utc)
                {
                    if (toUtc is null)
                        throw new ArgumentException(
                            "The rule's UNTIL is in UTC and the start is not, so the overload that converts occurrences to UTC must be used.",
                            nameof(start));

                    stampToUtc = stamp => GetStamp(toUtc(ToDateTime(stamp, start)));
                }
            }

            var expander = new IcalRecurrenceExpander(in _parts, GetBySetPos(), GetStamp(start), from.HasValue ? GetStamp(from.Value) : long.MinValue,
                to.HasValue ? GetStamp(to.Value) : long.MaxValue, untilStamp, stampToUtc, options.MaxRecurrenceIterations);
            return ToDateTimes(expander.Expand(), start);
        }

        private void CheckOccurrenceArguments(IcalOptions options, bool hasTo)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.MaxRecurrenceIterations < 1)
                throw new ArgumentException("MaxRecurrenceIterations must be at least 1.", nameof(options));

            string error;
            if (!Validate(in _parts, out error))
                throw new InvalidOperationException(error);
            if (!hasTo && _parts.Count == 0 && _parts.UntilKind == UntilKindNone)
                throw new ArgumentException("A rule with neither COUNT nor UNTIL repeats forever, so the end of a window must be given.", "to");
        }

        private static IEnumerable<IcalDateTime> ToDateTimes(IEnumerable<long> stamps, IcalDateTime start)
        {
            foreach (var stamp in stamps)
            {
                yield return ToDateTime(stamp, start);
            }
        }

        private static IEnumerable<IcalDate> ToDates(IEnumerable<long> stamps)
        {
            foreach (var stamp in stamps)
            {
                yield return IcalDate.FromDayNumber((int)(stamp / IcalRecurrenceExpander.SecondsPerDay));
            }
        }

        /// <summary>
        /// Gets a date-time from a stamp, with the kind and time zone of the start.
        /// </summary>
        private static IcalDateTime ToDateTime(long stamp, IcalDateTime start)
        {
            var date = IcalDate.FromDayNumber((int)(stamp / IcalRecurrenceExpander.SecondsPerDay));
            var secondOfDay = (int)(stamp % IcalRecurrenceExpander.SecondsPerDay);
            var time = new IcalTime(secondOfDay / 3600, secondOfDay / 60 % 60, secondOfDay % 60, start.Kind == IcalDateTimeKind.Utc);
            return start.TimeZoneId is null ? new IcalDateTime(date, time) : new IcalDateTime(date, time, start.TimeZoneId);
        }

        private static long GetStamp(IcalDate date)
        {
            return (long)date.DayNumber * IcalRecurrenceExpander.SecondsPerDay;
        }

        private static long GetEndOfDayStamp(IcalDate date)
        {
            return GetStamp(date) + IcalRecurrenceExpander.SecondsPerDay - 1;
        }

        /// <summary>
        /// Gets the stamp of the wall-clock fields of a date-time, whatever its kind.
        /// </summary>
        private static long GetStamp(IcalDateTime dateTime)
        {
            var time = dateTime.Time;
            return GetStamp(dateTime.Date) + time.Hour * 3600 + time.Minute * 60 + time.Second;
        }
    }
}
