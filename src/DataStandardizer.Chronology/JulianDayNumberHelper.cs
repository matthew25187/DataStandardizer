using System;

namespace DataStandardizer.Chronology
{
    internal static class JulianDayNumberHelper
    {
        private const int GregorianCalendarDayMinimum = 1;
        private const int GregorianCalendarMonthMinimum = 1;
        private const int GregorianCalendarMonthMaximum = 12;
        private const int HourMinimum = 0;
        private const int HourMaximum = 23;
        private const int MinuteMinimum = 0;
        private const int MinuteMaximum = 59;
        private const int SecondMinimum = 0;
        private const int SecondMaximum = 59;
        internal const int SecondsPerDay = 24 * SecondsPerHour;
        private const int SecondsPerHour = 60 * 60;
        private const int SecondsPerMinute = 60;
        private const int SubSecondDigits = 9;

        // The Julian Day Number of the day of the Unix epoch, 1970-01-01, and of the midnight that starts it.
        private const long UnixEpochDayNumber = 2440588;
        internal const decimal UnixEpochJdn = 2440587.5m;
        private const int YearMinimum = -4799;

        internal static decimal ConvertGregorianCalendarDateToJdn(int year, int month, int day)
        {
            return ConvertGregorianCalendarDateToDayNumber(year, month, day);
        }

        private static int ConvertGregorianCalendarDateToDayNumber(int year, int month, int day)
        {
            if (year < YearMinimum)
                throw new ArgumentOutOfRangeException(nameof(year), year, $"Dates preceding {Math.Abs(YearMinimum) + 1} BCE not supported.");

            if (month < GregorianCalendarMonthMinimum || month > GregorianCalendarMonthMaximum)
                throw new ArgumentOutOfRangeException(nameof(month), month, $"Month must be in the range {GregorianCalendarMonthMinimum} - {GregorianCalendarMonthMaximum}.");

            var monthLength = GetGregorianCalendarMonthLength(year, month);
            if (day < GregorianCalendarDayMinimum || day > monthLength)
                throw new ArgumentOutOfRangeException(nameof(day), day, $"Day must be in the range {GregorianCalendarDayMinimum} - {monthLength}.");

            var a = (int)decimal.Floor(decimal.Divide(14 - month, 12));
            var y = year + 4800 - a;
            var m = month + 12 * a - 3;
            var jdn = day + (int)decimal.Floor(decimal.Divide(153 * m + 2, 5)) + 365 * y + (int)decimal.Floor(decimal.Divide(y, 4)) - (int)decimal.Floor(decimal.Divide(y, 100)) + (int)decimal.Floor(decimal.Divide(y, 400)) - 32045;
            return jdn;
        }

        internal static (int Year, int Month, int Day) ConvertJdnToGregorianCalendarDate(decimal jdn)
        {
            var (dayNumber, _) = SplitJdn(jdn);
            return ConvertDayNumberToGregorianCalendarDate(dayNumber);
        }

        internal static (int Hour, int Minute, int Second) ConvertJdnToTimeOfDay(decimal jdn)
        {
            var (_, secondOfDay) = SplitJdn(jdn);
            return ConvertSecondOfDayToTimeOfDay(secondOfDay);
        }

        internal static (int Year, int Month, int Day, int Hour, int Minute, int Second) ConvertJdnToGregorianCalendarDateTime(decimal jdn)
        {
            var (dayNumber, secondOfDay) = SplitJdn(jdn);
            var (year, month, day) = ConvertDayNumberToGregorianCalendarDate(dayNumber);
            var (hour, minute, second) = ConvertSecondOfDayToTimeOfDay(secondOfDay);
            return (year, month, day, hour, minute, second);
        }

        /// <remarks>
        /// The arithmetic is checked, so that a day number too far from the epoch of the Julian Day Number to give a year
        /// throws <see cref="OverflowException"/> instead of giving a wrong date.
        /// </remarks>
        private static (int Year, int Month, int Day) ConvertDayNumberToGregorianCalendarDate(int dayNumber)
        {
            checked
            {
                var a = dayNumber + 32044;
                var b = (int)decimal.Floor(decimal.Divide(4 * a + 3, 146097));
                var c = a - (int)decimal.Floor(decimal.Divide(146097 * b, 4));
                var d = (int)decimal.Floor(decimal.Divide(4 * c + 3, 1461));
                var e = c - (int)decimal.Floor(decimal.Divide(1461 * d, 4));
                var m = (int)decimal.Floor(decimal.Divide(5 * e + 2, 153));
                var day = e - (int)decimal.Floor(decimal.Divide(153 * m + 2, 5)) + 1;
                var month = m + 3 - 12 * (int)decimal.Floor(decimal.Divide(m, 10));
                var year = 100 * b + d - 4800 + (int)decimal.Floor(decimal.Divide(m, 10));
                return (year, month, day);
            }
        }

