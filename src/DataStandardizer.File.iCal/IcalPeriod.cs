using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A PERIOD value (RFC 5545 §3.3.9): a start and either an end or a positive duration.
    /// </summary>
    /// <remarks>
    /// A <c>TZID</c> parameter on the property applies to both the start and the end.  The default value starts and ends at 0001-01-01 at
    /// midnight, floating, which is not a valid period; it can be formatted, but not parsed back.
    /// </remarks>
    public readonly struct IcalPeriod : IEquatable<IcalPeriod>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const int MaxTextLength = 64;

        private readonly IcalDateTime _start;
        private readonly IcalDateTime _end;
        private readonly IcalDuration _duration;
        private readonly bool _hasDuration;

        /// <summary>
        /// Creates a period with a start and an end.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.  If it has the same kind and time zone as <paramref name="start"/>, it must be later.</param>
        /// <exception cref="ArgumentException"><paramref name="end"/> is not later than <paramref name="start"/>.</exception>
        public IcalPeriod(IcalDateTime start, IcalDateTime end)
        {
            if (!IsEndAfterStart(start, end))
                throw new ArgumentException("The end of the period must be later than its start.", nameof(end));

            _start = start;
            _end = end;
            _duration = default(IcalDuration);
            _hasDuration = false;
        }

        /// <summary>
        /// Creates a period with a start and a duration.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="duration">The duration, which must be positive.</param>
        /// <exception cref="ArgumentException"><paramref name="duration"/> is zero or negative.</exception>
        public IcalPeriod(IcalDateTime start, IcalDuration duration)
        {
            if (!IsPositive(duration))
                throw new ArgumentException("The duration of the period must be positive.", nameof(duration));

            _start = start;
            _end = default(IcalDateTime);
            _duration = duration;
            _hasDuration = true;
        }

        /// <summary>
        /// Gets the start.
        /// </summary>
        public IcalDateTime Start => _start;

        /// <summary>
        /// Gets the end; or <c>null</c> if the period has a duration instead.
        /// </summary>
        public IcalDateTime? End => _hasDuration ? (IcalDateTime?)null : _end;

        /// <summary>
        /// Gets the duration; or <c>null</c> if the period has an end instead.
        /// </summary>
        public IcalDuration? Duration => _hasDuration ? _duration : (IcalDuration?)null;

        /// <summary>
        /// Parses a period whose date-times are floating or in UTC.
        /// </summary>
        /// <param name="value">The value, such as <c>19970101T180000Z/19970102T070000Z</c> or <c>19970101T180000Z/PT5H30M</c> in
        /// <see cref="IcalValueFormat.Text"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The period, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid period; otherwise <c>false</c>.</returns>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalPeriod result)
        {
            return TryParse(value, null, format, out result);
        }

        /// <summary>
        /// Parses a period, in a named time zone if a time zone identifier is given.
        /// </summary>
        /// <param name="value">The value, such as <c>19970101T180000/PT5H30M</c> in <see cref="IcalValueFormat.Text"/> form.</param>
        /// <param name="timeZoneId">The identifier from the property's <c>TZID</c> parameter; or <c>null</c> if it has none.  It applies to the
        /// start and to the end, so neither may then be in UTC.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The period, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid period; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="timeZoneId"/> is empty.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(ReadOnlySpan<char> value, string? timeZoneId, IcalValueFormat format, out IcalPeriod result)
#else
        public static bool TryParse(ReadOnlySpan<char> value, string timeZoneId, IcalValueFormat format, out IcalPeriod result)
