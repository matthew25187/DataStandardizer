using System;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Parses the date-time of RFC 5322 §3.3, with the obsolete forms of §4.3.
    /// </summary>
    /// <remarks>
    /// This works on bytes, not on the tokens of an <see cref="ImfTokenizer"/>, because the obsolete forms need no white space between their parts, as in
    /// <c>21Nov97</c>, which a tokenizer would take for one atom.  Each part below therefore takes the union of its modern and obsolete rules, and the modern
    /// rules add nothing to the obsolete ones but for the white space before a numeric zone.
    /// </remarks>
    internal static class ImfDateTimeParser
    {
        private const int MinimumYear = 1900;
        private const int LastHour = 23;
        private const int LastMinute = 59;
        private const int LeapSecond = 60;
        private const int LastOffsetMinute = 59;
        private const int MinutesPerHour = 60;
        private const int NameLength = 3;
        private const int MinimumYearDigits = 2;
        private const int ModernYearDigits = 4;
        private const int ObsoleteCenturyDivide = 50;
        private const int NoDayOfWeek = -1;

        // In the order of System.DayOfWeek.
        private static readonly string[] DayNames = { "sun", "mon", "tue", "wed", "thu", "fri", "sat" };

        private static readonly string[] MonthNames = { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };

        // ref. RFC 5322 §3.3: date-time = [ day-of-week "," ] date time [CFWS]; date = day month year; time = time-of-day zone
        internal static bool TryParse(ReadOnlySpan<byte> value, bool allowInvalidValues, out ImfDateTime result)
        {
            result = default;
            if (!ImfTokenizer.IsWellFormed(value)) return false;

            var position = 0;
            if (!TryReadDayOfWeek(value, ref position, out var dayOfWeek)) return false;
            if (!TryReadDayOfMonth(value, ref position, out var day)) return false;
            if (!TryReadName(value, ref position, MonthNames, out var monthIndex)) return false;
            if (!TryReadYearAndHour(value, ref position, out var year, out var hour)) return false;
            if (!TryReadMinuteAndSecond(value, ref position, out var minute, out var second)) return false;
            if (!TryReadZone(value, ref position, out var offsetMinutes, out var isOffsetUnknown)) return false;
            if (!ImfTokenizer.TrySkipCfws(value, ref position) || position != value.Length) return false;

            // ref. RFC 5322 §3.3: "A date-time specification MUST be semantically valid."  The year is any numeric year 1900 or later.
            var month = monthIndex + 1;
            if (year < MinimumYear || day < 1 || day > ImfDateTime.GetDaysInMonth(year, month)) return false;
            if (hour > LastHour || minute > LastMinute || second > LeapSecond) return false;

            var hasDayOfWeek = dayOfWeek != NoDayOfWeek;
            if (hasDayOfWeek && !allowInvalidValues && (int)ImfDateTime.GetDayOfWeek(year, month, day) != dayOfWeek) return false;

            result = new ImfDateTime(year, month, day, hour, minute, second, offsetMinutes, isOffsetUnknown, hasDayOfWeek);
            return true;
        }

        // ref. RFC 5322 §3.3: day-of-week = ([FWS] day-name) / obs-day-of-week
        // ref. RFC 5322 §4.3: obs-day-of-week = [CFWS] day-name [CFWS]
        private static bool TryReadDayOfWeek(ReadOnlySpan<byte> value, ref int position, out int dayOfWeek)
        {
            dayOfWeek = NoDayOfWeek;
            if (!ImfTokenizer.TrySkipCfws(value, ref position)) return false;
            if (position == value.Length || IsDigit(value[position])) return true;

            if (!TryReadName(value, ref position, DayNames, out dayOfWeek)) return false;
            if (!ImfTokenizer.TrySkipCfws(value, ref position)) return false;
            if (position == value.Length || value[position] != (byte)',') return false;

            position++;
            return true;
        }

        // ref. RFC 5322 §3.3: day = ([FWS] 1*2DIGIT FWS) / obs-day
        // ref. RFC 5322 §4.3: obs-day = [CFWS] 1*2DIGIT [CFWS]
        private static bool TryReadDayOfMonth(ReadOnlySpan<byte> value, ref int position, out int day)
        {
            day = 0;
            if (!ImfTokenizer.TrySkipCfws(value, ref position)) return false;

            var length = CountDigits(value, position);
            if (length < 1 || length > 2) return false;
            if (!TryGetNumber(value.Slice(position, length), out day)) return false;

            position += length;
            return ImfTokenizer.TrySkipCfws(value, ref position);
        }

        // ref. RFC 5322 §3.3: year = (FWS 4*DIGIT FWS) / obs-year; hour = 2DIGIT / obs-hour
        // ref. RFC 5322 §4.3: obs-year = [CFWS] 2*DIGIT [CFWS]; obs-hour = [CFWS] 2DIGIT [CFWS]
        // Nothing has to separate the two, so if nothing does, the last two digits of the run are the hour.
        private static bool TryReadYearAndHour(ReadOnlySpan<byte> value, ref int position, out int year, out int hour)
        {
            year = 0;
            hour = 0;
            if (!ImfTokenizer.TrySkipCfws(value, ref position)) return false;

            var start = position;
            var length = CountDigits(value, position);
            position += length;
            if (!ImfTokenizer.TrySkipCfws(value, ref position) || position == value.Length) return false;

            if (IsDigit(value[position]))
            {
                if (!TryReadTwoDigits(value, ref position, out hour) || !ImfTokenizer.TrySkipCfws(value, ref position)) return false;
            }
            else if (length >= MinimumYearDigits + 2)
            {
                length -= 2;
                if (!TryGetNumber(value.Slice(start + length, 2), out hour)) return false;
            }
            else
            {
                return false;
            }

            return length >= MinimumYearDigits && TryGetYear(value.Slice(start, length), out year);
        }

        // ref. RFC 5322 §4.3: two digits from 00 to 49 add 2000; two digits from 50 to 99, and three digits, add 1900.
        private static bool TryGetYear(ReadOnlySpan<byte> digits, out int year)
        {
            if (!TryGetNumber(digits, out year)) return false;

            if (digits.Length < ModernYearDigits)
            {
                year += digits.Length == MinimumYearDigits && year < ObsoleteCenturyDivide ? 2000 : 1900;
            }

            return true;
        }

        // ref. RFC 5322 §3.3: time-of-day = hour ":" minute [ ":" second ]; minute = 2DIGIT / obs-minute; second = 2DIGIT / obs-second
        // ref. RFC 5322 §4.3: obs-minute = [CFWS] 2DIGIT [CFWS]; obs-second = [CFWS] 2DIGIT [CFWS]
        private static bool TryReadMinuteAndSecond(ReadOnlySpan<byte> value, ref int position, out int minute, out int second)
        {
            minute = 0;
            second = 0;
            if (position == value.Length || value[position] != (byte)':') return false;

            position++;
            if (!ImfTokenizer.TrySkipCfws(value, ref position)) return false;
            if (!TryReadTwoDigits(value, ref position, out minute)) return false;
            if (!ImfTokenizer.TrySkipCfws(value, ref position)) return false;
            if (position == value.Length || value[position] != (byte)':') return true;

            position++;
            if (!ImfTokenizer.TrySkipCfws(value, ref position)) return false;
            if (!TryReadTwoDigits(value, ref position, out second)) return false;

            return ImfTokenizer.TrySkipCfws(value, ref position);
        }

        // ref. RFC 5322 §3.3: zone = (FWS ( "+" / "-" ) 4DIGIT) / obs-zone
        private static bool TryReadZone(ReadOnlySpan<byte> value, ref int position, out int offsetMinutes, out bool isOffsetUnknown)
        {
            offsetMinutes = 0;
            isOffsetUnknown = false;
            if (position == value.Length) return false;

            var current = value[position];
            if (current != (byte)'+' && current != (byte)'-') return TryReadObsoleteZone(value, ref position, out offsetMinutes, out isOffsetUnknown);

            // A comment before the zone may end the [CFWS] of the part before it, but the FWS of this rule still has to come last.
            var previous = value[position - 1];
            if (previous != (byte)' ' && previous != (byte)'\t') return false;

            position++;
            if (!TryReadTwoDigits(value, ref position, out var hours) || !TryReadTwoDigits(value, ref position, out var minutes)) return false;

            // ref. RFC 5322 §3.3: "the last two digits of the zone MUST be within the range 00 through 59"
            if (minutes > LastOffsetMinute) return false;

            offsetMinutes = hours * MinutesPerHour + minutes;

            // ref. RFC 5322 §3.3: "-0000" says that the date-time holds no information about the local time zone.
            if (current == (byte)'-')
            {
                isOffsetUnknown = offsetMinutes == 0;
                offsetMinutes = -offsetMinutes;
            }

            return true;
        }

        // ref. RFC 5322 §4.3: obs-zone = "UT" / "GMT" / "EST" / "EDT" / "CST" / "CDT" / "MST" / "MDT" / "PST" / "PDT" / %d65-73 / %d75-90 / %d97-105 / %d107-122
        private static bool TryReadObsoleteZone(ReadOnlySpan<byte> value, ref int position, out int offsetMinutes, out bool isOffsetUnknown)
        {
            offsetMinutes = 0;
            isOffsetUnknown = false;

            var start = position;
            while (position < value.Length && IsLetter(value[position]))
            {
                position++;
            }

            var name = value.Slice(start, position - start);
            if (name.Length == 1)
            {
                // ref. RFC 5322 §4.3: RFC 822 defined the military zones wrongly, so they SHOULD be considered equivalent to "-0000".
                isOffsetUnknown = true;
                return ToLower(name[0]) != (byte)'j';
            }

            if (IsName(name, "ut") || IsName(name, "gmt")) return true;

            if (!TryGetHoursBehindUniversalTime(name, out var hours)) return false;

            offsetMinutes = -hours * MinutesPerHour;
            return true;
        }

        // ref. RFC 5322 §4.3: "EDT" is "-0400", "EST" and "CDT" are "-0500", "CST" and "MDT" are "-0600", "MST" and "PDT" are "-0700", and "PST" is "-0800".
        private static bool TryGetHoursBehindUniversalTime(ReadOnlySpan<byte> name, out int hours)
        {
            hours = 0;
            if (IsName(name, "edt")) hours = 4;
            else if (IsName(name, "est") || IsName(name, "cdt")) hours = 5;
            else if (IsName(name, "cst") || IsName(name, "mdt")) hours = 6;
            else if (IsName(name, "mst") || IsName(name, "pdt")) hours = 7;
            else if (IsName(name, "pst")) hours = 8;

            return hours != 0;
        }

        // ABNF string literals are case-insensitive (RFC 5234 §2.3).
        private static bool TryReadName(ReadOnlySpan<byte> value, ref int position, string[] names, out int index)
        {
            index = 0;
            if (value.Length - position < NameLength) return false;

            var name = value.Slice(position, NameLength);
            for (var candidate = 0; candidate < names.Length; candidate++)
            {
                if (IsName(name, names[candidate]))
                {
                    index = candidate;
                    position += NameLength;
                    return true;
                }
            }

            return false;
        }

        private static bool IsName(ReadOnlySpan<byte> value, string lowerCaseName)
        {
            if (value.Length != lowerCaseName.Length) return false;

            for (var index = 0; index < value.Length; index++)
            {
                if (ToLower(value[index]) != lowerCaseName[index]) return false;
            }

            return true;
        }

        private static bool TryReadTwoDigits(ReadOnlySpan<byte> value, ref int position, out int number)
        {
            number = 0;
            if (value.Length - position < 2 || !IsDigit(value[position]) || !IsDigit(value[position + 1])) return false;

            number = (value[position] - (byte)'0') * 10 + (value[position + 1] - (byte)'0');
            position += 2;
            return true;
        }

        private static int CountDigits(ReadOnlySpan<byte> value, int position)
        {
            var count = 0;
            while (position + count < value.Length && IsDigit(value[position + count]))
            {
                count++;
            }

            return count;
        }

        // Fails if the number is too large for the type that holds it.
        private static bool TryGetNumber(ReadOnlySpan<byte> digits, out int number)
        {
            number = 0;
            long total = 0;
            foreach (var digit in digits)
            {
                total = total * 10 + (digit - (byte)'0');
                if (total > int.MaxValue) return false;
            }

            number = (int)total;
            return true;
        }

        private static bool IsDigit(byte value)
        {
            return value >= (byte)'0' && value <= (byte)'9';
        }

        private static bool IsLetter(byte value)
        {
            var lower = ToLower(value);
            return lower >= (byte)'a' && lower <= (byte)'z';
        }

        private static byte ToLower(byte value)
        {
            return value >= (byte)'A' && value <= (byte)'Z' ? (byte)(value + ('a' - 'A')) : value;
        }
    }
}
