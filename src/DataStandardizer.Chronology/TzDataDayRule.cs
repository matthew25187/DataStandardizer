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

            // A day is validated against the length of the month in a leap year, as zic does, so that Feb 29 is accepted here and rejected only when it is resolved in a common year.
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
        internal static DateTime Resolve(int year, int month, TzDataDayKind dayKind, int? day, DayOfWeek? dayOfWeek)
        {
            if (year < YearMinimum || year > YearMaximum)
                throw new ArgumentOutOfRangeException(nameof(year), year, $"Year must be in the range {YearMinimum} - {YearMaximum}.");

            DateTime anchor;
            int difference;

            switch (dayKind)
            {
                case TzDataDayKind.DayOfMonth:
                    if (day.GetValueOrDefault() > DateTime.DaysInMonth(year, month))
                        throw new ArgumentOutOfRangeException(nameof(year), year, $"Day {day} of month {month} does not occur in year {year}.");
                    return new DateTime(year, month, day.GetValueOrDefault());

                case TzDataDayKind.LastWeekday:
                    anchor = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                    difference = ((int)anchor.DayOfWeek - (int)dayOfWeek.GetValueOrDefault() + 7) % 7;
                    return anchor.AddDays(-difference);

                case TzDataDayKind.WeekdayOnOrAfter:
                    anchor = new DateTime(year, month, DayMinimum).AddDays(day.GetValueOrDefault() - DayMinimum);
                    difference = ((int)dayOfWeek.GetValueOrDefault() - (int)anchor.DayOfWeek + 7) % 7;
                    return anchor.AddDays(difference);

                case TzDataDayKind.WeekdayOnOrBefore:
                    anchor = new DateTime(year, month, DayMinimum).AddDays(day.GetValueOrDefault() - DayMinimum);
                    difference = ((int)anchor.DayOfWeek - (int)dayOfWeek.GetValueOrDefault() + 7) % 7;
                    return anchor.AddDays(-difference);

                default:
                    throw new ArgumentOutOfRangeException(nameof(dayKind), dayKind, "Day kind is not defined.");
            }
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
