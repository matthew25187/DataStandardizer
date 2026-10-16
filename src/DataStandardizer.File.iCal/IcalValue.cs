using System;
using System.Globalization;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Parses and formats the value types of RFC 5545 §3.3 that map to .NET types: BINARY, BOOLEAN, CAL-ADDRESS, FLOAT, INTEGER, TEXT and URI.
    /// </summary>
    /// <remarks>
    /// The other value types have types of their own, such as <see cref="IcalDateTime"/> and <see cref="IcalRecurrenceRule"/>.
    /// </remarks>
    public static class IcalValue
    {
        private const string Base64Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        private const string TrueText = "TRUE";
        private const string FalseText = "FALSE";
        private const string XmlTrueText = "true";
        private const string XmlFalseText = "false";

        #region TEXT

        /// <summary>
        /// Parses a TEXT value (RFC 5545 §3.3.11) into a span, removing its escaping: <c>\\</c>, <c>\;</c> and <c>\,</c> become the escaped
        /// character, and <c>\n</c> or <c>\N</c> a line feed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For a property that holds a list of TEXT values, split the list with <see cref="EnumerateTextList"/> first, then parse each item.
        /// </para>
        /// <para>
        /// A value in <see cref="IcalValueFormat.Json"/> form is the content of a jCal string, and a value in <see cref="IcalValueFormat.Xml"/>
        /// form the content of an xCal <c>text</c> element.  Neither has escaping, so the value is copied as it is.
        /// </para>
        /// </remarks>
        /// <param name="value">The value as it appears in the content line.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="destination">Receives the text.  It must be at least as long as <paramref name="value"/>, which is always enough.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if the value is not valid.</param>
        /// <returns><c>true</c> if the value is valid; <c>false</c> if it has a backslash that does not start a valid escape.</returns>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is shorter than <paramref name="value"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryParseText(ReadOnlySpan<char> value, IcalValueFormat format, Span<char> destination, out int charsWritten)
        {
            IcalValueFormatting.CheckFormat(format);
            if (destination.Length < value.Length)
                throw new ArgumentException("The destination must be at least as long as the value.", nameof(destination));

            if (format != IcalValueFormat.Text)
            {
                value.CopyTo(destination);
                charsWritten = value.Length;
                return true;
            }

            int errorIndex;
            if (!IcalEscaping.TryUnescapeText(value, destination, out charsWritten, out errorIndex))
            {
                charsWritten = 0;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses a TEXT value (RFC 5545 §3.3.11) into a string, removing its escaping: <c>\\</c>, <c>\;</c> and <c>\,</c> become the escaped
        /// character, and <c>\n</c> or <c>\N</c> a line feed.
        /// </summary>
        /// <param name="value">The value as it appears in the content line.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The text, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; <c>false</c> if it has a backslash that does not start a valid escape.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryParseText(ReadOnlySpan<char> value, IcalValueFormat format, [NotNullWhen(true)] out string? result)
#else
        public static bool TryParseText(ReadOnlySpan<char> value, IcalValueFormat format, out string result)
#endif
        {
            var buffer = new char[value.Length];
            int charsWritten;
            if (!TryParseText(value, format, buffer, out charsWritten))
            {
                result = null;
                return false;
            }

            result = new string(buffer, 0, charsWritten);
            return true;
        }

        /// <summary>
        /// Formats text as a TEXT value (RFC 5545 §3.3.11) into a span, escaping <c>\</c>, <c>;</c> and <c>,</c> with a backslash and writing
        /// each line break (CRLF, LF or a lone CR) as <c>\n</c>.
        /// </summary>
        /// <remarks>
        /// In <see cref="IcalValueFormat.Json"/> form, the content of a jCal string, and in <see cref="IcalValueFormat.Xml"/> form, the content
        /// of an xCal <c>text</c> element, the text is written as it is, with no escaping and its line breaks unchanged.
        /// </remarks>
        /// <param name="value">The text.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a control character other than a tab or a line break, or a lone
        /// surrogate; or, in <see cref="IcalValueFormat.Xml"/> form, a character XML cannot hold, such as U+FFFE.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryFormatText(ReadOnlySpan<char> value, Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            if (format == IcalValueFormat.Xml)
            {
                // ref. RFC 6321§3.6.11: the content of the element is simple text.  Only the characters of XML 1.0 can be written.
                if (!IcalValueFormatting.IsXmlText(value) || value.IndexOf('\x7F') >= 0)
                    throw new ArgumentException("The text holds a character that a TEXT value cannot hold.", nameof(value));

                var xmlWriter = new IcalSpanWriter(destination);
                xmlWriter.Append(value);
                return xmlWriter.TryComplete(out charsWritten);
            }

            if (format == IcalValueFormat.Json)
            {
                // ref. RFC 7265§3.6.11: the value is a JSON string, which can hold any character once escaped.  Only the characters a TEXT
                // value can hold are written.
                var invalidIndex = FindInvalidTextCharacter(value);
                if (invalidIndex >= 0)
                    throw new ArgumentException(
                        string.Format(CultureInfo.InvariantCulture, "The text holds the character U+{0:X4}, which a TEXT value cannot hold.",
                            (int)value[invalidIndex]), nameof(value));

                var jsonWriter = new IcalSpanWriter(destination);
                jsonWriter.Append(value);
                return jsonWriter.TryComplete(out charsWritten);
            }

            // ref. RFC 5545§3.3.11: text = *(TSAFE-CHAR / ":" / DQUOTE / ESCAPED-CHAR); TSAFE-CHAR excludes the control characters but tab.
            var writer = new IcalSpanWriter(destination);
            var index = 0;
            while (index < value.Length)
            {
                var plainLength = 0;
                while (index + plainLength < value.Length && !IsTextSpecialCharacter(value[index + plainLength]))
                {
                    plainLength++;
                }

                writer.Append(value.Slice(index, plainLength));
                index += plainLength;
                if (index == value.Length) break;

                var character = value[index++];
                switch (character)
                {
                    case '\\':
                    case ';':
                    case ',':
                        writer.Append('\\');
                        writer.Append(character);
                        break;
                    case '\r':
                    case '\n':
                        if (character == '\r' && index < value.Length && value[index] == '\n')
                        {
                            index++;
                        }

                        writer.Append('\\');
                        writer.Append('n');
                        break;
                    default:
                        if (char.IsHighSurrogate(character) && index < value.Length && char.IsLowSurrogate(value[index]))
                        {
                            writer.Append(character);
                            writer.Append(value[index++]);
                            break;
                        }

                        throw new ArgumentException(
                            string.Format(CultureInfo.InvariantCulture, "The text holds the character U+{0:X4}, which a TEXT value cannot hold.", (int)character),
                            nameof(value));
                }
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats text as a TEXT value (RFC 5545 §3.3.11), escaping <c>\</c>, <c>;</c> and <c>,</c> with a backslash and writing each line
        /// break (CRLF, LF or a lone CR) as <c>\n</c>.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a control character other than a tab or a line break, or a lone
        /// surrogate; or, in <see cref="IcalValueFormat.Xml"/> form, a character XML cannot hold, such as U+FFFE.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static string FormatText(string value, IcalValueFormat format)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            // Escaping at most doubles the length.
            var buffer = new char[value.Length * 2];
            int charsWritten;
            return IcalValueFormatting.ToString(buffer, TryFormatText(value.AsSpan(), buffer, out charsWritten, format), charsWritten);
        }

        /// <summary>
        /// Splits a list of TEXT values on the commas that are not escaped.  Each item keeps its escaping, so parse it with
        /// <see cref="TryParseText(ReadOnlySpan{char}, IcalValueFormat, Span{char}, out int)"/>.
        /// </summary>
        /// <remarks>
        /// In jCal each item of a list is a JSON string of its own, and in xCal an element of its own, so a value in
        /// <see cref="IcalValueFormat.Json"/> or <see cref="IcalValueFormat.Xml"/> form is a single item.
        /// </remarks>
        /// <param name="value">The list as it appears in the content line, such as <c>APPOINTMENT,EDUCATION\, TRAINING</c>.  An empty value is
        /// one empty item.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>An enumerator over the items, for use with <c>foreach</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static IcalTextListEnumerator EnumerateTextList(ReadOnlySpan<char> value, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            return new IcalTextListEnumerator(value, format != IcalValueFormat.Text);
        }

        #endregion

        #region BOOLEAN

        /// <summary>
        /// Parses a BOOLEAN value (RFC 5545 §3.3.2): <c>TRUE</c> or <c>FALSE</c>, in any case.
        /// </summary>
        /// <remarks>
        /// In <see cref="IcalValueFormat.Json"/> form the value is the text of a JSON literal (RFC 7265 §3.6.2): <c>true</c> or <c>false</c>, in
        /// lower case.  In <see cref="IcalValueFormat.Xml"/> form the value is an <c>xsd:boolean</c> (RFC 6321 §3.6.2): <c>true</c>,
        /// <c>false</c>, <c>1</c> or <c>0</c>, with any white space around it.
        /// </remarks>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The Boolean, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryParseBoolean(ReadOnlySpan<char> value, IcalValueFormat format, out bool result)
        {
            IcalValueFormatting.CheckFormat(format);

            if (format == IcalValueFormat.Xml)
            {
                // ref. XML Schema Part 2 §3.2.2.1: the literals of xsd:boolean are "true", "false", "1" and "0".
                var trimmed = IcalValueFormatting.TrimXmlWhitespace(value);
                result = trimmed.SequenceEqual(XmlTrueText.AsSpan()) || trimmed.SequenceEqual("1".AsSpan());
                return result || trimmed.SequenceEqual(XmlFalseText.AsSpan()) || trimmed.SequenceEqual("0".AsSpan());
            }

            if (format == IcalValueFormat.Json)
            {
                // ref. RFC 8259§3: the literal names are lower case.
                result = value.SequenceEqual(XmlTrueText.AsSpan());
                return result || value.SequenceEqual(XmlFalseText.AsSpan());
            }

            result = IcalValueFormatting.EqualsKeyword(value, TrueText);
            return result || IcalValueFormatting.EqualsKeyword(value, FalseText);
        }

        /// <summary>
        /// Formats a BOOLEAN value (RFC 5545 §3.3.2): <c>TRUE</c> or <c>FALSE</c>; or, in <see cref="IcalValueFormat.Json"/> and
        /// <see cref="IcalValueFormat.Xml"/> form, <c>true</c> or <c>false</c>.
        /// </summary>
        /// <param name="value">The Boolean.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryFormatBoolean(bool value, Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            var writer = new IcalSpanWriter(destination);
            if (format != IcalValueFormat.Text)
            {
                writer.Append(value ? XmlTrueText : XmlFalseText);
            }
            else
            {
                writer.Append(value ? TrueText : FalseText);
            }

            return writer.TryComplete(out charsWritten);
        }

        #endregion

        #region INTEGER

        /// <summary>
        /// Parses an INTEGER value (RFC 5545 §3.3.8): an optional sign and one or more digits, from -2147483648 to 2147483647.
        /// </summary>
        /// <remarks>
        /// In <see cref="IcalValueFormat.Json"/> form the value is the text of a JSON number (RFC 7265 §3.6.8), which has no plus sign or
        /// leading zeros, and may have a fraction or an exponent as long as the number is a whole one, such as <c>1.0</c> or <c>1e2</c>.  In
        /// <see cref="IcalValueFormat.Xml"/> form the value is an <c>xsd:integer</c> (RFC 6321 §3.6.8), which may have white space around it.
        /// </remarks>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The integer, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryParseInteger(ReadOnlySpan<char> value, IcalValueFormat format, out int result)
        {
            IcalValueFormatting.CheckFormat(format);
            if (format == IcalValueFormat.Xml)
            {
                value = IcalValueFormatting.TrimXmlWhitespace(value);
            }
            else if (format == IcalValueFormat.Json)
            {
                return TryParseJsonInteger(value, out result);
            }

            // ref. RFC 5545§3.3.8: integer = (["+"] / "-") 1*DIGIT
            result = 0;
            var index = 0;
            var isNegative = false;
            if (value.Length > 0 && (value[0] == '+' || value[0] == '-'))
            {
                isNegative = value[0] == '-';
                index = 1;
            }

            long number;
            if (!IcalValueFormatting.TryParseNumber(value, ref index, isNegative ? -(long)int.MinValue : int.MaxValue, out number) ||
                index != value.Length)
            {
                return false;
            }

            result = (int)(isNegative ? -number : number);
            return true;
        }

        /// <summary>
        /// Formats an INTEGER value (RFC 5545 §3.3.8).
        /// </summary>
        /// <param name="value">The integer.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryFormatInteger(int value, Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            var writer = new IcalSpanWriter(destination);
            if (value < 0)
            {
                writer.Append('-');
            }

            writer.AppendNumber(Math.Abs((long)value));
            return writer.TryComplete(out charsWritten);
        }

        #endregion

        #region FLOAT

        /// <summary>
        /// Parses a FLOAT value (RFC 5545 §3.3.7): an optional sign, one or more digits, and optionally a decimal point and one or more digits.
        /// There is no exponent.
        /// </summary>
        /// <remarks>
        /// In <see cref="IcalValueFormat.Xml"/> form the value is an <c>xsd:float</c> (RFC 6321 §3.6.7), which may also have an exponent, such
        /// as <c>1.5E3</c>, a decimal point with digits on only one side, and white space around it.  <c>INF</c>, <c>-INF</c> and <c>NaN</c> are
        /// not valid, since a FLOAT value cannot hold them.  In <see cref="IcalValueFormat.Json"/> form the value is the text of a JSON number
        /// (RFC 7265 §3.6.7), which has no plus sign or leading zeros, and may have an exponent.
        /// </remarks>
        /// <param name="value">The value, such as <c>-3.14</c>.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The number, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>, including when it is too large for a <see cref="double"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryParseFloat(ReadOnlySpan<char> value, IcalValueFormat format, out double result)
        {
            IcalValueFormatting.CheckFormat(format);
            if (format == IcalValueFormat.Xml) return TryParseXmlFloat(IcalValueFormatting.TrimXmlWhitespace(value), out result);

            if (format == IcalValueFormat.Json)
            {
                result = 0;
                return IcalValueFormatting.IsJsonNumber(value) &&
                       TryParseDouble(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, out result);
            }

            // ref. RFC 5545§3.3.7: float = (["+"] / "-") 1*DIGIT ["." 1*DIGIT]
            result = 0;
            var index = 0;
            if (value.Length > 0 && (value[0] == '+' || value[0] == '-'))
            {
                index = 1;
            }

            var integerStart = index;
            while (index < value.Length && IcalValueFormatting.IsDigit(value[index]))
            {
                index++;
            }

            if (index == integerStart) return false;

            if (index < value.Length)
            {
                if (value[index] != '.') return false;

                var fractionStart = ++index;
                while (index < value.Length && IcalValueFormatting.IsDigit(value[index]))
                {
                    index++;
                }

                if (index == fractionStart || index != value.Length) return false;
            }

            return TryParseDouble(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, out result);
        }

        /// <summary>
        /// Formats a FLOAT value (RFC 5545 §3.3.7) with the fewest digits that parse back to the same number, and no exponent.
        /// </summary>
        /// <param name="value">The number.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a number or is infinite, which a FLOAT value cannot
        /// hold; or <paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryFormatFloat(double value, Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            var writer = new IcalSpanWriter(destination);
            writer.Append(FormatFloatText(value));
            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats a FLOAT value (RFC 5545 §3.3.7) with the fewest digits that parse back to the same number, and no exponent.
        /// </summary>
        /// <param name="value">The number.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a number or is infinite, which a FLOAT value cannot
        /// hold; or <paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static string FormatFloat(double value, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            return FormatFloatText(value);
        }

        #endregion

        #region BINARY

        /// <summary>
        /// Parses a BINARY value (RFC 5545 §3.3.1): the base64 encoding of RFC 4648, with padding and no line breaks.
        /// </summary>
        /// <remarks>
        /// In <see cref="IcalValueFormat.Xml"/> form, white space may appear anywhere in the value to wrap it (RFC 6321 §3.6.1), and is ignored.
        /// </remarks>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The bytes, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryParseBinary(ReadOnlySpan<char> value, IcalValueFormat format, [NotNullWhen(true)] out byte[]? result)
