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

        public UnixTime(long value)
        {
            _value = value;
        }

        public static implicit operator long(UnixTime value)
        {
            return value._value;
        }

        public static implicit operator UnixTime(long value)
        {
            return new UnixTime(value);
        }

        public decimal JulianDayNumber => decimal.Divide(_value, JulianDayNumberHelper.SecondsPerDay) + JulianDayNumberHelper.UnixEpochJdn;

        System.DateTimeKind ISystemTimeKind.DateTimeKind => System.DateTimeKind.Utc;
    }
}