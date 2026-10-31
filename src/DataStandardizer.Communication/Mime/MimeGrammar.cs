using System;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Character classes and checks shared by the MIME value types.
    /// </summary>
    internal static class MimeGrammar
    {
        // ref. RFC 6838 §4.2: restricted-name = restricted-name-first *126restricted-name-chars
        internal const int MaxRestrictedNameLength = 127;

        /// <summary>
        /// Determine if a character may appear in an RFC 2045 §5.1 token.
        /// </summary>
        /// <remarks>
        /// token := 1*&lt;any (US-ASCII) CHAR except SPACE, CTLs, or tspecials&gt;
        /// </remarks>
        internal static bool IsTokenChar(char c)
        {
            if (c <= ' ' || c >= '\x7F') return false;

            switch (c)
            {
                // ref. RFC 2045 §5.1: tspecials
                case '(': case ')': case '<': case '>': case '@':
                case ',': case ';': case ':': case '\\': case '"':
                case '/': case '[': case ']': case '?': case '=':
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Determine if a string is an RFC 2045 §5.1 token.
        /// </summary>
        internal static bool IsToken(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty) return false;

            foreach (var c in value)
            {
                if (!IsTokenChar(c)) return false;
            }

            return true;
        }

        /// <summary>
        /// Determine if a string is an RFC 6838 §4.2 restricted-name.
        /// </summary>
        /// <remarks>
        /// restricted-name-first = ALPHA / DIGIT;
        /// restricted-name-chars = ALPHA / DIGIT / "!" / "#" / "$" / "&amp;" / "-" / "^" / "_" / "." / "+"
        /// </remarks>
        internal static bool IsRestrictedName(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty || value.Length > MaxRestrictedNameLength || !IsAsciiLetterOrDigit(value[0])) return false;

            for (var index = 1; index < value.Length; index++)
            {
                var c = value[index];
                if (IsAsciiLetterOrDigit(c)) continue;

                switch (c)
                {
                    case '!': case '#': case '$': case '&': case '-':
                    case '^': case '_': case '.': case '+':
                        continue;
                    default:
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determine if a string is a valid type, subtype or parameter name.
        /// </summary>
        internal static bool IsName(ReadOnlySpan<char> value, bool allowRfc2045Tokens)
        {
            return allowRfc2045Tokens ? IsToken(value) : IsRestrictedName(value);
        }

        /// <summary>
        /// Determine if a character may appear in a parameter value, either as itself or as a quoted-pair.
        /// </summary>
        /// <remarks>
        /// Printable US-ASCII, SPACE and HTAB (ref. RFC 5322 §3.2.4: qtext, quoted-pair).
        /// </remarks>
        internal static bool IsValueChar(char c)
        {
            return (c >= ' ' && c < '\x7F') || c == '\t';
        }

        /// <summary>
        /// Determine if every character of a parameter value can be written.
        /// </summary>
        internal static bool IsValue(string value)
        {
            foreach (var c in value)
            {
                if (!IsValueChar(c)) return false;
            }

            return true;
        }

        /// <summary>
        /// Determine if a character must be escaped as a quoted-pair within a quoted-string.
        /// </summary>
        internal static bool IsQuotedPairChar(char c)
        {
            return c == '"' || c == '\\';
        }

        internal static bool IsWhitespace(char c)
        {
            return c == ' ' || c == '\t';
        }

        internal static bool IsAsciiLetterOrDigit(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
        }

        /// <summary>
        /// Determine if a string starts with a prefix, ignoring the case of US-ASCII letters.
        /// </summary>
        internal static bool StartsWithIgnoreCase(string value, string prefix)
        {
            return value.Length >= prefix.Length && string.Compare(value, 0, prefix, 0, prefix.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Determine if a string equals a lower-case US-ASCII string, ignoring the case of US-ASCII letters.
        /// </summary>
        internal static bool EqualsIgnoreCase(ReadOnlySpan<char> value, string lowerCase)
        {
            if (value.Length != lowerCase.Length) return false;

            for (var index = 0; index < value.Length; index++)
            {
                var c = value[index];
                if (c >= 'A' && c <= 'Z') c = (char)(c + ('a' - 'A'));
                if (c != lowerCase[index]) return false;
            }

            return true;
        }

        /// <summary>
        /// Get the number of characters a parameter value takes when written, quoted if it is not a token.
        /// </summary>
        internal static int GetFormattedValueLength(string value)
        {
            if (IsToken(value.AsSpan())) return value.Length;

            var length = value.Length + 2;
            foreach (var c in value)
            {
                if (IsQuotedPairChar(c)) length++;
            }

            return length;
        }

        /// <summary>
        /// Write a parameter value, quoted if it is not a token.
        /// </summary>
        /// <returns><c>false</c> if <paramref name="destination"/> is too short.</returns>
        internal static bool TryWriteValue(string value, Span<char> destination, ref int position)
        {
            if (IsToken(value.AsSpan())) return TryWrite(value, destination, ref position);

            if (GetFormattedValueLength(value) > destination.Length - position) return false;

            destination[position++] = '"';
            foreach (var c in value)
            {
                if (IsQuotedPairChar(c)) destination[position++] = '\\';
                destination[position++] = c;
            }

            destination[position++] = '"';
            return true;
        }

        /// <summary>
        /// Write a string.
        /// </summary>
        /// <returns><c>false</c> if <paramref name="destination"/> is too short.</returns>
        internal static bool TryWrite(string value, Span<char> destination, ref int position)
        {
            if (!value.AsSpan().TryCopyTo(destination.Slice(position))) return false;

            position += value.Length;
            return true;
        }

        /// <summary>
        /// Write a character.
        /// </summary>
        /// <returns><c>false</c> if <paramref name="destination"/> is too short.</returns>
        internal static bool TryWrite(char value, Span<char> destination, ref int position)
        {
            if (position >= destination.Length) return false;

            destination[position++] = value;
            return true;
        }

        /// <summary>
        /// Read a quoted-string, starting at its opening quote, and decode its quoted-pairs.
        /// </summary>
        /// <param name="source">Text to read.</param>
        /// <param name="position">Position of the opening quote; on success, the position after the closing quote.</param>
        /// <param name="value">The decoded value.</param>
        /// <returns><c>true</c> if a well-formed quoted-string was read; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// quoted-string = DQUOTE *([FWS] qcontent) [FWS] DQUOTE; qcontent = qtext / quoted-pair (ref. RFC 5322 §3.2.4).
        /// Folding is not accepted, as the caller unfolds header fields first.
        /// </remarks>
        internal static bool TryReadQuotedString(ReadOnlySpan<char> source, ref int position, out string value)
        {
            value = string.Empty;

            // First pass: find the closing quote and count the quoted-pairs.
            var start = position + 1;
            var index = start;
            var escapeCount = 0;
            while (true)
            {
                if (index >= source.Length) return false;

                var c = source[index];
                if (c == '"') break;

                if (c == '\\')
                {
                    // ref. RFC 5322 §3.2.1: quoted-pair = ("\" (VCHAR / WSP))
                    if (index + 1 >= source.Length || !IsValueChar(source[index + 1])) return false;

                    escapeCount++;
                    index += 2;
                    continue;
                }

                if (!IsValueChar(c)) return false;

                index++;
            }

            var content = source.Slice(start, index - start);
            position = index + 1;
            if (escapeCount == 0)
            {
                value = content.ToString();
                return true;
            }

            // Second pass: drop the backslash of each quoted-pair.
            var decoded = new char[content.Length - escapeCount];
            var length = 0;
            for (var contentIndex = 0; contentIndex < content.Length; contentIndex++)
            {
                if (content[contentIndex] == '\\') contentIndex++;
                decoded[length++] = content[contentIndex];
            }

            value = new string(decoded);
            return true;
        }
    }
}