#else
        public static bool TryParseBinary(ReadOnlySpan<char> value, IcalValueFormat format, out byte[] result)
#endif
        {
            IcalValueFormatting.CheckFormat(format);
            if (format == IcalValueFormat.Xml)
            {
                value = RemoveXmlWhitespace(value);
            }

            // ref. RFC 4648§4: each 4 characters encode 3 bytes; "=" pads the last group.
            result = null;
            if (value.Length % 4 != 0) return false;

            var padding = 0;
            if (value.Length > 0 && value[value.Length - 1] == '=')
            {
                padding = value[value.Length - 2] == '=' ? 2 : 1;
            }

            var bytes = new byte[value.Length / 4 * 3 - padding];
            var byteIndex = 0;
            for (var index = 0; index < value.Length; index += 4)
            {
                var group = 0;
                var groupLength = index + 4 == value.Length ? 4 - padding : 4;
                for (var offset = 0; offset < 4; offset++)
                {
                    var sextet = 0;
                    if (offset < groupLength)
                    {
                        sextet = Base64Alphabet.IndexOf(value[index + offset]);
                        if (sextet < 0) return false;
                    }

                    group = (group << 6) | sextet;
                }

                for (var shift = 16; shift >= 0 && byteIndex < bytes.Length; shift -= 8)
                {
                    bytes[byteIndex++] = (byte)(group >> shift);
                }
            }

            result = bytes;
            return true;
        }

        /// <summary>
        /// Formats a BINARY value (RFC 5545 §3.3.1): the base64 encoding of RFC 4648, with padding and no line breaks.
        /// </summary>
        /// <param name="value">The bytes.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryFormatBinary(ReadOnlySpan<byte> value, Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            IcalValueFormatting.CheckFormat(format);

            var length = (value.Length + 2) / 3 * 4;
            if (destination.Length < length)
            {
                charsWritten = 0;
                return false;
            }

            var position = 0;
            for (var index = 0; index < value.Length; index += 3)
            {
                var remaining = Math.Min(3, value.Length - index);
                var group = value[index] << 16;
                if (remaining > 1)
                {
                    group |= value[index + 1] << 8;
                }

                if (remaining > 2)
                {
                    group |= value[index + 2];
                }

                for (var offset = 0; offset < 4; offset++)
                {
                    destination[position++] = offset <= remaining ? Base64Alphabet[(group >> (18 - 6 * offset)) & 0x3F] : '=';
                }
            }

            charsWritten = length;
            return true;
        }

        /// <summary>
        /// Formats a BINARY value (RFC 5545 §3.3.1): the base64 encoding of RFC 4648, with padding and no line breaks.
        /// </summary>
        /// <param name="value">The bytes.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static string FormatBinary(byte[] value, IcalValueFormat format)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var buffer = new char[(value.Length + 2) / 3 * 4];
            int charsWritten;
            return IcalValueFormatting.ToString(buffer, TryFormatBinary(value, buffer, out charsWritten, format), charsWritten);
        }

        #endregion

        #region URI and CAL-ADDRESS

        /// <summary>
        /// Parses a URI value (RFC 5545 §3.3.13): an absolute URI, which starts with a scheme.
        /// </summary>
        /// <remarks>
        /// In <see cref="IcalValueFormat.Xml"/> form the value is an <c>xsd:anyURI</c> (RFC 6321 §3.6.13), which may have white space around it.
        /// </remarks>
        /// <param name="value">The value, such as <c>http://example.com/my-report.txt</c>.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The URI, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryParseUri(ReadOnlySpan<char> value, IcalValueFormat format, [NotNullWhen(true)] out Uri? result)
