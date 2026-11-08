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
        // Null only in a default instance, which no constructor can produce.
#if NETCOREAPP3_0_OR_GREATER
        internal readonly ISystemTime? _systemTime;
#else
        internal readonly ISystemTime _systemTime;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemTimeWithGregorianCalendar"/> struct that wraps a system time.
        /// </summary>
        /// <param name="systemTime">The system time to wrap.</param>
        /// <exception cref="ArgumentNullException"><paramref name="systemTime"/> is <see langword="null"/>.</exception>
        public SystemTimeWithGregorianCalendar(ISystemTime systemTime)
        {
            _systemTime = systemTime ?? throw new ArgumentNullException(nameof(systemTime));
        }

        #region Public Properties

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The instance is a default instance, which wraps no system time, or the wrapped system time is a default instance that holds no date.</exception>
        public decimal JulianDayNumber => GetSystemTime().JulianDayNumber;

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The instance is a default instance, which wraps no system time, or the wrapped system time is a default instance that holds no date.</exception>
        public int Day => DoGetDay();

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The instance is a default instance, which wraps no system time, or the wrapped system time is a default instance that holds no date.</exception>
        public int Month => DoGetMonth();

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The instance is a default instance, which wraps no system time, or the wrapped system time is a default instance that holds no date.</exception>
        public int Year => DoGetYear();

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The instance is a default instance, which wraps no system time, or the wrapped system time is a default instance that holds no date.</exception>
        public int Hour => DoGetHour();

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The instance is a default instance, which wraps no system time, or the wrapped system time is a default instance that holds no date.</exception>
        public int Minute => DoGetMinute();

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The instance is a default instance, which wraps no system time, or the wrapped system time is a default instance that holds no date.</exception>
        public int Second => DoGetSecond();

        #endregion

        #region Private Methods

        private ISystemTime GetSystemTime()
        {
            return _systemTime ?? throw new InvalidOperationException($"The {nameof(SystemTimeWithGregorianCalendar)} instance is not initialised.");
        }

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