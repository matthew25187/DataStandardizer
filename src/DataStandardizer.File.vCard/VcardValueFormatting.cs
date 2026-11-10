using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Helpers shared by the value types for parsing and formatting their lexical forms.
    /// </summary>
    internal static class VcardValueFormatting
    {
        /// <summary>
        /// Checks that a value format is one that can be parsed and formatted.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        internal static void CheckFormat(VcardValueFormat format)
        {
            switch (format)
            {
                case VcardValueFormat.Text:
                case VcardValueFormat.Json:
                case VcardValueFormat.Xml:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "The value format is not recognised.");
            }
        }

        /// <summary>
        /// Checks that a value format and a version can be used together.  jCard (RFC 7095) and xCard (RFC 6351) represent vCard 4.0 (RFC 6350) alone, so the JSON and XML forms
        /// have no other version.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/> or <see cref="VcardValueFormat.Xml"/>, and <paramref name="version"/>
        /// isn't vCard 4.0.</exception>
        internal static void CheckFormat(VcardValueFormat format, VcardVersion version)
        {
            CheckFormat(format);
            CheckVersion(version);
            if (format == VcardValueFormat.Json && version != VcardVersion.V4_0)
                throw new ArgumentException("jCard (RFC 7095) represents vCard 4.0 alone, so a value in the JSON form is a value of vCard 4.0.", nameof(version));

            // ref. RFC 6351 §5.1: the namespace "includes the version number", that of vCard 4.0.
            if (format == VcardValueFormat.Xml && version != VcardVersion.V4_0)
                throw new ArgumentException("xCard (RFC 6351) represents vCard 4.0 alone, so a value in the XML form is a value of vCard 4.0.", nameof(version));
        }

        /// <summary>
        /// Checks that a value format is one that a structured value can be parsed from and formatted in.  In jCard a structured value is a JSON array with an element for each
        /// component (RFC 7095 §3.3.1.3), and in xCard an XML element for each (RFC 6351 §5), not a single string, so it has no form of its own there.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="valueName">The name of the value, such as <c>An N value</c>, for the message.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/> or <see cref="VcardValueFormat.Xml"/>.</exception>
        internal static void CheckStructuredFormat(VcardValueFormat format, string valueName)
        {
            CheckFormat(format);
            if (format == VcardValueFormat.Json)
                throw new NotSupportedException(valueName + " has no single lexical form in jCard (RFC 7095 §3.3.1.3), where a structured value is a JSON array with an element for " +
                                                "each component.  JcardReader and JcardWriter read and write that array.");

            // ref. RFC 6351 §5: "Properties having structured values (e.g., the "N" property) are expressed by XML element trees."
            if (format == VcardValueFormat.Xml)
                throw new NotSupportedException(valueName + " has no single lexical form in xCard (RFC 6351 §5), where a structured value is a tree of XML elements, one for each " +
                                                "component.  XcardReader and XcardWriter read and write those elements.");
        }

        /// <summary>
        /// Gets the version that a value formatted by a .NET format string is written in: vCard 4.0 for the JSON and XML forms, which have no other, and otherwise the value's own
        /// default.
        /// </summary>
        internal static VcardVersion GetFormattingVersion(VcardValueFormat format, VcardVersion defaultVersion)
        {
            return format == VcardValueFormat.Text ? defaultVersion : VcardVersion.V4_0;
        }

        /// <summary>
        /// The most places a JSON number's exponent may move its decimal point when it is expanded, so that a short number can't become a long text.
        /// </summary>
        internal const int MaxJsonExponentShift = 400;

        /// <summary>
        /// Rewrites a JSON number (RFC 8259 §6) with no exponent, moving its decimal point in the text so that every digit is kept.
        /// </summary>
        /// <param name="value">The text of the number.</param>
        /// <param name="isNegative"><c>true</c> if it has a minus sign.</param>
        /// <param name="integerDigits">The digits before the decimal point, with no leading zeros; empty for a number below 1.</param>
        /// <param name="fractionDigits">The digits after the decimal point, as written; empty if there are none.</param>
        /// <returns><c>false</c> if the text isn't a JSON number, or its exponent would move the point more than <see cref="MaxJsonExponentShift"/> places.</returns>
        internal static bool TryExpandJsonNumber(ReadOnlySpan<char> value, out bool isNegative, out string integerDigits, out string fractionDigits)
        {
            isNegative = false;
            integerDigits = string.Empty;
            fractionDigits = string.Empty;
            return IsJsonNumber(value) && TryExpandNumber(value, out isNegative, out integerDigits, out fractionDigits);
        }

        /// <summary>
        /// Rewrites the numeral of an <c>xsd:float</c> (XML Schema Part 2 §3.2.4.1) with no exponent, moving its decimal point in the text so that every digit is kept.
        /// </summary>
        /// <param name="value">The numeral, with no white space around it.</param>
        /// <param name="isNegative"><c>true</c> if it has a minus sign.</param>
        /// <param name="integerDigits">The digits before the decimal point, with no leading zeros; empty for a number below 1.</param>
        /// <param name="fractionDigits">The digits after the decimal point, as written; empty if there are none.</param>
        /// <returns><c>false</c> if the text isn't such a numeral, which <c>INF</c>, <c>-INF</c> and <c>NaN</c> are not, or its exponent would move the point more than
        /// <see cref="MaxJsonExponentShift"/> places.</returns>
        internal static bool TryExpandXmlFloat(ReadOnlySpan<char> value, out bool isNegative, out string integerDigits, out string fractionDigits)
        {
            isNegative = false;
            integerDigits = string.Empty;
            fractionDigits = string.Empty;
            return IsXmlFloatNumeral(value) && TryExpandNumber(value, out isNegative, out integerDigits, out fractionDigits);
        }

        /// <summary>
        /// Gets whether text is a numeral of <c>xsd:float</c> (XML Schema Part 2 §3.2.4.1), with no white space around it: an optional sign, a decimal numeral that has digits on
        /// at least one side of its point, and an optional exponent.  <c>INF</c>, <c>-INF</c> and <c>NaN</c> are not numerals.
        /// </summary>
        internal static bool IsXmlFloatNumeral(ReadOnlySpan<char> value)
        {
            var index = 0;
            if (index < value.Length && (value[index] == '+' || value[index] == '-'))
            {
                index++;
            }

            var digitCount = 0;
            while (index < value.Length && IsDigit(value[index]))
            {
                index++;
                digitCount++;
            }

            if (index < value.Length && value[index] == '.')
            {
                index++;
                while (index < value.Length && IsDigit(value[index]))
                {
                    index++;
                    digitCount++;
                }
            }

            if (digitCount == 0) return false;

            if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
            {
                index++;
                if (index < value.Length && (value[index] == '+' || value[index] == '-'))
                {
                    index++;
                }

                var exponentStart = index;
                while (index < value.Length && IsDigit(value[index]))
                {
                    index++;
                }

                if (index == exponentStart) return false;
            }

            return index == value.Length;
        }

        /// <summary>
        /// Rewrites a number whose grammar the caller has checked: an optional sign, digits with an optional point, and an optional exponent.
        /// </summary>
        private static bool TryExpandNumber(ReadOnlySpan<char> value, out bool isNegative, out string integerDigits, out string fractionDigits)
        {
            integerDigits = string.Empty;
            fractionDigits = string.Empty;
            var index = 0;
            isNegative = value[0] == '-';
            if (isNegative || value[0] == '+')
            {
                index++;
            }

            var integerStart = index;
            while (index < value.Length && IsDigit(value[index]))
            {
                index++;
            }

            var integerPart = value.Slice(integerStart, index - integerStart);
            var fractionPart = default(ReadOnlySpan<char>);
            if (index < value.Length && value[index] == '.')
            {
                var fractionStart = ++index;
                while (index < value.Length && IsDigit(value[index]))
                {
                    index++;
                }

                fractionPart = value.Slice(fractionStart, index - fractionStart);
            }

            var shift = 0;
            if (index < value.Length)
            {
                // The exponent: "e" or "E", an optional sign and its digits, which the caller has checked.
                index++;
                var isExponentNegative = value[index] == '-';
                if (value[index] == '-' || value[index] == '+')
                {
                    index++;
                }

                for (; index < value.Length; index++)
                {
                    shift = shift * 10 + (value[index] - '0');
                    if (shift > MaxJsonExponentShift) return false;
                }

                shift = isExponentNegative ? -shift : shift;
            }

            // Move the point: the digits are those of the integer then the fraction, and the point sits after the integer's, moved by the exponent.
            var digits = string.Concat(integerPart.ToString(), fractionPart.ToString());
            var pointIndex = integerPart.Length + shift;
            if (pointIndex <= 0)
            {
                fractionDigits = new string('0', -pointIndex) + digits;
            }
            else if (pointIndex >= digits.Length)
            {
                integerDigits = digits + new string('0', pointIndex - digits.Length);
            }
            else
            {
                integerDigits = digits.Substring(0, pointIndex);
                fractionDigits = digits.Substring(pointIndex);
            }

            integerDigits = integerDigits.TrimStart('0');
            return true;
        }

        /// <summary>
        /// Gets whether text is a JSON number (RFC 8259 §6): an optional minus sign, an integer with no leading zeros, an optional fraction and an optional exponent.
        /// </summary>
        internal static bool IsJsonNumber(ReadOnlySpan<char> value)
        {
            // ref. RFC 8259 §6: number = [ minus ] int [ frac ] [ exp ]; int = zero / ( digit1-9 *DIGIT )
            var index = 0;
            if (index < value.Length && value[index] == '-')
            {
                index++;
            }

            if (index == value.Length || !IsDigit(value[index])) return false;

            if (value[index++] != '0')
            {
                while (index < value.Length && IsDigit(value[index]))
                {
                    index++;
                }
            }

            if (index < value.Length && value[index] == '.')
            {
                var fractionStart = ++index;
                while (index < value.Length && IsDigit(value[index]))
                {
                    index++;
                }

                if (index == fractionStart) return false;
            }

            if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
            {
                index++;
                if (index < value.Length && (value[index] == '+' || value[index] == '-'))
                {
                    index++;
                }

                var exponentStart = index;
                while (index < value.Length && IsDigit(value[index]))
                {
                    index++;
                }

                if (index == exponentStart) return false;
            }

            return index == value.Length;
        }

        /// <summary>
        /// Gets whether a character is XML white space (XML 1.0 §2.3): a space, tab, carriage return or line feed.
        /// </summary>
        internal static bool IsXmlWhitespace(char character)
        {
            return character == ' ' || character == '\t' || character == '\r' || character == '\n';
        }

        /// <summary>
        /// Removes leading and trailing XML white space, as XML Schema does for the types whose white space is collapsed, such as <c>xsd:integer</c>.
        /// </summary>
        internal static ReadOnlySpan<char> TrimXmlWhitespace(ReadOnlySpan<char> value)
        {
            var start = 0;
            while (start < value.Length && IsXmlWhitespace(value[start]))
            {
                start++;
            }

            var end = value.Length;
            while (end > start && IsXmlWhitespace(value[end - 1]))
            {
                end--;
            }

            return value.Slice(start, end - start);
        }

        /// <summary>
        /// Gets whether text holds only characters an XML 1.0 document can hold (XML 1.0 §2.2): tab, line feed, carriage return, and the characters from U+0020 up, other than
        /// U+FFFE, U+FFFF and unpaired surrogates.
        /// </summary>
        internal static bool IsXmlText(ReadOnlySpan<char> value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character < ' ')
                {
                    if (character != '\t' && character != '\n' && character != '\r') return false;
                }
                else if (char.IsHighSurrogate(character))
                {
                    if (index + 1 == value.Length || !char.IsLowSurrogate(value[index + 1])) return false;

                    index++;
                }
                else if (char.IsLowSurrogate(character) || character == '\uFFFE' || character == '\uFFFF')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks that a version is one of the vCard versions.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        internal static void CheckVersion(VcardVersion version)
        {
            if (version != VcardVersion.V4_0 && version != VcardVersion.V3_0 && version != VcardVersion.V2_1)
                throw new ArgumentOutOfRangeException(nameof(version), version, "The vCard version is not recognised.");
        }

        /// <summary>
        /// Gets the value format named by a .NET format string: empty or <c>T</c> for <see cref="VcardValueFormat.Text"/>, <c>J</c> for
        /// <see cref="VcardValueFormat.Json"/> and <c>X</c> for <see cref="VcardValueFormat.Xml"/>, in either case.
        /// </summary>
        /// <exception cref="FormatException"><paramref name="format"/> is not one of those.</exception>
        internal static VcardValueFormat ParseFormatString(ReadOnlySpan<char> format)
        {
            if (format.Length == 0) return VcardValueFormat.Text;

            if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 'T':
                    case 't':
                        return VcardValueFormat.Text;
                    case 'J':
                    case 'j':
                        return VcardValueFormat.Json;
                    case 'X':
                    case 'x':
                        return VcardValueFormat.Xml;
                }
            }

            throw new FormatException("The format string is not recognised.  Use \"T\" for text, \"J\" for jCard or \"X\" for xCard.");
        }

        internal static bool IsDigit(char character)
        {
            return character >= '0' && character <= '9';
        }

        /// <summary>
        /// Parses a run of exactly <paramref name="count"/> decimal digits.
        /// </summary>
        internal static bool TryParseDigits(ReadOnlySpan<char> value, int start, int count, out int result)
        {
            result = 0;
            if (start + count > value.Length) return false;

            for (var index = start; index < start + count; index++)
            {
                var character = value[index];
                if (!IsDigit(character)) return false;

                result = result * 10 + (character - '0');
            }

            return true;
        }

        /// <summary>
        /// Parses a whole value of one or more decimal digits into a <see cref="long"/>.
        /// </summary>
        /// <returns><c>false</c> if the value is empty, has anything but digits, or doesn't fit a <see cref="long"/>.</returns>
        internal static bool TryParseNumber(ReadOnlySpan<char> value, out long result)
        {
            result = 0;
            if (value.Length == 0) return false;

            foreach (var character in value)
            {
                if (!IsDigit(character)) return false;

                var digit = character - '0';
                if (result > (long.MaxValue - digit) / 10) return false;

                result = result * 10 + digit;
            }

            return true;
        }

        /// <summary>
        /// Gets the index of the <c>T</c> between a date and a time; or -1 if there is none.
        /// </summary>
        internal static int IndexOfTimeDesignator(ReadOnlySpan<char> value, VcardVersion version)
        {
            // ref. RFC 6350 §4: time-designator = %x54, which is only upper case.
            // ref. RFC 2425 §5.8.4: date-time-list = date "T" time; an ABNF string is case-insensitive (RFC 2234 §2.3).
            return version == VcardVersion.V3_0 ? value.IndexOfAny('T', 't') : value.IndexOf('T');
        }

        /// <summary>
        /// Compares text with an upper-case ASCII keyword, ignoring the case of ASCII letters (RFC 5234 §2.3).
        /// </summary>
        internal static bool EqualsKeyword(ReadOnlySpan<char> value, string keyword)
        {
            if (value.Length != keyword.Length) return false;

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character >= 'a' && character <= 'z')
                {
                    character = (char)(character - 0x20);
                }

                if (character != keyword[index]) return false;
            }

            return true;
        }

        /// <summary>
        /// Makes a string from a value formatted into a buffer of a known length.
        /// </summary>
        internal static string ToString(char[] buffer, bool formatted, int charsWritten)
        {
            if (!formatted)
                throw new InvalidOperationException("The value did not fit the buffer sized for it.");

            return new string(buffer, 0, charsWritten);
        }

        /// <summary>
        /// Builds the exception for a value that a version has no form for.
        /// </summary>
        internal static InvalidOperationException BuildNotDefinedException(string valueTypeName, VcardVersion version)
        {
            return new InvalidOperationException("This " + valueTypeName + " value has no form in vCard version " + version + ".");
        }
    }

    /// <summary>
    /// Writes text into a span, and remembers whether it ran out of room.
    /// </summary>
    internal ref struct VcardSpanWriter
    {
        private readonly Span<char> _destination;
        private int _position;
        private bool _overflowed;

        internal VcardSpanWriter(Span<char> destination)
        {
            _destination = destination;
            _position = 0;
            _overflowed = false;
        }

        internal void Append(char character)
        {
            if (_position < _destination.Length)
            {
                _destination[_position++] = character;
            }
            else
            {
                _overflowed = true;
            }
        }

        internal void Append(ReadOnlySpan<char> text)
        {
            if (text.TryCopyTo(_destination.Slice(_position)))
            {
                _position += text.Length;
            }
            else
            {
                _overflowed = true;
            }
        }

        /// <summary>
        /// Appends a non-negative number of at most <paramref name="width"/> digits, padded with leading zeros.
        /// </summary>
        internal void AppendDigits(long value, int width)
        {
            if (_position + width > _destination.Length)
            {
                _overflowed = true;
                return;
            }

            for (var index = _position + width - 1; index >= _position; index--)
            {
                _destination[index] = (char)('0' + value % 10);
                value /= 10;
            }

            _position += width;
        }

        /// <summary>
        /// Gets the number of characters written, if everything fitted.
        /// </summary>
        /// <returns><c>true</c> if everything fitted; otherwise <c>false</c>, and <paramref name="charsWritten"/> is 0.</returns>
        internal bool TryComplete(out int charsWritten)
        {
            charsWritten = _overflowed ? 0 : _position;
            return !_overflowed;
        }
    }
}
