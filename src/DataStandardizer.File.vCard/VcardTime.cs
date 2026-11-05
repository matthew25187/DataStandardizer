using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A TIME value (RFC 6350 §4.3.2): a time of day, which may leave out its hour, its minute or its second, with an optional zone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// vCard 4.0 allows <c>102200</c>, <c>1022</c> and <c>10</c>, and the truncated forms <c>-2200</c>, <c>-22</c> and <c>--00</c>, each followed by nothing, <c>Z</c> or a UTC
    /// offset, all in the basic format of ISO 8601.  It forbids a decimal fraction.  RFC 2425 §5.8.4 gives vCard 3.0 the complete time alone, with optional colons, an optional
    /// fraction of a second such as <c>10:22:00,5</c>, and an optional zone.  vCard 2.1 uses a complete time in either the basic or the extended format of ISO 8601.
    /// </para>
    /// <para>
    /// A second of 60 is accepted at any time of day, since whether a leap second falls at a given local time can't be told from the value.  An hour of 24 is not: RFC 6350
    /// §4.3.2 says midnight is always 00.
    /// </para>
    /// <para>
    /// Two times are equal if they say the same thing the same way, not if they are the same instant: <c>10Z</c> and <c>10+00</c> differ.  A fraction of more than 18 digits
    /// keeps its first 18.
    /// </para>
    /// <para>
    /// A default instance is not initialised: its properties and <see cref="IsDefinedIn"/> throw <see cref="InvalidOperationException"/>,
    /// <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// </remarks>
    public readonly struct VcardTime : IEquatable<VcardTime>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const string UninitializedMessage = "The VcardTime instance is not initialised.";
        private const int MaxHour = 23;
        private const int MaxMinute = 59;
        private const int MaxSecond = 60;
        private const int MaxFractionDigits = 18;
        private const int None = -1;

        // The longest form is "10:22:00,", the fraction, and "-05:00".
        internal const int MaxLength = 9 + MaxFractionDigits + VcardUtcOffset.MaxLength;

        // Each is the number plus one, so that the default value has none.
        private readonly byte _hourPlusOne;
        private readonly byte _minutePlusOne;
        private readonly byte _secondPlusOne;
        private readonly bool _isUtc;
        private readonly byte _fractionDigitCount;
        private readonly long _fraction;
        private readonly VcardUtcOffset _offset;

        /// <summary>
        /// Creates a time with no zone.
        /// </summary>
        /// <param name="hour">The hour, from 0 to 23; or <c>null</c> to leave it out.</param>
        /// <param name="minute">The minute, from 0 to 59; or <c>null</c> to leave it out.</param>
        /// <param name="second">The second, from 0 to 60; or <c>null</c> to leave it out.</param>
        /// <exception cref="ArgumentOutOfRangeException">The hour, minute or second is out of range.</exception>
        /// <exception cref="ArgumentException">RFC 6350 §4 has no form for that combination: there is nothing, or a second and an hour but no minute.</exception>
        public VcardTime(int? hour, int? minute, int? second)
            : this(hour, minute, second, false)
        {
        }

        /// <summary>
        /// Creates a time with a UTC offset as its zone.
        /// </summary>
        /// <param name="hour">The hour, from 0 to 23; or <c>null</c> to leave it out.</param>
        /// <param name="minute">The minute, from 0 to 59; or <c>null</c> to leave it out.</param>
        /// <param name="second">The second, from 0 to 60; or <c>null</c> to leave it out.</param>
        /// <param name="offset">The offset.</param>
        /// <exception cref="ArgumentOutOfRangeException">The hour, minute or second is out of range.</exception>
        /// <exception cref="ArgumentException">RFC 6350 §4 has no form for that combination, or <paramref name="offset"/> is not initialised.</exception>
        public VcardTime(int? hour, int? minute, int? second, VcardUtcOffset offset)
            : this(hour, minute, second, false)
        {
            if (!offset.IsInitialized)
                throw new ArgumentException("The offset is not initialised.", nameof(offset));

            _offset = offset;
        }

        /// <summary>
        /// Creates a time that may be in UTC.
        /// </summary>
        /// <param name="hour">The hour, from 0 to 23; or <c>null</c> to leave it out.</param>
        /// <param name="minute">The minute, from 0 to 59; or <c>null</c> to leave it out.</param>
        /// <param name="second">The second, from 0 to 60; or <c>null</c> to leave it out.</param>
        /// <param name="isUtc"><c>true</c> to write the zone as <c>Z</c>; <c>false</c> for no zone.</param>
        /// <exception cref="ArgumentOutOfRangeException">The hour, minute or second is out of range.</exception>
        /// <exception cref="ArgumentException">RFC 6350 §4 has no form for that combination.</exception>
        public VcardTime(int? hour, int? minute, int? second, bool isUtc)
        {
            if (hour < 0 || hour > MaxHour)
                throw new ArgumentOutOfRangeException(nameof(hour), hour, "Hour must be in the range 0 - 23.");
            if (minute < 0 || minute > MaxMinute)
                throw new ArgumentOutOfRangeException(nameof(minute), minute, "Minute must be in the range 0 - 59.");
            if (second < 0 || second > MaxSecond)
                throw new ArgumentOutOfRangeException(nameof(second), second, "Second must be in the range 0 - 60.");
            if ((!hour.HasValue && !minute.HasValue && !second.HasValue) || (hour.HasValue && !minute.HasValue && second.HasValue))
                throw new ArgumentException("A time has an hour, a minute or a second, and an hour and a second have a minute between them.", nameof(second));

            _hourPlusOne = hour.HasValue ? (byte)(hour.Value + 1) : (byte)0;
            _minutePlusOne = minute.HasValue ? (byte)(minute.Value + 1) : (byte)0;
            _secondPlusOne = second.HasValue ? (byte)(second.Value + 1) : (byte)0;
            _isUtc = isUtc;
            _fractionDigitCount = 0;
            _fraction = 0;
            _offset = default(VcardUtcOffset);
        }

        private VcardTime(int hour, int minute, int second, long fraction, int fractionDigitCount, bool isUtc, VcardUtcOffset offset)
        {
            _hourPlusOne = (byte)(hour + 1);
            _minutePlusOne = (byte)(minute + 1);
            _secondPlusOne = (byte)(second + 1);
            _isUtc = isUtc;
            _fractionDigitCount = (byte)fractionDigitCount;
            _fraction = fraction;
            _offset = offset;
        }

        /// <summary>
        /// Gets the hour; or <c>null</c> if the time leaves it out.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public int? Hour
        {
            get
            {
                CheckInitialized();
                return _hourPlusOne == 0 ? (int?)null : _hourPlusOne - 1;
            }
        }

        /// <summary>
        /// Gets the minute; or <c>null</c> if the time leaves it out.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public int? Minute
        {
            get
            {
                CheckInitialized();
                return _minutePlusOne == 0 ? (int?)null : _minutePlusOne - 1;
            }
        }

        /// <summary>
        /// Gets the second; or <c>null</c> if the time leaves it out.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public int? Second
        {
            get
            {
                CheckInitialized();
                return _secondPlusOne == 0 ? (int?)null : _secondPlusOne - 1;
            }
        }

        /// <summary>
        /// Gets the fraction of a second, such as 0.5 for <c>10:22:00,5</c>; or <c>null</c> if the time has none.  Only RFC 2425 has one.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public decimal? SecondFraction
        {
            get
            {
                CheckInitialized();
                return _fractionDigitCount == 0 ? (decimal?)null : new decimal((int)(_fraction & 0xFFFFFFFF), (int)(_fraction >> 32), 0, false, _fractionDigitCount);
            }
        }

        /// <summary>
        /// Gets a flag indicating if the zone is written <c>Z</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public bool IsUtc
        {
            get
            {
                CheckInitialized();
                return _isUtc;
            }
        }

        /// <summary>
        /// Gets the zone, if it is a UTC offset; otherwise <c>null</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public VcardUtcOffset? UtcOffset
        {
            get
            {
                CheckInitialized();
                return _offset.IsInitialized ? _offset : (VcardUtcOffset?)null;
            }
        }

        /// <summary>
        /// Gets a flag indicating if a version can write this time.  vCard 4.0 and vCard 2.1 have no fraction, and vCard 3.0 and vCard 2.1 have only complete times.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        public bool IsDefinedIn(VcardVersion version)
        {
            CheckInitialized();
            VcardValueFormatting.CheckVersion(version);

            switch (version)
            {
                case VcardVersion.V3_0:
                    return IsComplete;
                case VcardVersion.V2_1:
                    return IsComplete && _fractionDigitCount == 0;
                default:
                    return _fractionDigitCount == 0;
            }
        }

        /// <summary>
        /// Parses a vCard 4.0 time, such as <c>102200</c>, <c>1022</c>, <c>10</c>, <c>-2200</c>, <c>--00</c>, <c>102200Z</c> or <c>102200-0800</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The time, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid time; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardTime result)
        {
            return TryParse(value, format, VcardVersion.V4_0, out result);
        }

        /// <summary>
        /// Parses a time in the form a version uses.
        /// </summary>
        /// <param name="value">The value: in vCard 3.0 and vCard 2.1 a complete time such as <c>10:22:00</c>, <c>102200</c> or <c>10:22:00-05:00</c>.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="result">The time, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid time; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardTime result)
        {
            VcardValueFormatting.CheckFormat(format);
            VcardValueFormatting.CheckVersion(version);

            return version == VcardVersion.V4_0 ? TryParseVersion40(value, out result) : TryParseLegacy(value, version, out result);
        }

        /// <summary>
        /// Parses a vCard 4.0 time.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The time.</returns>
        /// <exception cref="FormatException">The value is not a valid time.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardTime Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            return Parse(value, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Parses a time in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The time.</returns>
        /// <exception cref="FormatException">The value is not a valid time.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardTime Parse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version)
        {
            VcardTime result;
            if (!TryParse(value, format, version, out result))
                throw new FormatException("The value is not a valid TIME.");

            return result;
        }

        /// <summary>
        /// Formats the time into a span in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the time was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException">vCard 4.0 has no form for this time; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            return TryFormat(destination, out charsWritten, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the time into a span in the form a version uses: the basic format in vCard 4.0 and the extended format, <c>10:22:00</c>, in vCard 3.0 and vCard 2.1.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns><c>true</c> if the time was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this time; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format, VcardVersion version)
        {
            VcardValueFormatting.CheckFormat(format);
            VcardValueFormatting.CheckVersion(version);

            if (!IsInitialized)
            {
                charsWritten = 0;
                return true;
            }

            if (!IsDefinedIn(version))
                throw VcardValueFormatting.BuildNotDefinedException("TIME", version);

            var writer = new VcardSpanWriter(destination);
            Append(ref writer, version);
            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the time as a string in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException">vCard 4.0 has no form for this time; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format)
        {
            return ToString(format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the time as a string in the form a version uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this time; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format, VcardVersion version)
        {
            var buffer = new char[MaxLength];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format, version), charsWritten);
        }

        /// <summary>
        /// Formats the time in <see cref="VcardValueFormat.Text"/> form, as vCard 4.0 writes it or, for a fraction only vCard 3.0 has, as vCard 3.0 writes it; or gives the type
        /// name if the instance is not initialised.
        /// </summary>
        public override string ToString()
        {
            if (!IsInitialized) return typeof(VcardTime).FullName ?? nameof(VcardTime);

            return ToString(VcardValueFormat.Text, _fractionDigitCount == 0 ? VcardVersion.V4_0 : VcardVersion.V3_0);
        }

