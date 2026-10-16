using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A DATE-TIME value (RFC 5545 §3.3.5): a date and a time of day, which is floating, in UTC or in a named time zone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A zoned value carries the time zone identifier from its <c>TZID</c> parameter.  The identifier is not part of the value's lexical
    /// form, so it is supplied to <see cref="TryParse(ReadOnlySpan{char}, string, IcalValueFormat, out IcalDateTime)"/> and is not written by
    /// <see cref="TryFormat"/>.  It is compared ordinally, so identifiers that differ only in case are different zones.
    /// </para>
    /// <para>
    /// The default value is 0001-01-01 at midnight, floating.
    /// </para>
    /// </remarks>
    public readonly struct IcalDateTime : IEquatable<IcalDateTime>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        /// <summary>
        /// The length of the longest text form, <c>YYYYMMDDTHHMMSSZ</c>.
        /// </summary>
        internal const int MaxTextLength = IcalDate.TextLength + 1 + IcalTime.TextLength + 1;

        /// <summary>
        /// The length of the longest xCal form, <c>YYYY-MM-DDTHH:MM:SSZ</c>.
        /// </summary>
        internal const int MaxXmlLength = IcalDate.XmlLength + 1 + IcalTime.XmlLength + 1;

        private readonly IcalDate _date;
        private readonly IcalTime _time;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly string? _timeZoneId;
#else
        private readonly string _timeZoneId;
