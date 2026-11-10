using System;
using DataStandardizer.File.ContentLine;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The escaping of text in a value, which differs by version.
    /// </summary>
    /// <remarks>
    /// In vCard 4.0 (RFC 6350 §3.4, §4) and vCard 3.0 (RFC 2426 §4), a backslash escapes a backslash, a comma, a semicolon or a line break, and nothing else.  In vCard 2.1
    /// (§2.9, <c>strnosemi</c>), a backslash escapes only a semicolon or a line break, and anywhere else it is an ordinary character, as a comma always is.
    /// </remarks>
    internal static class VcardText
    {
        private const char NewLine = '\n';

        /// <summary>
        /// Checks that text holds only characters a value can hold, whatever its escaping.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="text"/> holds a control character other than a tab or a line break, or a lone surrogate.</exception>
        internal static void CheckCharacters(ReadOnlySpan<char> text, string parameterName)
        {
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (character != '\r' && character != '\n' && !IsValueCharacter(text, ref index))
                    throw new ArgumentException("The text holds a character that a vCard value cannot hold.", parameterName);
            }
        }

        /// <summary>
        /// Gets whether a whole value is one <c>text</c> value of a version.
        /// </summary>
        internal static bool IsValidText(ReadOnlySpan<char> value, VcardVersion version)
        {
            // ref. vCard 2.1 §2.1.3: a value decoded from quoted-printable can hold line breaks.
            if (version == VcardVersion.V2_1) return IsValidUnescapedText(value);

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                switch (character)
                {
                    case '\\':
                        if (!IsEscape(value, index)) return false;

                        index++;
                        break;
                    case ',':
                        // ref. RFC 6350 §4: TEXT-CHAR has no comma, so it has to be escaped.  RFC 2426 §4: SAFE-CHAR has none either.
                        return false;
                    case ';':
                        // ref. RFC 2426 §4: SAFE-CHAR has no semicolon.  RFC 6350 §4: TEXT-CHAR has one.
                        if (version != VcardVersion.V4_0) return false;

                        break;
                    default:
                        if (!IsValueCharacter(value, ref index)) return false;

                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets whether text with no escaping, such as a value of vCard 2.1 or the content of a JSON string of jCard, holds only characters a value can hold: those of
        /// <see cref="CheckCharacters"/>.
        /// </summary>
        internal static bool IsValidUnescapedText(ReadOnlySpan<char> value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character != '\r' && character != '\n' && !IsValueCharacter(value, ref index)) return false;
            }

            return true;
        }

        /// <summary>
        /// Checks a whole structured value, and counts the components it is made of.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="allowsLists"><c>true</c> if a comma may separate the items of a component, in vCard 4.0 and vCard 3.0.  Otherwise a comma has to be escaped there.</param>
        /// <param name="componentCount">The number of components.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        internal static bool TryCountComponents(ReadOnlySpan<char> value, VcardVersion version, bool allowsLists, out int componentCount)
        {
            componentCount = 1;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                switch (character)
                {
                    case ';':
                        componentCount++;
                        break;
                    case '\\':
                        if (version == VcardVersion.V2_1)
                        {
                            // ref. vCard 2.1 §2.9: strnosemi = *(*nonsemi ("\;" / "\" CRLF)) *nonsemi
                            index += GetEscapeLengthVersion21(value, index) - 1;
                        }
                        else
                        {
                            if (!IsEscape(value, index)) return false;

                            index++;
                        }

                        break;
                    case ',':
                        if (version != VcardVersion.V2_1 && !allowsLists) return false;

                        break;
                    default:
                        if (!IsValueCharacter(value, ref index)) return false;

                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the index of the first delimiter that isn't escaped; or the length of the value if there is none.
        /// </summary>
        internal static int IndexOfDelimiter(ReadOnlySpan<char> value, char delimiter, VcardVersion version)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character == delimiter) return index;

                if (character == '\\')
                {
                    index += version == VcardVersion.V2_1 ? GetEscapeLengthVersion21(value, index) - 1 : 1;
                }
            }

            return value.Length;
        }

        /// <summary>
        /// Removes the escaping from a valid item.
        /// </summary>
        /// <param name="source">The item, with its escaping in place.</param>
        /// <param name="destination">Receives the text, and is at least as long as <paramref name="source"/>.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The number of characters written.</returns>
        internal static int Unescape(ReadOnlySpan<char> source, Span<char> destination, VcardVersion version)
        {
            int written;
            if (version != VcardVersion.V2_1)
            {
                int errorIndex;
                ContentLineEscaping.TryUnescapeText(source, destination, out written, out errorIndex);
                return written;
            }

            written = 0;
            for (var index = 0; index < source.Length; index++)
            {
                var character = source[index];
                var escapeLength = character == '\\' ? GetEscapeLengthVersion21(source, index) : 1;
                if (escapeLength == 1)
                {
                    destination[written++] = character;
                }
                else if (escapeLength == 2)
                {
                    destination[written++] = ';';
                    index++;
                }
                else
                {
                    destination[written++] = NewLine;
                    index += 2;
                }
            }

            return written;
        }

        /// <summary>
        /// Removes the escaping from a valid item.
        /// </summary>
        internal static string Unescape(ReadOnlySpan<char> source, VcardVersion version)
        {
            if (source.IndexOf('\\') < 0) return source.ToString();

            var buffer = new char[source.Length];
            return new string(buffer, 0, Unescape(source, buffer, version));
        }

        /// <summary>
        /// Appends text with the escaping a version needs.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="text">The text, whose characters have been checked.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="escapesSemicolon"><c>true</c> to escape a semicolon where vCard 4.0 leaves it to the writer, in a value that isn't compound.</param>
        internal static void AppendEscaped(ref VcardSpanWriter writer, ReadOnlySpan<char> text, VcardVersion version, bool escapesSemicolon)
        {
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                switch (character)
                {
                    case '\r':
                    case '\n':
                        // Each line break, whether CRLF, LF or a lone CR, is one newline.
                        if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                        {
                            index++;
                        }

                        if (version == VcardVersion.V2_1)
                        {
                            writer.Append('\\');
                            writer.Append('\r');
                            writer.Append('\n');
                        }
                        else
                        {
                            writer.Append('\\');
                            writer.Append('n');
                        }

                        break;
                    case ';':
                        if (escapesSemicolon)
                        {
                            writer.Append('\\');
                        }

                        writer.Append(';');
                        break;
                    case '\\':
                    case ',':
                        if (version != VcardVersion.V2_1)
                        {
                            writer.Append('\\');
                        }

                        writer.Append(character);
                        break;
                    default:
                        writer.Append(character);
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the most characters <see cref="AppendEscaped"/> can write for text of a given length: three for each character, for a line break in vCard 2.1.
        /// </summary>
        internal static int GetMaxEscapedLength(int length)
        {
            return length * 3;
        }

        /// <summary>
        /// Gets whether vCard 2.1 can write a component of a compound value holding this text: a backslash at the end of one would escape the semicolon after it (§2.9), and a
        /// backslash before a semicolon or a line break would be taken for an escape.
        /// </summary>
        internal static bool CanWriteInVersion21Component(string text)
        {
            return text.Length == 0 || text[text.Length - 1] != '\\';
        }

        private static bool IsEscape(ReadOnlySpan<char> value, int backslashIndex)
        {
            // ref. RFC 6350 §3.4: "In all other cases, escaping MUST NOT be used."  RFC 2426 §4: ESCAPED-CHAR = "\\" / "\;" / "\," / "\n" / "\N"
            if (backslashIndex + 1 == value.Length) return false;

            switch (value[backslashIndex + 1])
            {
                case '\\':
                case ',':
                case ';':
                case 'n':
                case 'N':
                    return true;
                default:
                    return false;
            }
        }

        // The number of characters at a backslash that make one character of the text: 2 for "\;", 3 for "\" CRLF, and 1 for a backslash that stands for itself.
        private static int GetEscapeLengthVersion21(ReadOnlySpan<char> value, int backslashIndex)
        {
            if (backslashIndex + 1 < value.Length && value[backslashIndex + 1] == ';') return 2;
            if (backslashIndex + 2 < value.Length && value[backslashIndex + 1] == '\r' && value[backslashIndex + 2] == '\n') return 3;

            return 1;
        }

        // ref. RFC 6350 §3.3: WSP, printable ASCII and NON-ASCII, so a tab but no other control character.  A surrogate has to be half of a pair, which moves the index on.
        private static bool IsValueCharacter(ReadOnlySpan<char> value, ref int index)
        {
            var character = value[index];
            if (character < ' ') return character == '\t';
            if (character == '\x7F') return false;

            if (char.IsHighSurrogate(character))
            {
                if (index + 1 == value.Length || !char.IsLowSurrogate(value[index + 1])) return false;

                index++;
                return true;
            }

            return !char.IsLowSurrogate(character);
        }
    }
}
