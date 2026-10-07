using System;

namespace DataStandardizer.Chronology
{
    /// <summary>
    /// The moment at which a TZ Database zone line ceases to apply, corresponding to the UNTIL column of the source.
    /// </summary>
    /// <remarks>
    /// Columns omitted from the source take their earliest value, so that an UNTIL of <c>1970</c> denotes
    /// midnight wall clock time at the start of January 1, 1970.
    /// </remarks>
    public readonly struct TzDataUntil : IEquatable<TzDataUntil>
    {
        internal TzDataUntil(int year, int month, TzDataDayKind dayKind, int? day, DayOfWeek? dayOfWeek, TimeSpan time, TzDataTimeReference timeReference)
        {
            if (timeReference < TzDataTimeReference.Wall || timeReference > TzDataTimeReference.Universal)
                throw new ArgumentOutOfRangeException(nameof(timeReference), timeReference, "Time reference is not defined.");

            TzDataDayRule.Validate(month, dayKind, day, dayOfWeek);

            Year = year;
            Month = month;
            DayKind = dayKind;
            Day = day;
            DayOfWeek = dayOfWeek;
            Time = time;
            TimeReference = timeReference;
        }

        /// <summary>
        /// Gets the year.
        /// </summary>
        public int Year { get; }

        /// <summary>
        /// Gets the month, from 1 to 12.
        /// </summary>
        public int Month { get; }

        /// <summary>
        /// Gets how the day is specified.
        /// </summary>
        public TzDataDayKind DayKind { get; }

        /// <summary>
        /// Gets the day of the month, or the day from which the weekday is counted.
        /// </summary>
        /// <value>
        /// The day of the month, or <see langword="null"/> where <see cref="DayKind"/> is <see cref="TzDataDayKind.LastWeekday"/>.
        /// </value>
        public int? Day { get; }

        /// <summary>
        /// Gets the weekday.
        /// </summary>
        /// <value>
        /// The weekday, or <see langword="null"/> where <see cref="DayKind"/> is <see cref="TzDataDayKind.DayOfMonth"/>.
        /// </value>
        public DayOfWeek? DayOfWeek { get; }

        /// <summary>
        /// Gets the time of day, measured against <see cref="TimeReference"/>.
        /// </summary>
        public TimeSpan Time { get; }

        /// <summary>
        /// Gets the clock against which <see cref="Time"/> is measured.
        /// </summary>
        public TzDataTimeReference TimeReference { get; }

        /// <summary>
        /// Determines whether two values are equal.
        /// </summary>
        public static bool operator ==(TzDataUntil left, TzDataUntil right) => left.Equals(right);

        /// <summary>
        /// Determines whether two values are not equal.
        /// </summary>
        public static bool operator !=(TzDataUntil left, TzDataUntil right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(TzDataUntil other)
        {
            return Year == other.Year &&
                   Month == other.Month &&
                   DayKind == other.DayKind &&
                   Day == other.Day &&
                   DayOfWeek == other.DayOfWeek &&
                   Time == other.Time &&
                   TimeReference == other.TimeReference;
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is TzDataUntil other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Year;
                hashCode = (hashCode * 397) ^ Month;
                hashCode = (hashCode * 397) ^ (int)DayKind;
                hashCode = (hashCode * 397) ^ Day.GetHashCode();
                hashCode = (hashCode * 397) ^ DayOfWeek.GetHashCode();
                hashCode = (hashCode * 397) ^ Time.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)TimeReference;
                return hashCode;
            }
        }
    }
}
