using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Expands a recurrence rule (RFC 5545 §3.3.10) into its occurrences, in local wall-clock time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Times are held as stamps: whole seconds since 0001-01-01T00:00:00, read as wall-clock fields with no time zone.  The rule is stepped
    /// through one period of its frequency at a time, every INTERVAL periods.  Each period gives a set of days, those of the period that pass
    /// the BYxxx date parts, and a set of times of day; their product, in order, is the period's set of occurrences, from which BYSETPOS picks.
    /// </para>
    /// <para>
    /// Filtering the days of a period is how the expand and limit actions of the table in RFC 5545 §3.3.10 come out the same: a part that
    /// expands a period keeps the days it names, and a part that limits one drops the others.  A value that names no day of the period, such
    /// as BYMONTHDAY=30 in February, names nothing, so invalid dates are skipped rather than clamped.
    /// </para>
    /// </remarks>
    internal sealed class IcalRecurrenceExpander
    {
        internal const int SecondsPerDay = 86400;

        private const int SecondsPerHour = 3600;
        private const int SecondsPerMinute = 60;
        private const int HoursPerDay = 24;
        private const int MinutesPerDay = 1440;
        private const int MaxDaysPerPeriod = 366;

        // The layout of the rule's BYxxx sets; see IcalRecurrenceRule.
        private const int MonthDayNegativeOffset = 32;
        private const int YearDayNegativeOffset = 384;
        private const int ByDayStride = 128;
        private const int ByDayNegativeOffset = 64;

        private readonly IcalRecurrenceFrequency _frequency;
        private readonly int _interval;
        private readonly int _count;
        private readonly long _startStamp;
        private readonly long _fromStamp;
        private readonly long _toStamp;
        private readonly long _untilStamp;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly Func<long, long>? _toUtc;
#else
        private readonly Func<long, long> _toUtc;
#endif
        private readonly int _maxIterations;

        // Weeks start on the day numbers that leave this remainder when divided by 7.
        private readonly int _weekStartRemainder;

        private readonly ulong _byMonth;
        private readonly ulong _byMonthDay;
        private readonly ulong _byWeekNoPositive;
        private readonly ulong _byWeekNoNegative;
        private readonly IcalBitSet _byYearDay;
        private readonly bool _hasByYearDay;
        private readonly IcalBitSet _byDay;
        private readonly bool _hasByDay;
        private readonly bool _isByDayOrdinalInMonth;

        // Limits on the hour, minute and second of a period, for a frequency at or below that unit; 0 for none.
        private readonly ulong _hourLimit;
        private readonly ulong _minuteLimit;
        private readonly ulong _secondLimit;

        // The times of day, in seconds and increasing, that each day of a period expands to; for a frequency below DAILY they are added to
        // the start of the period.
        private readonly int[] _timeOffsets;
        private readonly int[] _bySetPos;

        /// <summary>
        /// Prepares to expand a rule.
        /// </summary>
        /// <param name="parts">The rule parts, which meet the constraints of <see cref="IcalRecurrenceRule.IsValid"/>.</param>
        /// <param name="bySetPos">The BYSETPOS values.</param>
        /// <param name="startStamp">The start (DTSTART).</param>
        /// <param name="fromStamp">The earliest occurrence to give.</param>
        /// <param name="toStamp">The stamp before which every occurrence given falls.</param>
        /// <param name="untilStamp">The last occurrence allowed by UNTIL, inclusive; <see cref="long.MaxValue"/> for none.</param>
        /// <param name="toUtc">Converts an occurrence to UTC, to compare with <paramref name="untilStamp"/> when that is in UTC; or
        /// <c>null</c> when it is in local time.</param>
        /// <param name="maxIterations">The number of consecutive periods allowed without an occurrence.</param>
        internal IcalRecurrenceExpander(in IcalRecurrenceRule.Parts parts, int[] bySetPos, long startStamp, long fromStamp, long toStamp,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            long untilStamp, Func<long, long>? toUtc, int maxIterations)
#else
            long untilStamp, Func<long, long> toUtc, int maxIterations)
