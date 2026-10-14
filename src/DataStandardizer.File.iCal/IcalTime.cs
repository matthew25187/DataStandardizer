using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A TIME value (RFC 5545 §3.3.12): a time of day, either floating or in UTC.
    /// </summary>
    /// <remarks>
    /// A second of 60 is allowed, for a positive leap second.  The default value is midnight, floating.
    /// </remarks>
    public readonly struct IcalTime : IEquatable<IcalTime>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        /// <summary>
        /// The length of the text form, <c>HHMMSS</c>, without a <c>Z</c>.
        /// </summary>
        internal const int TextLength = 6;

        private readonly byte _hour;
        private readonly byte _minute;
        private readonly byte _second;
        private readonly bool _isUtc;

        /// <summary>
        /// Creates a floating time.
        /// </summary>
        /// <param name="hour">The hour, from 0 to 23.</param>
        /// <param name="minute">The minute, from 0 to 59.</param>
        /// <param name="second">The second, from 0 to 60.  60 is a positive leap second.</param>
        /// <exception cref="ArgumentOutOfRangeException">The hour, minute or second is out of range.</exception>
        public IcalTime(int hour, int minute, int second) : this(hour, minute, second, false)
        {
        }

        /// <summary>
        /// Creates a time.
        /// </summary>
        /// <param name="hour">The hour, from 0 to 23.</param>
        /// <param name="minute">The minute, from 0 to 59.</param>
        /// <param name="second">The second, from 0 to 60.  60 is a positive leap second.</param>
        /// <param name="isUtc"><c>true</c> for a time in UTC; <c>false</c> for a floating time.</param>
        /// <exception cref="ArgumentOutOfRangeException">The hour, minute or second is out of range.</exception>
        public IcalTime(int hour, int minute, int second, bool isUtc)
        {
            if (hour < 0 || hour > 23)
                throw new ArgumentOutOfRangeException(nameof(hour), hour, "Hour must be in the range 0 - 23.");
            if (minute < 0 || minute > 59)
                throw new ArgumentOutOfRangeException(nameof(minute), minute, "Minute must be in the range 0 - 59.");
            if (second < 0 || second > 60)
                throw new ArgumentOutOfRangeException(nameof(second), second, "Second must be in the range 0 - 60.");

            _hour = (byte)hour;
            _minute = (byte)minute;
            _second = (byte)second;
            _isUtc = isUtc;
        }

        /// <summary>
        /// Gets the hour, from 0 to 23.
        /// </summary>
        public int Hour => _hour;

        /// <summary>
        /// Gets the minute, from 0 to 59.
        /// </summary>
        public int Minute => _minute;

        /// <summary>
        /// Gets the second, from 0 to 60.  60 is a positive leap second.
        /// </summary>
        public int Second => _second;

        /// <summary>
        /// Gets a flag indicating if the time is in UTC; otherwise it is floating.
        /// </summary>
        public bool IsUtc => _isUtc;

        /// <summary>
        /// Parses a time.
        /// </summary>
        /// <param name="value">The value, such as <c>230000</c> or <c>070000Z</c> in <see cref="IcalValueFormat.Text"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The time, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid time; otherwise <c>false</c>.</returns>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalTime result)
        {
            IcalValueFormatting.CheckFormat(format);

            return TryParseText(value, out result);
        }

        /// <summary>
        /// Parses a time.
        /// </summary>
        /// <param name="value">The value, such as <c>230000</c> or <c>070000Z</c> in <see cref="IcalValueFormat.Text"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The time.</returns>
        /// <exception cref="FormatException">The value is not a valid time.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static IcalTime Parse(ReadOnlySpan<char> value, IcalValueFormat format)
        {
            IcalTime result;
            if (!TryParse(value, format, out result))
                throw new FormatException("The value is not a valid TIME.");

            return result;
        }

        /// <summary>
        /// Formats the time into a span.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the time was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            var writer = new IcalSpanWriter(destination);
            AppendText(ref writer);
            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the time as a string.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public string ToString(IcalValueFormat format)
        {
            var buffer = new char[TextLength + 1];
            int charsWritten;
            return IcalValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format), charsWritten);
        }

        /// <summary>
        /// Formats the time in <see cref="IcalValueFormat.Text"/> form.
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
        /// Indicates whether this time equals another.  A floating time never equals a time in UTC.
        /// </summary>
        public bool Equals(IcalTime other)
        {
            return _hour == other._hour && _minute == other._minute && _second == other._second && _isUtc == other._isUtc;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalTime other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (_hour << 16) | (_minute << 8) | _second | (_isUtc ? 1 << 24 : 0);
        }

        /// <summary>
        /// Indicates whether two times are equal.
        /// </summary>
        public static bool operator ==(IcalTime left, IcalTime right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two times differ.
        /// </summary>
        public static bool operator !=(IcalTime left, IcalTime right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Compares the wall-clock fields of two times, ignoring whether they are in UTC.
        /// </summary>
        internal static int CompareClock(IcalTime left, IcalTime right)
        {
            var difference = left._hour - right._hour;
            if (difference == 0)
            {
                difference = left._minute - right._minute;
            }

            if (difference == 0)
            {
                difference = left._second - right._second;
            }

            return difference;
        }

        /// <summary>
        /// Parses the <c>HHMMSS[Z]</c> text form, which must be exactly the length of <paramref name="value"/>.
        /// </summary>
        internal static bool TryParseText(ReadOnlySpan<char> value, out IcalTime result)
        {
            // ref. RFC 5545§3.3.12: time = time-hour time-minute time-second [time-utc]
            result = default(IcalTime);
            int hour, minute, second;
            if (value.Length < TextLength || value.Length > TextLength + 1 ||
                !IcalValueFormatting.TryParseDigits(value, 0, 2, out hour) ||
                !IcalValueFormatting.TryParseDigits(value, 2, 2, out minute) ||
                !IcalValueFormatting.TryParseDigits(value, 4, 2, out second))
            {
                return false;
            }

            var isUtc = value.Length > TextLength;
            if (isUtc && !IcalValueFormatting.IsLetter(value[TextLength], 'Z')) return false;
            if (hour > 23 || minute > 59 || second > 60) return false;

            result = new IcalTime(hour, minute, second, isUtc);
            return true;
        }

        internal void AppendText(ref IcalSpanWriter writer)
        {
            writer.AppendDigits(_hour, 2);
            writer.AppendDigits(_minute, 2);
            writer.AppendDigits(_second, 2);
            if (_isUtc)
            {
                writer.Append('Z');
            }
        }
    }
}
