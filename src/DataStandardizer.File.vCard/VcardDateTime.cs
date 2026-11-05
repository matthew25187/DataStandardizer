using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A DATE-TIME value (RFC 6350 §4.3.3): a date and a time of day, such as <c>19961022T140000</c>, <c>--1022T1400</c> or <c>---22T14</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In vCard 4.0 the date may be truncated but not reduced (<c>date-noreduc</c>) and the time may be reduced but not truncated (<c>time-notrunc</c>).  vCard 3.0 and vCard 2.1
    /// have a complete date and a complete time; see <see cref="VcardDate"/> and <see cref="VcardTime"/>.  vCard 2.1 writes both in the same ISO 8601 format.
    /// </para>
    /// <para>
    /// In jCard (RFC 7095 §3.5.5), the <see cref="VcardValueFormat.Json"/> form, the date and the time of a vCard 4.0 value are in the extended format where it applies, such as
    /// <c>1996-10-22T14:00:00</c>, <c>--10-22T14:00</c> or <c>---22T14</c>.  jCard represents vCard 4.0 alone, so that form has no other version.
    /// </para>
    /// <para>
    /// A default instance is not initialised: <see cref="Date"/>, <see cref="Time"/> and <see cref="IsDefinedIn"/> throw <see cref="InvalidOperationException"/>,
    /// <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// </remarks>
    public readonly struct VcardDateTime : IEquatable<VcardDateTime>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const string UninitializedMessage = "The VcardDateTime instance is not initialised.";

        internal const int MaxLength = VcardDate.MaxLength + 1 + VcardTime.MaxLength;

        private readonly VcardDate _date;
        private readonly VcardTime _time;

        /// <summary>
        /// Creates a date and time.
        /// </summary>
        /// <param name="date">The date, which may be truncated but not reduced: it has a day, and if it has a year it has a month.</param>
        /// <param name="time">The time, which may be reduced but not truncated: it has an hour.</param>
        /// <exception cref="ArgumentException"><paramref name="date"/> or <paramref name="time"/> is not initialised or doesn't have that form, or no version can write them together:
        /// a time only an older version has, with a date that isn't complete.</exception>
        public VcardDateTime(VcardDate date, VcardTime time)
        {
            if (!date.IsNotReduced)
                throw new ArgumentException("The date must have a day, and a month if it has a year.", nameof(date));
            if (!time.IsNotTruncated)
                throw new ArgumentException("The time must have an hour.", nameof(time));
            if (!time.IsDefinedIn(VcardVersion.V4_0) && !date.IsComplete)
                throw new ArgumentException("Only a complete date can go with a time that vCard 4.0 can't write.", nameof(date));

            _date = date;
            _time = time;
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
                return _date;
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
                return _time;
            }
        }

        /// <summary>
        /// Gets a flag indicating if a version can write this value.  See <see cref="VcardDate.IsDefinedIn"/> and <see cref="VcardTime.IsDefinedIn"/>.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        public bool IsDefinedIn(VcardVersion version)
        {
            CheckInitialized();
            return _date.IsDefinedIn(version) && _time.IsDefinedIn(version);
        }

        /// <summary>
        /// Parses a vCard 4.0 date and time.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The date and time, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardDateTime result)
        {
            return TryParse(value, format, VcardVersion.V4_0, out result);
        }

        /// <summary>
        /// Parses a date and time in the form a version uses.
        /// </summary>
        /// <param name="value">The value: in vCard 3.0 and vCard 2.1 a complete one such as <c>1995-10-31T22:27:10Z</c> or <c>19951031T222710</c>.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="result">The date and time, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/>, which represents vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardDateTime result)
        {
            VcardValueFormatting.CheckFormat(format, version);

            return TryParseCore(value, format, version, out result);
        }

        /// <summary>
        /// Parses a vCard 4.0 date and time.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The date and time.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static VcardDateTime Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            return Parse(value, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Parses a date and time in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The date and time.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/>, which represents vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        public static VcardDateTime Parse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version)
        {
            VcardDateTime result;
            if (!TryParse(value, format, version, out result))
                throw new FormatException("The value is not a valid DATE-TIME.");

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
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        /// <exception cref="InvalidOperationException">vCard 4.0 has no form for this value; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            return TryFormat(destination, out charsWritten, format, VcardVersion.V4_0);
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
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/>, which represents vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this value; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format, VcardVersion version)
        {
            VcardValueFormatting.CheckFormat(format, version);

            if (!_date.IsInitialized)
            {
                charsWritten = 0;
                return true;
            }

            if (!IsDefinedIn(version))
                throw VcardValueFormatting.BuildNotDefinedException("DATE-TIME", version);

            var writer = new VcardSpanWriter(destination);
            Append(ref writer, format, version);
            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the value as a string in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        /// <exception cref="InvalidOperationException">vCard 4.0 has no form for this value; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format)
        {
            return ToString(format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the value as a string in the form a version uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/>, which represents vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this value; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format, VcardVersion version)
        {
            var buffer = new char[MaxLength];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format, version), charsWritten);
        }

        /// <summary>
        /// Formats the value in <see cref="VcardValueFormat.Text"/> form, as the first of vCard 4.0, vCard 3.0 and vCard 2.1 that can write it; or gives the type name if the instance
        /// is not initialised.
        /// </summary>
        public override string ToString()
        {
            if (!_date.IsInitialized) return typeof(VcardDateTime).FullName ?? nameof(VcardDateTime);

            return ToString(VcardValueFormat.Text, DefaultVersion);
        }

