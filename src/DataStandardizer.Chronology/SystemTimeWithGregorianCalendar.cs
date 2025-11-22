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
        private const int SecondsPerHour = 60 * 60;
        private const int SecondsPerMinute = 60;

        internal readonly ISystemTime _systemTime;

        public SystemTimeWithGregorianCalendar(ISystemTime systemTime)
        {
            _systemTime = systemTime ?? throw new ArgumentNullException(nameof(systemTime));
        }

        #region Public Properties

        public decimal JulianDayNumber => _systemTime.JulianDayNumber;

        public ushort Day => DoGetDay();

        public ushort Month => DoGetMonth();

        public ushort Year => DoGetYear();

        public ushort Hour => DoGetHour();

        public ushort Minute => DoGetMinute();

        public ushort Second => DoGetSecond();

        #endregion

        #region Private Methods

        private ushort DoGetDay()
        {
            var (_, _, day) = GetDateFromJulianDayNumber(JulianDayNumber);
            return day;
        }

        private ushort DoGetHour()
        {
            var (hour, _, _) = GetTimeFromJulianDayNumber(JulianDayNumber);
            return hour;
        }

        private ushort DoGetMinute()
        {
            var (_, minute, _) = GetTimeFromJulianDayNumber(JulianDayNumber);
            return minute;
        }

        private ushort DoGetMonth()
        {
            var (_, month, _) = GetDateFromJulianDayNumber(JulianDayNumber);
            return month;
        }

        private ushort DoGetSecond()
        {
            var (_, _, second) = GetTimeFromJulianDayNumber(JulianDayNumber);
            return second;
        }

        private ushort DoGetYear()
        {
            var (year, _, _) = GetDateFromJulianDayNumber(JulianDayNumber);
            return year;
        }

        private (ushort Year, ushort Month, ushort Day) GetDateFromJulianDayNumber(decimal julianDayNumber)
        {
            var jdnWithoutTime = Math.Truncate(julianDayNumber + .5m);
            int a = (int)(jdnWithoutTime + 32044);
            int b = (4 * a + 3) / 146097;
            int c = a - 146097 * b / 4;
            int d = (4 * c + 3) / 1461;
            int e = c - 1461 * d / 4;
            int m = (5 * e + 2) / 153;
            ushort day = (ushort)(e - (153 * m + 2) / 5 + 1);
            ushort month = (ushort)(m + 3 - 12 * m / 10);
            ushort year = (ushort)(100 * b + d - 4800 + m / 10);
            return (year, month, day);
        }

        private (ushort Hour, ushort Minute, ushort Second) GetTimeFromJulianDayNumber(decimal julianDayNumber)
        {
            var timeOfDay = julianDayNumber + .5m - Math.Truncate(julianDayNumber + .5m);
            var remainingSeconds = timeOfDay * 86400;
            var hour = (ushort)(remainingSeconds / SecondsPerHour);
            remainingSeconds -= hour * SecondsPerHour;
            var minute = (ushort)(remainingSeconds / SecondsPerMinute);
            remainingSeconds -= minute * SecondsPerMinute;
            ushort second = (ushort)remainingSeconds;
            return (hour, minute, second);
        }

        #endregion
    }
}