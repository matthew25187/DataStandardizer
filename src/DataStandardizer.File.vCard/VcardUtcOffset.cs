using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A UTC-OFFSET value (RFC 6350 §4.7): a signed difference from UTC in hours and, optionally, minutes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An offset keeps what was written: <c>-05</c> and <c>-0500</c> are different values that have the same <see cref="Offset"/>, and <c>-00</c> and <c>+00</c>
    /// differ by <see cref="IsNegative"/>.  RFC 6350 doesn't forbid a negative zero.
    /// </para>
    /// <para>
    /// The forms differ by version.  vCard 4.0 uses <c>-05</c> or <c>-0500</c> (RFC 6350 §4).  vCard 3.0 uses only <c>-05:00</c> (RFC 2426 §2.4.4).  vCard 2.1 allows all
    /// three, and hours up to 24 (vCard 2.1 §2.4.5).
    /// </para>
    /// <para>
    /// A default instance is not initialised: <see cref="IsNegative"/>, <see cref="Hours"/>, <see cref="Minutes"/>, <see cref="Offset"/> and
    /// <see cref="IsDefinedIn"/> throw <see cref="InvalidOperationException"/>, <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and
    /// <see cref="ToString()"/> gives the type name.
    /// </para>
    /// </remarks>
    public readonly struct VcardUtcOffset : IEquatable<VcardUtcOffset>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const string UninitializedMessage = "The VcardUtcOffset instance is not initialised.";

        // The longest form is "-05:00".
        internal const int MaxLength = 6;

        // ref. RFC 6350 §4.7: hour values are from 00 to 23.
        internal const int MaxHours = 23;

        // ref. vCard 2.1 §2.4.5: "Hour values are from 00 to 24".
        private const int MaxHoursVersion21 = 24;
        private const int MaxMinutes = 59;

        // Each is the number plus one, so that the default value has neither.
        private readonly byte _hoursPlusOne;
        private readonly byte _minutesPlusOne;
        private readonly bool _isNegative;

        /// <summary>
        /// Creates an offset.
        /// </summary>
        /// <param name="isNegative"><c>true</c> if the offset is behind UTC.</param>
        /// <param name="hours">The hours, from 0 to 23.</param>
        /// <param name="minutes">The minutes, from 0 to 59; or <c>null</c> to write only the hours.</param>
        /// <exception cref="ArgumentOutOfRangeException">The hours or minutes are out of range.</exception>
        public VcardUtcOffset(bool isNegative, int hours, int? minutes)
        {
            if (hours < 0 || hours > MaxHours)
                throw new ArgumentOutOfRangeException(nameof(hours), hours, "Hours must be in the range 0 - 23.");
            if (minutes < 0 || minutes > MaxMinutes)
                throw new ArgumentOutOfRangeException(nameof(minutes), minutes, "Minutes must be in the range 0 - 59.");

            _isNegative = isNegative;
            _hoursPlusOne = (byte)(hours + 1);
            _minutesPlusOne = minutes.HasValue ? (byte)(minutes.Value + 1) : (byte)0;
        }

        private VcardUtcOffset(bool isNegative, byte hoursPlusOne, byte minutesPlusOne)
        {
            _isNegative = isNegative;
            _hoursPlusOne = hoursPlusOne;
            _minutesPlusOne = minutesPlusOne;
        }

        /// <summary>
        /// Gets a flag indicating if the offset is written with a minus sign.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public bool IsNegative
        {
            get
            {
                CheckInitialized();
                return _isNegative;
            }
        }

        /// <summary>
        /// Gets the hours, with no sign.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public int Hours
        {
            get
            {
                CheckInitialized();
                return _hoursPlusOne - 1;
            }
        }

        /// <summary>
        /// Gets the minutes, with no sign; or <c>null</c> if the offset gives only hours.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public int? Minutes
        {
            get
            {
                CheckInitialized();
                return _minutesPlusOne == 0 ? (int?)null : _minutesPlusOne - 1;
            }
        }

        /// <summary>
        /// Gets the offset.  Positive offsets are ahead of UTC.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public TimeSpan Offset
        {
            get
            {
                CheckInitialized();
                var offset = new TimeSpan(_hoursPlusOne - 1, _minutesPlusOne == 0 ? 0 : _minutesPlusOne - 1, 0);
                return _isNegative ? offset.Negate() : offset;
            }
        }

        /// <summary>
        /// Gets a flag indicating if a version can write this offset.  Only vCard 2.1 has an hour of 24.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        public bool IsDefinedIn(VcardVersion version)
        {
            CheckInitialized();
            VcardValueFormatting.CheckVersion(version);

            return version == VcardVersion.V2_1 || _hoursPlusOne - 1 <= MaxHours;
        }

        /// <summary>
        /// Parses a vCard 4.0 offset, such as <c>-0500</c> or <c>+01</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The offset, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid offset; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardUtcOffset result)
        {
            return TryParse(value, format, VcardVersion.V4_0, out result);
        }

        /// <summary>
        /// Parses an offset in the form a version uses.
        /// </summary>
        /// <param name="value">The value: <c>-05</c> or <c>-0500</c> in vCard 4.0, <c>-05:00</c> in vCard 3.0, and any of them in vCard 2.1.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="result">The offset, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid offset; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardUtcOffset result)
        {
            VcardValueFormatting.CheckFormat(format);
            VcardValueFormatting.CheckVersion(version);

            // ref. RFC 6350 §4: utc-offset = sign hour [minute]
            // ref. RFC 2426 §2.4.4: utc-offset = ("+" / "-") time-hour ":" time-minute
            // ref. vCard 2.1 §2.4.5: +hhmm, +hh, or the extended format with a colon.
            switch (version)
            {
                case VcardVersion.V3_0:
                    return TryParseForm(value, false, false, true, MaxHours, out result);
                case VcardVersion.V2_1:
                    return TryParseForm(value, true, true, true, MaxHoursVersion21, out result);
                default:
                    return TryParseForm(value, true, true, false, MaxHours, out result);
            }
        }

        /// <summary>
        /// Parses a vCard 4.0 offset, such as <c>-0500</c> or <c>+01</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The offset.</returns>
        /// <exception cref="FormatException">The value is not a valid offset.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardUtcOffset Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            return Parse(value, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Parses an offset in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The offset.</returns>
        /// <exception cref="FormatException">The value is not a valid offset.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardUtcOffset Parse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version)
        {
            VcardUtcOffset result;
            if (!TryParse(value, format, version, out result))
                throw new FormatException("The value is not a valid UTC-OFFSET.");

            return result;
        }

        /// <summary>
        /// Formats the offset into a span in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the offset was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException">vCard 4.0 has no form for this offset; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            return TryFormat(destination, out charsWritten, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the offset into a span in the form a version uses: <c>-05</c> or <c>-0500</c> in vCard 4.0, always <c>-05:00</c> in vCard 3.0, and <c>-05</c> or
        /// <c>-05:00</c> in vCard 2.1.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns><c>true</c> if the offset was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this offset; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format, VcardVersion version)
        {
            VcardValueFormatting.CheckFormat(format);
            VcardValueFormatting.CheckVersion(version);

            if (_hoursPlusOne == 0)
            {
                charsWritten = 0;
                return true;
            }

            if (!IsDefinedIn(version))
                throw VcardValueFormatting.BuildNotDefinedException("UTC-OFFSET", version);

            var writer = new VcardSpanWriter(destination);
            Append(ref writer, version);
            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the offset as a string in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException">vCard 4.0 has no form for this offset; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format)
        {
            return ToString(format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the offset as a string in the form a version uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this offset; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format, VcardVersion version)
        {
            var buffer = new char[MaxLength];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format, version), charsWritten);
        }

        /// <summary>
        /// Formats the offset in <see cref="VcardValueFormat.Text"/> form, as vCard 4.0 writes it or, for an hour only vCard 2.1 has, as vCard 2.1 writes it; or gives the type
        /// name if the instance is not initialised.
        /// </summary>
        public override string ToString()
        {
            if (_hoursPlusOne == 0) return typeof(VcardUtcOffset).FullName ?? nameof(VcardUtcOffset);

            return ToString(VcardValueFormat.Text, IsDefinedIn(VcardVersion.V4_0) ? VcardVersion.V4_0 : VcardVersion.V2_1);
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
        /// Indicates whether this offset equals another: the same sign, hours and minutes, each present or left out alike.
        /// </summary>
        public bool Equals(VcardUtcOffset other)
        {
            return _hoursPlusOne == other._hoursPlusOne && _minutesPlusOne == other._minutesPlusOne && _isNegative == other._isNegative;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardUtcOffset other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _hoursPlusOne | (_minutesPlusOne << 8) | (_isNegative ? 1 << 16 : 0);
        }

        /// <summary>
        /// Indicates whether two offsets are equal.
        /// </summary>
        public static bool operator ==(VcardUtcOffset left, VcardUtcOffset right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two offsets differ.
        /// </summary>
        public static bool operator !=(VcardUtcOffset left, VcardUtcOffset right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Gets whether this instance has been initialised.
        /// </summary>
        internal bool IsInitialized => _hoursPlusOne != 0;

        /// <summary>
        /// Parses a whole value in the forms allowed.
        /// </summary>
        /// <param name="value">The value, starting with its sign.</param>
        /// <param name="allowHoursOnly"><c>true</c> to allow <c>-05</c>.</param>
        /// <param name="allowBasic"><c>true</c> to allow <c>-0500</c>.</param>
        /// <param name="allowExtended"><c>true</c> to allow <c>-05:00</c>.</param>
        /// <param name="maximumHours">The largest hour allowed.</param>
        /// <param name="result">The offset, if the value is valid.</param>
        internal static bool TryParseForm(ReadOnlySpan<char> value, bool allowHoursOnly, bool allowBasic, bool allowExtended, int maximumHours, out VcardUtcOffset result)
        {
            result = default(VcardUtcOffset);
            if (value.Length < 3 || (value[0] != '+' && value[0] != '-')) return false;

            int hours, minutes = -1;
            if (!VcardValueFormatting.TryParseDigits(value, 1, 2, out hours)) return false;

            switch (value.Length)
            {
                case 3:
                    if (!allowHoursOnly) return false;
                    break;
                case 5:
                    if (!allowBasic || !VcardValueFormatting.TryParseDigits(value, 3, 2, out minutes)) return false;
                    break;
                case 6:
                    if (!allowExtended || value[3] != ':' || !VcardValueFormatting.TryParseDigits(value, 4, 2, out minutes)) return false;
                    break;
                default:
                    return false;
            }

            if (hours > maximumHours || minutes > MaxMinutes) return false;

            result = new VcardUtcOffset(value[0] == '-', (byte)(hours + 1), (byte)(minutes + 1));
            return true;
        }

        /// <summary>
        /// Appends the offset in the form a version uses.  The caller has checked that the instance is initialised and the version can write it.
        /// </summary>
        internal void Append(ref VcardSpanWriter writer, VcardVersion version)
        {
            writer.Append(_isNegative ? '-' : '+');
            writer.AppendDigits(_hoursPlusOne - 1, 2);
            if (version == VcardVersion.V4_0)
            {
                if (_minutesPlusOne != 0)
                {
                    writer.AppendDigits(_minutesPlusOne - 1, 2);
                }

                return;
            }

            // ref. RFC 2426 §2.4.4: vCard 3.0 always has the minutes.  vCard 2.1 may leave them out.
            if (_minutesPlusOne != 0 || version == VcardVersion.V3_0)
            {
                writer.Append(':');
                writer.AppendDigits(_minutesPlusOne == 0 ? 0 : _minutesPlusOne - 1, 2);
            }
        }

        private void CheckInitialized()
        {
            if (_hoursPlusOne == 0)
                throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