#else
        public static bool TryParseUri(ReadOnlySpan<char> value, IcalValueFormat format, out Uri result)
#endif
        {
            IcalValueFormatting.CheckFormat(format);
            if (format == IcalValueFormat.Xml)
            {
                value = IcalValueFormatting.TrimXmlWhitespace(value);
            }

            // ref. RFC 3986§3: URI = scheme ":" hier-part [ "?" query ] [ "#" fragment ]; scheme = ALPHA *( ALPHA / DIGIT / "+" / "-" / "." ).
            // Checking the characters and the scheme first stops a path being taken as a file URI.
            result = null;
            return IsUriText(value) && Uri.TryCreate(value.ToString(), UriKind.Absolute, out result);
        }

        /// <summary>
        /// Formats a URI value (RFC 5545 §3.3.13), as the URI was given.
        /// </summary>
        /// <param name="value">The URI, which must be absolute.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not absolute.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryFormatUri(Uri value, Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (!value.IsAbsoluteUri)
                throw new ArgumentException("The URI must be absolute.", nameof(value));

            IcalValueFormatting.CheckFormat(format);

            var writer = new IcalSpanWriter(destination);
            writer.Append(value.OriginalString);
            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Parses a CAL-ADDRESS value (RFC 5545 §3.3.3): a URI identifying a calendar user, usually a <c>mailto</c> URI.
        /// </summary>
        /// <param name="value">The value, such as <c>mailto:jane_doe@example.com</c>.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The URI, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryParseCalAddress(ReadOnlySpan<char> value, IcalValueFormat format, [NotNullWhen(true)] out Uri? result)