        private static (int Hour, int Minute, int Second) ConvertSecondOfDayToTimeOfDay(int secondsRemaining)
        {
            var hour = secondsRemaining / SecondsPerHour;
            secondsRemaining -= hour * SecondsPerHour;
            var minute = secondsRemaining / SecondsPerMinute;
            secondsRemaining -= minute * SecondsPerMinute;
            var second = secondsRemaining;
            return (hour, minute, second);
        }

        internal static long ConvertGregorianCalendarDateTimeToUnixTime(int year, int month, int day, int hour, int minute, int second)
        {
            long dayNumber = ConvertGregorianCalendarDateToDayNumber(year, month, day);
            var secondOfDay = ConvertTimeOfDayToSeconds(hour, minute, second);
            return (dayNumber - UnixEpochDayNumber) * SecondsPerDay + secondOfDay;
        }

        internal static decimal ConvertTimeOfDayToJdn(int hour, int minute, int second)
        {
            var totalSeconds = ConvertTimeOfDayToSeconds(hour, minute, second);
            return decimal.Divide(totalSeconds, SecondsPerDay) - 0.5m;
        }

        /// <summary>
        /// Splits a Julian Day Number into the number of the day on which it falls and the whole seconds elapsed since the
        /// midnight that starts that day.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A Julian Day Number meant to fall on a whole second is often inexact, because a <see cref="decimal"/> can't hold a
        /// number of seconds divided by the number of seconds in a day exactly. A value meant to be <c>x</c> seconds can then
        /// arrive a tiny fraction below <c>x</c>, and truncating it would lose a whole second.
        /// </para>
        /// <para>
        /// The seconds are therefore first rounded to the nearest nanosecond (<see cref="SubSecondDigits"/> decimal places),
        /// which is far coarser than that error, and then truncated to a whole second. A fraction of a second is dropped, with
        /// one tolerance: a value less than half a nanosecond below a whole second is taken as that second.
        /// </para>
        /// <para>
        /// A day number that doesn't fit an <see cref="int"/> throws <see cref="OverflowException"/>.
        /// </para>
        /// </remarks>
        private static (int DayNumber, int SecondOfDay) SplitJdn(decimal jdn)
        {
            var secondsSinceMidnight = decimal.Floor(Math.Round((jdn + 0.5m) * SecondsPerDay, SubSecondDigits));
            var dayNumber = decimal.Floor(secondsSinceMidnight / SecondsPerDay);
            var secondOfDay = secondsSinceMidnight - dayNumber * SecondsPerDay;
            return (checked((int)dayNumber), (int)secondOfDay);
        }

        private static int ConvertTimeOfDayToSeconds(int hour, int minute, int second)
        {
            if (hour < HourMinimum || hour > HourMaximum)
                throw new ArgumentOutOfRangeException(nameof(hour), hour, $"Hour must be in the range {HourMinimum} - {HourMaximum}.");

            if (minute < MinuteMinimum || minute > MinuteMaximum)
                throw new ArgumentOutOfRangeException(nameof(minute), minute, $"Minute must be in the range {MinuteMinimum} - {MinuteMaximum}.");

            if (second < SecondMinimum || second > SecondMaximum)
                throw new ArgumentOutOfRangeException(nameof(second), second, $"Second must be in the range {SecondMinimum} - {SecondMaximum}.");

            return hour * SecondsPerHour + minute * SecondsPerMinute + second;
        }

        private static int GetGregorianCalendarMonthLength(int year, int month)
        {
            var isLeapYear = IsGregorianCalendarLeapYear(year);

            var monthLength = 0;
            switch (month)
            {
                case 1:
                    monthLength = 31;
                    break;
                case 2 when isLeapYear:
                    monthLength = 29;
                    break;
                case 2:
                    monthLength = 28;
                    break;
                case 3:
                    monthLength = 31;
                    break;
                case 4:
                    monthLength = 30;
                    break;
                case 5:
                    monthLength = 31;
                    break;
                case 6:
                    monthLength = 30;
                    break;
                case 7:
                    monthLength = 31;
                    break;
                case 8:
                    monthLength = 31;
                    break;
                case 9:
                    monthLength = 30;
                    break;
                case 10:
                    monthLength = 31;
                    break;
                case 11:
                    monthLength = 30;
                    break;
                case 12:
                    monthLength = 31;
                    break;
            }

            return monthLength;
        }

        private static bool IsGregorianCalendarLeapYear(int year)
        {
            return (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;
        }
    }
}