#if NET6_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The format is empty or <c>T</c> for <see cref="VcardValueFormat.Text"/>, <c>J</c> for <see cref="VcardValueFormat.Json"/> or <c>X</c> for
        /// <see cref="VcardValueFormat.Xml"/>.  The provider is ignored.</remarks>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return TryFormat(destination, out charsWritten, VcardValueFormatting.ParseFormatString(format));
        }

        /// <inheritdoc />
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString(VcardValueFormatting.ParseFormatString(format));
        }
#endif

        /// <summary>
        /// Indicates whether this time equals another: the same parts and the same zone, written the same way.
        /// </summary>
        public bool Equals(VcardTime other)
        {
            return _hourPlusOne == other._hourPlusOne && _minutePlusOne == other._minutePlusOne && _secondPlusOne == other._secondPlusOne &&
                   _isUtc == other._isUtc && _fractionDigitCount == other._fractionDigitCount && _fraction == other._fraction && _offset == other._offset;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardTime other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (_hourPlusOne | (_minutePlusOne << 8) | (_secondPlusOne << 16) | (_isUtc ? 1 << 24 : 0)) ^ _fraction.GetHashCode() ^ _offset.GetHashCode();
        }

        /// <summary>
        /// Indicates whether two times are equal.
        /// </summary>
        public static bool operator ==(VcardTime left, VcardTime right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two times differ.
        /// </summary>
        public static bool operator !=(VcardTime left, VcardTime right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Gets whether this instance has been initialised.
        /// </summary>
        internal bool IsInitialized => _hourPlusOne != 0 || _minutePlusOne != 0 || _secondPlusOne != 0;

        /// <summary>
        /// Gets whether the time starts with its hour: the form <c>time-notrunc</c> (RFC 6350 §4).
        /// </summary>
        internal bool IsNotTruncated => _hourPlusOne != 0;

        /// <summary>
        /// Gets whether the time has an hour, a minute and a second: the form <c>time-complete</c> (RFC 6350 §4).
        /// </summary>
        internal bool IsComplete => _hourPlusOne != 0 && _minutePlusOne != 0 && _secondPlusOne != 0;

        /// <summary>
        /// Parses the whole value as a vCard 4.0 <c>time</c>.
        /// </summary>
        internal static bool TryParseVersion40(ReadOnlySpan<char> value, out VcardTime result)
        {
            // ref. RFC 6350 §4: time = hour [minute [second]] [zone] / "-" minute [second] [zone] / "-" "-" second [zone]
            result = default(VcardTime);
            int hour = None, minute = None, second = None;
            var index = 0;
            if (value.Length >= 2 && value[0] == '-' && value[1] == '-')
            {
                index = 2;
                if (!TryReadPart(value, ref index, out second)) return false;
            }
            else
            {
                if (value.Length >= 1 && value[0] == '-')
                {
                    index = 1;
                    if (!TryReadPart(value, ref index, out minute)) return false;
                }
                else
                {
                    if (!TryReadPart(value, ref index, out hour)) return false;
                    if (IsDigitAt(value, index) && !TryReadPart(value, ref index, out minute)) return false;
                }

                if (minute != None && IsDigitAt(value, index) && !TryReadPart(value, ref index, out second)) return false;
            }

            if (hour > MaxHour || minute > MaxMinute || second > MaxSecond) return false;

            // ref. RFC 6350 §4: zone = utc-designator / utc-offset; utc-designator = %x5A; utc-offset = sign hour [minute]
            var isUtc = false;
            var offset = default(VcardUtcOffset);
            var zone = value.Slice(index);
            if (zone.Length == 1 && zone[0] == 'Z')
            {
                isUtc = true;
            }
            else if (zone.Length != 0 && !VcardUtcOffset.TryParseForm(zone, true, true, false, VcardUtcOffset.MaxHours, out offset))
            {
                return false;
            }

            result = new VcardTime(hour, minute, second, 0, 0, isUtc, offset);
            return true;
        }

        /// <summary>
        /// Parses the whole value as a complete time of vCard 3.0 or vCard 2.1.
        /// </summary>
        internal static bool TryParseLegacy(ReadOnlySpan<char> value, VcardVersion version, out VcardTime result)
        {
            // ref. RFC 2425 §5.8.4: time = time-hour [":"] time-minute [":"] time-second [time-secfrac] [time-zone]; time-secfrac = "," 1*DIGIT
            // ref. vCard 2.1 §2.6.2: the basic or extended format of ISO 8601.
            result = default(VcardTime);
            int hour, minute, second;
            var index = 0;
            if (!TryReadPart(value, ref index, out hour)) return false;

            var hasFirstColon = index < value.Length && value[index] == ':';
            if (hasFirstColon)
            {
                index++;
            }

            if (!TryReadPart(value, ref index, out minute)) return false;

            var hasSecondColon = index < value.Length && value[index] == ':';
            if (hasSecondColon)
            {
                index++;
            }

            if (version == VcardVersion.V2_1 && hasFirstColon != hasSecondColon) return false;
            if (!TryReadPart(value, ref index, out second)) return false;
            if (hour > MaxHour || minute > MaxMinute || second > MaxSecond) return false;

            long fraction = 0;
            var fractionDigitCount = 0;
            if (version == VcardVersion.V3_0 && index < value.Length && value[index] == ',')
            {
                var fractionStart = ++index;
                while (IsDigitAt(value, index))
                {
                    if (fractionDigitCount < MaxFractionDigits)
                    {
                        fraction = fraction * 10 + (value[index] - '0');
                        fractionDigitCount++;
                    }

                    index++;
                }

                if (index == fractionStart) return false;
            }

            var isUtc = false;
            var offset = default(VcardUtcOffset);
            var zone = value.Slice(index);
            if (zone.Length == 1 && zone[0] == 'Z')
            {
                isUtc = true;
            }
            else if (zone.Length != 0 && !TryParseLegacyZone(zone, version, hasFirstColon, out offset))
            {
                return false;
            }

            result = new VcardTime(hour, minute, second, fraction, fractionDigitCount, isUtc, offset);
            return true;
        }

        /// <summary>
        /// Appends the time in the form a version uses.  The caller has checked that the version can write it.
        /// </summary>
        internal void Append(ref VcardSpanWriter writer, VcardVersion version)
        {
            if (version == VcardVersion.V4_0)
            {
                if (_hourPlusOne != 0)
                {
                    writer.AppendDigits(_hourPlusOne - 1, 2);
                }
                else
                {
                    writer.Append('-');
                }

                if (_minutePlusOne != 0)
                {
                    writer.AppendDigits(_minutePlusOne - 1, 2);
                }
                else if (_hourPlusOne == 0)
                {
                    writer.Append('-');
                }

                if (_secondPlusOne != 0)
                {
                    writer.AppendDigits(_secondPlusOne - 1, 2);
                }
            }
            else
            {
                writer.AppendDigits(_hourPlusOne - 1, 2);
                writer.Append(':');
                writer.AppendDigits(_minutePlusOne - 1, 2);
                writer.Append(':');
                writer.AppendDigits(_secondPlusOne - 1, 2);
                if (_fractionDigitCount != 0)
                {
                    writer.Append(',');
                    writer.AppendDigits(_fraction, _fractionDigitCount);
                }
            }

            if (_isUtc)
            {
                writer.Append('Z');
            }
            else if (_offset.IsInitialized)
            {
                _offset.Append(ref writer, version);
            }
        }

        private static bool TryParseLegacyZone(ReadOnlySpan<char> zone, VcardVersion version, bool isExtended, out VcardUtcOffset offset)
        {
            // ref. RFC 2425 §5.8.4: time-numzone = sign time-hour [":"] time-minute
            // ref. vCard 2.1 §2.4.5: +hhmm or +hh in the basic format, and with a colon in the extended format.
            return version == VcardVersion.V3_0
                ? VcardUtcOffset.TryParseForm(zone, false, true, true, VcardUtcOffset.MaxHours, out offset)
                : VcardUtcOffset.TryParseForm(zone, true, !isExtended, isExtended, VcardUtcOffset.MaxHours, out offset);
        }

        private static bool TryReadPart(ReadOnlySpan<char> value, ref int index, out int part)
        {
            if (!VcardValueFormatting.TryParseDigits(value, index, 2, out part))
            {
                part = None;
                return false;
            }

            index += 2;
            return true;
        }

        private static bool IsDigitAt(ReadOnlySpan<char> value, int index)
        {
            return index < value.Length && VcardValueFormatting.IsDigit(value[index]);
        }

        private void CheckInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
