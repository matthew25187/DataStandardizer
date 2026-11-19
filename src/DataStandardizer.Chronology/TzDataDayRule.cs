using System;

namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Validates and resolves the day specifications shared by TZ Database rules and zone line UNTIL columns.
    /// </summary>
    internal static class TzDataDayRule
    {
        private const int MonthMinimum = 1;
        private const int MonthMaximum = 12;
        private const int DayMinimum = 1;
        private const int YearMinimum = 1;
        private const int YearMaximum = 9999;
        private const int LeapYear = 2000;

        internal static void Validate(int month, TzDataDayKind dayKind, int? day, DayOfWeek? dayOfWeek)
        {
            if (month < MonthMinimum || month > MonthMaximum)
                throw new ArgumentOutOfRangeException(nameof(month), month, $"Month must be in the range {MonthMinimum} - {MonthMaximum}.");

            if (dayOfWeek.HasValue && (dayOfWeek.Value < DayOfWeek.Sunday || dayOfWeek.Value > DayOfWeek.Saturday))
                throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, "Day of week is not defined.");

            // A day is validated against the length of the month in a leap year, as zic does, so that Feb 29 is accepted here. In a common year, Resolve counts a weekday on or before Feb 29 back from Feb 28, and rejects Feb 29 as a fixed day or as the day from which a weekday is counted forward.
            var dayMaximum = DateTime.DaysInMonth(LeapYear, month);

            switch (dayKind)
            {
                case TzDataDayKind.DayOfMonth:
                    if (dayOfWeek.HasValue)
                        throw new ArgumentException("Day of week must not be specified for a fixed day of the month.", nameof(dayOfWeek));
                    ValidateDay(day, dayMaximum);
                    break;

                case TzDataDayKind.LastWeekday:
                    if (day.HasValue)
                        throw new ArgumentException("Day must not be specified for the last weekday of the month.", nameof(day));
                    if (!dayOfWeek.HasValue)
                        throw new ArgumentException("Day of week must be specified for the last weekday of the month.", nameof(dayOfWeek));
                    break;

                case TzDataDayKind.WeekdayOnOrAfter:
                case TzDataDayKind.WeekdayOnOrBefore:
                    if (!dayOfWeek.HasValue)
                        throw new ArgumentException("Day of week must be specified for a weekday relative to a day of the month.", nameof(dayOfWeek));
                    ValidateDay(day, dayMaximum);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dayKind), dayKind, "Day kind is not defined.");
            }
        }

        /// <summary>
        /// Resolves a validated day specification to a date in the given year.
        /// </summary>
        /// <remarks>
        /// A weekday relative to a day of the month may fall in the adjacent month, or the adjacent year, as for <c>Fri&lt;=1</c>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The date falls outside the range of <see cref="DateTime"/>, or the day does not occur in <paramref name="year"/>.
        /// </exception>
        internal static DateTime Resolve(int year, int month, TzDataDayKind dayKind, int? day, DayOfWeek? dayOfWeek)
        {
            if (year < YearMinimum || year > YearMaximum)
                throw new ArgumentOutOfRangeException(nameof(year), year, $"Year must be in the range {YearMinimum} - {YearMaximum}.");

            if (!TryResolve(year, month, dayKind, day, dayOfWeek, out var date))
                throw new ArgumentOutOfRangeException(nameof(year), year, $"The day falls outside the range of DateTime in year {year}.");

            return date;
        }

        /// <summary>
        /// Resolves a validated day specification to a date in the given year, where the date falls within the range of <see cref="DateTime"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A day that falls outside the range of <see cref="DateTime"/>, as for <c>Fri&lt;=1</c> in January of year 1 or a year outside the range, isn't an error in the data, so it returns <see langword="false"/>.
        /// </para>
        /// <para>
        /// February 29 in a common year follows zic (<c>rpytime</c> in <c>zic.c</c>): a weekday on or before it counts back from February 28, and a fixed day or a weekday on or after it is an error in the data, which throws.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The day does not occur in <paramref name="year"/>.</exception>
        internal static bool TryResolve(int year, int month, TzDataDayKind dayKind, int? day, DayOfWeek? dayOfWeek, out DateTime date)
        {
            if (year < YearMinimum || year > YearMaximum)
            {
                date = default(DateTime);
                return false;
            }

            var daysInMonth = DateTime.DaysInMonth(year, month);
            int anchorDay;
            int difference;

            switch (dayKind)
            {
                case TzDataDayKind.DayOfMonth:
                    // Only February 29 can exceed the month, since a day is validated against a leap year.
                    if (day.GetValueOrDefault() > daysInMonth)
                        throw new ArgumentOutOfRangeException(nameof(year), year, $"Day {day} of month {month} does not occur in year {year}.");
                    date = new DateTime(year, month, day.GetValueOrDefault());
                    return true;

                case TzDataDayKind.LastWeekday:
                    anchorDay = daysInMonth;
                    difference = -(((int)DayOfWeekOf(year, month, anchorDay) - (int)dayOfWeek.GetValueOrDefault() + 7) % 7);
                    break;

                case TzDataDayKind.WeekdayOnOrAfter:
                    if (day.GetValueOrDefault() > daysInMonth)
                        throw new ArgumentOutOfRangeException(nameof(year), year, $"Day {day} of month {month} does not occur in year {year}.");
                    anchorDay = day.GetValueOrDefault();
                    difference = ((int)dayOfWeek.GetValueOrDefault() - (int)DayOfWeekOf(year, month, anchorDay) + 7) % 7;
                    break;

                case TzDataDayKind.WeekdayOnOrBefore:
                    // February 29 in a common year counts back from February 28, as zic does.
                    anchorDay = Math.Min(day.GetValueOrDefault(), daysInMonth);
                    difference = -(((int)DayOfWeekOf(year, month, anchorDay) - (int)dayOfWeek.GetValueOrDefault() + 7) % 7);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dayKind), dayKind, "Day kind is not defined.");
            }

            var ticks = new DateTime(year, month, anchorDay).Ticks + difference * TimeSpan.TicksPerDay;
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
            {
                date = default(DateTime);
                return false;
            }

            date = new DateTime(ticks);
            return true;
        }

        private static DayOfWeek DayOfWeekOf(int year, int month, int day)
        {
            return new DateTime(year, month, day).DayOfWeek;
        }

        private static void ValidateDay(int? day, int dayMaximum)
        {
            if (!day.HasValue)
                throw new ArgumentException("Day must be specified.", nameof(day));

            if (day.Value < DayMinimum || day.Value > dayMaximum)
                throw new ArgumentOutOfRangeException(nameof(day), day, $"Day must be in the range {DayMinimum} - {dayMaximum}.");
        }
    }
}
