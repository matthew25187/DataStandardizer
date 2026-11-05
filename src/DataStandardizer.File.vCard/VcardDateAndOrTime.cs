using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A DATE-AND-OR-TIME value (RFC 6350 §4.3.4): a date and time, a date, or a time written with a <c>T</c> before it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// vCard 3.0 and vCard 2.1 have no such type.  Their <c>BDAY</c> property is a date or a date-time, so in those versions this type reads and writes those two and has no form
    /// for a time alone.
    /// </para>
    /// <para>
    /// In jCard (RFC 7095 §3.5.6), the <see cref="VcardValueFormat.Json"/> form, a vCard 4.0 value is a date, a date and time, or a time after a <c>T</c>, each in the form of
    /// <see cref="VcardDate"/>, <see cref="VcardDateTime"/> or <see cref="VcardTime"/>, such as <c>--02-03</c> or <c>T12:30</c>.  jCard represents vCard 4.0 alone, so that form
    /// has no other version.
    /// </para>
    /// <para>
    /// A default instance is not initialised: <see cref="Kind"/>, <see cref="Date"/>, <see cref="Time"/> and <see cref="IsDefinedIn"/> throw
    /// <see cref="InvalidOperationException"/>, <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// </remarks>
    public readonly struct VcardDateAndOrTime : IEquatable<VcardDateAndOrTime>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const string UninitializedMessage = "The VcardDateAndOrTime instance is not initialised.";

        private readonly VcardDate _date;
        private readonly VcardTime _time;

        /// <summary>
        /// Creates a value holding a date alone.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <exception cref="ArgumentException"><paramref name="date"/> is not initialised.</exception>
        public VcardDateAndOrTime(VcardDate date)
        {
            if (!date.IsInitialized)
                throw new ArgumentException("The date is not initialised.", nameof(date));

            _date = date;
            _time = default(VcardTime);
        }

        /// <summary>
        /// Creates a value holding a time alone.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <exception cref="ArgumentException"><paramref name="time"/> is not initialised, or vCard 4.0, the only version with a time alone, can't write it.</exception>
        public VcardDateAndOrTime(VcardTime time)
        {
            if (!time.IsInitialized)
                throw new ArgumentException("The time is not initialised.", nameof(time));
            if (!time.IsDefinedIn(VcardVersion.V4_0))
                throw new ArgumentException("Only vCard 4.0 has a time alone, and it can't write this time.", nameof(time));

            _date = default(VcardDate);
            _time = time;
        }

        /// <summary>
        /// Creates a value holding a date and a time.
        /// </summary>
        /// <param name="dateTime">The date and time.</param>
        /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not initialised.</exception>
        public VcardDateAndOrTime(VcardDateTime dateTime)
        {
            if (!dateTime.IsInitialized)
                throw new ArgumentException("The date and time is not initialised.", nameof(dateTime));

            _date = dateTime.Date;
            _time = dateTime.Time;
        }

        /// <summary>
        /// Gets what the value holds.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public VcardDateAndOrTimeKind Kind
        {
            get
            {
                CheckInitialized();
                if (!_time.IsInitialized) return VcardDateAndOrTimeKind.Date;

                return _date.IsInitialized ? VcardDateAndOrTimeKind.DateTime : VcardDateAndOrTimeKind.Time;
            }
        }

        /// <summary>
        /// Gets the date; or <c>null</c> if the value holds a time alone.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public VcardDate? Date
        {
            get
            {
                CheckInitialized();
                return _date.IsInitialized ? _date : (VcardDate?)null;
            }
        }

        /// <summary>
        /// Gets the time; or <c>null</c> if the value holds a date alone.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public VcardTime? Time
        {
            get
            {
                CheckInitialized();
                return _time.IsInitialized ? _time : (VcardTime?)null;
            }
        }

        /// <summary>
        /// Gets a flag indicating if a version can write this value.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        public bool IsDefinedIn(VcardVersion version)
        {
            CheckInitialized();
            VcardValueFormatting.CheckVersion(version);

            if (!_date.IsInitialized) return version == VcardVersion.V4_0 && _time.IsDefinedIn(version);

            return _date.IsDefinedIn(version) && (!_time.IsInitialized || _time.IsDefinedIn(version));
        }

        /// <summary>
        /// Parses a vCard 4.0 value, such as <c>19961022T140000</c>, <c>--1022T1400</c>, <c>1985-04</c>, <c>---12</c>, <c>T102200</c> or <c>T--00</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The value, if it is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardDateAndOrTime result)
        {
            return TryParse(value, format, VcardVersion.V4_0, out result);
        }

        /// <summary>
        /// Parses a value in the form a version uses.
        /// </summary>
        /// <param name="value">The value: in vCard 3.0 and vCard 2.1 a date or a date and time.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="result">The value, if it is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/>, which represents vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardDateAndOrTime result)
        {
            VcardValueFormatting.CheckFormat(format, version);

            // ref. RFC 6350 §4: date-and-or-time = date-time / date / time-designator time.  ref. RFC 7095 §3.5.6: the same, in the forms of jCard.
            result = default(VcardDateAndOrTime);
            var designatorIndex = VcardValueFormatting.IndexOfTimeDesignator(value, version);
            if (designatorIndex < 0)
            {
                VcardDate date;
                if (!VcardDate.TryParse(value, format, version, out date)) return false;

                result = new VcardDateAndOrTime(date);
                return true;
            }

            if (designatorIndex == 0)
            {
                VcardTime time;
                if (version != VcardVersion.V4_0) return false;

                var timePart = value.Slice(1);
                if (!(format == VcardValueFormat.Json ? VcardTime.TryParseJson(timePart, out time) : VcardTime.TryParseVersion40(timePart, out time))) return false;

                result = new VcardDateAndOrTime(time);
                return true;
            }

            VcardDateTime dateTime;
            if (!VcardDateTime.TryParseCore(value, format, version, out dateTime)) return false;

            result = new VcardDateAndOrTime(dateTime);
            return true;
        }

        /// <summary>
        /// Parses a vCard 4.0 value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static VcardDateAndOrTime Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            return Parse(value, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Parses a value in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/>, which represents vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        public static VcardDateAndOrTime Parse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version)
        {
            VcardDateAndOrTime result;
            if (!TryParse(value, format, version, out result))
                throw new FormatException("The value is not a valid DATE-AND-OR-TIME.");

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

            if (!IsInitialized)
            {
                charsWritten = 0;
                return true;
            }

            if (!IsDefinedIn(version))
                throw VcardValueFormatting.BuildNotDefinedException("DATE-AND-OR-TIME", version);

            var writer = new VcardSpanWriter(destination);
            var isJson = format == VcardValueFormat.Json;
            if (_date.IsInitialized)
            {
                if (isJson)
                {
                    _date.AppendJson(ref writer);
                }
                else
                {
                    _date.Append(ref writer, version);
                }
            }

            if (_time.IsInitialized)
            {
                writer.Append('T');
                if (isJson)
                {
                    _time.AppendJson(ref writer);
                }
                else
                {
                    _time.Append(ref writer, version);
                }
            }

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
            var buffer = new char[VcardDateTime.MaxLength];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format, version), charsWritten);
        }

        /// <summary>
        /// Formats the value in <see cref="VcardValueFormat.Text"/> form, as the first of vCard 4.0, vCard 3.0 and vCard 2.1 that can write it; or gives the type name if the instance
        /// is not initialised.
        /// </summary>
        public override string ToString()
        {
            if (!IsInitialized) return typeof(VcardDateAndOrTime).FullName ?? nameof(VcardDateAndOrTime);

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
        /// Indicates whether this value equals another: the same date and the same time, each present or absent alike.
        /// </summary>
        public bool Equals(VcardDateAndOrTime other)
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
            return obj is VcardDateAndOrTime other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _date.GetHashCode() ^ (_time.GetHashCode() * 397);
        }

        /// <summary>
        /// Indicates whether two values are equal.
        /// </summary>
        public static bool operator ==(VcardDateAndOrTime left, VcardDateAndOrTime right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two values differ.
        /// </summary>
        public static bool operator !=(VcardDateAndOrTime left, VcardDateAndOrTime right)
        {
            return !left.Equals(right);
        }

        private bool IsInitialized => _date.IsInitialized || _time.IsInitialized;

        private VcardVersion DefaultVersion => _time.IsInitialized ? _time.DefaultVersion : VcardVersion.V4_0;

        private void CheckInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