#if NET6_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The format is empty or <c>T</c> for <see cref="VcardValueFormat.Text"/>, <c>J</c> for <see cref="VcardValueFormat.Json"/> or <c>X</c> for
        /// <see cref="VcardValueFormat.Xml"/>.  The value is written as <see cref="ToString()"/> writes it.  The provider is ignored.</remarks>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            var valueFormat = VcardValueFormatting.ParseFormatString(format);
            return TryFormat(destination, out charsWritten, valueFormat, VcardValueFormatting.GetFormattingVersion(valueFormat, DefaultVersion));
        }

        /// <inheritdoc />
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            var valueFormat = VcardValueFormatting.ParseFormatString(format);
            return ToString(valueFormat, VcardValueFormatting.GetFormattingVersion(valueFormat, DefaultVersion));
        }
#endif

        /// <summary>
        /// Indicates whether this value equals another: the same date and the same time.
        /// </summary>
        public bool Equals(VcardDateTime other)
        {
            return _date == other._date && _time == other._time;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardDateTime other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _date.GetHashCode() ^ (_time.GetHashCode() * 397);
        }

        /// <summary>
        /// Indicates whether two values are equal.
        /// </summary>
        public static bool operator ==(VcardDateTime left, VcardDateTime right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two values differ.
        /// </summary>
        public static bool operator !=(VcardDateTime left, VcardDateTime right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Gets whether this instance has been initialised.
        /// </summary>
        internal bool IsInitialized => _date.IsInitialized;

        /// <summary>
        /// Gets the first of vCard 4.0, vCard 3.0 and vCard 2.1 that can write this value.
        /// </summary>
        internal VcardVersion DefaultVersion => _time.DefaultVersion;

        /// <summary>
        /// Parses the whole value as a date and time of a version, in a lexical form.  The caller has checked that they can be used together.
        /// </summary>
        internal static bool TryParseCore(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardDateTime result)
        {
            return format == VcardValueFormat.Json ? TryParseJson(value, out result) : TryParseCore(value, version, out result);
        }

        /// <summary>
        /// Appends the value in a lexical form and the form a version uses.  The caller has checked that the version can write it.
        /// </summary>
        internal void Append(ref VcardSpanWriter writer, VcardValueFormat format, VcardVersion version)
        {
            if (format == VcardValueFormat.Json)
            {
                _date.AppendJson(ref writer);
                writer.Append('T');
                _time.AppendJson(ref writer);
                return;
            }

            _date.Append(ref writer, version);
            writer.Append('T');
            _time.Append(ref writer, version);
        }

        /// <summary>
        /// Parses the whole value as a jCard <c>date-time</c> (RFC 7095 §3.5.5): a <c>date-noreduc</c> and a <c>time-notrunc</c> in the forms of §3.5.3 and §3.5.4.
        /// </summary>
        private static bool TryParseJson(ReadOnlySpan<char> value, out VcardDateTime result)
        {
            // ref. RFC 7095 §3.5.5: "the same date and time format restrictions noted in Sections 3.5.3 and 3.5.4 apply", which are those of RFC 6350 §4.3.3.
            result = default(VcardDateTime);
            var designatorIndex = value.IndexOf('T');
            if (designatorIndex < 0) return false;

            VcardDate date;
            VcardTime time;
            if (!VcardDate.TryParseJsonNotReduced(value.Slice(0, designatorIndex), out date) || !VcardTime.TryParseJson(value.Slice(designatorIndex + 1), out time) ||
                !time.IsNotTruncated)
            {
                return false;
            }

            result = new VcardDateTime(date, time);
            return true;
        }

        /// <summary>
        /// Parses the whole value as a date and time of a version.
        /// </summary>
        internal static bool TryParseCore(ReadOnlySpan<char> value, VcardVersion version, out VcardDateTime result)
        {
            // ref. RFC 6350 §4: date-time = date-noreduc time-designator time-notrunc; time-designator = %x54
            // ref. RFC 2425 §5.8.4: date-time-list = date "T" time *("," date "T" time)
            result = default(VcardDateTime);
            var designatorIndex = VcardValueFormatting.IndexOfTimeDesignator(value, version);
            if (designatorIndex < 0) return false;

            var datePart = value.Slice(0, designatorIndex);
            var timePart = value.Slice(designatorIndex + 1);
            VcardDate date;
            VcardTime time;
            if (version == VcardVersion.V4_0)
            {
                if (!VcardDate.TryParseNotReduced(datePart, out date) || !VcardTime.TryParseVersion40(timePart, out time) || !time.IsNotTruncated) return false;
            }
            else
            {
                if (!VcardDate.TryParseLegacy(datePart, version, out date) || !VcardTime.TryParseLegacy(timePart, version, out time)) return false;

                // ref. vCard 2.1 §2.6.2: the value conforms to "the basic or extended format of ISO 8601", which doesn't mix them.
                if (version == VcardVersion.V2_1 && (datePart[4] == '-') != (timePart[2] == ':')) return false;
            }

            result = new VcardDateTime(date, time);
            return true;
        }

        private void CheckInitialized()
        {
            if (!_date.IsInitialized)
                throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
