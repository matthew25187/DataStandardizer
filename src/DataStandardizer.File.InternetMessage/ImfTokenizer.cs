using System;
using System.Buffers;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Splits the body of a structured header field into the lexical tokens of RFC 5322 §3.2, with the obsolete forms of §4 and the UTF-8 of RFC 6532.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A token is a position in the input, so nothing is allocated until <see cref="GetText"/> is called.  Comments are counted rather than
    /// recursed into, and the work is linear in the input, so hostile nesting costs nothing extra.
    /// </para>
    /// <para>
    /// The input has to be well-formed UTF-8, which <see cref="IsWellFormed"/> checks.  The grammar works on bytes, and every byte of a
    /// multi-byte character is above 127, so each one passes as <c>UTF8-non-ascii</c> (RFC 6532 §3.2).
    /// </para>
    /// <para>
    /// Encoded words (RFC 2047) can appear in only two places here: in a comment, which <see cref="TryReadWithComments"/> returns as a token, and
    /// in an atom that is a word of a phrase, which <see cref="ImfPhrase"/> reports.  They are never in a quoted-string (RFC 2047 §5(3)).
    /// </para>
    /// </remarks>
    internal ref struct ImfTokenizer
    {
        private const byte CarriageReturn = (byte)'\r';
        private const byte LineFeed = (byte)'\n';
        private const byte Space = (byte)' ';
        private const byte Tab = (byte)'\t';
        private const byte Backslash = (byte)'\\';
        private const byte Quote = (byte)'"';
        private const byte OpenParenthesis = (byte)'(';
        private const byte CloseParenthesis = (byte)')';
        private const byte OpenBracket = (byte)'[';
        private const byte CloseBracket = (byte)']';

        private enum ContentKind
        {
            QuotedString,
            Comment,
            DomainLiteral
        }

        private readonly ReadOnlySpan<byte> _input;
        private int _position;

        internal ImfTokenizer(ReadOnlySpan<byte> input)
        {
            _input = input;
            _position = 0;
        }

        /// <summary>
        /// Determines if some input is well-formed UTF-8, as the tokenizer needs.
        /// </summary>
        internal static bool IsWellFormed(ReadOnlySpan<byte> input)
        {
            return ImfHeaderScanner.IndexOfInvalidUtf8(input) < 0;
        }

        /// <summary>
        /// Passes over any folding white space and comments at a position in some input, for a grammar that doesn't divide into tokens.
        /// </summary>
        /// <returns><c>false</c> if the input is not valid at this point.</returns>
        internal static bool TrySkipCfws(ReadOnlySpan<byte> input, ref int position)
        {
            var tokenizer = new ImfTokenizer(input) { _position = position };
            while (true)
            {
                if (!tokenizer.TrySkipFoldingWhiteSpace(out _)) return false;

                position = tokenizer._position;
                if (position == input.Length || input[position] != OpenParenthesis) return true;
                if (!tokenizer.TrySkipComment()) return false;
            }
        }

        /// <summary>
        /// Reads the next token, passing over folding white space and comments.
        /// </summary>
        /// <returns><c>false</c> if the input is not valid at this point.</returns>
        internal bool TryRead(out ImfToken token)
        {
            var isPrecededByCfws = false;
            while (true)
            {
                if (!TryReadWithComments(out token)) return false;
                if (token.Kind != ImfTokenKind.Comment)
                {
                    token = new ImfToken(token.Kind, token.Start, token.Length, isPrecededByCfws || token.IsPrecededByCfws);
                    return true;
                }

                isPrecededByCfws = true;
            }
        }

        /// <summary>
        /// Gets the next token without moving past it.
        /// </summary>
        /// <returns><c>false</c> if the input is not valid at this point.</returns>
        internal bool TryPeek(out ImfToken token)
        {
            var copy = this;
            return copy.TryRead(out token);
        }

        /// <summary>
        /// Reads the next token, passing over folding white space and returning each outermost comment as a token.
        /// </summary>
        /// <returns><c>false</c> if the input is not valid at this point.</returns>
        internal bool TryReadWithComments(out ImfToken token)
        {
            token = default;
            if (!TrySkipFoldingWhiteSpace(out var isPrecededByFws)) return false;

            var start = _position;
            if (start == _input.Length)
            {
                token = new ImfToken(ImfTokenKind.End, start, 0, isPrecededByFws);
                return true;
            }

            ImfTokenKind kind;
            var isValid = true;
            switch (_input[start])
            {
                case Quote:
                    kind = ImfTokenKind.QuotedString;
                    isValid = TrySkipQuotedString();
                    break;
                case OpenParenthesis:
                    kind = ImfTokenKind.Comment;
                    isValid = TrySkipComment();
                    break;
                case OpenBracket:
                    kind = ImfTokenKind.DomainLiteral;
                    isValid = TrySkipDomainLiteral();
                    break;
                case (byte)'<':
                    kind = ImfTokenKind.LessThan;
                    _position++;
                    break;
                case (byte)'>':
                    kind = ImfTokenKind.GreaterThan;
                    _position++;
                    break;
                case (byte)'@':
                    kind = ImfTokenKind.At;
                    _position++;
                    break;
                case (byte)',':
                    kind = ImfTokenKind.Comma;
                    _position++;
                    break;
                case (byte)':':
                    kind = ImfTokenKind.Colon;
                    _position++;
                    break;
                case (byte)';':
                    kind = ImfTokenKind.Semicolon;
                    _position++;
                    break;
                case (byte)'.':
                    kind = ImfTokenKind.Period;
                    _position++;
                    break;
                default:
                    kind = ImfTokenKind.Atom;
                    isValid = TrySkipAtom();
                    break;
            }

            if (!isValid) return false;

            token = new ImfToken(kind, start, _position - start, isPrecededByFws);
            return true;
        }

        /// <summary>
        /// Gets the bytes of a token as they appear in the input.
        /// </summary>
        internal ReadOnlySpan<byte> GetBytes(ImfToken token)
        {
            return _input.Slice(token.Start, token.Length);
        }

        /// <summary>
        /// Decodes what an atom, quoted-string or domain-literal stands for.
        /// </summary>
        /// <remarks>
        /// A quoted-string loses its quotes, its quoted-pairs lose their backslashes and its folds lose their line breaks (RFC 5322 §3.2.4).  A
        /// domain-literal keeps its brackets and loses the white space just inside them, and other white space in it becomes one space (RFC 5322 §3.2.2).
        /// </remarks>
        internal string GetText(ImfToken token)
        {
            var source = GetBytes(token);
            var buffer = ArrayPool<byte>.Shared.Rent(source.Length);
            try
            {
                int length;
                switch (token.Kind)
                {
                    case ImfTokenKind.QuotedString:
                        length = CopyQuotedStringContent(source.Slice(1, source.Length - 2), buffer);
                        break;
                    case ImfTokenKind.DomainLiteral:
                        length = CopyDomainLiteral(source, buffer);
                        break;
                    default:
                        source.CopyTo(buffer);
                        length = source.Length;
                        break;
                }

                return Encoding.UTF8.GetString(buffer, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        // ref. RFC 5322 §3.2.2: FWS = ([*WSP CRLF] 1*WSP) / obs-FWS; §4.2: obs-FWS = 1*WSP *(CRLF 1*WSP)
        private bool TrySkipFoldingWhiteSpace(out bool skippedAny)
        {
            skippedAny = false;
            while (_position < _input.Length)
            {
                var current = _input[_position];
                if (current == Space || current == Tab)
                {
                    _position++;
                }
                else if (current == CarriageReturn)
                {
                    if (!IsFold(_input, _position)) return false;

                    _position += 2;
                }
                else
                {
                    break;
                }

                skippedAny = true;
            }

            return true;
        }

        private static bool IsFold(ReadOnlySpan<byte> input, int position)
        {
            return position + 2 < input.Length && input[position + 1] == LineFeed && (input[position + 2] == Space || input[position + 2] == Tab);
        }

        private bool TrySkipAtom()
        {
            var start = _position;
            while (_position < _input.Length && ImfGrammar.IsAtext((char)_input[_position]))
            {
                _position++;
            }

            return _position > start;
        }

        // ref. RFC 5322 §3.2.4: quoted-string = [CFWS] DQUOTE *([FWS] qcontent) [FWS] DQUOTE [CFWS]
        private bool TrySkipQuotedString()
        {
            _position++;
            while (_position < _input.Length)
            {
                var current = _input[_position];
                if (current == Quote)
                {
                    _position++;
                    return true;
                }

                if (!TrySkipContent(current, ContentKind.QuotedString)) return false;
            }

            return false;
        }

        // ref. RFC 5322 §3.2.2: comment = "(" *([FWS] ccontent) [FWS] ")"; ccontent = ctext / quoted-pair / comment
        private bool TrySkipComment()
        {
            var depth = 0;
            while (_position < _input.Length)
            {
                var current = _input[_position];
                if (current == OpenParenthesis)
                {
                    depth++;
                    _position++;
                }
                else if (current == CloseParenthesis)
                {
                    depth--;
                    _position++;
                    if (depth == 0) return true;
                }
                else if (!TrySkipContent(current, ContentKind.Comment))
                {
                    return false;
                }
            }

            return false;
        }

        // ref. RFC 5322 §3.4.1: domain-literal = [CFWS] "[" *([FWS] dtext) [FWS] "]" [CFWS]
        private bool TrySkipDomainLiteral()
        {
            _position++;
            while (_position < _input.Length)
            {
                var current = _input[_position];
                if (current == CloseBracket)
                {
                    _position++;
                    return true;
                }

                if (!TrySkipContent(current, ContentKind.DomainLiteral)) return false;
            }

            return false;
        }

        // Passes over one fold, white space character, quoted-pair or character of text.
        private bool TrySkipContent(byte current, ContentKind kind)
        {
            if (current == Backslash)
            {
                // ref. RFC 5322 §3.2.1: quoted-pair = ("\" (VCHAR / WSP)) / obs-qp; §4.1: obs-qp = "\" (%d0 / obs-NO-WS-CTL / LF / CR)
                // Together, these and the UTF8-non-ascii of RFC 6532 §3.2 allow any byte.
                if (_position + 1 == _input.Length) return false;

                _position += 2;
                return true;
            }

            if (current == CarriageReturn)
            {
                if (!IsFold(_input, _position)) return false;

                _position += 2;
                return true;
            }

            if (current != Space && current != Tab && !IsText(current, kind) && !ImfGrammar.IsObsoleteControl((char)current)) return false;

            _position++;
            return true;
        }

        private static bool IsText(byte value, ContentKind kind)
        {
            switch (kind)
            {
                case ContentKind.QuotedString:
                    return ImfGrammar.IsQtext((char)value);
                case ContentKind.Comment:
                    return ImfGrammar.IsCtext((char)value);
                default:
                    return ImfGrammar.IsDtext((char)value);
            }
        }

        private static int CopyQuotedStringContent(ReadOnlySpan<byte> source, byte[] destination)
        {
            var length = 0;
            for (var index = 0; index < source.Length; index++)
            {
                var current = source[index];
                if (current == Backslash)
                {
                    current = source[++index];
                }
                else if (current == CarriageReturn)
                {
                    // Only a fold has one that is not in a quoted-pair: the line feed goes as well, and the white space after it stays.
                    index++;
                    continue;
                }

                destination[length++] = current;
            }

            return length;
        }

        private static int CopyDomainLiteral(ReadOnlySpan<byte> source, byte[] destination)
        {
            var length = 0;
            destination[length++] = OpenBracket;

            var isSpacePending = false;
            for (var index = 1; index < source.Length - 1; index++)
            {
                var current = source[index];
                if (current == Space || current == Tab)
                {
                    isSpacePending = true;
                    continue;
                }

                if (current == CarriageReturn)
                {
                    index++;
                    continue;
                }

                if (current == Backslash)
                {
                    current = source[++index];
                }

                if (isSpacePending && length > 1)
                {
                    destination[length++] = Space;
                }

                isSpacePending = false;
                destination[length++] = current;
            }

            destination[length++] = CloseBracket;
            return length;
        }
    }
}
