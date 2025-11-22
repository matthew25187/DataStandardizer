using System;

namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Represents a system time implementation that combines the Gregorian calendar date and time components.
    /// </summary>
    /// <remarks>
    /// This struct provides a concrete implementation of the <see cref="DataStandardizer.Chronology.ISystemTimeWithDateTime"/> interface,
    /// utilizing the Gregorian calendar for date calculations. It encapsulates both date and time details, including year, month, day,
    /// hour, minute, and second, while also supporting the Julian Day Number representation.
    /// </remarks>
    public readonly struct SystemTimeWithGregorianCalendar : ISystemTimeWithDateTime
    {
        internal readonly ISystemTime _systemTime;

        public SystemTimeWithGregorianCalendar(ISystemTime systemTime)
        {
            _systemTime = systemTime ?? throw new ArgumentNullException(nameof(systemTime));
        }

        #region Public Properties

        public decimal JulianDayNumber => _systemTime.JulianDayNumber;

        public int Day => DoGetDay();

        public int Month => DoGetMonth();

        public int Year => DoGetYear();

        public int Hour => DoGetHour();

        public int Minute => DoGetMinute();

        public int Second => DoGetSecond();

        #endregion

        #region Private Methods

        private int DoGetDay()
        {
            var (_, _, day) = JulianDayNumberHelper.ConvertJdnToGregorianCalendarDate(JulianDayNumber);
            return day;
        }

        private int DoGetHour()
        {
            var (hour, _, _) = JulianDayNumberHelper.ConvertJdnToTimeOfDay(JulianDayNumber);
            return hour;
        }

        private int DoGetMinute()
        {
            var (_, minute, _) = JulianDayNumberHelper.ConvertJdnToTimeOfDay(JulianDayNumber);
            return minute;
        }

        private int DoGetMonth()
        {
            var (_, month, _) = JulianDayNumberHelper.ConvertJdnToGregorianCalendarDate(JulianDayNumber);
            return month;
        }

        private int DoGetSecond()
        {
            var (_, _, second) = JulianDayNumberHelper.ConvertJdnToTimeOfDay(JulianDayNumber);
            return second;
        }

        private int DoGetYear()
        {
            var (year, _, _) = JulianDayNumberHelper.ConvertJdnToGregorianCalendarDate(JulianDayNumber);
            return year;
        }

        #endregion
    }
}