#endif

        /// <summary>
        /// Creates a floating date-time, or one in UTC if <paramref name="time"/> is in UTC.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="time">The time of day.</param>
        public IcalDateTime(IcalDate date, IcalTime time)
        {
            _date = date;
            _time = time;
            _timeZoneId = null;
        }

        /// <summary>
        /// Creates a date-time in a named time zone.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="time">The local time of day.  It must not be in UTC.</param>
        /// <param name="timeZoneId">The time zone identifier, as given by a <c>TZID</c> parameter.</param>
        /// <exception cref="ArgumentNullException"><paramref name="timeZoneId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="timeZoneId"/> is empty, or <paramref name="time"/> is in UTC.</exception>
        public IcalDateTime(IcalDate date, IcalTime time, string timeZoneId)
        {
            if (timeZoneId is null)
                throw new ArgumentNullException(nameof(timeZoneId));
            if (timeZoneId.Length == 0)
                throw new ArgumentException("The time zone identifier cannot be empty.", nameof(timeZoneId));
            // ref. RFC 5545§3.2.19: the TZID parameter MUST NOT be applied to times in UTC.
            if (time.IsUtc)
                throw new ArgumentException("A time in UTC cannot have a time zone identifier.", nameof(time));

            _date = date;
            _time = time;
            _timeZoneId = timeZoneId;
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        public IcalDate Date => _date;

        /// <summary>
        /// Gets the time of day.  Its <see cref="IcalTime.IsUtc"/> is <c>true</c> only if <see cref="Kind"/> is <see cref="IcalDateTimeKind.Utc"/>.
        /// </summary>
        public IcalTime Time => _time;

        /// <summary>
        /// Gets the form of the value: floating, in UTC or in a named time zone.
        /// </summary>
        public IcalDateTimeKind Kind => _timeZoneId != null ? IcalDateTimeKind.Zoned : _time.IsUtc ? IcalDateTimeKind.Utc : IcalDateTimeKind.Floating;

        /// <summary>
        /// Gets the time zone identifier of a zoned value; or <c>null</c> if the value is floating or in UTC.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? TimeZoneId => _timeZoneId;
#else
        public string TimeZoneId => _timeZoneId;
#endif

        /// <summary>
        /// Creates a date-time from a <see cref="DateTime"/>, to the whole second.  A <see cref="DateTimeKind.Utc"/> value gives a date-time in
        /// UTC, and any other kind a floating one.
        /// </summary>
        /// <param name="value">The date and time.  Fractions of a second are dropped.</param>
        public static IcalDateTime FromDateTime(DateTime value)
        {
            return new IcalDateTime(IcalDate.FromDateTime(value),
                new IcalTime(value.Hour, value.Minute, value.Second, value.Kind == DateTimeKind.Utc));
        }

        /// <summary>
        /// Creates a date-time in a named time zone from the wall-clock time of a <see cref="DateTime"/>, to the whole second.
        /// </summary>
        /// <param name="value">The local date and time in the zone.  Fractions of a second and <see cref="DateTime.Kind"/> are ignored.</param>
        /// <param name="timeZoneId">The time zone identifier, as given by a <c>TZID</c> parameter.</param>
        /// <exception cref="ArgumentNullException"><paramref name="timeZoneId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="timeZoneId"/> is empty.</exception>
        public static IcalDateTime FromDateTime(DateTime value, string timeZoneId)
        {
            return new IcalDateTime(IcalDate.FromDateTime(value), new IcalTime(value.Hour, value.Minute, value.Second), timeZoneId);
        }

        /// <summary>
        /// Converts the value to a <see cref="DateTime"/>: of kind <see cref="DateTimeKind.Utc"/> for a value in UTC, otherwise of kind
        /// <see cref="DateTimeKind.Unspecified"/> holding the wall-clock time.
        /// </summary>
        /// <exception cref="InvalidOperationException">The value is a leap second, which <see cref="DateTime"/> cannot represent.</exception>
        public DateTime ToDateTime()
        {
            if (_time.Second == 60)
                throw new InvalidOperationException("A leap second cannot be converted to a DateTime.");

            var date = _date.ToDateTime();
            return new DateTime(date.Year, date.Month, date.Day, _time.Hour, _time.Minute, _time.Second,
                _time.IsUtc ? DateTimeKind.Utc : DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Parses a floating date-time, or one in UTC.
        /// </summary>
        /// <param name="value">The value, such as <c>19980118T230000</c> or <c>19980119T070000Z</c> in <see cref="IcalValueFormat.Text"/>
        /// form, or <c>1998-01-19T07:00:00Z</c> in <see cref="IcalValueFormat.Json"/> and <see cref="IcalValueFormat.Xml"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The date-time, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid date-time; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalDateTime result)
        {
            IcalValueFormatting.CheckFormat(format);

            return format != IcalValueFormat.Text ? TryParseXml(value, null, out result) : TryParseText(value, null, out result);
        }

        /// <summary>
        /// Parses a date-time, in a named time zone if a time zone identifier is given.
        /// </summary>
        /// <param name="value">The value, such as <c>19980119T020000</c> in <see cref="IcalValueFormat.Text"/> form or <c>1998-01-19T02:00:00</c>
        /// in <see cref="IcalValueFormat.Json"/> and <see cref="IcalValueFormat.Xml"/> form.</param>
        /// <param name="timeZoneId">The identifier from the value's <c>TZID</c> parameter; or <c>null</c> if it has none.  A value in UTC with a
        /// time zone identifier is not valid.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The date-time, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid date-time; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="timeZoneId"/> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(ReadOnlySpan<char> value, string? timeZoneId, IcalValueFormat format, out IcalDateTime result)
#else
        public static bool TryParse(ReadOnlySpan<char> value, string timeZoneId, IcalValueFormat format, out IcalDateTime result)
#endif
        {
            CheckTimeZoneId(timeZoneId);
            IcalValueFormatting.CheckFormat(format);

            return format != IcalValueFormat.Text ? TryParseXml(value, timeZoneId, out result) : TryParseText(value, timeZoneId, out result);
        }

        /// <summary>
        /// Parses a floating date-time, or one in UTC.
        /// </summary>
        /// <param name="value">The value, such as <c>19980118T230000</c> or <c>19980119T070000Z</c> in <see cref="IcalValueFormat.Text"/>
        /// form, or <c>1998-01-19T07:00:00Z</c> in <see cref="IcalValueFormat.Json"/> and <see cref="IcalValueFormat.Xml"/> form.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The date-time.</returns>
        /// <exception cref="FormatException">The value is not a valid date-time.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static IcalDateTime Parse(ReadOnlySpan<char> value, IcalValueFormat format)
        {
            return Parse(value, null, format);
        }

        /// <summary>
        /// Parses a date-time, in a named time zone if a time zone identifier is given.
        /// </summary>
        /// <param name="value">The value, such as <c>19980119T020000</c> in <see cref="IcalValueFormat.Text"/> form or <c>1998-01-19T02:00:00</c>
        /// in <see cref="IcalValueFormat.Json"/> and <see cref="IcalValueFormat.Xml"/> form.</param>
        /// <param name="timeZoneId">The identifier from the value's <c>TZID</c> parameter; or <c>null</c> if it has none.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The date-time.</returns>
        /// <exception cref="FormatException">The value is not a valid date-time, or is in UTC and has a time zone identifier.</exception>
        /// <exception cref="ArgumentException"><paramref name="timeZoneId"/> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static IcalDateTime Parse(ReadOnlySpan<char> value, string? timeZoneId, IcalValueFormat format)
#else
        public static IcalDateTime Parse(ReadOnlySpan<char> value, string timeZoneId, IcalValueFormat format)
#endif
        {
            IcalDateTime result;
            if (!TryParse(value, timeZoneId, format, out result))
                throw new FormatException("The value is not a valid DATE-TIME.");

            return result;
        }

        /// <summary>
        /// Formats the date-time into a span.  The time zone identifier of a zoned value is not written; it belongs in a <c>TZID</c> parameter.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the date-time was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            var writer = new IcalSpanWriter(destination);
            if (format != IcalValueFormat.Text)
            {
                AppendXml(ref writer);
            }
            else
            {
                AppendText(ref writer);
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the date-time as a string.  The time zone identifier of a zoned value is not written.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public string ToString(IcalValueFormat format)
        {
            var buffer = new char[MaxXmlLength];
            int charsWritten;
            return IcalValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format), charsWritten);
        }

        /// <summary>
        /// Formats the date-time in <see cref="IcalValueFormat.Text"/> form.  The time zone identifier of a zoned value is not written.
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
        /// Indicates whether this date-time equals another: the same date, time and kind, and for zoned values the same time zone identifier.
        /// </summary>
        public bool Equals(IcalDateTime other)
        {
            return _date == other._date && _time == other._time && string.Equals(_timeZoneId, other._timeZoneId, StringComparison.Ordinal);
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalDateTime other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = _date.GetHashCode() * 31 + _time.GetHashCode();
            return _timeZoneId is null ? hashCode : hashCode * 31 + StringComparer.Ordinal.GetHashCode(_timeZoneId);
        }

        /// <summary>
        /// Indicates whether two date-times are equal.
        /// </summary>
        public static bool operator ==(IcalDateTime left, IcalDateTime right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two date-times differ.
        /// </summary>
        public static bool operator !=(IcalDateTime left, IcalDateTime right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Compares the wall-clock fields of two date-times, ignoring their kind and time zone.
        /// </summary>
        internal static int CompareClock(IcalDateTime left, IcalDateTime right)
        {
            var difference = left._date.CompareTo(right._date);
            return difference != 0 ? difference : IcalTime.CompareClock(left._time, right._time);
        }

        /// <summary>
        /// Parses the <c>YYYYMMDDTHHMMSS[Z]</c> text form, which must be exactly the length of <paramref name="value"/>.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static bool TryParseText(ReadOnlySpan<char> value, string? timeZoneId, out IcalDateTime result)
#else
        internal static bool TryParseText(ReadOnlySpan<char> value, string timeZoneId, out IcalDateTime result)
#endif
        {
            // ref. RFC 5545§3.3.5: date-time = date "T" time
            result = default(IcalDateTime);
            IcalDate date;
            IcalTime time;
            if (value.Length <= IcalDate.TextLength || !IcalValueFormatting.IsLetter(value[IcalDate.TextLength], 'T') ||
                !IcalDate.TryParseText(value.Slice(0, IcalDate.TextLength), out date) ||
                !IcalTime.TryParseText(value.Slice(IcalDate.TextLength + 1), out time))
            {
                return false;
            }

            if (timeZoneId is null)
            {
                result = new IcalDateTime(date, time);
                return true;
            }

            if (time.IsUtc) return false;

            result = new IcalDateTime(date, time, timeZoneId);
            return true;
        }

        /// <summary>
        /// Parses the <c>YYYY-MM-DDTHH:MM:SS[Z]</c> xCal form, which must be exactly the length of <paramref name="value"/>.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static bool TryParseXml(ReadOnlySpan<char> value, string? timeZoneId, out IcalDateTime result)
#else
        internal static bool TryParseXml(ReadOnlySpan<char> value, string timeZoneId, out IcalDateTime result)
#endif
        {
            // ref. RFC 6321§3.6.5: pattern-date-time = "\d\d\d\d-\d\d-\d\dT\d\d:\d\d:\d\dZ?".
            result = default(IcalDateTime);
            IcalDate date;
            IcalTime time;
            if (value.Length <= IcalDate.XmlLength || !IcalValueFormatting.IsLetter(value[IcalDate.XmlLength], 'T') ||
                !IcalDate.TryParseXml(value.Slice(0, IcalDate.XmlLength), out date) ||
                !IcalTime.TryParseXml(value.Slice(IcalDate.XmlLength + 1), out time))
            {
                return false;
            }

            if (timeZoneId is null)
            {
                result = new IcalDateTime(date, time);
                return true;
            }

            if (time.IsUtc) return false;

            result = new IcalDateTime(date, time, timeZoneId);
            return true;
        }

        internal void AppendText(ref IcalSpanWriter writer)
        {
            _date.AppendText(ref writer);
            writer.Append('T');
            _time.AppendText(ref writer);
        }

        internal void AppendXml(ref IcalSpanWriter writer)
        {
            _date.AppendXml(ref writer);
            writer.Append('T');
            _time.AppendXml(ref writer);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static void CheckTimeZoneId(string? timeZoneId)
#else
        internal static void CheckTimeZoneId(string timeZoneId)
#endif
        {
            if (timeZoneId != null && timeZoneId.Length == 0)
                throw new ArgumentException("The time zone identifier cannot be empty.", nameof(timeZoneId));
        }
    }
}
