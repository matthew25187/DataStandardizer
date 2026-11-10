using System;
using System.Buffers;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Formats and checks header fields for the writers.
    /// </summary>
    internal static class ImfHeaderFormatter
    {
        // ref. RFC 5322 §2.1.1: each line "MUST be no more than 998 characters, and SHOULD be no more than 78 characters", excluding the CRLF.
        // ref. RFC 6532 §3.4: the first limit is in octets, and the second stays in characters.
        internal const int MaxLineLength = 998;
        internal const int TargetLineLength = 78;

        private const byte CarriageReturn = 0x0D;
        private const byte Colon = 0x3A;
        private const byte LineFeed = 0x0A;
        private const byte Space = 0x20;
        private const byte Tab = 0x09;

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        /// <summary>
        /// Gets the UTF-8 of a string that a caller gave as a field name or body.
        /// </summary>
        /// <exception cref="ImfFileException">The string is not valid Unicode.</exception>
        internal static byte[] GetBytes(string text)
        {
            try
            {
                return StrictUtf8.GetBytes(text);
            }
            catch (EncoderFallbackException exception)
            {
                throw new ImfFileException("A header field holds a surrogate code unit that is not part of a pair, so it can't be written as UTF-8.", exception);
            }
        }

        /// <summary>
        /// Gets the body of an unstructured field that holds some text, which is encoded as the encoding allows.
        /// </summary>
        /// <exception cref="ImfFileException">The text can't be written.</exception>
        internal static byte[] GetTextBody(ReadOnlySpan<byte> name, string text, ImfHeaderEncoding encoding)
        {
            if (!ImfGrammar.IsWritableText(text))
                throw Error("Header field text must not hold a line break, other control character, or surrogate code unit that is not part of a pair.", name);

            return GetBytes(ImfHeaderText.FormatUnstructuredCore(text, encoding));
        }

        /// <summary>
        /// Checks a field name, and formats a field into a buffer: the name, the colon, the body folded as its kind allows, and the final line break.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The unfolded field body.</param>
        /// <param name="kind">How the field may be folded.</param>
        /// <param name="encoding">How text that is not US-ASCII may be written.</param>
        /// <param name="lineBreak">The line break.</param>
        /// <param name="buffer">A buffer from the shared pool, replaced by a larger one if it is too short.</param>
        /// <returns>The number of bytes written to <paramref name="buffer"/>.</returns>
        /// <exception cref="ImfFileException">The name or body can't be written.</exception>
        internal static int Format(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value, ImfHeaderFieldKind kind, ImfHeaderEncoding encoding,
            ReadOnlySpan<byte> lineBreak, ref byte[] buffer)
        {
            CheckName(name);
            var whitespaceCount = CheckValue(name, value, encoding);

            // A field need have no space after the colon, but one is usual, and it gives the first fold somewhere to go.
            var addSpace = !value.IsEmpty && !IsWhitespace(value[0]);
            var bodyLength = value.Length + (addSpace ? 1 : 0);
            var capacity = name.Length + 1 + bodyLength + (whitespaceCount + 2) * lineBreak.Length;
            if (buffer.Length < capacity)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                buffer = ArrayPool<byte>.Shared.Rent(capacity);
            }

            name.CopyTo(buffer);
            buffer[name.Length] = Colon;
            var written = name.Length + 1;

            var body = ArrayPool<byte>.Shared.Rent(bodyLength + 1);
            try
            {
                if (addSpace) body[0] = Space;
                value.CopyTo(new Span<byte>(body, addSpace ? 1 : 0, value.Length));
                written = Fold(name, new ReadOnlySpan<byte>(body, 0, bodyLength), kind, lineBreak, buffer, written);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(body);
            }

            lineBreak.CopyTo(new Span<byte>(buffer, written, lineBreak.Length));
            return written + lineBreak.Length;
        }

        /// <summary>
        /// Checks that bytes a caller gave as a raw field are one whole field, so that writing them can't add a second field or end the header section.
        /// </summary>
        /// <exception cref="ImfFileException">The bytes are not one whole field.</exception>
        internal static void CheckRawField(ReadOnlySpan<byte> rawField, bool lineFeedIsLineBreak, bool acceptBareLineFeed)
        {
            GetRawFieldName(rawField);
            if (rawField[rawField.Length - 1] != LineFeed)
                throw new ImfFileException("A raw header field must end with a line break (RFC 5322 §3.6.8).");

            for (var index = 0; index < rawField.Length; index++)
            {
                if (rawField[index] == CarriageReturn)
                {
                    if (lineFeedIsLineBreak || index + 1 == rawField.Length || rawField[index + 1] != LineFeed)
                        throw new ImfFileException("A raw header field holds a carriage return that is not part of a line break (RFC 5322 §2.2).");
                }
                else if (rawField[index] == LineFeed)
                {
                    if (!lineFeedIsLineBreak && !acceptBareLineFeed && rawField[index - 1] != CarriageReturn)
                        throw new ImfFileException("A raw header field holds a line feed that is not part of a line break (RFC 5322 §2.2).");
                    if (index + 1 < rawField.Length && !IsWhitespace(rawField[index + 1]))
                        throw new ImfFileException("A raw header field must be one field: each line after the first must start with a space or tab (RFC 5322 §2.2.3).");
                }
            }
        }

        /// <summary>
        /// Gets the name of a raw field, without any white space before the colon.
        /// </summary>
        /// <exception cref="ImfFileException">The bytes don't start with a field name and a colon.</exception>
        internal static ReadOnlySpan<byte> GetRawFieldName(ReadOnlySpan<byte> rawField)
        {
            var nameLength = 0;
            while (nameLength < rawField.Length && IsNameByte(rawField[nameLength])) nameLength++;

            var colon = nameLength;
            while (colon < rawField.Length && IsWhitespace(rawField[colon])) colon++;

            if (nameLength == 0 || colon == rawField.Length || rawField[colon] != Colon)
                throw new ImfFileException("A raw header field must start with a field name and a colon (RFC 5322 §3.6.8).");

            return rawField.Slice(0, nameLength);
        }

        private static void CheckName(ReadOnlySpan<byte> name)
        {
            // ref. RFC 5322 §3.6.8: field-name = 1*ftext
            if (name.IsEmpty)
                throw new ImfFileException("A header field name must not be empty (RFC 5322 §3.6.8).");

            foreach (var value in name)
            {
                if (!IsNameByte(value))
                    throw Error("A header field name may hold only printable US-ASCII characters other than a colon (RFC 5322 §3.6.8).", name);
            }
        }

        /// <summary>
        /// Checks the bytes of a field body.
        /// </summary>
        /// <returns>The number of spaces and tabs in the body.</returns>
        private static int CheckValue(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value, ImfHeaderEncoding encoding)
        {
            var whitespaceCount = 0;
            var hasNonAscii = false;
            foreach (var octet in value)
            {
                if (IsWhitespace(octet))
                {
                    whitespaceCount++;
                }
                else if (octet >= 0x80)
                {
                    hasNonAscii = true;
                }
                else if (octet < 0x20 || octet == 0x7F)
                {
                    // ref. RFC 5322 §3.2.5: unstructured text is VCHAR and WSP, and §4 forbids generating the obsolete control characters.
                    throw Error("A header field body must not hold a line break or other control character.  Give it unfolded, or write it as a raw field.", name);
                }
            }

            if (!hasNonAscii) return whitespaceCount;

            if (encoding != ImfHeaderEncoding.Utf8)
                throw Error("A header field body is not US-ASCII.  It can be written only with a HeaderEncoding of Utf8 (RFC 6532), or as encoded words (RFC 2047), which WriteTextField and AddTextField do for text.", name);

            CheckUtf8(name, value);
            return whitespaceCount;
        }

        private static void CheckUtf8(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            var copy = ArrayPool<byte>.Shared.Rent(value.Length);
            try
            {
                value.CopyTo(copy);
                StrictUtf8.GetCharCount(copy, 0, value.Length);
            }
            catch (DecoderFallbackException exception)
            {
                var error = new ImfFileException("A header field body is not valid UTF-8 (RFC 6532 §3.2).", exception);
                error.Data[ImfFileException.DataItemName.FieldName] = GetNameText(name);
                throw error;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(copy);
            }
        }

        /// <summary>
        /// Writes a field body, with a line break before a space or tab wherever a line would otherwise be too long.  The space or tab stays, so
        /// unfolding gives the same body back (RFC 5322 §2.2.3).
        /// </summary>
        /// <param name="name">The field name, which the first line starts with.</param>
        /// <param name="body">The body.</param>
        /// <param name="kind">How the field may be folded.</param>
        /// <param name="lineBreak">The line break.</param>
        /// <param name="output">The buffer to write to.</param>
        /// <param name="written">The number of bytes already written, which are the name and colon.</param>
        /// <returns>The number of bytes now written.</returns>
        private static int Fold(ReadOnlySpan<byte> name, ReadOnlySpan<byte> body, ImfHeaderFieldKind kind, ReadOnlySpan<byte> lineBreak, byte[] output, int written)
        {
            if (written > MaxLineLength)
                throw Error("A header field can't be folded to keep each line within 998 octets (RFC 5322 §2.1.1).", name);

            // ref. RFC 5322 §4.2: only the obsolete syntax can have a line of only white space, so nothing after the last word can start a new line.
            var lastWordEnd = body.Length - 1;
            while (lastWordEnd >= 0 && IsWhitespace(body[lastWordEnd])) lastWordEnd--;

            var isStructured = kind == ImfHeaderFieldKind.Structured;
            var lineStart = 0;
            var usedOctets = written;
            var hasText = true;
            var tokens = default(StructuredTokenState);
            while (true)
            {
                var tokensAtBreak = tokens;
                var octets = usedOctets;
                var characters = usedOctets;
                var breakAt = -1;
                var index = lineStart;
                while (index < body.Length)
                {
                    var octet = body[index];
                    if (IsWhitespace(octet) && hasText && index < lastWordEnd && tokens.CanFold)
                    {
                        if (characters <= TargetLineLength)
                        {
                            breakAt = index;
                            tokensAtBreak = tokens;
                        }
                        else
                        {
                            if (breakAt < 0)
                            {
                                breakAt = index;
                                tokensAtBreak = tokens;
                            }

                            break;
                        }
                    }
                    else if (characters > TargetLineLength && breakAt >= 0)
                    {
                        break;
                    }

                    if (!IsWhitespace(octet)) hasText = true;
                    if (isStructured) tokens.Advance(octet);

                    octets++;
                    if ((octet & 0xC0) != 0x80) characters++;
                    index++;
                    if (octets > MaxLineLength && breakAt < 0)
                        throw Error("A header field can't be folded to keep each line within 998 octets (RFC 5322 §2.1.1).", name);
                }

                if (index == body.Length && (characters <= TargetLineLength || breakAt < 0))
                {
                    body.Slice(lineStart).CopyTo(new Span<byte>(output, written, body.Length - lineStart));
                    return written + body.Length - lineStart;
                }

                var lineLength = breakAt - lineStart;
                body.Slice(lineStart, lineLength).CopyTo(new Span<byte>(output, written, lineLength));
                written += lineLength;
                lineBreak.CopyTo(new Span<byte>(output, written, lineBreak.Length));
                written += lineBreak.Length;

                lineStart = breakAt;
                tokens = tokensAtBreak;
                usedOctets = 0;
                hasText = false;
            }
        }

        private static ImfFileException Error(string message, ReadOnlySpan<byte> name)
        {
            var error = new ImfFileException(message);
            error.Data[ImfFileException.DataItemName.FieldName] = GetNameText(name);
            return error;
        }

        private static string GetNameText(ReadOnlySpan<byte> name)
        {
            var text = new StringBuilder(name.Length);
            foreach (var value in name)
            {
                text.Append((char)value);
            }

            return text.ToString();
        }

        private static bool IsNameByte(byte value)
        {
            // ref. RFC 5322 §3.6.8: ftext = %d33-57 / %d59-126
            return value >= 33 && value <= 126 && value != Colon;
        }

        private static bool IsWhitespace(byte value)
        {
            return value == Space || value == Tab;
        }

        /// <summary>
        /// Tracks whether a position in a structured field body is inside a quoted string, comment or domain literal (RFC 5322 §3.2.2, §3.2.4 and §3.4.1), and so whether
        /// a space or tab there can start a fold.
        /// </summary>
        private struct StructuredTokenState
        {
            private const byte Backslash = 0x5C;
            private const byte CloseBracket = 0x5D;
            private const byte CloseParenthesis = 0x29;
            private const byte OpenBracket = 0x5B;
            private const byte OpenParenthesis = 0x28;
            private const byte Quote = 0x22;

            private int _commentDepth;
            private bool _isEscaped;
            private bool _isInDomainLiteral;
            private bool _isInQuotedString;

            // ref. RFC 5322 §3.2.2: comment = "(" *([FWS] ccontent) [FWS] ")", so a comment may fold between its words, as encoded words in one need to (RFC 2047 §5(2)).
            // A space or tab in a quoted-pair is not FWS.
            internal bool CanFold => !_isEscaped && !_isInQuotedString && !_isInDomainLiteral;

            private bool IsInsideToken => _isInQuotedString || _isInDomainLiteral || _commentDepth > 0;

            internal void Advance(byte octet)
            {
                if (_isEscaped)
                {
                    _isEscaped = false;
                }
                else if (IsInsideToken && octet == Backslash)
                {
                    _isEscaped = true;
                }
                else if (_isInQuotedString)
                {
                    _isInQuotedString = octet != Quote;
                }
                else if (_isInDomainLiteral)
                {
                    _isInDomainLiteral = octet != CloseBracket;
                }
                else if (octet == OpenParenthesis)
                {
                    _commentDepth++;
                }
                else if (_commentDepth > 0)
                {
                    if (octet == CloseParenthesis) _commentDepth--;
                }
                else if (octet == Quote)
                {
                    _isInQuotedString = true;
                }
                else if (octet == OpenBracket)
                {
                    _isInDomainLiteral = true;
                }
            }
        }
    }
}