#else
        public static bool TryParseCalAddress(ReadOnlySpan<char> value, IcalValueFormat format, out Uri result)
#endif
        {
            // ref. RFC 5545§3.3.3: cal-address = uri
            return TryParseUri(value, format, out result);
        }

        /// <summary>
        /// Formats a CAL-ADDRESS value (RFC 5545 §3.3.3), as the URI was given.
        /// </summary>
        /// <param name="value">The URI, which must be absolute.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not absolute.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not an <see cref="IcalValueFormat"/> member.</exception>
        public static bool TryFormatCalAddress(Uri value, Span<char> destination, out int charsWritten, IcalValueFormat format)
        {
            return TryFormatUri(value, destination, out charsWritten, format);
        }

        #endregion

        /// <summary>
        /// Parses the text of a JSON number that is a whole number in the range of an <see cref="int"/>.
        /// </summary>
        private static bool TryParseJsonInteger(ReadOnlySpan<char> value, out int result)
        {
            // ref. RFC 7265§3.6.8: a JSON number that resolves to an integer, so a fraction or an exponent is allowed only where it does.
            result = 0;
            double number;
            if (!IcalValueFormatting.IsJsonNumber(value) ||
                !TryParseDouble(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, out number) ||
                Math.Floor(number) != number || number < int.MinValue || number > int.MaxValue)
            {
                return false;
            }

            result = (int)number;
            return true;
        }

        /// <summary>
        /// Finds the first character that a TEXT value cannot hold: a control character other than a tab or a line break, or a lone surrogate.
        /// </summary>
        /// <returns>The index of the character; or -1 if there is none.</returns>
        private static int FindInvalidTextCharacter(ReadOnlySpan<char> value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character == '\t' || character == '\r' || character == '\n') continue;

                if (char.IsHighSurrogate(character) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                {
                    index++;
                    continue;
                }

                if (character < ' ' || character == '\x7F' || char.IsSurrogate(character)) return index;
            }

            return -1;
        }

        private static bool IsTextSpecialCharacter(char character)
        {
            return character == '\\' || character == ';' || character == ',' || (character < ' ' && character != '\t') || character == '\x7F' ||
                   char.IsSurrogate(character);
        }

        private static ReadOnlySpan<char> RemoveXmlWhitespace(ReadOnlySpan<char> value)
        {
            var whitespaceCount = 0;
            foreach (var character in value)
            {
                if (IcalValueFormatting.IsXmlWhitespace(character))
                {
                    whitespaceCount++;
                }
            }

            if (whitespaceCount == 0) return value;

            var buffer = new char[value.Length - whitespaceCount];
            var length = 0;
            foreach (var character in value)
            {
                if (!IcalValueFormatting.IsXmlWhitespace(character))
                {
                    buffer[length++] = character;
                }
            }

            return buffer;
        }

        /// <summary>
        /// Parses the numeric literals of <c>xsd:float</c>, whose white space has been removed.
        /// </summary>
        private static bool TryParseXmlFloat(ReadOnlySpan<char> value, out double result)
        {
            // ref. XML Schema Part 2 §3.2.4.1: a mantissa, a decimal numeral, optionally followed by "E" or "e" and an integer exponent.
            result = 0;
            var index = 0;
            if (value.Length > 0 && (value[0] == '+' || value[0] == '-'))
            {
                index = 1;
            }

            var digitCount = 0;
            while (index < value.Length && IcalValueFormatting.IsDigit(value[index]))
            {
                index++;
                digitCount++;
            }

            if (index < value.Length && value[index] == '.')
            {
                index++;
                while (index < value.Length && IcalValueFormatting.IsDigit(value[index]))
                {
                    index++;
                    digitCount++;
                }
            }

            if (digitCount == 0) return false;

            if (index < value.Length && IcalValueFormatting.IsLetter(value[index], 'E'))
            {
                index++;
                if (index < value.Length && (value[index] == '+' || value[index] == '-'))
                {
                    index++;
                }

                var exponentStart = index;
                while (index < value.Length && IcalValueFormatting.IsDigit(value[index]))
                {
                    index++;
                }

                if (index == exponentStart) return false;
            }

            if (index != value.Length) return false;

            return TryParseDouble(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, out result);
        }

        private static bool TryParseDouble(ReadOnlySpan<char> value, NumberStyles styles, out double result)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            if (!double.TryParse(value, styles, CultureInfo.InvariantCulture, out result)) return false;
