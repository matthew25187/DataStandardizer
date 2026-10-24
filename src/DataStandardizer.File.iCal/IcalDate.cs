using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A DATE value (RFC 5545 §3.3.4): a calendar date with no time of day.
    /// </summary>
    /// <remarks>
    /// The year runs from 1 to 9999, the range of <see cref="DateTime"/>.  The default value is 0001-01-01.
    /// </remarks>
    public readonly struct IcalDate : IEquatable<IcalDate>, IComparable<IcalDate>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        /// <summary>
        /// The length of the text form, <c>YYYYMMDD</c>.
        /// </summary>
        internal const int TextLength = 8;

        /// <summary>
        /// The length of the xCal form, <c>YYYY-MM-DD</c>.
        /// </summary>
        internal const int XmlLength = 10;

        /// <summary>
        /// The day number of 9999-12-31, the last date.
        /// </summary>
        internal const int MaxDayNumber = 3652058;

        private const long TicksPerDay = TimeSpan.TicksPerDay;

        // Days since 0001-01-01, so the default value is a valid date.
        private readonly int _dayNumber;

        /// <summary>
        /// Creates a date.
        /// </summary>
        /// <param name="year">The year, from 1 to 9999.</param>
        /// <param name="month">The month, from 1 to 12.</param>
        /// <param name="day">The day of the month, from 1 to the number of days in the month.</param>
        /// <exception cref="ArgumentOutOfRangeException">The year, month or day is out of range.</exception>
        public IcalDate(int year, int month, int day)
        {
            if (year < 1 || year > 9999)
                throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be in the range 1 - 9999.");
            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be in the range 1 - 12.");
            if (day < 1 || day > DateTime.DaysInMonth(year, month))
                throw new ArgumentOutOfRangeException(nameof(day), day, "Day must be in the range 1 - the number of days in the month.");

            _dayNumber = GetDayNumber(year, month, day);
        }

        private IcalDate(int dayNumber)
        {
            _dayNumber = dayNumber;
        }

        /// <summary>
        /// Gets the year.
        /// </summary>
        public int Year => ToDateTime().Year;

        /// <summary>
        /// Gets the month, from 1 to 12.
        /// </summary>
        public int Month => ToDateTime().Month;

        /// <summary>
        /// Gets the day of the month.
        /// </summary>
        public int Day => ToDateTime().Day;

        /// <summary>
        /// Gets the day of the week.
        /// </summary>
        public DayOfWeek DayOfWeek => (DayOfWeek)((_dayNumber + 1) % 7);

        /// <summary>
        /// Creates a date from the date part of a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="value">The date and time.  Its time of day and <see cref="DateTime.Kind"/> are ignored.</param>
        public static IcalDate FromDateTime(DateTime value)
        {
            return new IcalDate((int)(value.Ticks / TicksPerDay));
        }

        /// <summary>
        /// Converts the date to a <see cref="DateTime"/> at midnight, of kind <see cref="DateTimeKind.Unspecified"/>.
        /// </summary>
        public DateTime ToDateTime()
        {
            return new DateTime(_dayNumber * TicksPerDay, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Parses a date.
        /// </summary>
        /// <param name="value">The value, such as <c>19970714</c> in <see cref="IcalValueFormat.Text"/> form or <c>1997-07-14</c> in
        /// <see cref="IcalValueFormat.Xml"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The date, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid date; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalDate result)
        {
            IcalValueFormatting.CheckFormat(format);

            return format != IcalValueFormat.Text ? TryParseXml(value, out result) : TryParseText(value, out result);
        }

        /// <summary>
        /// Parses a date.
        /// </summary>
        /// <param name="value">The value, such as <c>19970714</c> in <see cref="IcalValueFormat.Text"/> form or <c>1997-07-14</c> in
        /// <see cref="IcalValueFormat.Xml"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The date.</returns>
        /// <exception cref="FormatException">The value is not a valid date.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static IcalDate Parse(ReadOnlySpan<char> value, IcalValueFormat format)
        {
            IcalDate result;
            if (!TryParse(value, format, out result))
                throw new FormatException("The value is not a valid DATE.");

            return result;
        }

        /// <summary>
        /// Formats the date into a span.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the date was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            var writer = new IcalSpanWriter(destination);
            if (format != IcalValueFormat.Text)
            {
                AppendXml(ref writer);
            }
            else
            {
                AppendText(ref writer);
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the date as a string.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public string ToString(IcalValueFormat format)
        {
            var buffer = new char[XmlLength];
            int charsWritten;
            return IcalValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format), charsWritten);
        }

        /// <summary>
        /// Formats the date in <see cref="IcalValueFormat.Text"/> form.
        /// </summary>
        public override string ToString()
        {
            return ToString(IcalValueFormat.Text);
        }

#if NET6_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The format is empty or <c>T</c> for <see cref="IcalValueFormat.Text"/>, <c>J</c> for <see cref="IcalValueFormat.Json"/> or
        /// <c>X</c> for <see cref="IcalValueFormat.Xml"/>.  The provider is ignored.</remarks>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return TryFormat(destination, out charsWritten, IcalValueFormatting.ParseFormatString(format));
        }

        /// <inheritdoc />
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString(IcalValueFormatting.ParseFormatString(format));
        }
