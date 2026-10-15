using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Helpers shared by the value types for parsing and formatting their lexical forms.
    /// </summary>
    internal static class IcalValueFormatting
    {
        /// <summary>
        /// Checks that a value format is one that can be parsed and formatted.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        internal static void CheckFormat(IcalValueFormat format)
        {
            switch (format)
            {
                case IcalValueFormat.Text:
                case IcalValueFormat.Json:
                case IcalValueFormat.Xml:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "The value format is not recognised.");
            }
        }

        /// <summary>
        /// Gets the value format named by a .NET format string: empty or <c>T</c> for <see cref="IcalValueFormat.Text"/>, <c>J</c> for
        /// <see cref="IcalValueFormat.Json"/> and <c>X</c> for <see cref="IcalValueFormat.Xml"/>, in either case.
        /// </summary>
        /// <exception cref="FormatException"><paramref name="format"/> is not one of those.</exception>
        internal static IcalValueFormat ParseFormatString(ReadOnlySpan<char> format)
        {
            if (format.Length == 0) return IcalValueFormat.Text;

            if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 'T':
                    case 't':
                        return IcalValueFormat.Text;
                    case 'J':
                    case 'j':
                        return IcalValueFormat.Json;
                    case 'X':
                    case 'x':
                        return IcalValueFormat.Xml;
                }
            }

            throw new FormatException("The format string is not recognised.  Use \"T\" for text, \"J\" for jCal or \"X\" for xCal.");
        }

        /// <summary>
        /// Gets whether a character is XML white space (XML 1.0 §2.3): a space, tab, carriage return or line feed.
        /// </summary>
        internal static bool IsXmlWhitespace(char character)
        {
            return character == ' ' || character == '\t' || character == '\r' || character == '\n';
        }

        /// <summary>
        /// Removes leading and trailing XML white space, as XML Schema does for the types whose white space is collapsed, such as
        /// <c>xsd:integer</c>.
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
        /// Gets whether text holds only characters an XML 1.0 document can hold (XML 1.0 §2.2): tab, line feed, carriage return, and the
        /// characters from U+0020 up, other than U+FFFE, U+FFFF and unpaired surrogates.
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
                else if (char.IsLowSurrogate(character) || character == '￾' || character == '￿')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets whether text is a JSON number (RFC 8259 §6): an optional minus sign, an integer with no leading zeros, an optional fraction and
        /// an optional exponent.
        /// </summary>
        internal static bool IsJsonNumber(ReadOnlySpan<char> value)
        {
            // ref. RFC 8259§6: number = [ minus ] int [ frac ] [ exp ]; int = zero / ( digit1-9 *DIGIT )
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

            if (index < value.Length && IsLetter(value[index], 'E'))
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
        /// Parses a run of one or more decimal digits, as far as it goes, into a non-negative <see cref="long"/> no greater than
        /// <paramref name="maximum"/>.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <param name="index">The index of the first digit; on return, the index after the last.</param>
        /// <param name="maximum">The largest value allowed.</param>
        /// <param name="result">The number.</param>
        /// <returns><c>true</c> if there was at least one digit and the number is no greater than <paramref name="maximum"/>.</returns>
        internal static bool TryParseNumber(ReadOnlySpan<char> value, ref int index, long maximum, out long result)
        {
            result = 0;
            var start = index;
            while (index < value.Length && IsDigit(value[index]))
            {
                var digit = value[index] - '0';
                if (result > (maximum - digit) / 10) return false;

                result = result * 10 + digit;
                index++;
            }

            return index > start;
        }

        /// <summary>
        /// Gets whether a character is a given ASCII letter, in either case.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="upperCaseLetter">The letter, in upper case.</param>
        internal static bool IsLetter(char character, char upperCaseLetter)
        {
            return character == upperCaseLetter || character == (char)(upperCaseLetter | 0x20);
        }

        /// <summary>
        /// Compares text with an upper-case ASCII keyword, ignoring the case of ASCII letters.
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
        /// Gets the number of decimal digits in a non-negative number.
        /// </summary>
        internal static int CountDigits(long value)
        {
            var digits = 1;
            while (value >= 10)
            {
                value /= 10;
                digits++;
            }

            return digits;
        }

        /// <summary>
        /// Formats a value that fits in a buffer of a known length into a string.
        /// </summary>
        internal static string ToString(char[] buffer, bool formatted, int charsWritten)
        {
            if (!formatted)
                throw new InvalidOperationException("The value did not fit the buffer sized for it.");

            return new string(buffer, 0, charsWritten);
        }
    }

    /// <summary>
    /// Writes text into a span, and remembers whether it ran out of room.
    /// </summary>
    internal ref struct IcalSpanWriter
    {
        private readonly Span<char> _destination;
        private int _position;
        private bool _overflowed;

        internal IcalSpanWriter(Span<char> destination)
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

        internal void Append(string text)
        {
            Append(text.AsSpan());
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
        /// Appends a non-negative number, padded with leading zeros to <paramref name="width"/> digits.
        /// </summary>
        internal void AppendDigits(long value, int width)
        {
            var digits = Math.Max(width, IcalValueFormatting.CountDigits(value));
            if (_position + digits > _destination.Length)
            {
                _overflowed = true;
                return;
            }

            for (var index = _position + digits - 1; index >= _position; index--)
            {
                _destination[index] = (char)('0' + value % 10);
                value /= 10;
            }

            _position += digits;
        }

        /// <summary>
        /// Appends a non-negative number with no padding.
        /// </summary>
        internal void AppendNumber(long value)
        {
            AppendDigits(value, 1);
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
