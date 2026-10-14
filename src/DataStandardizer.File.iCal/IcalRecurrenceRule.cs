using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A RECUR value (RFC 5545 §3.3.10): a recurrence rule, such as <c>FREQ=MONTHLY;BYDAY=-1FR;COUNT=6</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type parses, checks and formats a rule, and <see cref="GetOccurrences(IcalDateTime, IcalDateTime?, IcalDateTime?)"/> expands it
    /// into occurrences in local time.
    /// </para>
    /// <para>
    /// The values of each BYxxx rule part are held as a set, so their order and any repeats are not kept.  They are written in a fixed order:
    /// positive values in increasing order, then negative ones in decreasing order (<c>1,2,-1,-2</c>), and BYDAY values grouped by day from
    /// Sunday.  The rule parts are written in a fixed order too, starting with FREQ as RFC 5545 requires.
    /// </para>
    /// <para>
    /// A rule is built from a frequency with the <c>With</c> methods, each of which returns a new rule.  Those methods check each value, but
    /// not the constraints between rule parts, which a rule under construction may break for a while.  <see cref="IsValid"/> checks those,
    /// and <see cref="TryFormat"/> refuses to write a rule that breaks them.
    /// </para>
    /// <para>
    /// The default value has no frequency, so it is not a valid rule.
    /// </para>
    /// </remarks>
    public readonly partial struct IcalRecurrenceRule : IEquatable<IcalRecurrenceRule>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private enum ListPart
        {
            BySecond = 0,
            ByMinute = 1,
            ByHour = 2,
            ByMonthDay = 3,
            ByYearDay = 4,
            ByWeekNo = 5,
            ByMonth = 6,
            BySetPos = 7
        }

        private const int ListPartCount = 8;
        private const int PartFrequency = 0;
        private const int PartUntil = 1;
        private const int PartCount = 2;
        private const int PartInterval = 3;
        private const int PartByDay = 4;
        private const int PartWeekStart = 5;
        private const int FirstListPart = 6;

        private const byte UntilKindNone = 0;
        private const byte UntilKindDate = 1;
        private const byte UntilKindDateTime = 2;

        // In a set of signed values, -n is held at NegativeOffset + n.
        private const int NegativeOffset = 384;

        // BYDAY values for a weekday are held at weekday * ByDayStride + ordinal, with -n at weekday * ByDayStride + ByDayNegativeOffset + n.
        private const int ByDayStride = 128;
        private const int ByDayNegativeOffset = 64;

        private const int InitialTextLength = 128;

        // ref. RFC 5545§3.3.10: the order of the rule part names, other than the list parts, matches the Part constants.
        private static readonly string[] PartNames =
        {
            "FREQ", "UNTIL", "COUNT", "INTERVAL", "BYDAY", "WKST",
            "BYSECOND", "BYMINUTE", "BYHOUR", "BYMONTHDAY", "BYYEARDAY", "BYWEEKNO", "BYMONTH", "BYSETPOS"
        };

        private static readonly string[] FrequencyNames = { "SECONDLY", "MINUTELY", "HOURLY", "DAILY", "WEEKLY", "MONTHLY", "YEARLY" };

        // Indexed by ListPart.  Signed parts allow -max to -1 and 1 to max; the others min to max.
        private static readonly bool[] ListPartIsSigned = { false, false, false, true, true, true, false, true };
        private static readonly int[] ListPartMinimums = { 0, 0, 0, 1, 1, 1, 1, 1 };
        private static readonly int[] ListPartMaximums = { 60, 59, 23, 31, 366, 53, 12, 366 };
        private static readonly int[] ListPartMaxDigits = { 2, 2, 2, 2, 3, 2, 2, 3 };

        /// <summary>
        /// The rule parts, held inline so that parsing does not allocate.
        /// </summary>
        internal struct Parts
        {
            internal IcalRecurrenceFrequency Frequency;
            internal int Count;
            internal int Interval;
            internal byte UntilKind;
            internal IcalDateTime Until;
            internal int WeekStart;
            internal ulong BySecond;
            internal ulong ByMinute;
            internal ulong ByHour;
            internal ulong ByMonth;
            internal ulong ByMonthDay;
            internal ulong ByWeekNoPositive;
            internal ulong ByWeekNoNegative;
            internal IcalBitSet ByYearDay;
            internal IcalBitSet BySetPos;
            internal IcalBitSet ByDay;
        }

        private readonly Parts _parts;

        /// <summary>
        /// Creates a rule with a frequency and no other rule parts.
        /// </summary>
        /// <param name="frequency">The frequency.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="frequency"/> is not an <see cref="IcalRecurrenceFrequency"/>
        /// member.</exception>
        public IcalRecurrenceRule(IcalRecurrenceFrequency frequency)
        {
            CheckFrequency(frequency);

            _parts = new Parts { Frequency = frequency };
        }

        private IcalRecurrenceRule(in Parts parts)
        {
            _parts = parts;
        }

        /// <summary>
        /// Gets the frequency (FREQ); or 0 for the default value, which has none.
        /// </summary>
        public IcalRecurrenceFrequency Frequency => _parts.Frequency;

        /// <summary>
        /// Gets the interval (INTERVAL) at which the rule repeats, in units of <see cref="Frequency"/>.  It is 1 if the rule does not give one.
        /// </summary>
        public int Interval => _parts.Interval == 0 ? 1 : _parts.Interval;

        /// <summary>
        /// Gets the number of occurrences (COUNT); or <c>null</c> if the rule does not give one.
        /// </summary>
        public int? Count => _parts.Count == 0 ? (int?)null : _parts.Count;

        /// <summary>
        /// Gets the last date (UNTIL) when it is a date; or <c>null</c> if the rule has no UNTIL or it is a date-time.
        /// </summary>
        public IcalDate? UntilDate => _parts.UntilKind == UntilKindDate ? _parts.Until.Date : (IcalDate?)null;

        /// <summary>
        /// Gets the last date-time (UNTIL) when it is a date-time, which is floating or in UTC; or <c>null</c> if the rule has no UNTIL or it is
        /// a date.
        /// </summary>
        public IcalDateTime? UntilDateTime => _parts.UntilKind == UntilKindDateTime ? _parts.Until : (IcalDateTime?)null;

        /// <summary>
        /// Gets the day on which the week starts (WKST); or <c>null</c> if the rule does not give one, in which case it is Monday.
        /// </summary>
        public DayOfWeek? WeekStart => _parts.WeekStart == 0 ? (DayOfWeek?)null : (DayOfWeek)(_parts.WeekStart - 1);

        /// <summary>
        /// Gets a flag indicating if the rule meets the constraints of RFC 5545 §3.3.10 that apply between its rule parts.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>FREQ is given.</description></item>
        /// <item><description>UNTIL and COUNT are not both given.</description></item>
        /// <item><description>A BYDAY value with an ordinal is given only when FREQ is MONTHLY or YEARLY, and not with BYWEEKNO when FREQ is
        /// YEARLY.</description></item>
        /// <item><description>BYMONTHDAY is not given when FREQ is WEEKLY.</description></item>
        /// <item><description>BYYEARDAY is not given when FREQ is DAILY, WEEKLY or MONTHLY.</description></item>
        /// <item><description>BYWEEKNO is given only when FREQ is YEARLY.</description></item>
        /// <item><description>BYSETPOS is given only with another BYxxx rule part.</description></item>
        /// </list>
        /// Constraints that depend on the DTSTART property, such as the value type of UNTIL, are not checked here.
        /// </remarks>
        public bool IsValid
        {
            get
            {
                string error;
                return Validate(in _parts, out error);
            }
        }

        /// <summary>
        /// Gets the seconds (BYSECOND), from 0 to 60; or an empty array if the rule does not give any.
        /// </summary>
        public int[] GetBySecond()
        {
            return GetListValues(in _parts, ListPart.BySecond);
        }

        /// <summary>
        /// Gets the minutes (BYMINUTE), from 0 to 59; or an empty array if the rule does not give any.
        /// </summary>
        public int[] GetByMinute()
        {
            return GetListValues(in _parts, ListPart.ByMinute);
        }

        /// <summary>
        /// Gets the hours (BYHOUR), from 0 to 23; or an empty array if the rule does not give any.
        /// </summary>
        public int[] GetByHour()
        {
            return GetListValues(in _parts, ListPart.ByHour);
        }

        /// <summary>
        /// Gets the days of the week (BYDAY); or an empty array if the rule does not give any.
        /// </summary>
        public IcalRecurrenceDay[] GetByDay()
        {
            var values = new IcalRecurrenceDay[IcalBitSet.Count(in _parts.ByDay, 0, IcalBitSet.Capacity)];
            var valueIndex = 0;
            for (var weekday = DayOfWeek.Sunday; weekday <= DayOfWeek.Saturday; weekday++)
            {
                for (var candidate = 0; candidate < 2 * IcalRecurrenceDay.MaxOrdinal + 1; candidate++)
                {
                    var ordinal = GetByDayCandidate(candidate);
                    if (IcalBitSet.Get(in _parts.ByDay, GetByDayIndex(weekday, ordinal)))
                    {
                        values[valueIndex++] = new IcalRecurrenceDay(weekday, ordinal);
                    }
                }
            }

            return values;
        }

        /// <summary>
        /// Gets the days of the month (BYMONTHDAY), from 1 to 31 or -31 to -1; or an empty array if the rule does not give any.
        /// </summary>
        public int[] GetByMonthDay()
        {
            return GetListValues(in _parts, ListPart.ByMonthDay);
        }

        /// <summary>
        /// Gets the days of the year (BYYEARDAY), from 1 to 366 or -366 to -1; or an empty array if the rule does not give any.
        /// </summary>
        public int[] GetByYearDay()
        {
            return GetListValues(in _parts, ListPart.ByYearDay);
        }

        /// <summary>
        /// Gets the ISO 8601 week numbers (BYWEEKNO), from 1 to 53 or -53 to -1; or an empty array if the rule does not give any.
        /// </summary>
        public int[] GetByWeekNo()
        {
            return GetListValues(in _parts, ListPart.ByWeekNo);
        }

        /// <summary>
        /// Gets the months (BYMONTH), from 1 to 12; or an empty array if the rule does not give any.
        /// </summary>
        public int[] GetByMonth()
        {
            return GetListValues(in _parts, ListPart.ByMonth);
        }

        /// <summary>
        /// Gets the positions within each set of occurrences (BYSETPOS), from 1 to 366 or -366 to -1; or an empty array if the rule does not
        /// give any.
        /// </summary>
        public int[] GetBySetPos()
        {
            return GetListValues(in _parts, ListPart.BySetPos);
        }

        /// <summary>
        /// Gets a copy of the rule with another frequency (FREQ).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="frequency"/> is not an <see cref="IcalRecurrenceFrequency"/>
        /// member.</exception>
        public IcalRecurrenceRule WithFrequency(IcalRecurrenceFrequency frequency)
        {
            CheckFrequency(frequency);

            var parts = _parts;
            parts.Frequency = frequency;
            return new IcalRecurrenceRule(in parts);
        }

        /// <summary>
        /// Gets a copy of the rule that ends on a date (UNTIL), and so has no COUNT.
        /// </summary>
        /// <param name="until">The last date, which must match the value type of the DTSTART property.</param>
        public IcalRecurrenceRule WithUntil(IcalDate until)
        {
            var parts = _parts;
            parts.Count = 0;
            parts.UntilKind = UntilKindDate;
            parts.Until = new IcalDateTime(until, default(IcalTime));
            return new IcalRecurrenceRule(in parts);
        }

        /// <summary>
        /// Gets a copy of the rule that ends at a date-time (UNTIL), and so has no COUNT.
        /// </summary>
        /// <param name="until">The last date-time.  It is floating if DTSTART is floating, and otherwise in UTC.</param>
        /// <exception cref="ArgumentException"><paramref name="until"/> is in a named time zone.</exception>
        public IcalRecurrenceRule WithUntil(IcalDateTime until)
        {
            if (until.Kind == IcalDateTimeKind.Zoned)
                throw new ArgumentException("UNTIL must be floating or in UTC.", nameof(until));

            var parts = _parts;
            parts.Count = 0;
            parts.UntilKind = UntilKindDateTime;
            parts.Until = until;
            return new IcalRecurrenceRule(in parts);
        }

        /// <summary>
        /// Gets a copy of the rule that ends after a number of occurrences (COUNT), and so has no UNTIL.
        /// </summary>
        /// <param name="count">The number of occurrences, at least 1.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1.</exception>
        public IcalRecurrenceRule WithCount(int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), count, "The count must be at least 1.");

            var parts = _parts;
            parts.Count = count;
            parts.UntilKind = UntilKindNone;
            parts.Until = default(IcalDateTime);
            return new IcalRecurrenceRule(in parts);
        }

        /// <summary>
        /// Gets a copy of the rule with neither UNTIL nor COUNT, so that it repeats forever.
        /// </summary>
        public IcalRecurrenceRule WithoutEnd()
        {
            var parts = _parts;
            parts.Count = 0;
            parts.UntilKind = UntilKindNone;
            parts.Until = default(IcalDateTime);
            return new IcalRecurrenceRule(in parts);
        }

        /// <summary>
        /// Gets a copy of the rule with an interval (INTERVAL).  An interval of 1 is written, although it is the default.
        /// </summary>
        /// <param name="interval">The interval, at least 1.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> is less than 1.</exception>
        public IcalRecurrenceRule WithInterval(int interval)
        {
            if (interval < 1)
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "The interval must be at least 1.");

            var parts = _parts;
            parts.Interval = interval;
            return new IcalRecurrenceRule(in parts);
        }

        /// <summary>
        /// Gets a copy of the rule with a day on which the week starts (WKST).
        /// </summary>
        /// <param name="weekStart">The day; or <c>null</c> to remove WKST.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="weekStart"/> is not a <see cref="DayOfWeek"/> member.</exception>
        public IcalRecurrenceRule WithWeekStart(DayOfWeek? weekStart)
        {
            if (weekStart < DayOfWeek.Sunday || weekStart > DayOfWeek.Saturday)
                throw new ArgumentOutOfRangeException(nameof(weekStart), weekStart, "The day of the week is not recognised.");

            var parts = _parts;
            parts.WeekStart = weekStart.HasValue ? (int)weekStart.Value + 1 : 0;
            return new IcalRecurrenceRule(in parts);
        }

        /// <summary>
        /// Gets a copy of the rule with other seconds (BYSECOND).
        /// </summary>
        /// <param name="values">The seconds, from 0 to 60; or <c>null</c> or none to remove BYSECOND.</param>
        /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
        public IcalRecurrenceRule WithBySecond(params int[] values)
        {
            return WithListValues(ListPart.BySecond, values);
        }

        /// <summary>
        /// Gets a copy of the rule with other minutes (BYMINUTE).
        /// </summary>
        /// <param name="values">The minutes, from 0 to 59; or <c>null</c> or none to remove BYMINUTE.</param>
        /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
        public IcalRecurrenceRule WithByMinute(params int[] values)
        {
            return WithListValues(ListPart.ByMinute, values);
        }

        /// <summary>
        /// Gets a copy of the rule with other hours (BYHOUR).
        /// </summary>
        /// <param name="values">The hours, from 0 to 23; or <c>null</c> or none to remove BYHOUR.</param>
        /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
        public IcalRecurrenceRule WithByHour(params int[] values)
        {
            return WithListValues(ListPart.ByHour, values);
        }

        /// <summary>
        /// Gets a copy of the rule with other days of the week (BYDAY).
        /// </summary>
        /// <param name="values">The days; or <c>null</c> or none to remove BYDAY.</param>
        public IcalRecurrenceRule WithByDay(params IcalRecurrenceDay[] values)
        {
            var parts = _parts;
            parts.ByDay = default(IcalBitSet);
            if (values != null)
            {
                foreach (var value in values)
                {
                    parts.ByDay.Set(GetByDayIndex(value.Weekday, value.Ordinal));
                }
            }

            return new IcalRecurrenceRule(in parts);
        }

        /// <summary>
        /// Gets a copy of the rule with other days of the month (BYMONTHDAY).
        /// </summary>
        /// <param name="values">The days, from 1 to 31 or -31 to -1; or <c>null</c> or none to remove BYMONTHDAY.</param>
        /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
        public IcalRecurrenceRule WithByMonthDay(params int[] values)
        {
            return WithListValues(ListPart.ByMonthDay, values);
        }

        /// <summary>
        /// Gets a copy of the rule with other days of the year (BYYEARDAY).
        /// </summary>
        /// <param name="values">The days, from 1 to 366 or -366 to -1; or <c>null</c> or none to remove BYYEARDAY.</param>
        /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
        public IcalRecurrenceRule WithByYearDay(params int[] values)
        {
            return WithListValues(ListPart.ByYearDay, values);
        }

        /// <summary>
        /// Gets a copy of the rule with other ISO 8601 week numbers (BYWEEKNO).
        /// </summary>
        /// <param name="values">The weeks, from 1 to 53 or -53 to -1; or <c>null</c> or none to remove BYWEEKNO.</param>
        /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
        public IcalRecurrenceRule WithByWeekNo(params int[] values)
        {
            return WithListValues(ListPart.ByWeekNo, values);
        }

        /// <summary>
        /// Gets a copy of the rule with other months (BYMONTH).
        /// </summary>
        /// <param name="values">The months, from 1 to 12; or <c>null</c> or none to remove BYMONTH.</param>
        /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
        public IcalRecurrenceRule WithByMonth(params int[] values)
        {
            return WithListValues(ListPart.ByMonth, values);
        }

        /// <summary>
        /// Gets a copy of the rule with other positions within each set of occurrences (BYSETPOS).
        /// </summary>
        /// <param name="values">The positions, from 1 to 366 or -366 to -1; or <c>null</c> or none to remove BYSETPOS.</param>
        /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
        public IcalRecurrenceRule WithBySetPos(params int[] values)
        {
            return WithListValues(ListPart.BySetPos, values);
        }

        /// <summary>
        /// Parses a rule, and checks it against the constraints listed under <see cref="IsValid"/>.
        /// </summary>
        /// <param name="value">The value, such as <c>FREQ=YEARLY;BYDAY=1SU;BYMONTH=4</c> in <see cref="IcalValueFormat.Text"/> form.  Rule part
        /// names and values are not case-sensitive.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The rule, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid rule; otherwise <c>false</c>.</returns>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalRecurrenceRule result)
        {
            IcalValueFormatting.CheckFormat(format);

            result = default(IcalRecurrenceRule);
            var parts = new Parts();
            var seenParts = 0;
            var rest = value;
            while (true)
            {
                // ref. RFC 5545§3.3.10: recur = recur-rule-part *( ";" recur-rule-part )
                var separatorIndex = rest.IndexOf(';');
                var part = separatorIndex < 0 ? rest : rest.Slice(0, separatorIndex);
                if (!TryParsePart(part, ref parts, ref seenParts)) return false;
                if (separatorIndex < 0) break;

                rest = rest.Slice(separatorIndex + 1);
            }

            string error;
            if (!Validate(in parts, out error)) return false;

            result = new IcalRecurrenceRule(in parts);
            return true;
        }

        /// <summary>
        /// Parses a rule, and checks it against the constraints listed under <see cref="IsValid"/>.
        /// </summary>
        /// <param name="value">The value, such as <c>FREQ=YEARLY;BYDAY=1SU;BYMONTH=4</c> in <see cref="IcalValueFormat.Text"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The rule.</returns>
        /// <exception cref="FormatException">The value is not a valid rule.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static IcalRecurrenceRule Parse(ReadOnlySpan<char> value, IcalValueFormat format)
        {
            IcalRecurrenceRule result;
            if (!TryParse(value, format, out result))
                throw new FormatException("The value is not a valid RECUR.");

            return result;
        }

        /// <summary>
        /// Formats the rule into a span.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the rule was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="InvalidOperationException">The rule breaks a constraint listed under <see cref="IsValid"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            string error;
            if (!Validate(in _parts, out error))
                throw new InvalidOperationException(error);

            var writer = new IcalSpanWriter(destination);
            AppendText(ref writer);
            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the rule as a string.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="InvalidOperationException">The rule breaks a constraint listed under <see cref="IsValid"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public string ToString(IcalValueFormat format)
        {
            for (var length = InitialTextLength; ; length *= 2)
            {
                var buffer = new char[length];
                int charsWritten;
                if (TryFormat(buffer, out charsWritten, format)) return new string(buffer, 0, charsWritten);
            }
        }

        /// <summary>
        /// Formats the rule in <see cref="IcalValueFormat.Text"/> form.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="ToString(IcalValueFormat)"/>, this does not check the rule, so it also shows a rule that is not valid.
        /// </remarks>
        public override string ToString()
        {
            for (var length = InitialTextLength; ; length *= 2)
            {
                var buffer = new char[length];
                var writer = new IcalSpanWriter(buffer);
                AppendText(ref writer);
                int charsWritten;
                if (writer.TryComplete(out charsWritten)) return new string(buffer, 0, charsWritten);
            }
        }

