using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The separator line before a message in an mbox file (RFC 4155 Appendix A): <c>From</c>, a space, the address of the sender, a space, and the UTC date and time that the message was
    /// received, in the form of the <c>ctime</c> function with no time zone.
    /// </summary>
    /// <remarks>
    /// An instance is immutable.  One that was read keeps its line as it was, so it is written back byte for byte.  With <see cref="ImfOptions.AllowInvalidMboxSeparator"/>, a line in
    /// another form can be read, and then <see cref="Sender"/> or <see cref="Date"/> is <c>null</c> if the line doesn't give one in a form that can be understood.
    /// </remarks>
    public sealed class MboxEnvelope
    {
        private const byte Colon = 0x3A;
        private const byte DoubleQuote = 0x22;
        private const byte Backslash = 0x5C;
        private const byte Space = 0x20;
        private const byte Tab = 0x09;

        // ref. RFC 4155 Appendix A: "From", a single space
        private const int PrefixLength = 5;

        // The length of "Www Mmm dd hh:mm:ss yyyy".
        private const int TimestampLength = 24;

        private const int MinimumRawYear = 1000;
        private const int MaximumHour = 23;
        private const int MaximumMinuteOrSecond = 59;

        // ref. ISO C struct tm: tm_sec goes up to 60, so ctime can give a leap second.
        private const int LeapSecond = 60;

        private static readonly string[] DayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private static readonly string[] MonthNames = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        private static readonly ImfOptions AddressOptions = new ImfOptions();

        private readonly byte[] _rawLine;

        /// <summary>
        /// Creates a separator line.
        /// </summary>
        /// <param name="sender">The sender, of which only the address is used.</param>
        /// <param name="date">The date and time that the message was received, which is written in UTC, to the second.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sender"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The address of <paramref name="sender"/> isn't the <c>addr-spec</c> of US-ASCII characters that RFC 4155 Appendix A requires.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The year of <paramref name="date"/> in UTC doesn't have four digits.</exception>
        public MboxEnvelope(ImfMailbox sender, DateTimeOffset date)
        {
            if (sender is null)
                throw new ArgumentNullException(nameof(sender));

            var utc = date.ToUniversalTime();
            if (utc.Year < MinimumRawYear)
                throw new ArgumentOutOfRangeException(nameof(date), "The year must have four digits.");

            var line = string.Format(CultureInfo.InvariantCulture, "From {0} {1} {2} {3,2} {4:00}:{5:00}:{6:00} {7}", sender.Address, DayNames[(int)utc.DayOfWeek], MonthNames[utc.Month - 1], utc.Day,
                utc.Hour, utc.Minute, utc.Second, utc.Year);
            var rawLine = Encoding.UTF8.GetBytes(line);

            if (!TryParseStrict(rawLine, out var parsedSender, out var parsedDate))
                throw new ArgumentException("The address isn't an addr-spec of US-ASCII characters, as RFC 4155 Appendix A requires.", nameof(sender));

            _rawLine = rawLine;
            Sender = parsedSender;
            Date = parsedDate;
            IsConforming = true;
        }

        private MboxEnvelope(byte[] rawLine, [AllowNull] ImfMailbox sender, DateTimeOffset? date, bool isConforming)
        {
            _rawLine = rawLine;
            Sender = sender;
            Date = date;
            IsConforming = isConforming;
        }

        /// <summary>
        /// Gets the sender, or <c>null</c> if the line was read under <see cref="ImfOptions.AllowInvalidMboxSeparator"/> and doesn't give one as an <c>addr-spec</c>.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public ImfMailbox? Sender { get; }
#else
        public ImfMailbox Sender { get; }
#endif

        /// <summary>
        /// Gets the date and time that the message was received, or <c>null</c> if the line was read under <see cref="ImfOptions.AllowInvalidMboxSeparator"/> and doesn't give one that can be
        /// understood, such as one with a time zone given as a name other than UTC or GMT.  A leap second becomes second 59 of the same minute, as <see cref="DateTimeOffset"/> has no leap
        /// seconds, and <see cref="RawLine"/> keeps it.
        /// </summary>
        public DateTimeOffset? Date { get; }

        /// <summary>
        /// Gets the bytes of the line, without its line break.
        /// </summary>
        public ReadOnlyMemory<byte> RawLine => _rawLine;

        /// <summary>
        /// Gets a flag indicating if the line has the form that RFC 4155 Appendix A requires.
        /// </summary>
        internal bool IsConforming { get; }

        /// <summary>
        /// Gets the line, without its line break.
        /// </summary>
        /// <returns>The line, with each byte as one character.</returns>
        public override string ToString()
        {
            var text = new StringBuilder(_rawLine.Length);
            foreach (var value in _rawLine)
            {
                text.Append((char)value);
            }

            return text.ToString();
        }

        /// <summary>
        /// Reads a separator line.
        /// </summary>
        /// <param name="line">The line, without its line break.</param>
        /// <param name="allowOtherForms"><c>true</c> if a line in another form that common programs write is accepted.</param>
        /// <param name="result">The separator line.</param>
        /// <returns><c>true</c> if the line is a separator line.</returns>
        internal static bool TryParse(ReadOnlySpan<byte> line, bool allowOtherForms, [MaybeNullWhen(false)] out MboxEnvelope result)
        {
            if (TryParseStrict(line, out var sender, out var date))
            {
                result = new MboxEnvelope(line.ToArray(), sender, date, true);
                return true;
            }

            if (allowOtherForms && TryParseRelaxed(line, out sender, out var relaxedDate))
            {
                result = new MboxEnvelope(line.ToArray(), sender, relaxedDate, false);
                return true;
            }

            result = null;
            return false;
        }

        // ref. RFC 4155 Appendix A
        private static bool TryParseStrict(ReadOnlySpan<byte> line, [MaybeNullWhen(false)] out ImfMailbox sender, out DateTimeOffset date)
        {
            sender = null;
            date = default;
            var senderLength = line.Length - PrefixLength - TimestampLength - 1;
            if (senderLength < 1 || !HasPrefix(line) || line[line.Length - TimestampLength - 1] != Space) return false;

            return TryParseTimestamp(line.Slice(line.Length - TimestampLength), out date) && TryParseSender(line.Slice(PrefixLength, senderLength), out sender);
        }

        private static bool HasPrefix(ReadOnlySpan<byte> line)
        {
            return line.Length >= PrefixLength && line[0] == 'F' && line[1] == 'r' && line[2] == 'o' && line[3] == 'm' && line[4] == Space;
        }

        // An addr-spec has no comments or folding, which the grammar of a field allows around its tokens, so these are refused before the tokens are read.
        private static bool TryParseSender(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfMailbox sender)
        {
            sender = null;
            var isQuoted = false;
            for (var index = 0; index < value.Length; index++)
            {
                var octet = value[index];
                if (octet < Space || octet >= 0x7F) return false;

                if (isQuoted)
                {
                    if (octet == Backslash) index++;
                    else if (octet == DoubleQuote) isQuoted = false;
                }
                else if (octet == DoubleQuote)
                {
                    isQuoted = true;
                }
                else if (octet == Space || octet == '(' || octet == ')' || octet == '<' || octet == '>')
                {
                    return false;
                }
            }

            return !isQuoted && ImfAddressParser.TryParseMailbox(value, AddressOptions, out sender);
        }

        // ref. ISO C asctime, which ctime uses: "%.3s %.3s%3d %.2d:%.2d:%.2d %d", so the day is padded with a space.
        private static bool TryParseTimestamp(ReadOnlySpan<byte> value, out DateTimeOffset date)
        {
            date = default;
            if (value[3] != Space || value[7] != Space || value[10] != Space || value[13] != Colon || value[16] != Colon || value[19] != Space) return false;

            var dayOfWeek = IndexOfName(DayNames, value.Slice(0, 3));
            var month = IndexOfName(MonthNames, value.Slice(4, 3)) + 1;
            if (dayOfWeek < 0 || month < 1) return false;

            int day;
            if (value[8] == Space)
            {
                if (!TryParseDigits(value.Slice(9, 1), out day)) return false;
            }
            else if (value[8] == '0' || !TryParseDigits(value.Slice(8, 2), out day))
            {
                return false;
            }

            if (!TryParseDigits(value.Slice(11, 2), out var hour) || !TryParseDigits(value.Slice(14, 2), out var minute) || !TryParseDigits(value.Slice(17, 2), out var second)
                || !TryParseDigits(value.Slice(20, 4), out var year) || year < MinimumRawYear)
                return false;

            return TryBuildDate(year, month, day, hour, minute, second, TimeSpan.Zero, out date) && (int)date.DayOfWeek == dayOfWeek;
        }

        // The forms that common programs write: another padding of the day, a time with no seconds, a time zone before or after the year, more white space, and a sender that isn't an addr-spec,
        // such as the "-" of Thunderbird or the MAILER-DAEMON of a mail server.
        private static bool TryParseRelaxed(ReadOnlySpan<byte> line, [MaybeNull] out ImfMailbox sender, out DateTimeOffset? date)
        {
            sender = null;
            date = null;
            if (!HasPrefix(line)) return false;

            var rest = SkipWhiteSpace(line.Slice(PrefixLength));
            var senderField = ReadField(ref rest);
            var dayOfWeekField = ReadField(ref rest);
            var monthField = ReadField(ref rest);
            var dayField = ReadField(ref rest);
            var timeField = ReadField(ref rest);
            var fifthField = ReadField(ref rest);
            var sixthField = ReadField(ref rest);
            if (senderField.IsEmpty || fifthField.IsEmpty || !rest.IsEmpty) return false;

            var month = IndexOfName(MonthNames, monthField) + 1;
            if (IndexOfName(DayNames, dayOfWeekField) < 0 || month < 1 || dayField.Length > 2 || !TryParseDigits(dayField, out var day) || !TryParseTime(timeField, out var hour, out var minute, out var second))
                return false;

            var isYearFifth = TryParseYear(fifthField, out var year);
            if (!isYearFifth && !TryParseYear(sixthField, out year)) return false;

            if (!TryParseZone(isYearFifth ? sixthField : fifthField, out var offset, out var isZoneKnown)) return false;

            if (TryParseSender(senderField, out var mailbox)) sender = mailbox;
            if (isZoneKnown && TryBuildDate(year, month, day, hour, minute, second, offset, out var value)) date = value;

            return true;
        }

        private static ReadOnlySpan<byte> SkipWhiteSpace(ReadOnlySpan<byte> value)
        {
            var index = 0;
            while (index < value.Length && (value[index] == Space || value[index] == Tab)) index++;

            return value.Slice(index);
        }

        private static ReadOnlySpan<byte> ReadField(ref ReadOnlySpan<byte> rest)
        {
            var index = 0;
            while (index < rest.Length && rest[index] != Space && rest[index] != Tab) index++;

            var field = rest.Slice(0, index);
            rest = SkipWhiteSpace(rest.Slice(index));
            return field;
        }

        private static bool TryParseYear(ReadOnlySpan<byte> value, out int year)
        {
            const int yearLength = 4;
            year = 0;
            return value.Length == yearLength && TryParseDigits(value, out year);
        }

        private static bool TryParseTime(ReadOnlySpan<byte> value, out int hour, out int minute, out int second)
        {
            hour = minute = second = 0;
            const int shortLength = 5;
            const int longLength = 8;
            if ((value.Length != shortLength && value.Length != longLength) || value[2] != Colon) return false;

            if (!TryParseDigits(value.Slice(0, 2), out hour) || !TryParseDigits(value.Slice(3, 2), out minute)) return false;

            return value.Length == shortLength || (value[5] == Colon && TryParseDigits(value.Slice(6, 2), out second));
        }

        private static bool TryParseZone(ReadOnlySpan<byte> value, out TimeSpan offset, out bool isKnown)
        {
            const int numericLength = 5;
            offset = TimeSpan.Zero;
            isKnown = true;
            if (value.IsEmpty) return true;

            if (value[0] == '+' || value[0] == '-')
            {
                if (value.Length != numericLength || !TryParseDigits(value.Slice(1, 2), out var hours) || !TryParseDigits(value.Slice(3, 2), out var minutes)) return false;

                offset = new TimeSpan(hours, minutes, 0);
                if (value[0] == '-') offset = offset.Negate();

                return true;
            }

            foreach (var octet in value)
            {
                if (!(octet >= 'A' && octet <= 'Z') && !(octet >= 'a' && octet <= 'z')) return false;
            }

            isKnown = NameEquals("UTC", value) || NameEquals("GMT", value);
            return true;
        }

        private static bool TryBuildDate(int year, int month, int day, int hour, int minute, int second, TimeSpan offset, out DateTimeOffset date)
        {
            date = default;
            // The most a time zone offset can be.
            const int maximumOffsetHours = 14;
            if (year < 1 || day < 1 || day > DateTime.DaysInMonth(year, month) || hour > MaximumHour || minute > MaximumMinuteOrSecond || second > LeapSecond
                || offset.Duration() > TimeSpan.FromHours(maximumOffsetHours))
                return false;

            var clock = new DateTime(year, month, day, hour, minute, Math.Min(second, MaximumMinuteOrSecond), DateTimeKind.Unspecified);
            var utcTicks = clock.Ticks - offset.Ticks;
            if (utcTicks < DateTime.MinValue.Ticks || utcTicks > DateTime.MaxValue.Ticks) return false;

            date = new DateTimeOffset(clock, offset);
            return true;
        }

        private static bool TryParseDigits(ReadOnlySpan<byte> value, out int result)
        {
            result = 0;
            if (value.IsEmpty) return false;

            foreach (var octet in value)
            {
                if (octet < '0' || octet > '9') return false;

                result = result * 10 + (octet - '0');
            }

            return true;
        }

        private static int IndexOfName(string[] names, ReadOnlySpan<byte> value)
        {
            for (var index = 0; index < names.Length; index++)
            {
                if (NameEquals(names[index], value)) return index;
            }

            return -1;
        }

        private static bool NameEquals(string name, ReadOnlySpan<byte> value)
        {
            if (value.Length != name.Length) return false;

            for (var index = 0; index < name.Length; index++)
            {
                if (value[index] != name[index]) return false;
            }

            return true;
        }
    }
}