#endif
        {
            IcalDateTime.CheckTimeZoneId(timeZoneId);
            IcalValueFormatting.CheckFormat(format);

            // ref. RFC 5545§3.3.9: period = period-explicit / period-start
            //   period-explicit = date-time "/" date-time
            //   period-start    = date-time "/" dur-value
            result = default(IcalPeriod);
            var slashIndex = value.IndexOf('/');
            IcalDateTime start;
            if (slashIndex < 0 || !IcalDateTime.TryParseText(value.Slice(0, slashIndex), timeZoneId, out start)) return false;

            var rest = value.Slice(slashIndex + 1);
            if (rest.Length > 0 && (rest[0] == '+' || rest[0] == '-' || IcalValueFormatting.IsLetter(rest[0], 'P')))
            {
                IcalDuration duration;
                if (!IcalDuration.TryParseText(rest, out duration) || !IsPositive(duration)) return false;

                result = new IcalPeriod(start, duration);
                return true;
            }

            IcalDateTime end;
            if (!IcalDateTime.TryParseText(rest, timeZoneId, out end) || !IsEndAfterStart(start, end)) return false;

            result = new IcalPeriod(start, end);
            return true;
        }

        /// <summary>
        /// Parses a period whose date-times are floating or in UTC.
        /// </summary>
        /// <param name="value">The value, such as <c>19970101T180000Z/PT5H30M</c> in <see cref="IcalValueFormat.Text"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The period.</returns>
        /// <exception cref="FormatException">The value is not a valid period.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static IcalPeriod Parse(ReadOnlySpan<char> value, IcalValueFormat format)
        {
            return Parse(value, null, format);
        }

        /// <summary>
        /// Parses a period, in a named time zone if a time zone identifier is given.
        /// </summary>
        /// <param name="value">The value, such as <c>19970101T180000/PT5H30M</c> in <see cref="IcalValueFormat.Text"/> form.</param>
        /// <param name="timeZoneId">The identifier from the property's <c>TZID</c> parameter; or <c>null</c> if it has none.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The period.</returns>
        /// <exception cref="FormatException">The value is not a valid period.</exception>
        /// <exception cref="ArgumentException"><paramref name="timeZoneId"/> is empty.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static IcalPeriod Parse(ReadOnlySpan<char> value, string? timeZoneId, IcalValueFormat format)
#else
        public static IcalPeriod Parse(ReadOnlySpan<char> value, string timeZoneId, IcalValueFormat format)
#endif
        {
            IcalPeriod result;
            if (!TryParse(value, timeZoneId, format, out result))
                throw new FormatException("The value is not a valid PERIOD.");

            return result;
        }

        /// <summary>
        /// Formats the period into a span.  The time zone identifier of zoned date-times is not written; it belongs in a <c>TZID</c> parameter.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the period was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="IcalValueFormat.Json"/> or
        /// <see cref="IcalValueFormat.Xml"/>, which are not supported yet.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            var writer = new IcalSpanWriter(destination);
            _start.AppendText(ref writer);
            writer.Append('/');
            if (_hasDuration)
            {
                _duration.AppendText(ref writer);
            }
            else
            {
                _end.AppendText(ref writer);
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the period as a string.  The time zone identifier of zoned date-times is not written.
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
        /// Formats the period in <see cref="IcalValueFormat.Text"/> form.  The time zone identifier of zoned date-times is not written.
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
        /// Indicates whether this period equals another.  A period with an end never equals one with a duration.
        /// </summary>
        public bool Equals(IcalPeriod other)
        {
            return _hasDuration == other._hasDuration && _start == other._start && _end == other._end && _duration == other._duration;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalPeriod other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _start.GetHashCode() * 31 + (_hasDuration ? _duration.GetHashCode() : _end.GetHashCode());
        }

        /// <summary>
        /// Indicates whether two periods are equal.
        /// </summary>
        public static bool operator ==(IcalPeriod left, IcalPeriod right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two periods differ.
        /// </summary>
        public static bool operator !=(IcalPeriod left, IcalPeriod right)
        {
            return !left.Equals(right);
        }

        private static bool IsPositive(IcalDuration duration)
        {
            // ref. RFC 5545§3.3.9: period-start is a start and a positive duration of time.
            return !duration.IsNegative && duration != default(IcalDuration) && duration != IcalDuration.FromWeeks(0);
        }

        private static bool IsEndAfterStart(IcalDateTime start, IcalDateTime end)
        {
            // ref. RFC 5545§3.3.9: the start of the period MUST be before the end.  Only values of the same kind and time zone can be compared
            // without resolving the zone.
            if (start.Kind != end.Kind || !string.Equals(start.TimeZoneId, end.TimeZoneId, StringComparison.Ordinal)) return true;

            return IcalDateTime.CompareClock(start, end) < 0;
        }
    }
}