#endif
        {
            _frequency = parts.Frequency;
            _interval = parts.Interval == 0 ? 1 : parts.Interval;
            _count = parts.Count;
            _startStamp = startStamp;
            _fromStamp = fromStamp;
            _toStamp = toStamp;
            _untilStamp = untilStamp;
            _toUtc = toUtc;
            _maxIterations = maxIterations;
            _bySetPos = bySetPos;

            // ref. RFC 5545§3.3.10: WKST defaults to MO.  Day number 0 is a Monday.
            var weekStart = parts.WeekStart == 0 ? (int)DayOfWeek.Monday : parts.WeekStart - 1;
            _weekStartRemainder = (weekStart + 6) % 7;

            _byMonth = parts.ByMonth;
            _byMonthDay = parts.ByMonthDay;
            _byWeekNoPositive = parts.ByWeekNoPositive;
            _byWeekNoNegative = parts.ByWeekNoNegative;
            _byYearDay = parts.ByYearDay;
            _hasByYearDay = !IcalBitSet.IsEmpty(in parts.ByYearDay);
            _byDay = parts.ByDay;
            _hasByDay = !IcalBitSet.IsEmpty(in parts.ByDay);

            // ref. RFC 5545§3.3.10: an ordinal BYDAY counts within the month for MONTHLY, and for YEARLY with BYMONTH; otherwise within the
            // year.
            _isByDayOrdinalInMonth = _frequency == IcalRecurrenceFrequency.Monthly ||
                                     (_frequency == IcalRecurrenceFrequency.Yearly && parts.ByMonth != 0);

            // ref. RFC 5545§3.8.5.3 and §3.3.10: where no BYxxx part gives the day, it is taken from DTSTART.
            var startDay = (int)(startStamp / SecondsPerDay);
            if ((_byWeekNoPositive | _byWeekNoNegative) == 0 && !_hasByYearDay && _byMonthDay == 0 && !_hasByDay)
            {
                var startDate = IcalDate.FromDayNumber(startDay).ToDateTime();
                switch (_frequency)
                {
                    case IcalRecurrenceFrequency.Yearly:
                        if (_byMonth == 0)
                        {
                            _byMonth = 1UL << startDate.Month;
                        }

                        _byMonthDay = 1UL << startDate.Day;
                        break;
                    case IcalRecurrenceFrequency.Monthly:
                        _byMonthDay = 1UL << startDate.Day;
                        break;
                    case IcalRecurrenceFrequency.Weekly:
                        _byDay.Set((int)GetDayOfWeek(startDay) * ByDayStride);
                        _hasByDay = true;
                        break;
                }
            }

            // ref. RFC 5545§3.3.10: BYHOUR, BYMINUTE and BYSECOND expand a frequency above their unit, and limit one at or below it.  Where
            // they are not given, a frequency above the unit takes it from DTSTART.
            var startSecondOfDay = (int)(startStamp % SecondsPerDay);
            var hours = GetTimeValues(parts.ByHour, startSecondOfDay / SecondsPerHour, IcalRecurrenceFrequency.Hourly, 23, out _hourLimit);
            var minutes = GetTimeValues(parts.ByMinute, startSecondOfDay / SecondsPerMinute % 60, IcalRecurrenceFrequency.Minutely, 59,
                out _minuteLimit);

            // A second of 60 cannot be placed without a table of leap seconds, so it is never generated.
            var seconds = GetTimeValues(parts.BySecond, startSecondOfDay % 60, IcalRecurrenceFrequency.Secondly, 59, out _secondLimit);

            _timeOffsets = new int[hours.Count * minutes.Count * seconds.Count];
            var offsetIndex = 0;
            foreach (var hour in hours)
            {
                foreach (var minute in minutes)
                {
                    foreach (var second in seconds)
                    {
                        _timeOffsets[offsetIndex++] = hour * SecondsPerHour + minute * SecondsPerMinute + second;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the occurrences, in increasing order, from the start up to the first of COUNT, UNTIL, the end of the window and the end of
        /// 9999.
        /// </summary>
        /// <exception cref="InvalidOperationException">More consecutive periods than the iteration limit give no occurrence.</exception>
        internal IEnumerable<long> Expand()
        {
            var days = new int[MaxDaysPerPeriod];
            var positions = _bySetPos.Length == 0 ? null : new int[_bySetPos.Length];
            var firstPeriod = GetPeriod(_startStamp);
            var period = firstPeriod;

            // Without COUNT, the periods before the window need not be counted, so they are skipped.
            if (_count == 0 && _fromStamp > _startStamp)
            {
                period = Align(firstPeriod, GetPeriod(_fromStamp));
            }

            var produced = 0;
            var idlePeriods = 0;
            while (true)
            {
                int firstDay, endDay;
                long periodStart;
                if (!TryGetPeriodRange(period, out firstDay, out endDay, out periodStart) || periodStart >= _toStamp ||
                    (_toUtc is null && periodStart > _untilStamp))
                {
                    yield break;
                }

                if (++idlePeriods > _maxIterations)
                    throw new InvalidOperationException(string.Format(
                        "The recurrence rule gave no occurrence in {0} consecutive periods, so it is taken never to match again.", _maxIterations));

                int dayCount;
                var timeBase = 0;
                if (_frequency < IcalRecurrenceFrequency.Daily)
                {
                    long skipTo;
                    if (!MatchesSubDailyPeriod(periodStart, out skipTo))
                    {
                        period = Align(firstPeriod, GetPeriod(skipTo));
                        continue;
                    }

                    days[0] = firstDay;
                    dayCount = 1;
                    timeBase = (int)(periodStart % SecondsPerDay);
                }
                else
                {
                    dayCount = CollectDays(period, firstDay, endDay, days);
                }

                var timeCount = _timeOffsets.Length;
                var total = dayCount * timeCount;
                var selected = positions is null ? total : SelectPositions(total, positions);
                for (var index = 0; index < selected; index++)
                {
                    var position = positions is null ? index : positions[index];
                    var stamp = (long)days[position / timeCount] * SecondsPerDay + timeBase + _timeOffsets[position % timeCount];
                    if (stamp < _startStamp) continue;

                    idlePeriods = 0;
                    if (IsAfterUntil(stamp) || stamp >= _toStamp) yield break;

                    produced++;
                    if (stamp >= _fromStamp) yield return stamp;
                    if (produced == _count) yield break;
                }

                period += _interval;
            }
        }

        /// <summary>
        /// Gets the hours, minutes or seconds a day expands to, and the limit for a frequency at or below their unit.
        /// </summary>
        private List<int> GetTimeValues(ulong byValues, int startValue, IcalRecurrenceFrequency unit, int maximum, out ulong limit)
        {
            var values = new List<int>();
            limit = 0;
            if (_frequency <= unit)
            {
                // The period gives the value, which the time part limits.
                limit = byValues;
                values.Add(0);
                return values;
            }

            if (byValues == 0)
            {
                byValues = 1UL << startValue;
            }

            for (var value = 0; value <= maximum; value++)
            {
                if ((byValues & (1UL << value)) != 0)
                {
                    values.Add(value);
                }
            }

            return values;
        }

        /// <summary>
        /// Checks the day, hour, minute and second of a period of a frequency below DAILY.
        /// </summary>
        /// <param name="periodStart">The start of the period.</param>
        /// <param name="skipTo">If the period does not match, the stamp before which no period can match, for the same reason.</param>
        private bool MatchesSubDailyPeriod(long periodStart, out long skipTo)
        {
            var day = (int)(periodStart / SecondsPerDay);
            var dayStart = (long)day * SecondsPerDay;
            var secondOfDay = (int)(periodStart - dayStart);
            skipTo = 0;
            if (!MatchesDate(day))
            {
                skipTo = dayStart + SecondsPerDay;
                return false;
            }

            var hour = secondOfDay / SecondsPerHour;
            if (_hourLimit != 0 && (_hourLimit & (1UL << hour)) == 0)
            {
                skipTo = dayStart + (hour + 1) * SecondsPerHour;
                return false;
            }

            var minute = secondOfDay / SecondsPerMinute % 60;
            if (_minuteLimit != 0 && (_minuteLimit & (1UL << minute)) == 0)
            {
                skipTo = dayStart + hour * SecondsPerHour + (minute + 1) * SecondsPerMinute;
                return false;
            }

            if (_secondLimit != 0 && (_secondLimit & (1UL << (secondOfDay % 60))) == 0)
            {
                skipTo = periodStart + 1;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks the date parts against one day, for a frequency below DAILY, which allows neither BYWEEKNO nor an ordinal BYDAY.
        /// </summary>
        private bool MatchesDate(int day)
        {
            var date = IcalDate.FromDayNumber(day).ToDateTime();
            var daysInYear = GetDaysInYear(date.Year);
            return MatchesDay(day, date.Month, date.Day, DateTime.DaysInMonth(date.Year, date.Month), date.DayOfYear, daysInYear, 0, 0, 0, 0);
        }

        /// <summary>
        /// Collects the days of a period of DAILY or above that pass the date parts.
        /// </summary>
        /// <returns>The number of days collected.</returns>
        private int CollectDays(long period, int firstDay, int endDay, int[] days)
        {
            var date = IcalDate.FromDayNumber(firstDay).ToDateTime();
            var year = date.Year;
            var month = date.Month;
            var dayOfMonth = date.Day;
            var dayOfYear = date.DayOfYear;
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var daysInYear = GetDaysInYear(year);

            // ref. RFC 5545§3.3.10: BYWEEKNO is allowed only with YEARLY, so the period is a year and its days lie in the weeks of that year
            // and the last week of the one before or the first week of the one after.
            int previousWeek1 = 0, week1 = 0, nextWeek1 = 0, followingWeek1 = 0;
            if ((_byWeekNoPositive | _byWeekNoNegative) != 0)
            {
                var periodYear = (int)period;
                previousWeek1 = GetWeek1Start(periodYear - 1);
                week1 = GetWeek1Start(periodYear);
                nextWeek1 = GetWeek1Start(periodYear + 1);
                followingWeek1 = GetWeek1Start(periodYear + 2);
            }

            var count = 0;
            for (var day = firstDay; day < endDay; day++)
            {
                if (MatchesDay(day, month, dayOfMonth, daysInMonth, dayOfYear, daysInYear, previousWeek1, week1, nextWeek1, followingWeek1))
                {
                    days[count++] = day;
                }

                if (++dayOfMonth > daysInMonth)
                {
                    dayOfMonth = 1;
                    if (++month > 12)
                    {
                        month = 1;
                        year++;
                        dayOfYear = 0;
                        daysInYear = GetDaysInYear(year);
                    }

                    if (year <= 9999)
                    {
                        daysInMonth = DateTime.DaysInMonth(year, month);
                    }
                }

                dayOfYear++;
            }

            return count;
        }

        /// <summary>
        /// Checks the date parts against one day.  The week arguments are used only for BYWEEKNO.
        /// </summary>
        private bool MatchesDay(int day, int month, int dayOfMonth, int daysInMonth, int dayOfYear, int daysInYear, int previousWeek1, int week1,
            int nextWeek1, int followingWeek1)
        {
            if (_byMonth != 0 && (_byMonth & (1UL << month)) == 0) return false;

            if ((_byWeekNoPositive | _byWeekNoNegative) != 0)
            {
                // ref. RFC 5545§3.3.10: week 1 is the first week, starting on WKST, with at least four days of the year.
                int weekStart, nextWeekStart;
                if (day < week1)
                {
                    weekStart = previousWeek1;
                    nextWeekStart = week1;
                }
                else if (day < nextWeek1)
                {
                    weekStart = week1;
                    nextWeekStart = nextWeek1;
                }
                else
                {
                    weekStart = nextWeek1;
                    nextWeekStart = followingWeek1;
                }

                var weekNo = (day - weekStart) / 7 + 1;
                var weeksInYear = (nextWeekStart - weekStart) / 7;
                if ((_byWeekNoPositive & (1UL << weekNo)) == 0 && (_byWeekNoNegative & (1UL << (weeksInYear - weekNo + 1))) == 0) return false;
            }

            if (_hasByYearDay && !IcalBitSet.Get(in _byYearDay, dayOfYear) &&
                !IcalBitSet.Get(in _byYearDay, YearDayNegativeOffset + daysInYear - dayOfYear + 1))
            {
                return false;
            }

            if (_byMonthDay != 0 && (_byMonthDay & (1UL << dayOfMonth)) == 0 &&
                (_byMonthDay & (1UL << (MonthDayNegativeOffset + daysInMonth - dayOfMonth + 1))) == 0)
            {
                return false;
            }

            if (_hasByDay)
            {
                var weekdayIndex = (int)GetDayOfWeek(day) * ByDayStride;
                if (!IcalBitSet.Get(in _byDay, weekdayIndex))
                {
                    var position = _isByDayOrdinalInMonth ? dayOfMonth : dayOfYear;
                    var length = _isByDayOrdinalInMonth ? daysInMonth : daysInYear;
                    if (!IcalBitSet.Get(in _byDay, weekdayIndex + (position - 1) / 7 + 1) &&
                        !IcalBitSet.Get(in _byDay, weekdayIndex + ByDayNegativeOffset + (length - position) / 7 + 1))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Picks the BYSETPOS positions from a period's set of occurrences.
        /// </summary>
        /// <param name="total">The number of occurrences in the set.</param>
        /// <param name="positions">Receives the zero-based positions picked, in increasing order and without repeats.</param>
        /// <returns>The number of positions picked.</returns>
        private int SelectPositions(int total, int[] positions)
        {
            var count = 0;
            foreach (var value in _bySetPos)
            {
                var position = value > 0 ? value - 1 : total + value;
                if (position < 0 || position >= total) continue;

                var insertAt = count;
                while (insertAt > 0 && positions[insertAt - 1] > position)
                {
                    insertAt--;
                }

                if (insertAt > 0 && positions[insertAt - 1] == position) continue;

                Array.Copy(positions, insertAt, positions, insertAt + 1, count - insertAt);
                positions[insertAt] = position;
                count++;
            }

            return count;
        }

        private bool IsAfterUntil(long stamp)
        {
            return _toUtc is null ? stamp > _untilStamp : _toUtc(stamp) > _untilStamp;
        }

        /// <summary>
        /// Gets the number of the period of the rule's frequency that holds a stamp.
        /// </summary>
        private long GetPeriod(long stamp)
        {
            var day = (int)(stamp / SecondsPerDay);
            switch (_frequency)
            {
                case IcalRecurrenceFrequency.Yearly:
                    return IcalDate.FromDayNumber(day).ToDateTime().Year;
                case IcalRecurrenceFrequency.Monthly:
                    var date = IcalDate.FromDayNumber(day).ToDateTime();
                    return date.Year * 12L + date.Month - 1;
                case IcalRecurrenceFrequency.Weekly:
                    return FloorDivide(day - _weekStartRemainder, 7);
                case IcalRecurrenceFrequency.Daily:
                    return day;
                case IcalRecurrenceFrequency.Hourly:
                    return stamp / SecondsPerHour;
                case IcalRecurrenceFrequency.Minutely:
                    return stamp / SecondsPerMinute;
                default:
                    return stamp;
            }
        }

        /// <summary>
        /// Gets the days and the start of a period.
        /// </summary>
        /// <param name="period">The period number.</param>
        /// <param name="firstDay">The first day of the period, no earlier than 0001-01-01.</param>
        /// <param name="endDay">The day after the last day of the period, no later than the day after 9999-12-31.</param>
        /// <param name="periodStart">The stamp of the start of the period, no earlier than 0001-01-01.</param>
        /// <returns><c>true</c> if the period starts no later than 9999-12-31; otherwise <c>false</c>.</returns>
        private bool TryGetPeriodRange(long period, out int firstDay, out int endDay, out long periodStart)
        {
            long first;
            long end;
            periodStart = -1;
            switch (_frequency)
            {
                case IcalRecurrenceFrequency.Yearly:
                    first = period <= 9999 ? IcalDate.GetDayNumber((int)period, 1, 1) : long.MaxValue;
                    end = period < 9999 ? IcalDate.GetDayNumber((int)period + 1, 1, 1) : IcalDate.MaxDayNumber + 1;
                    break;
                case IcalRecurrenceFrequency.Monthly:
                    var year = (int)(period / 12);
                    var month = (int)(period % 12) + 1;
                    first = year <= 9999 ? IcalDate.GetDayNumber(year, month, 1) : long.MaxValue;
                    end = year <= 9999 ? first + DateTime.DaysInMonth(year, month) : long.MaxValue;
                    break;
                case IcalRecurrenceFrequency.Weekly:
                    first = period * 7 + _weekStartRemainder;
                    end = first + 7;
                    break;
                case IcalRecurrenceFrequency.Daily:
                    first = period;
                    end = period + 1;
                    break;
                default:
                    periodStart = _frequency == IcalRecurrenceFrequency.Hourly ? period * SecondsPerHour
                        : _frequency == IcalRecurrenceFrequency.Minutely ? period * SecondsPerMinute : period;
                    first = periodStart / SecondsPerDay;
                    end = first + 1;
                    break;
            }

            firstDay = (int)Math.Max(Math.Min(first, int.MaxValue), 0);
            endDay = (int)Math.Min(end, IcalDate.MaxDayNumber + 1);
            if (periodStart < 0)
            {
                periodStart = (long)firstDay * SecondsPerDay;
            }

            return first <= IcalDate.MaxDayNumber;
        }

        /// <summary>
        /// Gets the first period, counting INTERVAL periods at a time from the first, that is no earlier than a target period.
        /// </summary>
        private long Align(long firstPeriod, long target)
        {
            if (target <= firstPeriod) return firstPeriod;

            return firstPeriod + (target - firstPeriod + _interval - 1) / _interval * _interval;
        }

        /// <summary>
        /// Gets the day number of the first day of week 1 of a year, from year 0 to year 10001.
        /// </summary>
        private int GetWeek1Start(int year)
        {
            // Week 1 holds 4 January.  Day numbers are extended before 0001-01-01 and after 9999-12-31 by the proleptic Gregorian calendar.
            var priorYears = year - 1;
            var january4 = year >= 1
                ? 365 * priorYears + priorYears / 4 - priorYears / 100 + priorYears / 400 + 3
                : 3 - 366;
            return (int)FloorDivide(january4 - _weekStartRemainder, 7) * 7 + _weekStartRemainder;
        }

        /// <summary>
        /// Gets the number of days in a year, which may be past 9999 when the last period runs to the end of the calendar.
        /// </summary>
        private static int GetDaysInYear(int year)
        {
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0) ? 366 : 365;
        }

        private static DayOfWeek GetDayOfWeek(int day)
        {
            return (DayOfWeek)((day + 1) % 7);
        }

        private static long FloorDivide(long dividend, long divisor)
        {
            var quotient = dividend / divisor;
            return dividend % divisor < 0 ? quotient - 1 : quotient;
        }
    }
}
