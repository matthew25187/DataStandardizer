using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A DURATION value (RFC 5545 §3.3.6): a number of weeks, or a number of days and an exact time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Weeks and days are <em>nominal</em>: added to a date-time, they move it by calendar days, which across a daylight saving change can be
    /// 23 or 25 hours.  Hours, minutes and seconds are <em>exact</em>.  So <c>P1D</c> and <c>PT24H</c> are different durations, and this type
    /// keeps them apart: they are not equal.  For the same reason a duration in weeks (<c>P1W</c>) is not equal to the same number of days
    /// (<c>P7D</c>), although the two have the same effect.
    /// </para>
    /// <para>
    /// The whole duration has one sign, so the parts are all negative or all positive.  A zero duration is never negative.  The exact time is
    /// written in hours, minutes and seconds, so <c>PT90M</c> is written back as <c>PT1H30M</c>.  The default value is zero, written
    /// <c>PT0S</c>.
    /// </para>
    /// </remarks>
    public readonly struct IcalDuration : IEquatable<IcalDuration>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        // A sign, "P", up to 10 digits and "D", "T", and up to 12 digits of hours with 2 each of minutes and seconds and their letters.
        private const int MaxTextLength = 2 + 11 + 1 + 13 + 3 + 3;

        private static readonly long MaxSeconds = TimeSpan.MaxValue.Ticks / TimeSpan.TicksPerSecond;

        private readonly int _weeks;
        private readonly int _days;
        private readonly long _seconds;
        private readonly bool _isNegative;
        private readonly bool _isWeeks;

        /// <summary>
        /// Creates a duration of days and an exact time.
        /// </summary>
        /// <param name="days">The number of nominal days.</param>
        /// <param name="time">The exact time, a whole number of seconds.</param>
        /// <exception cref="ArgumentException"><paramref name="days"/> and <paramref name="time"/> have opposite signs, or
        /// <paramref name="time"/> is not a whole number of seconds.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="days"/> is <see cref="int.MinValue"/>.</exception>
        public IcalDuration(int days, TimeSpan time)
        {
            if (days == int.MinValue)
                throw new ArgumentOutOfRangeException(nameof(days), days, "The number of days is out of range.");
            if (time.Ticks % TimeSpan.TicksPerSecond != 0)
                throw new ArgumentException("The time must be a whole number of seconds.", nameof(time));
            if ((days < 0 && time.Ticks > 0) || (days > 0 && time.Ticks < 0))
                throw new ArgumentException("The days and the time must have the same sign.", nameof(time));

            var seconds = time.Ticks / TimeSpan.TicksPerSecond;
            _isNegative = days < 0 || seconds < 0;
            _weeks = 0;
            _days = Math.Abs(days);
            _seconds = Math.Abs(seconds);
            _isWeeks = false;
        }

        private IcalDuration(bool isNegative, bool isWeeks, int weeks, int days, long seconds)
        {
            _isNegative = isNegative && (weeks != 0 || days != 0 || seconds != 0);
            _isWeeks = isWeeks;
            _weeks = weeks;
            _days = days;
            _seconds = seconds;
        }

        /// <summary>
        /// Gets a flag indicating if the duration is negative.
        /// </summary>
        public bool IsNegative => _isNegative;

        /// <summary>
        /// Gets a flag indicating if the duration is given in weeks.  If so, <see cref="Days"/> and <see cref="Time"/> are zero.
        /// </summary>
        public bool IsWeeks => _isWeeks;

        /// <summary>
        /// Gets the number of nominal weeks, negative if the duration is negative.
        /// </summary>
        public int Weeks => _isNegative ? -_weeks : _weeks;

        /// <summary>
        /// Gets the number of nominal days, negative if the duration is negative.  This does not include <see cref="Weeks"/>.
        /// </summary>
        public int Days => _isNegative ? -_days : _days;

        /// <summary>
        /// Gets the exact time, negative if the duration is negative.
        /// </summary>
        public TimeSpan Time => new TimeSpan((_isNegative ? -_seconds : _seconds) * TimeSpan.TicksPerSecond);

        /// <summary>
        /// Creates a duration of weeks.
        /// </summary>
        /// <param name="weeks">The number of nominal weeks.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="weeks"/> is <see cref="int.MinValue"/>.</exception>
        public static IcalDuration FromWeeks(int weeks)
        {
            if (weeks == int.MinValue)
                throw new ArgumentOutOfRangeException(nameof(weeks), weeks, "The number of weeks is out of range.");

            return new IcalDuration(weeks < 0, true, Math.Abs(weeks), 0, 0);
        }

        /// <summary>
        /// Gets the same duration with the opposite sign.
        /// </summary>
        public IcalDuration Negate()
        {
            return new IcalDuration(!_isNegative, _isWeeks, _weeks, _days, _seconds);
        }

        /// <summary>
        /// Parses a duration.
        /// </summary>
        /// <param name="value">The value, such as <c>P15DT5H0M20S</c>, <c>-PT15M</c> or <c>P7W</c>, which is the same in
        /// <see cref="IcalValueFormat.Text"/>, <see cref="IcalValueFormat.Json"/> and <see cref="IcalValueFormat.Xml"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The duration, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid duration; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalDuration result)
        {
            IcalValueFormatting.CheckFormat(format);

            return TryParseText(value, out result);
        }

        /// <summary>
        /// Parses a duration.
        /// </summary>
        /// <param name="value">The value, such as <c>P15DT5H0M20S</c>, <c>-PT15M</c> or <c>P7W</c>, which is the same in
        /// <see cref="IcalValueFormat.Text"/>, <see cref="IcalValueFormat.Json"/> and <see cref="IcalValueFormat.Xml"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The duration.</returns>
        /// <exception cref="FormatException">The value is not a valid duration.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static IcalDuration Parse(ReadOnlySpan<char> value, IcalValueFormat format)
        {
            IcalDuration result;
            if (!TryParse(value, format, out result))
                throw new FormatException("The value is not a valid DURATION.");

            return result;
        }

        /// <summary>
        /// Formats the duration into a span.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the duration was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            var writer = new IcalSpanWriter(destination);
            AppendText(ref writer);
            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the duration as a string.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public string ToString(IcalValueFormat format)
        {
            var buffer = new char[MaxTextLength];
            int charsWritten;
            return IcalValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format), charsWritten);
        }

        /// <summary>
        /// Formats the duration in <see cref="IcalValueFormat.Text"/> form.
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
        /// Indicates whether this duration equals another.  Nominal and exact parts are compared separately, so <c>P1D</c> does not equal
        /// <c>PT24H</c>, and a duration in weeks does not equal one in days.
        /// </summary>
        public bool Equals(IcalDuration other)
        {
            return _isNegative == other._isNegative && _isWeeks == other._isWeeks && _weeks == other._weeks && _days == other._days &&
                   _seconds == other._seconds;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalDuration other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = _weeks * 31 + _days;
            hashCode = hashCode * 31 + _seconds.GetHashCode();
            return hashCode * 4 + (_isNegative ? 2 : 0) + (_isWeeks ? 1 : 0);
        }

        /// <summary>
        /// Indicates whether two durations are equal.
        /// </summary>
        public static bool operator ==(IcalDuration left, IcalDuration right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two durations differ.
        /// </summary>
        public static bool operator !=(IcalDuration left, IcalDuration right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Parses the text form, which must be exactly the length of <paramref name="value"/>.
        /// </summary>
        internal static bool TryParseText(ReadOnlySpan<char> value, out IcalDuration result)
        {
            // ref. RFC 5545§3.3.6:
            //   dur-value  = (["+"] / "-") "P" (dur-date / dur-time / dur-week)
            //   dur-date   = dur-day [dur-time]
            //   dur-time   = "T" (dur-hour / dur-minute / dur-second)
            //   dur-week   = 1*DIGIT "W"
            //   dur-hour   = 1*DIGIT "H" [dur-minute]
            //   dur-minute = 1*DIGIT "M" [dur-second]
            //   dur-second = 1*DIGIT "S"
            //   dur-day    = 1*DIGIT "D"
            result = default(IcalDuration);
            var index = 0;
            var isNegative = false;
            if (index < value.Length && (value[index] == '+' || value[index] == '-'))
            {
                isNegative = value[index] == '-';
                index++;
            }

            if (index >= value.Length || !IcalValueFormatting.IsLetter(value[index], 'P')) return false;

            index++;
            long number;
            var days = 0L;
            if (index < value.Length && IcalValueFormatting.IsDigit(value[index]))
            {
                if (!IcalValueFormatting.TryParseNumber(value, ref index, int.MaxValue, out number) || index >= value.Length) return false;

                if (IcalValueFormatting.IsLetter(value[index], 'W'))
                {
                    if (index + 1 != value.Length) return false;

                    result = new IcalDuration(isNegative, true, (int)number, 0, 0);
                    return true;
                }

                if (!IcalValueFormatting.IsLetter(value[index], 'D')) return false;

                days = number;
                index++;
                if (index == value.Length)
                {
                    result = new IcalDuration(isNegative, false, 0, (int)days, 0);
                    return true;
                }
            }

            if (index >= value.Length || !IcalValueFormatting.IsLetter(value[index], 'T')) return false;

            index++;
            long seconds;
            if (!TryParseTime(value, index, out seconds)) return false;

            result = new IcalDuration(isNegative, false, 0, (int)days, seconds);
            return true;
        }

        internal void AppendText(ref IcalSpanWriter writer)
        {
            if (_isNegative)
            {
                writer.Append('-');
            }

            writer.Append('P');
            if (_isWeeks)
            {
                writer.AppendNumber(_weeks);
                writer.Append('W');
                return;
            }

            if (_days != 0)
            {
                writer.AppendNumber(_days);
                writer.Append('D');
                if (_seconds == 0) return;
            }

            // ref. RFC 5545§3.3.6: a part can be left out only at the start or the end of the time, so a zero minute between an hour and a
            // second is written.
            writer.Append('T');
            var hours = _seconds / 3600;
            var minutes = _seconds / 60 % 60;
            var seconds = _seconds % 60;
            if (hours != 0)
            {
                writer.AppendNumber(hours);
                writer.Append('H');
            }

            if (minutes != 0 || (hours != 0 && seconds != 0))
            {
                writer.AppendNumber(minutes);
                writer.Append('M');
            }

            if (seconds != 0 || _seconds == 0)
            {
                writer.AppendNumber(seconds);
                writer.Append('S');
            }
        }

        /// <summary>
        /// Parses the part of a duration after the "T", to the end of the value.
        /// </summary>
        private static bool TryParseTime(ReadOnlySpan<char> value, int index, out long totalSeconds)
        {
            totalSeconds = 0;

            // Each designator may follow only those before it, with no gaps: H, then M, then S.
            const string designators = "HMS";
            var nextDesignator = 0;
            while (index < value.Length)
            {
                long number;
                if (!IcalValueFormatting.TryParseNumber(value, ref index, MaxSeconds, out number) || index >= value.Length) return false;

                var designator = -1;
                for (var candidate = nextDesignator; candidate < designators.Length; candidate++)
                {
                    if (IcalValueFormatting.IsLetter(value[index], designators[candidate]))
                    {
                        designator = candidate;
                        break;
                    }
                }

                // A part can be skipped only before the first one given.
                if (designator < 0 || (nextDesignator > 0 && designator != nextDesignator)) return false;

                var multiplier = designator == 0 ? 3600L : designator == 1 ? 60L : 1L;
                if (number > (MaxSeconds - totalSeconds) / multiplier) return false;

                totalSeconds += number * multiplier;
                nextDesignator = designator + 1;
                index++;
            }

            // dur-time needs at least one part.
            return nextDesignator > 0;
        }
    }
}
