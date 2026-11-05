using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A TIMESTAMP value (RFC 6350 §4.3.5): a complete date and a complete time of day with an optional zone, such as <c>19961022T140000</c>, <c>19961022T140000Z</c> or
    /// <c>19961022T140000-05</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// vCard 3.0 and vCard 2.1 have no TIMESTAMP type.  Their <c>REV</c> property is a date-time, in the form <see cref="VcardDateTime"/> describes for them, and this type reads
    /// and writes that form.
    /// </para>
    /// <para>
    /// A default instance is not initialised: <see cref="Date"/>, <see cref="Time"/> and <see cref="IsDefinedIn"/> throw <see cref="InvalidOperationException"/>,
    /// <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// </remarks>
    public readonly struct VcardTimestamp : IEquatable<VcardTimestamp>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const string UninitializedMessage = "The VcardTimestamp instance is not initialised.";

        private readonly VcardDateTime _value;

        /// <summary>
        /// Creates a timestamp.
        /// </summary>
        /// <param name="date">The date, which must have a year, a month and a day.</param>
        /// <param name="time">The time, which must have an hour, a minute and a second.</param>
        /// <exception cref="ArgumentException"><paramref name="date"/> or <paramref name="time"/> is not initialised or not complete.</exception>
        public VcardTimestamp(VcardDate date, VcardTime time)
        {
            if (!date.IsInitialized || !date.IsComplete)
                throw new ArgumentException("The date must have a year, a month and a day.", nameof(date));
            if (!time.IsComplete)
                throw new ArgumentException("The time must have an hour, a minute and a second.", nameof(time));

            _value = new VcardDateTime(date, time);
        }

        private VcardTimestamp(VcardDateTime value)
        {
            _value = value;
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public VcardDate Date
        {
            get
            {
                CheckInitialized();
                return _value.Date;
            }
        }

        /// <summary>
        /// Gets the time.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public VcardTime Time
        {
            get
            {
                CheckInitialized();
                return _value.Time;
            }
        }

        /// <summary>
        /// Gets a flag indicating if a version can write this value.  See <see cref="VcardTime.IsDefinedIn"/>.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        public bool IsDefinedIn(VcardVersion version)
        {
            CheckInitialized();
            return _value.IsDefinedIn(version);
        }

        /// <summary>
        /// Parses a vCard 4.0 timestamp.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The timestamp, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardTimestamp result)
        {
            return TryParse(value, format, VcardVersion.V4_0, out result);
        }

        /// <summary>
        /// Parses a timestamp in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="result">The timestamp, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardTimestamp result)
        {
            VcardValueFormatting.CheckFormat(format);
            VcardValueFormatting.CheckVersion(version);

            // ref. RFC 6350 §4: timestamp = date-complete time-designator time-complete
            result = default(VcardTimestamp);
            VcardDateTime dateTime;
            if (!VcardDateTime.TryParseCore(value, version, out dateTime) || !dateTime.Date.IsComplete || !dateTime.Time.IsComplete) return false;

            result = new VcardTimestamp(dateTime);
            return true;
        }

        /// <summary>
        /// Parses a vCard 4.0 timestamp.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The timestamp.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardTimestamp Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            return Parse(value, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Parses a timestamp in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The timestamp.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardTimestamp Parse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version)
        {
            VcardTimestamp result;
            if (!TryParse(value, format, version, out result))
                throw new FormatException("The value is not a valid TIMESTAMP.");

            return result;
        }

        /// <summary>
        /// Formats the value into a span in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException">vCard 4.0 has no form for this value; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            return _value.TryFormat(destination, out charsWritten, format);
        }

        /// <summary>
        /// Formats the value into a span in the form a version uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this value; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format, VcardVersion version)
        {
            return _value.TryFormat(destination, out charsWritten, format, version);
        }

        /// <summary>
        /// Formats the value as a string in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException">vCard 4.0 has no form for this value; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format)
        {
            return _value.ToString(format);
        }

        /// <summary>
        /// Formats the value as a string in the form a version uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this value; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format, VcardVersion version)
        {
            return _value.ToString(format, version);
        }

        /// <summary>
        /// Formats the value in <see cref="VcardValueFormat.Text"/> form, as vCard 4.0 writes it or, for a fraction only vCard 3.0 has, as vCard 3.0 writes it; or gives the type
        /// name if the instance is not initialised.
        /// </summary>
        public override string ToString()
        {
            return _value.IsInitialized ? _value.ToString() : typeof(VcardTimestamp).FullName ?? nameof(VcardTimestamp);
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
        /// Indicates whether this value equals another: the same date and the same time.
        /// </summary>
        public bool Equals(VcardTimestamp other)
        {
            return _value == other._value;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardTimestamp other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Indicates whether two values are equal.
        /// </summary>
        public static bool operator ==(VcardTimestamp left, VcardTimestamp right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two values differ.
        /// </summary>
        public static bool operator !=(VcardTimestamp left, VcardTimestamp right)
        {
            return !left.Equals(right);
        }

        private void CheckInitialized()
        {
            if (!_value.IsInitialized)
                throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
