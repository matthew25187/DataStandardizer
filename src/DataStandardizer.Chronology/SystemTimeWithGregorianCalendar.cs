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
    public readonly struct SystemTimeWithGregorianCalendar : ISystemTimeWithDateTime, IEquatable<SystemTimeWithGregorianCalendar>, ISystemTimeKind
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

        // A default instance wraps no system time, and has no kind of its own.
        DateTimeKind ISystemTimeKind.DateTimeKind => (_systemTime as ISystemTimeKind)?.DateTimeKind ?? DateTimeKind.Unspecified;

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

        #region Public Methods

        /// <summary>
        /// Indicates whether this instance wraps a system time equal to the one that <paramref name="other"/> wraps.
        /// </summary>
        /// <param name="other">The instance to compare with this instance.</param>
        /// <returns>
        /// <see langword="true"/> if both instances wrap equal system times, or both are default instances; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(SystemTimeWithGregorianCalendar other)
        {
            return Equals(_systemTime, other._systemTime);
        }

        /// <inheritdoc/>
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is SystemTimeWithGregorianCalendar other && Equals(other);
        }

        /// <inheritdoc/>
        /// <remarks>A default instance returns 0.</remarks>
        public override int GetHashCode()
        {
            return _systemTime?.GetHashCode() ?? 0;
        }

        /// <inheritdoc/>
        /// <remarks>Returns the full name of the type, for a default instance as for any other.</remarks>
        public override string ToString()
        {
            return $"{typeof(SystemTimeWithGregorianCalendar).Namespace}.{nameof(SystemTimeWithGregorianCalendar)}";
        }

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