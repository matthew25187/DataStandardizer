namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Represents a point in time as the number of seconds elapsed since the Unix epoch 
    /// (January 1, 1970, 00:00:00 UTC).
    /// </summary>
    /// <remarks>
    /// This struct provides implicit conversions to and from <see cref="System.Int64"/> 
    /// and includes functionality to calculate the Julian Day Number. Because Unix time is defined in UTC,
    /// <see cref="SystemTimeExtensions.ToDateTime{T}(T)"/> returns a <see cref="System.DateTime"/> of kind
    /// <see cref="System.DateTimeKind.Utc"/> for it, and for a <see cref="SystemTimeWithGregorianCalendar"/> that wraps it.
    /// </remarks>
    public readonly struct UnixTime : ISystemTime, ISystemTimeKind
    {
        private readonly long _value;

        /// <summary>
        /// Initialises a new instance of the <see cref="UnixTime"/> struct.
        /// </summary>
        /// <param name="value">The number of seconds elapsed since the Unix epoch (January 1, 1970, 00:00:00 UTC); negative for an earlier time.</param>
        public UnixTime(long value)
        {
            _value = value;
        }

        /// <summary>
        /// Converts a Unix time to the number of seconds elapsed since the Unix epoch.
        /// </summary>
        /// <param name="value">The Unix time to convert.</param>
        /// <returns>The number of seconds elapsed since the Unix epoch (January 1, 1970, 00:00:00 UTC); 0 for a default instance.</returns>
        public static implicit operator long(UnixTime value)
        {
            return value._value;
        }

        /// <summary>
        /// Converts a number of seconds elapsed since the Unix epoch to a Unix time.
        /// </summary>
        /// <param name="value">The number of seconds elapsed since the Unix epoch (January 1, 1970, 00:00:00 UTC); negative for an earlier time.</param>
        /// <returns>The Unix time that is <paramref name="value"/> seconds after the Unix epoch.</returns>
        public static implicit operator UnixTime(long value)
        {
            return new UnixTime(value);
        }

        /// <summary>
        /// Gets the Julian date of the time: the number of days, with a fraction for the time of day, since noon UTC on January 1, 4713 BC in the proleptic Julian calendar.
        /// </summary>
        /// <value>
        /// The Unix time in days, plus 2440587.5, the Julian date of the Unix epoch. A time at midnight UTC has a fraction of .5.
        /// </value>
        public decimal JulianDayNumber => decimal.Divide(_value, JulianDayNumberHelper.SecondsPerDay) + JulianDayNumberHelper.UnixEpochJdn;

        System.DateTimeKind ISystemTimeKind.DateTimeKind => System.DateTimeKind.Utc;
    }
}