#else
            if (!double.TryParse(value.ToString(), styles, CultureInfo.InvariantCulture, out result)) return false;
#endif

            return !double.IsInfinity(result);
        }

        private static string FormatFloatText(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "A FLOAT value cannot be NaN or infinite.");

            var text = value.ToString("R", CultureInfo.InvariantCulture);
            var exponentIndex = text.IndexOf('E');
            if (exponentIndex < 0) return text;

            // ref. RFC 5545§3.3.7: there is no exponent, so move the decimal point instead.
            var isNegative = text[0] == '-';
            var mantissa = text.Substring(isNegative ? 1 : 0, exponentIndex - (isNegative ? 1 : 0));
            var exponent = int.Parse(text.Substring(exponentIndex + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            var pointIndex = mantissa.IndexOf('.');
            var digits = pointIndex < 0 ? mantissa : mantissa.Remove(pointIndex, 1);
            var newPointIndex = (pointIndex < 0 ? mantissa.Length : pointIndex) + exponent;

            string expanded;
            if (newPointIndex <= 0)
            {
                expanded = "0." + new string('0', -newPointIndex) + digits;
            }
            else if (newPointIndex >= digits.Length)
            {
                expanded = digits + new string('0', newPointIndex - digits.Length);
            }
            else
            {
                expanded = digits.Substring(0, newPointIndex) + "." + digits.Substring(newPointIndex);
            }

            return isNegative ? "-" + expanded : expanded;
        }

        private static bool IsUriText(ReadOnlySpan<char> value)
        {
            if (value.Length == 0 || !IsAsciiLetter(value[0])) return false;

            // ref. RFC 3986§2: a URI is made of unreserved and reserved characters and percent-encodings, all of them printable ASCII.
            foreach (var character in value)
            {
                if (character <= ' ' || character >= '\x7F') return false;

                switch (character)
                {
                    case '"':
                    case '<':
                    case '>':
                    case '\\':
                    case '^':
                    case '`':
                    case '{':
                    case '|':
                    case '}':
                        return false;
                }
            }

            for (var index = 1; index < value.Length; index++)
            {
                var character = value[index];
                if (character == ':') return true;
                if (!IsAsciiLetter(character) && !IcalValueFormatting.IsDigit(character) && character != '+' && character != '-' && character != '.')
                {
                    return false;
                }
            }

            return false;
        }

        private static bool IsAsciiLetter(char character)
        {
            return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
        }
    }

    /// <summary>
    /// Enumerates the items of a list of TEXT values, split on the commas that are not escaped.  Get one from
    /// <see cref="IcalValue.EnumerateTextList"/>.
    /// </summary>
    public ref struct IcalTextListEnumerator
    {
        private ReadOnlySpan<char> _rest;
        private ReadOnlySpan<char> _current;
        private bool _isDone;
        private readonly bool _isSingleItem;

        internal IcalTextListEnumerator(ReadOnlySpan<char> value, bool isSingleItem)
        {
            _rest = value;
            _current = default(ReadOnlySpan<char>);
            _isDone = false;
            _isSingleItem = isSingleItem;
        }

        /// <summary>
        /// Gets the current item, with its escaping in place.
        /// </summary>
        public ReadOnlySpan<char> Current => _current;

        /// <summary>
        /// Gets the enumerator, so that it can be used with <c>foreach</c>.
        /// </summary>
        public IcalTextListEnumerator GetEnumerator()
        {
            return this;
        }

        /// <summary>
        /// Moves to the next item.
        /// </summary>
        /// <returns><c>true</c> if there is another item; otherwise <c>false</c>.</returns>
        public bool MoveNext()
        {
            if (_isDone) return false;

            if (_isSingleItem)
            {
                _current = _rest;
                _rest = default(ReadOnlySpan<char>);
                _isDone = true;
                return true;
            }

            // A backslash escapes the character after it, so a comma after a backslash is part of the item.
            for (var index = 0; index < _rest.Length; index++)
            {
                var character = _rest[index];
                if (character == '\\')
                {
                    index++;
                }
                else if (character == ',')
                {
                    _current = _rest.Slice(0, index);
                    _rest = _rest.Slice(index + 1);
                    return true;
                }
            }

            _current = _rest;
            _rest = default(ReadOnlySpan<char>);
            _isDone = true;
            return true;
        }
    }
}
