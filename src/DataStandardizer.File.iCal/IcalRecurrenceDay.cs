using System;
using System.Globalization;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A value of the BYDAY rule part of a recurrence rule (RFC 5545 §3.3.10): a day of the week, optionally with an ordinal that picks one
    /// occurrence of it within the month or year, such as <c>MO</c>, <c>1SU</c> or <c>-1FR</c>.
    /// </summary>
    public readonly struct IcalRecurrenceDay : IEquatable<IcalRecurrenceDay>
    {
        internal const int MaxOrdinal = 53;

        private static readonly string[] WeekdayNames = { "SU", "MO", "TU", "WE", "TH", "FR", "SA" };

        private readonly DayOfWeek _weekday;
        private readonly sbyte _ordinal;

        /// <summary>
        /// Creates a value for every occurrence of a day of the week.
        /// </summary>
        /// <param name="weekday">The day of the week.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="weekday"/> is not a <see cref="DayOfWeek"/> member.</exception>
        public IcalRecurrenceDay(DayOfWeek weekday) : this(weekday, 0)
        {
        }

        /// <summary>
        /// Creates a value for one occurrence of a day of the week.
        /// </summary>
        /// <param name="weekday">The day of the week.</param>
        /// <param name="ordinal">Which occurrence within the month or year: 1 for the first, -1 for the last, and so on to 53 or -53; or 0 for
        /// every occurrence.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="weekday"/> is not a <see cref="DayOfWeek"/> member, or
        /// <paramref name="ordinal"/> is out of range.</exception>
        public IcalRecurrenceDay(DayOfWeek weekday, int ordinal)
        {
            if (weekday < DayOfWeek.Sunday || weekday > DayOfWeek.Saturday)
                throw new ArgumentOutOfRangeException(nameof(weekday), weekday, "The day of the week is not recognised.");
            if (ordinal < -MaxOrdinal || ordinal > MaxOrdinal)
                throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "The ordinal must be in the range -53 - 53.");

            _weekday = weekday;
            _ordinal = (sbyte)ordinal;
        }

        /// <summary>
        /// Gets the day of the week.
        /// </summary>
        public DayOfWeek Weekday => _weekday;

        /// <summary>
        /// Gets which occurrence within the month or year is meant: positive counting from the start, negative from the end; or 0 for every
        /// occurrence.
        /// </summary>
        public int Ordinal => _ordinal;

        /// <summary>
        /// Indicates whether this value equals another.
        /// </summary>
        public bool Equals(IcalRecurrenceDay other)
        {
            return _weekday == other._weekday && _ordinal == other._ordinal;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalRecurrenceDay other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int)_weekday * 256 + _ordinal;
        }

        /// <summary>
        /// Formats the value as it appears in a BYDAY rule part, such as <c>MO</c> or <c>-1SU</c>.
        /// </summary>
        public override string ToString()
        {
            return _ordinal == 0 ? WeekdayNames[(int)_weekday] : _ordinal.ToString(CultureInfo.InvariantCulture) + WeekdayNames[(int)_weekday];
        }

        /// <summary>
        /// Indicates whether two values are equal.
        /// </summary>
        public static bool operator ==(IcalRecurrenceDay left, IcalRecurrenceDay right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two values differ.
        /// </summary>
        public static bool operator !=(IcalRecurrenceDay left, IcalRecurrenceDay right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Gets the two-letter name of a day of the week, such as <c>MO</c>.
        /// </summary>
        internal static string GetWeekdayName(DayOfWeek weekday)
        {
            return WeekdayNames[(int)weekday];
        }

        /// <summary>
        /// Parses a two-letter name of a day of the week, in either case.
        /// </summary>
        internal static bool TryParseWeekday(ReadOnlySpan<char> value, out DayOfWeek weekday)
        {
            for (var index = 0; index < WeekdayNames.Length; index++)
            {
                if (IcalValueFormatting.EqualsKeyword(value, WeekdayNames[index]))
                {
                    weekday = (DayOfWeek)index;
                    return true;
                }
            }

            weekday = DayOfWeek.Sunday;
            return false;
        }
    }
}
