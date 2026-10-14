using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Decodes the escaping used inside content lines.  Neither decoding ever lengthens a value, so a destination as long as the source is
    /// always enough.
    /// </summary>
    internal static class IcalEscaping
    {
        /// <summary>
        /// The character a decoded newline becomes.  It is never taken from <see cref="Environment.NewLine"/>.
        /// </summary>
        internal const char NewLine = '\n';

        /// <summary>
        /// Decodes RFC 6868 caret encoding in a parameter value: <c>^n</c> becomes a newline, <c>^'</c> a double quote and <c>^^</c> a caret.  A
        /// caret followed by anything else is left as it is.
        /// </summary>
        /// <returns>The number of characters written to <paramref name="destination"/>.</returns>
        internal static int DecodeCaret(ReadOnlySpan<char> source, Span<char> destination)
        {
            var written = 0;
            var index = 0;
            while (index < source.Length)
            {
                var caretIndex = source.Slice(index).IndexOf('^');
                if (caretIndex < 0)
                {
                    source.Slice(index).CopyTo(destination.Slice(written));
                    written += source.Length - index;
                    break;
                }

                source.Slice(index, caretIndex).CopyTo(destination.Slice(written));
                written += caretIndex;
                index += caretIndex;

                var decoded = index + 1 < source.Length ? DecodeCaretCharacter(source[index + 1]) : '\0';
                if (decoded != '\0')
                {
                    destination[written++] = decoded;
                    index += 2;
                }
                else
                {
                    destination[written++] = '^';
                    index++;
                }
            }

            return written;
        }

        /// <summary>
        /// Removes RFC 5545 §3.3.11 TEXT escaping: <c>\\</c>, <c>\;</c> and <c>\,</c> become the escaped character, and <c>\n</c> or <c>\N</c> a
        /// newline.
        /// </summary>
        /// <param name="source">The escaped value.</param>
        /// <param name="destination">Receives the unescaped value.</param>
        /// <param name="written">The number of characters written to <paramref name="destination"/>.</param>
        /// <param name="errorIndex">The index in <paramref name="source"/> of a backslash that does not start a valid escape; otherwise -1.</param>
        /// <returns><c>true</c> if the value was unescaped; <c>false</c> if it contains an invalid escape.</returns>
        internal static bool TryUnescapeText(ReadOnlySpan<char> source, Span<char> destination, out int written, out int errorIndex)
        {
            written = 0;
            var index = 0;
            while (index < source.Length)
            {
                var backslashIndex = source.Slice(index).IndexOf('\\');
                if (backslashIndex < 0)
                {
                    source.Slice(index).CopyTo(destination.Slice(written));
                    written += source.Length - index;
                    break;
                }

                source.Slice(index, backslashIndex).CopyTo(destination.Slice(written));
                written += backslashIndex;
                index += backslashIndex;

                var unescaped = index + 1 < source.Length ? UnescapeTextCharacter(source[index + 1]) : '\0';
                if (unescaped == '\0')
                {
                    errorIndex = index;
                    return false;
                }

                destination[written++] = unescaped;
                index += 2;
            }

            errorIndex = -1;
            return true;
        }

        private static char DecodeCaretCharacter(char character)
        {
            switch (character)
            {
                case 'n':
                    return NewLine;
                case '\'':
                    return '"';
                case '^':
                    return '^';
                default:
                    return '\0';
            }
        }

        private static char UnescapeTextCharacter(char character)
        {
            switch (character)
            {
                case '\\':
                case ';':
                case ',':
                    return character;
                case 'n':
                case 'N':
                    return NewLine;
                default:
                    return '\0';
            }
        }
    }
}
