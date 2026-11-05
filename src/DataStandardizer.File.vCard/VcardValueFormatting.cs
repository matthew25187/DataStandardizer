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
                    return;
                case VcardValueFormat.Json:
                case VcardValueFormat.Xml:
                    throw new NotSupportedException("Only the text form of a vCard value is implemented.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "The value format is not recognised.");
            }
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