#endif

        /// <summary>
        /// Indicates whether this date equals another.
        /// </summary>
        public bool Equals(IcalDate other)
        {
            return _dayNumber == other._dayNumber;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalDate other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _dayNumber;
        }

        /// <summary>
        /// Compares this date with another.
        /// </summary>
        /// <returns>Less than zero if this date is earlier, zero if they are the same date, and greater than zero if this date is later.</returns>
        public int CompareTo(IcalDate other)
        {
            return _dayNumber.CompareTo(other._dayNumber);
        }

        /// <summary>
        /// Indicates whether two dates are equal.
        /// </summary>
        public static bool operator ==(IcalDate left, IcalDate right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two dates differ.
        /// </summary>
        public static bool operator !=(IcalDate left, IcalDate right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Indicates whether one date is earlier than another.
        /// </summary>
        public static bool operator <(IcalDate left, IcalDate right)
        {
            return left._dayNumber < right._dayNumber;
        }

        /// <summary>
        /// Indicates whether one date is no later than another.
        /// </summary>
        public static bool operator <=(IcalDate left, IcalDate right)
        {
            return left._dayNumber <= right._dayNumber;
        }

        /// <summary>
        /// Indicates whether one date is later than another.
        /// </summary>
        public static bool operator >(IcalDate left, IcalDate right)
        {
            return left._dayNumber > right._dayNumber;
        }

        /// <summary>
        /// Indicates whether one date is no earlier than another.
        /// </summary>
        public static bool operator >=(IcalDate left, IcalDate right)
        {
            return left._dayNumber >= right._dayNumber;
        }

        /// <summary>
        /// Gets the number of days since 0001-01-01.
        /// </summary>
        internal int DayNumber => _dayNumber;

        /// <summary>
        /// Creates a date from a number of days since 0001-01-01, from 0 to <see cref="MaxDayNumber"/>.
        /// </summary>
        internal static IcalDate FromDayNumber(int dayNumber)
        {
            return new IcalDate(dayNumber);
        }

        /// <summary>
        /// Gets the number of days since 0001-01-01 of a valid date.
        /// </summary>
        internal static int GetDayNumber(int year, int month, int day)
        {
            return (int)(new DateTime(year, month, day).Ticks / TicksPerDay);
        }

        /// <summary>
        /// Parses the <c>YYYYMMDD</c> text form, which must be exactly the length of <paramref name="value"/>.
        /// </summary>
        internal static bool TryParseText(ReadOnlySpan<char> value, out IcalDate result)
        {
            // ref. RFC 5545§3.3.4: date-value = date-fullyear date-month date-mday
            result = default(IcalDate);
            int year, month, day;
            if (value.Length != TextLength ||
                !IcalValueFormatting.TryParseDigits(value, 0, 4, out year) ||
                !IcalValueFormatting.TryParseDigits(value, 4, 2, out month) ||
                !IcalValueFormatting.TryParseDigits(value, 6, 2, out day))
            {
                return false;
            }

            if (year < 1 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) return false;

            result = new IcalDate(GetDayNumber(year, month, day));
            return true;
        }

        /// <summary>
        /// Parses the <c>YYYY-MM-DD</c> xCal form, which must be exactly the length of <paramref name="value"/>.
        /// </summary>
        internal static bool TryParseXml(ReadOnlySpan<char> value, out IcalDate result)
        {
            // ref. RFC 6321§3.6.4: the date components are separated by "-" characters; pattern-date = "\d\d\d\d-\d\d-\d\d".
            result = default(IcalDate);
            int year, month, day;
            if (value.Length != XmlLength || value[4] != '-' || value[7] != '-' ||
                !IcalValueFormatting.TryParseDigits(value, 0, 4, out year) ||
                !IcalValueFormatting.TryParseDigits(value, 5, 2, out month) ||
                !IcalValueFormatting.TryParseDigits(value, 8, 2, out day))
            {
                return false;
            }

            if (year < 1 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) return false;

            result = new IcalDate(GetDayNumber(year, month, day));
            return true;
        }

        internal void AppendText(ref IcalSpanWriter writer)
        {
            var dateTime = ToDateTime();
            writer.AppendDigits(dateTime.Year, 4);
            writer.AppendDigits(dateTime.Month, 2);
            writer.AppendDigits(dateTime.Day, 2);
        }

        internal void AppendXml(ref IcalSpanWriter writer)
        {
            var dateTime = ToDateTime();
            writer.AppendDigits(dateTime.Year, 4);
            writer.Append('-');
            writer.AppendDigits(dateTime.Month, 2);
            writer.Append('-');
            writer.AppendDigits(dateTime.Day, 2);
        }
    }
}
