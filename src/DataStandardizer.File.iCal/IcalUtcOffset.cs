using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A UTC-OFFSET value (RFC 5545 §3.3.14): the difference of a local time from UTC, to the second.
    /// </summary>
    /// <remarks>
    /// Seconds are written only when they are not zero.  The default value is no offset, written <c>+0000</c>.
    /// </remarks>
    public readonly struct IcalUtcOffset : IEquatable<IcalUtcOffset>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const int MaxTextLength = 7;
        private const int SecondsPerDay = 24 * 60 * 60;

        private readonly int _totalSeconds;

        /// <summary>
        /// Creates an offset.
        /// </summary>
        /// <param name="offset">The offset, a whole number of seconds less than a day either side of zero.  Positive offsets are ahead of
        /// UTC.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is a day or more either side of zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> is not a whole number of seconds.</exception>
        public IcalUtcOffset(TimeSpan offset)
        {
            if (offset.Ticks <= -SecondsPerDay * TimeSpan.TicksPerSecond || offset.Ticks >= SecondsPerDay * TimeSpan.TicksPerSecond)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "The offset must be less than a day either side of zero.");
            if (offset.Ticks % TimeSpan.TicksPerSecond != 0)
                throw new ArgumentException("The offset must be a whole number of seconds.", nameof(offset));

            _totalSeconds = (int)(offset.Ticks / TimeSpan.TicksPerSecond);
        }

        private IcalUtcOffset(int totalSeconds)
        {
            _totalSeconds = totalSeconds;
        }

        /// <summary>
        /// Gets the offset.  Positive offsets are ahead of UTC.
        /// </summary>
        public TimeSpan Offset => TimeSpan.FromSeconds(_totalSeconds);

        /// <summary>
        /// Parses an offset.
        /// </summary>
        /// <param name="value">The value, such as <c>-0500</c> or <c>+013045</c> in <see cref="IcalValueFormat.Text"/> form.  <c>-0000</c> and
        /// <c>-000000</c> are not valid.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The offset, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid offset; otherwise <c>false</c>.</returns>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalUtcOffset result)
        {
            IcalValueFormatting.CheckFormat(format);

            // ref. RFC 5545§3.3.14: utc-offset = time-numzone; time-numzone = ("+" / "-") time-hour time-minute [time-second]
            result = default(IcalUtcOffset);
            if (value.Length != 5 && value.Length != 7) return false;

            var sign = value[0];
            if (sign != '+' && sign != '-') return false;

            int hour, minute, second = 0;
            if (!IcalValueFormatting.TryParseDigits(value, 1, 2, out hour) || !IcalValueFormatting.TryParseDigits(value, 3, 2, out minute) ||
                (value.Length == 7 && !IcalValueFormatting.TryParseDigits(value, 5, 2, out second)))
            {
                return false;
            }

            if (hour > 23 || minute > 59 || second > 59) return false;

            var totalSeconds = hour * 3600 + minute * 60 + second;

            // ref. RFC 5545§3.3.14: the values "-0000" and "-000000" are not allowed.
            if (sign == '-')
            {
                if (totalSeconds == 0) return false;

                totalSeconds = -totalSeconds;
            }

            result = new IcalUtcOffset(totalSeconds);
            return true;
        }

        /// <summary>
        /// Parses an offset.
        /// </summary>
        /// <param name="value">The value, such as <c>-0500</c> or <c>+013045</c> in <see cref="IcalValueFormat.Text"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The offset.</returns>
        /// <exception cref="FormatException">The value is not a valid offset.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static IcalUtcOffset Parse(ReadOnlySpan<char> value, IcalValueFormat format)
        {
            IcalUtcOffset result;
            if (!TryParse(value, format, out result))
                throw new FormatException("The value is not a valid UTC-OFFSET.");

            return result;
        }

        /// <summary>
        /// Formats the offset into a span.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the offset was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            var writer = new IcalSpanWriter(destination);
            var totalSeconds = Math.Abs(_totalSeconds);
            writer.Append(_totalSeconds < 0 ? '-' : '+');
            writer.AppendDigits(totalSeconds / 3600, 2);
            writer.AppendDigits(totalSeconds / 60 % 60, 2);
            if (totalSeconds % 60 != 0)
            {
                writer.AppendDigits(totalSeconds % 60, 2);
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the offset as a string.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public string ToString(IcalValueFormat format)
        {
            var buffer = new char[MaxTextLength];
            int charsWritten;
            return IcalValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format), charsWritten);
        }

        /// <summary>
        /// Formats the offset in <see cref="IcalValueFormat.Text"/> form.
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
        /// Indicates whether this offset equals another.
        /// </summary>
        public bool Equals(IcalUtcOffset other)
        {
            return _totalSeconds == other._totalSeconds;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalUtcOffset other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _totalSeconds;
        }

        /// <summary>
        /// Indicates whether two offsets are equal.
        /// </summary>
        public static bool operator ==(IcalUtcOffset left, IcalUtcOffset right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two offsets differ.
        /// </summary>
        public static bool operator !=(IcalUtcOffset left, IcalUtcOffset right)
        {
            return !left.Equals(right);
        }
    }
}
