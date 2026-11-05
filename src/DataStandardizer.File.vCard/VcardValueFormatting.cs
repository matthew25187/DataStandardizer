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
        /// <exception cref="NotSupportedException"><paramref name="format"/> is a form that isn't implemented yet.</exception>
        internal static void CheckFormat(VcardValueFormat format)
        {
            switch (format)
            {
                case VcardValueFormat.Text:
                case VcardValueFormat.Json:
                    return;
                case VcardValueFormat.Xml:
                    throw new NotSupportedException("The xCard form of a vCard value is not implemented yet.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "The value format is not recognised.");
            }
        }

        /// <summary>
        /// Checks that a value format and a version can be used together.  jCard (RFC 7095) represents vCard 4.0 (RFC 6350) alone, so the JSON form has no other version.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is a form that isn't implemented yet.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/> and <paramref name="version"/> isn't vCard 4.0.</exception>
        internal static void CheckFormat(VcardValueFormat format, VcardVersion version)
        {
            CheckFormat(format);
            CheckVersion(version);
            if (format == VcardValueFormat.Json && version != VcardVersion.V4_0)
                throw new ArgumentException("jCard (RFC 7095) represents vCard 4.0 alone, so a value in the JSON form is a value of vCard 4.0.", nameof(version));
        }

        /// <summary>
        /// Checks that a value format is one that a structured value can be parsed from and formatted in.  In jCard a structured value is a JSON array with an element for each
        /// component (RFC 7095 §3.3.1.3), not a single string, so it has no form of its own there.
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
        }

        /// <summary>
        /// Gets the version that a value formatted by a .NET format string is written in: vCard 4.0 for the JSON form, which has no other, and otherwise the value's own default.
        /// </summary>
        internal static VcardVersion GetFormattingVersion(VcardValueFormat format, VcardVersion defaultVersion)
        {
            return format == VcardValueFormat.Json ? VcardVersion.V4_0 : defaultVersion;
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
