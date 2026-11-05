using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A DATE value (RFC 6350 §4.3.1): a calendar date, which may leave out its year, its month or its day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// vCard 4.0 allows a complete date (<c>19850412</c>), a reduced one (<c>1985-04</c>, <c>1985</c>) and a truncated one (<c>--0412</c>, <c>--04</c>, <c>---12</c>), all in the
    /// basic format of ISO 8601.  So <c>1985-04-12</c> is not a vCard 4.0 date.  vCard 3.0 and vCard 2.1 have only complete dates, in which the extended format is allowed:
    /// <c>date-fullyear ["-"] date-month ["-"] date-mday</c> (RFC 2425 §5.8.4), or either format of ISO 8601 (vCard 2.1 §2.2.4).
    /// </para>
    /// <para>
    /// In jCard (RFC 7095 §3.5.3), the <see cref="VcardValueFormat.Json"/> form, a vCard 4.0 date is in the extended format where it applies: <c>1985-04-12</c>, <c>1985-04</c>,
    /// <c>1985</c>, <c>--04-12</c>, <c>--04</c> and <c>---12</c>.  jCard represents vCard 4.0 alone, so that form has no other version.
    /// </para>
    /// <para>
    /// In xCard (RFC 6351 Appendix A), the <see cref="VcardValueFormat.Xml"/> form, a date is in the basic format, as in vCard 4.0, but its pattern has no year alone: only
    /// <c>19850412</c>, <c>1985-04</c>, <c>--0412</c>, <c>--04</c> and <c>---12</c>.  So <c>1985</c> doesn't parse in that form, and a date of a year alone throws
    /// <see cref="InvalidOperationException"/> when formatted in it.  xCard represents vCard 4.0 alone, so that form has no other version.
    /// </para>
    /// <para>
    /// The year runs from 0 to 9999, as RFC 6350 §4 says, so a date can't always become a <see cref="DateTime"/>.  A date with no year allows 29 February.
    /// </para>
    /// <para>
    /// A default instance is not initialised: <see cref="Year"/>, <see cref="Month"/>, <see cref="Day"/>, <see cref="IsComplete"/> and <see cref="IsDefinedIn"/> throw
    /// <see cref="InvalidOperationException"/>, <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// </remarks>
    public readonly struct VcardDate : IEquatable<VcardDate>
