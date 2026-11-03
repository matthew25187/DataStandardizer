using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A date-time (RFC 5322 §3.3), as in a Date or Resent-Date header field or at the end of a Received header field, such as
    /// <c>Fri, 21 Nov 1997 09:55:06 -0600</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This keeps what <see cref="DateTimeOffset"/> can't represent: a leap second, whether a day of the week was given, a year after 9999, an offset of more than 14
    /// hours, and the difference between <c>+0000</c> and <c>-0000</c>, which says that the local offset is unknown (RFC 5322 §3.3).
    /// </para>
    /// <para>
    /// The obsolete syntax (RFC 5322 §4.3) is read: a two- or three-digit year, a zone name, and comments and white space between the parts.  A zone name is
    /// read as its numeric zone, and a military zone as <c>-0000</c>, because its meaning was defined wrongly in RFC 822.  The name itself is not kept.  No other
    /// zone name is in the grammar, so §4.3's advice on them doesn't apply and they are rejected.  A year too large for an <see cref="int"/> is also rejected.
    /// </para>
    /// <para>
    /// <see cref="ToString()"/> writes the modern form.
    /// </para>
    /// <para>
    /// Two values are equal only if they would be written the same way, so <c>+0000</c> and <c>-0000</c> differ, and so do the same moment at two offsets.  The
    /// ordering is by moment, with a leap second after the second before it and before the one after it.  Values at the same moment are then ordered by offset, then
    /// with an unknown offset last, then with a day of the week last, so that only equal values compare as 0.  A value made with <c>default</c> holds no date-time:
    /// it equals only another such value, and comes before every other value.  Its other members throw <see cref="InvalidOperationException"/>, but for
    /// <see cref="ToString()"/>, which gives the name of the type.
    /// </para>
    /// </remarks>
    public readonly struct ImfDateTime : IEquatable<ImfDateTime>, IComparable<ImfDateTime>
    {
        private const string InvalidFormatMessage = "The date-time is not in a valid format.";
        private const string NotInitialisedMessage = "The ImfDateTime instance is not initialised.";
        private const string UnknownOffsetMessage = "The local offset of the date-time is unknown (RFC 5322 §3.3).";
        private const string NotRepresentableMessage = "The date-time cannot be represented as a DateTimeOffset.";
        private const string YearOutOfRangeMessage = "RFC 5322 §3.3 allows only years from 1900.";

        private const int MinimumYear = 1900;
        private const int LastValidSecond = 59;
        private const int SecondsPerMinute = 60;
        private const int SecondsPerHour = 3600;
        private const int SecondsPerDay = 86400;
        private const int MinutesPerHour = 60;
        private const int DaysPerWeek = 7;
        private const int February = 2;

        private static readonly int[] DaysBeforeMonth = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };

        private static readonly string[] DayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        private static readonly string[] MonthNames = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        private readonly int _year;
        private readonly short _offsetMinutes;
        private readonly byte _month;
        private readonly byte _day;
        private readonly byte _hour;
        private readonly byte _minute;
        private readonly byte _second;
        private readonly bool _isOffsetUnknown;
        private readonly bool _hasDayOfWeek;

        /// <summary>
        /// Initializes a new date-time from a <see cref="DateTimeOffset"/>, with its offset and with no day of the week given.
        /// </summary>
        /// <remarks>
        /// Any fraction of a second is dropped, as RFC 5322 §3.3 has no form for it.  An offset of zero is <c>+0000</c>, not <c>-0000</c>.
        /// </remarks>
        /// <param name="value">The date and time.</param>
        /// <exception cref="ArgumentOutOfRangeException">The year of <paramref name="value"/> is before 1900 (RFC 5322 §3.3).</exception>
        public ImfDateTime(DateTimeOffset value)
        {
            if (value.Year < MinimumYear) throw new ArgumentOutOfRangeException(nameof(value), YearOutOfRangeMessage);

            _year = value.Year;
            _month = (byte)value.Month;
            _day = (byte)value.Day;
            _hour = (byte)value.Hour;
            _minute = (byte)value.Minute;
            _second = (byte)value.Second;
            _offsetMinutes = (short)(value.Offset.Hours * MinutesPerHour + value.Offset.Minutes);
            _isOffsetUnknown = false;
            _hasDayOfWeek = false;
        }

        internal ImfDateTime(int year, int month, int day, int hour, int minute, int second, int offsetMinutes, bool isOffsetUnknown, bool hasDayOfWeek)
        {
            _year = year;
            _month = (byte)month;
            _day = (byte)day;
            _hour = (byte)hour;
            _minute = (byte)minute;
            _second = (byte)second;
            _offsetMinutes = (short)offsetMinutes;
            _isOffsetUnknown = isOffsetUnknown;
            _hasDayOfWeek = hasDayOfWeek;
        }

        /// <summary>
        /// Gets the year, which is 1900 or later.
        /// </summary>
        /// <exception cref="InvalidOperationException">This value was made with <c>default</c>.</exception>
        public int Year
        {
            get
            {
                ThrowIfNotInitialised();
                return _year;
            }
        }

        /// <summary>
        /// Gets the month, from 1 to 12.
        /// </summary>
        /// <exception cref="InvalidOperationException">This value was made with <c>default</c>.</exception>
        public int Month
        {
            get
            {
                ThrowIfNotInitialised();
                return _month;
            }
        }

        /// <summary>
        /// Gets the day of the month, from 1.
        /// </summary>
        /// <exception cref="InvalidOperationException">This value was made with <c>default</c>.</exception>
        public int Day
        {
            get
            {
                ThrowIfNotInitialised();
                return _day;
            }
        }

        /// <summary>
        /// Gets the hour, from 0 to 23.
        /// </summary>
        /// <exception cref="InvalidOperationException">This value was made with <c>default</c>.</exception>
        public int Hour
        {
            get
            {
                ThrowIfNotInitialised();
                return _hour;
            }
        }

        /// <summary>
        /// Gets the minute, from 0 to 59.
        /// </summary>
        /// <exception cref="InvalidOperationException">This value was made with <c>default</c>.</exception>
        public int Minute
        {
            get
            {
                ThrowIfNotInitialised();
                return _minute;
            }
        }

        /// <summary>
        /// Gets the second, from 0 to 60, where 60 is a leap second.  It is 0 if the seconds were left out.
        /// </summary>
        /// <exception cref="InvalidOperationException">This value was made with <c>default</c>.</exception>
        public int Second
        {
            get
            {
                ThrowIfNotInitialised();
                return _second;
            }
        }

        /// <summary>
        /// Gets the offset from Universal Time.  It is zero if <see cref="IsOffsetUnknown"/> is <c>true</c>, because the date and time are then in Universal Time.
        /// </summary>
        /// <exception cref="InvalidOperationException">This value was made with <c>default</c>.</exception>
        public TimeSpan Offset
        {
            get
            {
                ThrowIfNotInitialised();
                return TimeSpan.FromMinutes(_offsetMinutes);
            }
        }

        /// <summary>
        /// Gets a flag indicating if the local offset is unknown, as for <c>-0000</c> and the military zones (RFC 5322 §3.3 and §4.3).
        /// </summary>
        /// <exception cref="InvalidOperationException">This value was made with <c>default</c>.</exception>
        public bool IsOffsetUnknown
        {
            get
            {
                ThrowIfNotInitialised();
                return _isOffsetUnknown;
            }
        }

        /// <summary>
        /// Gets a flag indicating if a day of the week was given.
        /// </summary>
        /// <exception cref="InvalidOperationException">This value was made with <c>default</c>.</exception>
        public bool HasDayOfWeek
        {
            get
            {
                ThrowIfNotInitialised();
                return _hasDayOfWeek;
            }
        }

        /// <summary>
        /// Gets the day of the week implied by the date, whether or not one was given.
        /// </summary>
        /// <exception cref="InvalidOperationException">This value was made with <c>default</c>.</exception>
        public DayOfWeek DayOfWeek
        {
            get
            {
                ThrowIfNotInitialised();
                return GetDayOfWeek(_year, _month, _day);
            }
        }

        private bool IsInitialised => _year != 0;

        /// <summary>
        /// Parses a date-time.
        /// </summary>
        /// <param name="value">The date-time as UTF-8 (RFC 6532), which may be folded, such as <c>Fri, 21 Nov 1997 09:55:06 -0600</c>.</param>
        /// <returns>The date-time.</returns>
        /// <exception cref="FormatException">The value is not a valid date-time.</exception>
        public static ImfDateTime Parse(ReadOnlySpan<byte> value)
        {
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a date-time.
        /// </summary>
        /// <param name="value">The date-time as UTF-8 (RFC 6532), which may be folded, such as <c>Fri, 21 Nov 1997 09:55:06 -0600</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowInvalidValues"/>.</param>
        /// <returns>The date-time.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid date-time.</exception>
        public static ImfDateTime Parse(ReadOnlySpan<byte> value, ImfOptions options)
        {
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a date-time.
        /// </summary>
        /// <param name="value">The date-time, which may be folded, such as <c>Fri, 21 Nov 1997 09:55:06 -0600</c>.</param>
        /// <returns>The date-time.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid date-time.</exception>
        public static ImfDateTime Parse(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a date-time.
        /// </summary>
        /// <param name="value">The date-time, which may be folded, such as <c>Fri, 21 Nov 1997 09:55:06 -0600</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowInvalidValues"/>.</param>
        /// <returns>The date-time.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid date-time.</exception>
        public static ImfDateTime Parse(string value, ImfOptions options)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a date-time.
        /// </summary>
        /// <param name="value">The date-time as UTF-8 (RFC 6532), which may be folded, such as <c>Fri, 21 Nov 1997 09:55:06 -0600</c>.</param>
        /// <param name="result">The date-time, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid date-time; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<byte> value, out ImfDateTime result)
        {
            return ImfDateTimeParser.TryParse(value, false, out result);
        }

        /// <summary>
        /// Parses a date-time.
        /// </summary>
        /// <param name="value">The date-time as UTF-8 (RFC 6532), which may be folded, such as <c>Fri, 21 Nov 1997 09:55:06 -0600</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowInvalidValues"/>.</param>
        /// <param name="result">The date-time, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid date-time; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        public static bool TryParse(ReadOnlySpan<byte> value, ImfOptions options, out ImfDateTime result)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            return ImfDateTimeParser.TryParse(value, options.AllowInvalidValues, out result);
        }

        /// <summary>
        /// Parses a date-time.
        /// </summary>
        /// <param name="value">The date-time, which may be folded, such as <c>Fri, 21 Nov 1997 09:55:06 -0600</c>.</param>
        /// <param name="result">The date-time, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid date-time; otherwise <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, out ImfDateTime result)
        {
            result = default;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, out result);
        }

        /// <summary>
        /// Parses a date-time.
        /// </summary>
        /// <param name="value">The date-time, which may be folded, such as <c>Fri, 21 Nov 1997 09:55:06 -0600</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowInvalidValues"/>.</param>
        /// <param name="result">The date-time, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid date-time; otherwise <c>false</c>, including for <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, ImfOptions options, out ImfDateTime result)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            result = default;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, options, out result);
        }

        /// <summary>
        /// Converts the date-time to a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <remarks>
        /// A leap second becomes second 59 of the same minute, as <see cref="DateTimeOffset"/> has no leap seconds.
        /// </remarks>
        /// <returns>The date and time, with their offset.</returns>
        /// <exception cref="InvalidOperationException">The local offset is unknown, the date-time is outside what <see cref="DateTimeOffset"/> can represent, or this
        /// value was made with <c>default</c>.</exception>
        public DateTimeOffset ToDateTimeOffset()
        {
            return ToDateTimeOffset(false);
        }

        /// <summary>
        /// Converts the date-time to a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <remarks>
        /// A leap second becomes second 59 of the same minute, as <see cref="DateTimeOffset"/> has no leap seconds.
        /// </remarks>
        /// <param name="treatUnknownOffsetAsUniversalTime"><c>true</c> to convert an unknown local offset as if it were <c>+0000</c>; <c>false</c> to throw.</param>
        /// <returns>The date and time, with their offset.</returns>
        /// <exception cref="InvalidOperationException">The local offset is unknown and <paramref name="treatUnknownOffsetAsUniversalTime"/> is <c>false</c>, the
        /// date-time is outside what <see cref="DateTimeOffset"/> can represent, or this value was made with <c>default</c>.</exception>
        public DateTimeOffset ToDateTimeOffset(bool treatUnknownOffsetAsUniversalTime)
        {
            ThrowIfNotInitialised();
            if (_isOffsetUnknown && !treatUnknownOffsetAsUniversalTime) throw new InvalidOperationException(UnknownOffsetMessage);

            try
            {
                return new DateTimeOffset(_year, _month, _day, _hour, _minute, Math.Min((int)_second, LastValidSecond), TimeSpan.FromMinutes(_offsetMinutes));
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new InvalidOperationException(NotRepresentableMessage, exception);
            }
        }

        /// <summary>
        /// Indicates whether this date-time equals another: they would be written the same way.
        /// </summary>
        public bool Equals(ImfDateTime other)
        {
            return _year == other._year &&
                _month == other._month &&
                _day == other._day &&
                _hour == other._hour &&
                _minute == other._minute &&
                _second == other._second &&
                _offsetMinutes == other._offsetMinutes &&
                _isOffsetUnknown == other._isOffsetUnknown &&
                _hasDayOfWeek == other._hasDayOfWeek;
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is ImfDateTime other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (!IsInitialised) return 0;

            var hash = ImfGrammar.CombineHashCodes(_year, _month);
            hash = ImfGrammar.CombineHashCodes(hash, _day);
            hash = ImfGrammar.CombineHashCodes(hash, _hour);
            hash = ImfGrammar.CombineHashCodes(hash, _minute);
            hash = ImfGrammar.CombineHashCodes(hash, _second);
            hash = ImfGrammar.CombineHashCodes(hash, _offsetMinutes);
            hash = ImfGrammar.CombineHashCodes(hash, _isOffsetUnknown ? 1 : 0);
            return ImfGrammar.CombineHashCodes(hash, _hasDayOfWeek ? 1 : 0);
        }

        /// <summary>
        /// Compares this date-time with another, by moment and then by the differences that make them unequal.
        /// </summary>
        /// <param name="other">The other date-time.</param>
        /// <returns>A negative number if this one comes first, zero if they are equal, or a positive number if the other comes first.</returns>
        public int CompareTo(ImfDateTime other)
        {
            if (!IsInitialised || !other.IsInitialised) return IsInitialised.CompareTo(other.IsInitialised);

            var result = GetMoment().CompareTo(other.GetMoment());
            if (result == 0) result = _second.CompareTo(other._second);
            if (result == 0) result = _offsetMinutes.CompareTo(other._offsetMinutes);
            if (result == 0) result = _isOffsetUnknown.CompareTo(other._isOffsetUnknown);
            if (result == 0) result = _hasDayOfWeek.CompareTo(other._hasDayOfWeek);

            return result;
        }

        /// <summary>
        /// Indicates whether two date-times are equal.
        /// </summary>
        public static bool operator ==(ImfDateTime left, ImfDateTime right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two date-times differ.
        /// </summary>
        public static bool operator !=(ImfDateTime left, ImfDateTime right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Indicates whether one date-time comes before another.
        /// </summary>
        public static bool operator <(ImfDateTime left, ImfDateTime right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Indicates whether one date-time comes before another or equals it.
        /// </summary>
        public static bool operator <=(ImfDateTime left, ImfDateTime right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Indicates whether one date-time comes after another.
        /// </summary>
        public static bool operator >(ImfDateTime left, ImfDateTime right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Indicates whether one date-time comes after another or equals it.
        /// </summary>
        public static bool operator >=(ImfDateTime left, ImfDateTime right)
        {
            return left.CompareTo(right) >= 0;
        }

        /// <summary>
        /// Formats the date-time in the modern form, such as <c>Fri, 21 Nov 1997 09:55:06 -0600</c>, with a day of the week if one was given.
        /// </summary>
        /// <returns>The date-time, or the name of this type if this value was made with <c>default</c>.</returns>
        public override string ToString()
        {
            return ToString(false);
        }

        /// <summary>
        /// Formats the date-time in the modern form, such as <c>Fri, 21 Nov 1997 09:55:06 -0600</c>.
        /// </summary>
        /// <param name="includeDayOfWeek"><c>true</c> to write a day of the week even if none was given; <c>false</c> to write one only if it was.</param>
        /// <returns>The date-time, or the name of this type if this value was made with <c>default</c>.</returns>
        public string ToString(bool includeDayOfWeek)
        {
            if (!IsInitialised) return typeof(ImfDateTime).ToString();

            var output = new StringBuilder();
            if (includeDayOfWeek || _hasDayOfWeek)
            {
                output.Append(DayNames[(int)GetDayOfWeek(_year, _month, _day)]).Append(", ");
            }

            output.Append((int)_day).Append(' ').Append(MonthNames[_month - 1]).Append(' ').Append(_year).Append(' ');
            AppendTwoDigits(output, _hour);
            output.Append(':');
            AppendTwoDigits(output, _minute);
            output.Append(':');
            AppendTwoDigits(output, _second);
            output.Append(' ');

            // ref. RFC 5322 §3.3: "+0000" is Universal Time with a known offset, so only an unknown offset is written "-0000".
            output.Append(_offsetMinutes < 0 || _isOffsetUnknown ? '-' : '+');
            var offsetMinutes = Math.Abs((int)_offsetMinutes);
            AppendTwoDigits(output, offsetMinutes / MinutesPerHour);
            AppendTwoDigits(output, offsetMinutes % MinutesPerHour);

            return output.ToString();
        }

        /// <summary>
        /// Gets the number of days in a month of a year in the Gregorian calendar.
        /// </summary>
        internal static int GetDaysInMonth(int year, int month)
        {
            var nextMonthStart = month == DaysBeforeMonth.Length ? 365 : DaysBeforeMonth[month];
            var days = nextMonthStart - DaysBeforeMonth[month - 1];
            return month == February && IsLeapYear(year) ? days + 1 : days;
        }

        /// <summary>
        /// Gets the day of the week of a date in the Gregorian calendar.
        /// </summary>
        internal static DayOfWeek GetDayOfWeek(int year, int month, int day)
        {
            // Day 1 is Monday 1 January of year 1, so a multiple of 7 is a Sunday, the first value of DayOfWeek.
            return (DayOfWeek)(GetDayNumber(year, month, day) % DaysPerWeek);
        }

        private static bool IsLeapYear(int year)
        {
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        }

        private static long GetDayNumber(int year, int month, int day)
        {
            long yearsBefore = year - 1;
            var days = yearsBefore * 365 + yearsBefore / 4 - yearsBefore / 100 + yearsBefore / 400;
            days += DaysBeforeMonth[month - 1] + day;
            return month > February && IsLeapYear(year) ? days + 1 : days;
        }

        private static void AppendTwoDigits(StringBuilder output, int value)
        {
            output.Append((char)('0' + value / 10)).Append((char)('0' + value % 10));
        }

        // The second of Universal Time, with a leap second counted as the second before it: CompareTo puts it after that one.
        private long GetMoment()
        {
            var local = GetDayNumber(_year, _month, _day) * SecondsPerDay + _hour * SecondsPerHour + _minute * SecondsPerMinute + Math.Min((int)_second, LastValidSecond);
            return local - _offsetMinutes * SecondsPerMinute;
        }

        private void ThrowIfNotInitialised()
        {
            if (!IsInitialised) throw new InvalidOperationException(NotInitialisedMessage);
        }
    }
}