#if NET6_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The format is empty or <c>T</c> for <see cref="IcalValueFormat.Text"/>, <c>J</c> for <see cref="IcalValueFormat.Json"/> or
        /// <c>X</c> for <see cref="IcalValueFormat.Xml"/>.  The provider is ignored.</remarks>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return TryFormat(destination, out charsWritten, IcalValueFormatting.ParseFormatString(format));
        }

        /// <inheritdoc />
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString(IcalValueFormatting.ParseFormatString(format));
        }
#endif

        /// <summary>
        /// Indicates whether this rule equals another: the same rule parts, with the same sets of values.
        /// </summary>
        public bool Equals(IcalRecurrenceRule other)
        {
            return _parts.Frequency == other._parts.Frequency && _parts.Count == other._parts.Count && _parts.Interval == other._parts.Interval &&
                   _parts.UntilKind == other._parts.UntilKind && _parts.Until == other._parts.Until &&
                   _parts.WeekStart == other._parts.WeekStart && _parts.BySecond == other._parts.BySecond &&
                   _parts.ByMinute == other._parts.ByMinute && _parts.ByHour == other._parts.ByHour && _parts.ByMonth == other._parts.ByMonth &&
                   _parts.ByMonthDay == other._parts.ByMonthDay && _parts.ByWeekNoPositive == other._parts.ByWeekNoPositive &&
                   _parts.ByWeekNoNegative == other._parts.ByWeekNoNegative && _parts.ByYearDay.Equals(other._parts.ByYearDay) &&
                   _parts.BySetPos.Equals(other._parts.BySetPos) && _parts.ByDay.Equals(other._parts.ByDay);
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalRecurrenceRule other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = (int)_parts.Frequency;
            hashCode = hashCode * 31 + _parts.Count;
            hashCode = hashCode * 31 + _parts.Interval;
            hashCode = hashCode * 31 + _parts.Until.GetHashCode();
            hashCode = hashCode * 31 + _parts.WeekStart;
            hashCode = hashCode * 31 + (_parts.BySecond ^ _parts.ByMinute ^ _parts.ByHour ^ _parts.ByMonth ^ _parts.ByMonthDay ^
                                        _parts.ByWeekNoPositive ^ _parts.ByWeekNoNegative).GetHashCode();
            hashCode = hashCode * 31 + _parts.ByYearDay.GetHashCode();
            hashCode = hashCode * 31 + _parts.BySetPos.GetHashCode();
            return hashCode * 31 + _parts.ByDay.GetHashCode();
        }

        /// <summary>
        /// Indicates whether two rules are equal.
        /// </summary>
        public static bool operator ==(IcalRecurrenceRule left, IcalRecurrenceRule right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two rules differ.
        /// </summary>
        public static bool operator !=(IcalRecurrenceRule left, IcalRecurrenceRule right)
        {
            return !left.Equals(right);
        }

        private void AppendText(ref IcalSpanWriter writer)
        {
            // ref. RFC 5545§3.3.10: FREQ MUST be the first rule part, for applications that pre-date RFC 5545.
            var hasFrequency = _parts.Frequency >= IcalRecurrenceFrequency.Secondly && _parts.Frequency <= IcalRecurrenceFrequency.Yearly;
            var separator = false;
            if (hasFrequency)
            {
                AppendPartName(ref writer, PartFrequency, ref separator);
                writer.Append(FrequencyNames[(int)_parts.Frequency - 1]);
            }

            if (_parts.UntilKind != UntilKindNone)
            {
                AppendPartName(ref writer, PartUntil, ref separator);
                if (_parts.UntilKind == UntilKindDate)
                {
                    _parts.Until.Date.AppendText(ref writer);
                }
                else
                {
                    _parts.Until.AppendText(ref writer);
                }
            }

            if (_parts.Count != 0)
            {
                AppendPartName(ref writer, PartCount, ref separator);
                writer.AppendNumber(_parts.Count);
            }

            if (_parts.Interval != 0)
            {
                AppendPartName(ref writer, PartInterval, ref separator);
                writer.AppendNumber(_parts.Interval);
            }

            AppendListPart(ref writer, ListPart.BySecond, ref separator);
            AppendListPart(ref writer, ListPart.ByMinute, ref separator);
            AppendListPart(ref writer, ListPart.ByHour, ref separator);
            AppendByDay(ref writer, ref separator);
            AppendListPart(ref writer, ListPart.ByMonthDay, ref separator);
            AppendListPart(ref writer, ListPart.ByYearDay, ref separator);
            AppendListPart(ref writer, ListPart.ByWeekNo, ref separator);
            AppendListPart(ref writer, ListPart.ByMonth, ref separator);
            AppendListPart(ref writer, ListPart.BySetPos, ref separator);

            if (_parts.WeekStart != 0)
            {
                AppendPartName(ref writer, PartWeekStart, ref separator);
                writer.Append(IcalRecurrenceDay.GetWeekdayName((DayOfWeek)(_parts.WeekStart - 1)));
            }
        }

        private void AppendListPart(ref IcalSpanWriter writer, ListPart part, ref bool separator)
        {
            if (!HasListPart(in _parts, part)) return;

            AppendPartName(ref writer, FirstListPart + (int)part, ref separator);
            var first = true;
            var candidateCount = GetCandidateCount(part);
            for (var candidate = 0; candidate < candidateCount; candidate++)
            {
                var value = GetCandidate(part, candidate);
                if (!ContainsListValue(in _parts, part, value)) continue;

                if (!first)
                {
                    writer.Append(',');
                }

                AppendSigned(ref writer, value);
                first = false;
            }
        }

        private void AppendByDay(ref IcalSpanWriter writer, ref bool separator)
        {
            if (IcalBitSet.IsEmpty(in _parts.ByDay)) return;

            AppendPartName(ref writer, PartByDay, ref separator);
            var first = true;
            for (var weekday = DayOfWeek.Sunday; weekday <= DayOfWeek.Saturday; weekday++)
            {
                for (var candidate = 0; candidate < 2 * IcalRecurrenceDay.MaxOrdinal + 1; candidate++)
                {
                    var ordinal = GetByDayCandidate(candidate);
                    if (!IcalBitSet.Get(in _parts.ByDay, GetByDayIndex(weekday, ordinal))) continue;

                    if (!first)
                    {
                        writer.Append(',');
                    }

                    if (ordinal != 0)
                    {
                        AppendSigned(ref writer, ordinal);
                    }

                    writer.Append(IcalRecurrenceDay.GetWeekdayName(weekday));
                    first = false;
                }
            }
        }

        private static void AppendPartName(ref IcalSpanWriter writer, int partIndex, ref bool separator)
        {
            if (separator)
            {
                writer.Append(';');
            }

            writer.Append(PartNames[partIndex]);
            writer.Append('=');
            separator = true;
        }

        private static void AppendSigned(ref IcalSpanWriter writer, int value)
        {
            if (value < 0)
            {
                writer.Append('-');
            }

            writer.AppendNumber(Math.Abs(value));
        }

        private IcalRecurrenceRule WithListValues(ListPart part, int[] values)
        {
            var parts = _parts;
            ClearListPart(ref parts, part);
            if (values != null)
            {
                foreach (var value in values)
                {
                    if (!IsInRange(part, value))
                    {
                        var message = ListPartIsSigned[(int)part]
                            ? string.Format("{0} values must be in the range 1 - {1} or -{1} - -1.", PartNames[FirstListPart + (int)part],
                                ListPartMaximums[(int)part])
                            : string.Format("{0} values must be in the range {1} - {2}.", PartNames[FirstListPart + (int)part],
                                ListPartMinimums[(int)part], ListPartMaximums[(int)part]);
                        throw new ArgumentOutOfRangeException(nameof(values), value, message);
                    }

                    AddListValue(ref parts, part, value);
                }
            }

            return new IcalRecurrenceRule(in parts);
        }

        private static void CheckFrequency(IcalRecurrenceFrequency frequency)
        {
            if (frequency < IcalRecurrenceFrequency.Secondly || frequency > IcalRecurrenceFrequency.Yearly)
                throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "The frequency is not recognised.");
        }

        private static bool Validate(in Parts parts, out string error)
        {
            // ref. RFC 5545§3.3.10, which this follows in order.
            var frequency = parts.Frequency;
            error = string.Empty;
            if (frequency < IcalRecurrenceFrequency.Secondly || frequency > IcalRecurrenceFrequency.Yearly)
            {
                error = "The rule has no frequency (FREQ).";
            }
            else if (parts.UntilKind != UntilKindNone && parts.Count != 0)
            {
                error = "UNTIL and COUNT cannot both be given.";
            }
            else if (HasByDayOrdinal(in parts) && frequency != IcalRecurrenceFrequency.Monthly && frequency != IcalRecurrenceFrequency.Yearly)
            {
                error = "A BYDAY value with an ordinal is allowed only when FREQ is MONTHLY or YEARLY.";
            }
            else if (HasByDayOrdinal(in parts) && frequency == IcalRecurrenceFrequency.Yearly && HasListPart(in parts, ListPart.ByWeekNo))
            {
                error = "A BYDAY value with an ordinal is not allowed with BYWEEKNO when FREQ is YEARLY.";
            }
            else if (HasListPart(in parts, ListPart.ByMonthDay) && frequency == IcalRecurrenceFrequency.Weekly)
            {
                error = "BYMONTHDAY is not allowed when FREQ is WEEKLY.";
            }
            else if (HasListPart(in parts, ListPart.ByYearDay) &&
                     (frequency == IcalRecurrenceFrequency.Daily || frequency == IcalRecurrenceFrequency.Weekly ||
                      frequency == IcalRecurrenceFrequency.Monthly))
            {
                error = "BYYEARDAY is not allowed when FREQ is DAILY, WEEKLY or MONTHLY.";
            }
            else if (HasListPart(in parts, ListPart.ByWeekNo) && frequency != IcalRecurrenceFrequency.Yearly)
            {
                error = "BYWEEKNO is allowed only when FREQ is YEARLY.";
            }
            else if (HasListPart(in parts, ListPart.BySetPos) && !HasOtherByPart(in parts))
            {
                error = "BYSETPOS must be used with another BYxxx rule part.";
            }

            return error.Length == 0;
        }

        private static bool HasByDayOrdinal(in Parts parts)
        {
            for (var weekday = 0; weekday < 7; weekday++)
            {
                if (IcalBitSet.Any(in parts.ByDay, weekday * ByDayStride + 1, (weekday + 1) * ByDayStride)) return true;
            }

            return false;
        }

        private static bool HasOtherByPart(in Parts parts)
        {
            if (!IcalBitSet.IsEmpty(in parts.ByDay)) return true;

            for (var part = 0; part < ListPartCount; part++)
            {
                if ((ListPart)part != ListPart.BySetPos && HasListPart(in parts, (ListPart)part)) return true;
            }

            return false;
        }

        private static bool TryParsePart(ReadOnlySpan<char> part, ref Parts parts, ref int seenParts)
        {
            // ref. RFC 5545§3.3.10: each rule part MUST NOT occur more than once.
            var equalsIndex = part.IndexOf('=');
            if (equalsIndex <= 0) return false;

            var partIndex = GetPartIndex(part.Slice(0, equalsIndex));
            if (partIndex < 0 || (seenParts & (1 << partIndex)) != 0) return false;

            seenParts |= 1 << partIndex;
            var value = part.Slice(equalsIndex + 1);
            if (value.Length == 0) return false;

            int number;
            switch (partIndex)
            {
                case PartFrequency:
                    for (var index = 0; index < FrequencyNames.Length; index++)
                    {
                        if (IcalValueFormatting.EqualsKeyword(value, FrequencyNames[index]))
                        {
                            parts.Frequency = (IcalRecurrenceFrequency)(index + 1);
                            return true;
                        }
                    }

                    return false;
                case PartUntil:
                    // ref. RFC 5545§3.3.10: enddate = date / date-time.  UNTIL has no TZID, so a date-time is floating or in UTC.
                    if (value.Length == IcalDate.TextLength)
                    {
                        IcalDate date;
                        if (!IcalDate.TryParseText(value, out date)) return false;

                        parts.UntilKind = UntilKindDate;
                        parts.Until = new IcalDateTime(date, default(IcalTime));
                        return true;
                    }

                    parts.UntilKind = UntilKindDateTime;
                    return IcalDateTime.TryParseText(value, null, out parts.Until);
                case PartCount:
                    if (!TryParsePositiveInteger(value, out number)) return false;

                    parts.Count = number;
                    return true;
                case PartInterval:
                    if (!TryParsePositiveInteger(value, out number)) return false;

                    parts.Interval = number;
                    return true;
                case PartByDay:
                    return TryParseByDay(value, ref parts);
                case PartWeekStart:
                    DayOfWeek weekStart;
                    if (!IcalRecurrenceDay.TryParseWeekday(value, out weekStart)) return false;

                    parts.WeekStart = (int)weekStart + 1;
                    return true;
                default:
                    return TryParseListPart(value, (ListPart)(partIndex - FirstListPart), ref parts);
            }
        }

        private static int GetPartIndex(ReadOnlySpan<char> name)
        {
            for (var index = 0; index < PartNames.Length; index++)
            {
                if (IcalValueFormatting.EqualsKeyword(name, PartNames[index])) return index;
            }

            return -1;
        }

        private static bool TryParsePositiveInteger(ReadOnlySpan<char> value, out int result)
        {
            // ref. RFC 5545§3.3.10: COUNT and INTERVAL are 1*DIGIT; INTERVAL is a positive integer, and a COUNT of 0 would leave no occurrence.
            result = 0;
            var index = 0;
            long number;
            if (!IcalValueFormatting.TryParseNumber(value, ref index, int.MaxValue, out number) || index != value.Length || number == 0) return false;

            result = (int)number;
            return true;
        }

        private static bool TryParseListPart(ReadOnlySpan<char> value, ListPart part, ref Parts parts)
        {
            var rest = value;
            while (true)
            {
                var commaIndex = rest.IndexOf(',');
                var item = commaIndex < 0 ? rest : rest.Slice(0, commaIndex);
                int number;
                if (!TryParseListItem(item, ListPartIsSigned[(int)part], ListPartMaxDigits[(int)part], out number) || !IsInRange(part, number))
                {
                    return false;
                }

                AddListValue(ref parts, part, number);
                if (commaIndex < 0) return true;

                rest = rest.Slice(commaIndex + 1);
            }
        }

        private static bool TryParseByDay(ReadOnlySpan<char> value, ref Parts parts)
        {
            // ref. RFC 5545§3.3.10: weekdaynum = [[plus / minus] ordwk] weekday; ordwk = 1*2DIGIT ;1 to 53
            var rest = value;
            while (true)
            {
                var commaIndex = rest.IndexOf(',');
                var item = commaIndex < 0 ? rest : rest.Slice(0, commaIndex);
                if (item.Length < 2) return false;

                DayOfWeek weekday;
                if (!IcalRecurrenceDay.TryParseWeekday(item.Slice(item.Length - 2), out weekday)) return false;

                var ordinal = 0;
                if (item.Length > 2 &&
                    (!TryParseListItem(item.Slice(0, item.Length - 2), true, 2, out ordinal) || ordinal == 0 ||
                     Math.Abs(ordinal) > IcalRecurrenceDay.MaxOrdinal))
                {
                    return false;
                }

                parts.ByDay.Set(GetByDayIndex(weekday, ordinal));
                if (commaIndex < 0) return true;

                rest = rest.Slice(commaIndex + 1);
            }
        }

        /// <summary>
        /// Parses an optionally signed list value of one to <paramref name="maxDigits"/> digits.
        /// </summary>
        private static bool TryParseListItem(ReadOnlySpan<char> item, bool isSigned, int maxDigits, out int value)
        {
            value = 0;
            var index = 0;
            var isNegative = false;
            if (isSigned && item.Length > 0 && (item[0] == '+' || item[0] == '-'))
            {
                isNegative = item[0] == '-';
                index = 1;
            }

            var digits = item.Length - index;
            if (digits < 1 || digits > maxDigits || !IcalValueFormatting.TryParseDigits(item, index, digits, out value)) return false;

            if (isNegative)
            {
                value = -value;
            }

            return true;
        }

        private static bool IsInRange(ListPart part, int value)
        {
            var maximum = ListPartMaximums[(int)part];
            return ListPartIsSigned[(int)part]
                ? value != 0 && value >= -maximum && value <= maximum
                : value >= ListPartMinimums[(int)part] && value <= maximum;
        }

        /// <summary>
        /// Gets the number of values a list part can hold.
        /// </summary>
        private static int GetCandidateCount(ListPart part)
        {
            var maximum = ListPartMaximums[(int)part];
            return ListPartIsSigned[(int)part] ? 2 * maximum : maximum - ListPartMinimums[(int)part] + 1;
        }

        /// <summary>
        /// Gets a value a list part can hold, in the order they are written: positive values increasing, then negative values decreasing.
        /// </summary>
        private static int GetCandidate(ListPart part, int candidate)
        {
            var maximum = ListPartMaximums[(int)part];
            if (!ListPartIsSigned[(int)part]) return ListPartMinimums[(int)part] + candidate;

            return candidate < maximum ? candidate + 1 : maximum - candidate - 1;
        }

        /// <summary>
        /// Gets a BYDAY ordinal, in the order they are written: none, then positive increasing, then negative decreasing.
        /// </summary>
        private static int GetByDayCandidate(int candidate)
        {
            return candidate <= IcalRecurrenceDay.MaxOrdinal ? candidate : IcalRecurrenceDay.MaxOrdinal - candidate;
        }

        private static int GetByDayIndex(DayOfWeek weekday, int ordinal)
        {
            return (int)weekday * ByDayStride + (ordinal >= 0 ? ordinal : ByDayNegativeOffset - ordinal);
        }

        private static int[] GetListValues(in Parts parts, ListPart part)
        {
            var count = 0;
            var candidateCount = GetCandidateCount(part);
            for (var candidate = 0; candidate < candidateCount; candidate++)
            {
                if (ContainsListValue(in parts, part, GetCandidate(part, candidate)))
                {
                    count++;
                }
            }

            var values = new int[count];
            var valueIndex = 0;
            for (var candidate = 0; candidate < candidateCount && valueIndex < count; candidate++)
            {
                var value = GetCandidate(part, candidate);
                if (ContainsListValue(in parts, part, value))
                {
                    values[valueIndex++] = value;
                }
            }

            return values;
        }

        private static bool HasListPart(in Parts parts, ListPart part)
        {
            switch (part)
            {
                case ListPart.BySecond:
                    return parts.BySecond != 0;
                case ListPart.ByMinute:
                    return parts.ByMinute != 0;
                case ListPart.ByHour:
                    return parts.ByHour != 0;
                case ListPart.ByMonthDay:
                    return parts.ByMonthDay != 0;
                case ListPart.ByYearDay:
                    return !IcalBitSet.IsEmpty(in parts.ByYearDay);
                case ListPart.ByWeekNo:
                    return (parts.ByWeekNoPositive | parts.ByWeekNoNegative) != 0;
                case ListPart.ByMonth:
                    return parts.ByMonth != 0;
                default:
                    return !IcalBitSet.IsEmpty(in parts.BySetPos);
            }
        }

        private static bool ContainsListValue(in Parts parts, ListPart part, int value)
        {
            switch (part)
            {
                case ListPart.BySecond:
                    return (parts.BySecond & (1UL << value)) != 0;
                case ListPart.ByMinute:
                    return (parts.ByMinute & (1UL << value)) != 0;
                case ListPart.ByHour:
                    return (parts.ByHour & (1UL << value)) != 0;
                case ListPart.ByMonthDay:
                    return (parts.ByMonthDay & (1UL << GetMonthDayIndex(value))) != 0;
                case ListPart.ByYearDay:
                    return IcalBitSet.Get(in parts.ByYearDay, GetSignedIndex(value));
                case ListPart.ByWeekNo:
                    return ((value > 0 ? parts.ByWeekNoPositive : parts.ByWeekNoNegative) & (1UL << Math.Abs(value))) != 0;
                case ListPart.ByMonth:
                    return (parts.ByMonth & (1UL << value)) != 0;
                default:
                    return IcalBitSet.Get(in parts.BySetPos, GetSignedIndex(value));
            }
        }

        private static void AddListValue(ref Parts parts, ListPart part, int value)
        {
            switch (part)
            {
                case ListPart.BySecond:
                    parts.BySecond |= 1UL << value;
                    break;
                case ListPart.ByMinute:
                    parts.ByMinute |= 1UL << value;
                    break;
                case ListPart.ByHour:
                    parts.ByHour |= 1UL << value;
                    break;
                case ListPart.ByMonthDay:
                    parts.ByMonthDay |= 1UL << GetMonthDayIndex(value);
                    break;
                case ListPart.ByYearDay:
                    parts.ByYearDay.Set(GetSignedIndex(value));
                    break;
                case ListPart.ByWeekNo:
                    if (value > 0)
                    {
                        parts.ByWeekNoPositive |= 1UL << value;
                    }
                    else
                    {
                        parts.ByWeekNoNegative |= 1UL << -value;
                    }

                    break;
                case ListPart.ByMonth:
                    parts.ByMonth |= 1UL << value;
                    break;
                default:
                    parts.BySetPos.Set(GetSignedIndex(value));
                    break;
            }
        }

        private static void ClearListPart(ref Parts parts, ListPart part)
        {
            switch (part)
            {
                case ListPart.BySecond:
                    parts.BySecond = 0;
                    break;
                case ListPart.ByMinute:
                    parts.ByMinute = 0;
                    break;
                case ListPart.ByHour:
                    parts.ByHour = 0;
                    break;
                case ListPart.ByMonthDay:
                    parts.ByMonthDay = 0;
                    break;
                case ListPart.ByYearDay:
                    parts.ByYearDay = default(IcalBitSet);
                    break;
                case ListPart.ByWeekNo:
                    parts.ByWeekNoPositive = 0;
                    parts.ByWeekNoNegative = 0;
                    break;
                case ListPart.ByMonth:
                    parts.ByMonth = 0;
                    break;
                default:
                    parts.BySetPos = default(IcalBitSet);
                    break;
            }
        }

        private static int GetMonthDayIndex(int value)
        {
            return value > 0 ? value : 32 - value;
        }

        private static int GetSignedIndex(int value)
        {
            return value > 0 ? value : NegativeOffset - value;
        }
    }
}