#if NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const string UninitializedMessage = "The VcardDate instance is not initialised.";
        private const int MaxYear = 9999;
        private const int MaxMonth = 12;
        private const int MaxDay = 31;
        private const int February = 2;
        private const int None = -1;

        // The longest form is "YYYY-MM-DD".
        internal const int MaxLength = 10;

        // The year is the number plus one, so that the default value has no parts.
        private readonly short _yearPlusOne;
        private readonly byte _month;
        private readonly byte _day;

        /// <summary>
        /// Creates a complete date.
        /// </summary>
        /// <param name="year">The year, from 0 to 9999.</param>
        /// <param name="month">The month, from 1 to 12.</param>
        /// <param name="day">The day of the month, from 1 to the number of days in the month.</param>
        /// <exception cref="ArgumentOutOfRangeException">The year, month or day is out of range.</exception>
        public VcardDate(int year, int month, int day)
        {
            CheckYear(year);
            CheckMonth(month);
            if (!IsValidDay(year, month, day))
                throw new ArgumentOutOfRangeException(nameof(day), day, "Day must be in the range 1 - the number of days in the month.");

            _yearPlusOne = (short)(year + 1);
            _month = (byte)month;
            _day = (byte)day;
        }

        private VcardDate(short yearPlusOne, byte month, byte day)
        {
            _yearPlusOne = yearPlusOne;
            _month = month;
            _day = day;
        }

        /// <summary>
        /// Gets the year; or <c>null</c> if the date leaves it out.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public int? Year
        {
            get
            {
                CheckInitialized();
                return _yearPlusOne == 0 ? (int?)null : _yearPlusOne - 1;
            }
        }

        /// <summary>
        /// Gets the month, from 1 to 12; or <c>null</c> if the date leaves it out.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public int? Month
        {
            get
            {
                CheckInitialized();
                return _month == 0 ? (int?)null : _month;
            }
        }

        /// <summary>
        /// Gets the day of the month; or <c>null</c> if the date leaves it out.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public int? Day
        {
            get
            {
                CheckInitialized();
                return _day == 0 ? (int?)null : _day;
            }
        }

        /// <summary>
        /// Gets a flag indicating if the date has a year, a month and a day.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public bool IsComplete
        {
            get
            {
                CheckInitialized();
                return HasYearMonthAndDay;
            }
        }

        /// <summary>
        /// Creates a date of a year alone, written <c>1985</c>.
        /// </summary>
        /// <param name="year">The year, from 0 to 9999.</param>
        /// <exception cref="ArgumentOutOfRangeException">The year is out of range.</exception>
        public static VcardDate FromYear(int year)
        {
            CheckYear(year);
            return Create(year, None, None);
        }

        /// <summary>
        /// Creates a date of a year and a month, written <c>1985-04</c>.
        /// </summary>
        /// <param name="year">The year, from 0 to 9999.</param>
        /// <param name="month">The month, from 1 to 12.</param>
        /// <exception cref="ArgumentOutOfRangeException">The year or month is out of range.</exception>
        public static VcardDate FromYearAndMonth(int year, int month)
        {
            CheckYear(year);
            CheckMonth(month);
            return Create(year, month, None);
        }

        /// <summary>
        /// Creates a date of a month alone, written <c>--04</c>.
        /// </summary>
        /// <param name="month">The month, from 1 to 12.</param>
        /// <exception cref="ArgumentOutOfRangeException">The month is out of range.</exception>
        public static VcardDate FromMonth(int month)
        {
            CheckMonth(month);
            return Create(None, month, None);
        }

        /// <summary>
        /// Creates a date of a month and a day, written <c>--0412</c>.
        /// </summary>
        /// <param name="month">The month, from 1 to 12.</param>
        /// <param name="day">The day of the month, from 1 to the most days the month can have.</param>
        /// <exception cref="ArgumentOutOfRangeException">The month or day is out of range.</exception>
        public static VcardDate FromMonthAndDay(int month, int day)
        {
            CheckMonth(month);
            if (!IsValidDay(None, month, day))
                throw new ArgumentOutOfRangeException(nameof(day), day, "Day must be in the range 1 - the most days the month can have.");

            return Create(None, month, day);
        }

        /// <summary>
        /// Creates a date of a day alone, written <c>---12</c>.
        /// </summary>
        /// <param name="day">The day of the month, from 1 to 31.</param>
        /// <exception cref="ArgumentOutOfRangeException">The day is out of range.</exception>
        public static VcardDate FromDay(int day)
        {
            if (!IsValidDay(None, None, day))
                throw new ArgumentOutOfRangeException(nameof(day), day, "Day must be in the range 1 - 31.");

            return Create(None, None, day);
        }

        /// <summary>
        /// Gets a flag indicating if a version can write this date.  vCard 4.0 can write them all, and vCard 3.0 and vCard 2.1 only complete dates.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        public bool IsDefinedIn(VcardVersion version)
        {
            CheckInitialized();
            VcardValueFormatting.CheckVersion(version);

            return version == VcardVersion.V4_0 || HasYearMonthAndDay;
        }

        /// <summary>
        /// Parses a vCard 4.0 date, such as <c>19850412</c>, <c>1985-04</c>, <c>1985</c>, <c>--0412</c> or <c>---12</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The date, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid date; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardDate result)
        {
            return TryParse(value, format, VcardVersion.V4_0, out result);
        }

        /// <summary>
        /// Parses a date in the form a version uses.
        /// </summary>
        /// <param name="value">The value: in vCard 3.0 and vCard 2.1 a complete date such as <c>1996-04-15</c> or <c>19960415</c>.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="result">The date, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid date; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/> or <see cref="VcardValueFormat.Xml"/>, which represent vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardDate result)
        {
            VcardValueFormatting.CheckFormat(format, version);

            if (format == VcardValueFormat.Json) return TryParseJson(value, out result);

            // ref. RFC 6351 Appendix A: value-date = "\d{8}|\d{4}-\d\d|--\d\d(\d\d)?|---\d\d", which has no year alone.
            if (format == VcardValueFormat.Xml) return TryParseVersion40(value, out result) && !result.IsYearAlone;

            return version == VcardVersion.V4_0 ? TryParseVersion40(value, out result) : TryParseLegacy(value, version, out result);
        }

        /// <summary>
        /// Parses a vCard 4.0 date.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The date.</returns>
        /// <exception cref="FormatException">The value is not a valid date.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        public static VcardDate Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            return Parse(value, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Parses a date in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The date.</returns>
        /// <exception cref="FormatException">The value is not a valid date.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/> or <see cref="VcardValueFormat.Xml"/>, which represent vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        public static VcardDate Parse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version)
        {
            VcardDate result;
            if (!TryParse(value, format, version, out result))
                throw new FormatException("The value is not a valid DATE.");

            return result;
        }

        /// <summary>
        /// Formats the date into a span in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the date was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            return TryFormat(destination, out charsWritten, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the date into a span in the form a version uses: the basic format in vCard 4.0 and the extended format, <c>1996-04-15</c>, in vCard 3.0 and vCard 2.1.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns><c>true</c> if the date was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/> or <see cref="VcardValueFormat.Xml"/>, which represent vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this date; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format, VcardVersion version)
        {
            VcardValueFormatting.CheckFormat(format, version);

            if (!IsInitialized)
            {
                charsWritten = 0;
                return true;
            }

            if (!IsDefinedIn(version))
                throw VcardValueFormatting.BuildNotDefinedException("DATE", version);
            if (format == VcardValueFormat.Xml && IsYearAlone)
                throw new InvalidOperationException("xCard has no form for a DATE of a year alone, since its pattern has none: RFC 6351 Appendix A.");

            var writer = new VcardSpanWriter(destination);
            if (format == VcardValueFormat.Json)
            {
                AppendJson(ref writer);
            }
            else
            {
                Append(ref writer, version);
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the date as a string in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        public string ToString(VcardValueFormat format)
        {
            return ToString(format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the date as a string in the form a version uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/> or <see cref="VcardValueFormat.Xml"/>, which represent vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this date; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format, VcardVersion version)
        {
            var buffer = new char[MaxLength];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format, version), charsWritten);
        }

        /// <summary>
        /// Formats the date in <see cref="VcardValueFormat.Text"/> form, as vCard 4.0 writes it; or gives the type name if the instance is not initialised.
        /// </summary>
        public override string ToString()
        {
            return IsInitialized ? ToString(VcardValueFormat.Text) : typeof(VcardDate).FullName ?? nameof(VcardDate);
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
        /// Indicates whether this date equals another: the same year, month and day, each present or left out alike.
        /// </summary>
        public bool Equals(VcardDate other)
        {
            return _yearPlusOne == other._yearPlusOne && _month == other._month && _day == other._day;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardDate other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (ushort)_yearPlusOne | (_month << 16) | (_day << 24);
        }

        /// <summary>
        /// Indicates whether two dates are equal.
        /// </summary>
        public static bool operator ==(VcardDate left, VcardDate right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two dates differ.
        /// </summary>
        public static bool operator !=(VcardDate left, VcardDate right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Gets whether this instance has been initialised.
        /// </summary>
        internal bool IsInitialized => _yearPlusOne != 0 || _month != 0 || _day != 0;

        /// <summary>
        /// Gets whether the date has a form of <c>date-noreduc</c> (RFC 6350 §4): year, month and day; month and day; or day alone.
        /// </summary>
        internal bool IsNotReduced => _day != 0 && (_yearPlusOne == 0 || _month != 0);

        private bool HasYearMonthAndDay => _yearPlusOne != 0 && _month != 0 && _day != 0;

        /// <summary>
        /// Gets a flag indicating if the date is a year alone, such as <c>1985</c>, which xCard has no form for.
        /// </summary>
        internal bool IsYearAlone => _yearPlusOne != 0 && _month == 0 && _day == 0;

        /// <summary>
        /// Parses the whole value as a vCard 4.0 <c>date</c>.
        /// </summary>
        internal static bool TryParseVersion40(ReadOnlySpan<char> value, out VcardDate result)
        {
            // ref. RFC 6350 §4: date = year [month day] / year "-" month / "--" month [day] / "--" "-" day
            result = default(VcardDate);
            int year, month;
            switch (value.Length)
            {
                case 4:
                    if (value[0] == '-')
                    {
                        return value[1] == '-' && VcardValueFormatting.TryParseDigits(value, 2, 2, out month) && TryCreate(None, month, None, out result);
                    }

                    return VcardValueFormatting.TryParseDigits(value, 0, 4, out year) && TryCreate(year, None, None, out result);
                case 7:
                    return value[4] == '-' &&
                           VcardValueFormatting.TryParseDigits(value, 0, 4, out year) &&
                           VcardValueFormatting.TryParseDigits(value, 5, 2, out month) &&
                           TryCreate(year, month, None, out result);
                default:
                    return TryParseNotReduced(value, out result);
            }
        }

        /// <summary>
        /// Parses the whole value as a vCard 4.0 <c>date-noreduc</c>.
        /// </summary>
        internal static bool TryParseNotReduced(ReadOnlySpan<char> value, out VcardDate result)
        {
            // ref. RFC 6350 §4: date-noreduc = year month day / "--" month day / "--" "-" day
            result = default(VcardDate);
            int year, month, day;
            switch (value.Length)
            {
                case 8:
                    return VcardValueFormatting.TryParseDigits(value, 0, 4, out year) &&
                           VcardValueFormatting.TryParseDigits(value, 4, 2, out month) &&
                           VcardValueFormatting.TryParseDigits(value, 6, 2, out day) &&
                           TryCreate(year, month, day, out result);
                case 6:
                    return value[0] == '-' && value[1] == '-' &&
                           VcardValueFormatting.TryParseDigits(value, 2, 2, out month) &&
                           VcardValueFormatting.TryParseDigits(value, 4, 2, out day) &&
                           TryCreate(None, month, day, out result);
                case 5:
                    return value[0] == '-' && value[1] == '-' && value[2] == '-' &&
                           VcardValueFormatting.TryParseDigits(value, 3, 2, out day) &&
                           TryCreate(None, None, day, out result);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Parses the whole value as a jCard <c>date</c> (RFC 7095 §3.5.3).
        /// </summary>
        internal static bool TryParseJson(ReadOnlySpan<char> value, out VcardDate result)
        {
            // ref. RFC 7095 §3.5.3: date = date-noreduc / year / year "-" month / "--" month
            result = default(VcardDate);
            int year, month;
            switch (value.Length)
            {
                case 4:
                    if (value[0] == '-')
                    {
                        return value[1] == '-' && VcardValueFormatting.TryParseDigits(value, 2, 2, out month) && TryCreate(None, month, None, out result);
                    }

                    return VcardValueFormatting.TryParseDigits(value, 0, 4, out year) && TryCreate(year, None, None, out result);
                case 7 when value[0] != '-':
                    return value[4] == '-' &&
                           VcardValueFormatting.TryParseDigits(value, 0, 4, out year) &&
                           VcardValueFormatting.TryParseDigits(value, 5, 2, out month) &&
                           TryCreate(year, month, None, out result);
                default:
                    return TryParseJsonNotReduced(value, out result);
            }
        }

        /// <summary>
        /// Parses the whole value as a jCard <c>date-noreduc</c> (RFC 7095 §3.5.3).
        /// </summary>
        internal static bool TryParseJsonNotReduced(ReadOnlySpan<char> value, out VcardDate result)
        {
            // ref. RFC 7095 §3.5.3: date-complete = year "-" month "-" day; date-noreduc = date-complete / "--" month "-" day / "---" day
            result = default(VcardDate);
            int year, month, day;
            switch (value.Length)
            {
                case 10:
                    return value[4] == '-' && value[7] == '-' &&
                           VcardValueFormatting.TryParseDigits(value, 0, 4, out year) &&
                           VcardValueFormatting.TryParseDigits(value, 5, 2, out month) &&
                           VcardValueFormatting.TryParseDigits(value, 8, 2, out day) &&
                           TryCreate(year, month, day, out result);
                case 7:
                    return value[0] == '-' && value[1] == '-' && value[4] == '-' &&
                           VcardValueFormatting.TryParseDigits(value, 2, 2, out month) &&
                           VcardValueFormatting.TryParseDigits(value, 5, 2, out day) &&
                           TryCreate(None, month, day, out result);
                case 5:
                    return value[0] == '-' && value[1] == '-' && value[2] == '-' &&
                           VcardValueFormatting.TryParseDigits(value, 3, 2, out day) &&
                           TryCreate(None, None, day, out result);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Parses the whole value as a complete date of vCard 3.0 or vCard 2.1.
        /// </summary>
        internal static bool TryParseLegacy(ReadOnlySpan<char> value, VcardVersion version, out VcardDate result)
        {
            // ref. RFC 2425 §5.8.4: date = date-fullyear ["-"] date-month ["-"] date-mday
            // ref. vCard 2.1 §2.2.4: "the ISO 8601 calendar date, complete representation, in either basic or extended format".
            result = default(VcardDate);
            int year, month, day;
            if (!VcardValueFormatting.TryParseDigits(value, 0, 4, out year)) return false;

            var index = 4;
            var hasFirstSeparator = index < value.Length && value[index] == '-';
            if (hasFirstSeparator)
            {
                index++;
            }

            if (!VcardValueFormatting.TryParseDigits(value, index, 2, out month)) return false;

            index += 2;
            var hasSecondSeparator = index < value.Length && value[index] == '-';
            if (hasSecondSeparator)
            {
                index++;
            }

            if (version == VcardVersion.V2_1 && hasFirstSeparator != hasSecondSeparator) return false;

            return VcardValueFormatting.TryParseDigits(value, index, 2, out day) && index + 2 == value.Length && TryCreate(year, month, day, out result);
        }

        /// <summary>
        /// Appends the date in the form a version uses.  The caller has checked that the version can write it.
        /// </summary>
        internal void Append(ref VcardSpanWriter writer, VcardVersion version)
        {
            if (version != VcardVersion.V4_0)
            {
                writer.AppendDigits(_yearPlusOne - 1, 4);
                writer.Append('-');
                writer.AppendDigits(_month, 2);
                writer.Append('-');
                writer.AppendDigits(_day, 2);
                return;
            }

            if (_yearPlusOne == 0)
            {
                writer.Append('-');
                writer.Append('-');
                if (_month == 0)
                {
                    writer.Append('-');
                }
                else
                {
                    writer.AppendDigits(_month, 2);
                }
            }
            else
            {
                writer.AppendDigits(_yearPlusOne - 1, 4);
                if (_month != 0)
                {
                    // ref. RFC 6350 §4.3.1: "YYYYMM is disallowed to prevent confusion with YYMMDD".
                    if (_day == 0)
                    {
                        writer.Append('-');
                    }

                    writer.AppendDigits(_month, 2);
                }
            }

            if (_day != 0)
            {
                writer.AppendDigits(_day, 2);
            }
        }

        /// <summary>
        /// Appends the date in the form of jCard (RFC 7095 §3.5.3): the extended format of ISO 8601 where it applies, and otherwise the basic format.
        /// </summary>
        internal void AppendJson(ref VcardSpanWriter writer)
        {
            if (_yearPlusOne == 0)
            {
                writer.Append('-');
                writer.Append('-');
                if (_month == 0)
                {
                    writer.Append('-');
                }
                else
                {
                    writer.AppendDigits(_month, 2);
                }
            }
            else
            {
                writer.AppendDigits(_yearPlusOne - 1, 4);
                if (_month != 0)
                {
                    writer.Append('-');
                    writer.AppendDigits(_month, 2);
                }
            }

            if (_day != 0)
            {
                if (_month != 0)
                {
                    writer.Append('-');
                }

                writer.AppendDigits(_day, 2);
            }
        }

        private static VcardDate Create(int year, int month, int day)
        {
            return new VcardDate((short)(year + 1), month == None ? (byte)0 : (byte)month, day == None ? (byte)0 : (byte)day);
        }

        private static bool TryCreate(int year, int month, int day, out VcardDate result)
        {
            result = default(VcardDate);
            if (month != None && (month < 1 || month > MaxMonth)) return false;
            if (day != None && !IsValidDay(year, month, day)) return false;

            result = Create(year, month, day);
            return true;
        }

        // ref. RFC 6350 §4: day = 01-28/29/30/31 depending on month and leap year.  With no year or no month, the largest it can be.
        private static bool IsValidDay(int year, int month, int day)
        {
            int days;
            switch (month)
            {
                case February:
                    days = year == None || IsLeapYear(year) ? 29 : 28;
                    break;
                case 4:
                case 6:
                case 9:
                case 11:
                    days = 30;
                    break;
                default:
                    days = MaxDay;
                    break;
            }

            return day >= 1 && day <= days;
        }

        // The proleptic Gregorian calendar of ISO 8601.  DateTime.IsLeapYear doesn't take year 0.
        private static bool IsLeapYear(int year)
        {
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        }

        private static void CheckYear(int year)
        {
            if (year < 0 || year > MaxYear)
                throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be in the range 0 - 9999.");
        }

        private static void CheckMonth(int month)
        {
            if (month < 1 || month > MaxMonth)
                throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be in the range 1 - 12.");
        }

        private void CheckInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
