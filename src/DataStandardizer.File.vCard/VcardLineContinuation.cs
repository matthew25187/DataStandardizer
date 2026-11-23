using System;
using DataStandardizer.Communication.Mime;
using DataStandardizer.File.ContentLine;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Finds the two ways a value continues on later lines in vCard 2.1: a quoted-printable soft line break, and a base64 value, which ends at an empty line.
    /// </summary>
    /// <remarks>
    /// It applies on the lines of a vCard 2.1 card, or on every line under <see cref="VcardOptions.AcceptLegacySyntax"/>.  Elsewhere a line ending in <c>=</c>
    /// is only that.
    /// </remarks>
    internal sealed class VcardLineContinuation : IContentLineContinuation
    {
        private enum ValueKind
        {
            NotFound,
            Other,
            QuotedPrintable,
            Base64
        }

        private readonly bool _acceptLegacySyntax;
        private readonly bool _acceptRfc6350FoldsInVersion21;
        private readonly VcardLexicalState _state;

        // How far the search for the value has got, kept so that a line with many folds before its value is searched once.
        private bool _isInQuotes;
        private ValueKind _kind;
        private int _searchPosition;
        private int _valueOffset;

        // Whether the physical line being ended follows a fold, rather than a soft line break or nothing, and the same for the one after it.  The scanner can ask
        // about one physical line more than once, so the answer for the next is taken up only when a call is about another physical line.
        private bool _followsFold;
        private bool _nextFollowsFold;
        private int _physicalLineStart;
        private int _physicalLineEnd;

        // The length of the longest physical line of the logical line so far.
        private int _longestPhysicalLine;

        internal VcardLineContinuation(VcardLexicalState state, bool acceptLegacySyntax, bool acceptRfc6350FoldsInVersion21)
        {
            _state = state;
            _acceptLegacySyntax = acceptLegacySyntax;
            _acceptRfc6350FoldsInVersion21 = acceptRfc6350FoldsInVersion21;
        }

        /// <summary>
        /// Gets the length of the longest physical line of the last logical line that holds a <c>QUOTED-PRINTABLE</c> value, not counting its line break; or 0 if
        /// the line's value isn't quoted-printable.
        /// </summary>
        /// <remarks>
        /// Each physical line of the logical line is counted whole, including those with the property name and parameters.  A physical line that ends the data
        /// without a line break isn't counted.
        /// </remarks>
        internal int LongestQuotedPrintableLine => _kind == ValueKind.QuotedPrintable ? _longestPhysicalLine : 0;

        /// <inheritdoc/>
        public ContentLineContinuationKind GetContinuation(ReadOnlySpan<char> line, int physicalLineStart, out int dataLength)
        {
            dataLength = line.Length;
            if (!_state.IsVersion21 && !_acceptLegacySyntax) return ContentLineContinuationKind.None;

            if (physicalLineStart == 0)
            {
                _isInQuotes = false;
                _kind = ValueKind.NotFound;
                _searchPosition = 0;
                _followsFold = false;
                _nextFollowsFold = false;
                _longestPhysicalLine = 0;
            }
            else if (physicalLineStart != _physicalLineStart || line.Length != _physicalLineEnd)
            {
                _followsFold = _nextFollowsFold;
            }

            _physicalLineStart = physicalLineStart;
            _physicalLineEnd = line.Length;

            MeasurePhysicalLine(line, physicalLineStart);
            if (_kind == ValueKind.NotFound)
            {
                FindValue(line);
            }

            switch (_kind)
            {
                case ValueKind.QuotedPrintable:
                    // ref. vCard 2.1 §2.1.3 and RFC 2045 §6.7 rule 5.  What the next line starts with is data, so no fold can follow.
                    int quotedPrintableLength;
                    if (!MimeQuotedPrintable.EndsWithSoftLineBreak(line.Slice(_valueOffset), out quotedPrintableLength))
                    {
                        // The line ends here, or the next physical line is a fold.
                        _nextFollowsFold = true;
                        return ContentLineContinuationKind.None;
                    }

                    _nextFollowsFold = false;
                    dataLength = _valueOffset + quotedPrintableLength;
                    return ContentLineContinuationKind.Join;
                case ValueKind.Base64:
                    // ref. vCard 2.1 §2.9: "the end of the text is marked with two CRLF sequences".
                    if (physicalLineStart > 0 && physicalLineStart == line.Length) return ContentLineContinuationKind.End;

                    return IsBase64Text(line.Slice(Math.Max(physicalLineStart, _valueOffset)))
                        ? ContentLineContinuationKind.JoinUntilEnd
                        : ContentLineContinuationKind.Invalid;
                default:
                    _nextFollowsFold = true;
                    return ContentLineContinuationKind.None;
            }
        }

        /// <summary>
        /// Forgets the line before, as the reader starts to read another.
        /// </summary>
        internal void StartLine()
        {
            _kind = ValueKind.NotFound;
            _longestPhysicalLine = 0;
        }

        /// <summary>
        /// Notes the length of the physical line just ended, which counts if the value turns out to be quoted-printable.  ref. vCard 2.1 §2.1.3: "Quoted-Printable lines of text must also be
        /// limited to less than 76 characters.  The 76 characters does not include the CRLF (RFC 822) line break sequence."
        /// </summary>
        private void MeasurePhysicalLine(ReadOnlySpan<char> line, int physicalLineStart)
        {
            var physicalLength = line.Length - physicalLineStart;

            // A fold by the rule of RFC 6350 §3.2 removed the white space that started the physical line, as well as the line break.
            if (_followsFold && (!_state.IsVersion21 || _acceptRfc6350FoldsInVersion21)) physicalLength++;

            if (physicalLength > _longestPhysicalLine) _longestPhysicalLine = physicalLength;
        }

        /// <summary>
        /// Looks for the ':' before the value in what has been added to the line, and if it is there, finds the encoding from the parameters before it.
        /// </summary>
        private void FindValue(ReadOnlySpan<char> line)
        {
            for (var index = _searchPosition; index < line.Length; index++)
            {
                var character = line[index];
                if (character == '"')
                {
                    _isInQuotes = !_isInQuotes;
                }
                else if (character == ':' && !_isInQuotes)
                {
                    _valueOffset = index + 1;
                    _kind = GetKind(line.Slice(0, index));
                    return;
                }
            }

            _searchPosition = line.Length;
        }

        /// <summary>
        /// Finds the encoding from the first parameter that gives one, which is the one <see cref="VcardValueDecoder"/> uses.
        /// </summary>
        /// <param name="nameAndParameters">The line before the ':' that starts its value.</param>
        private static ValueKind GetKind(ReadOnlySpan<char> nameAndParameters)
        {
            var isInQuotes = false;
            var parameterStart = -1;
            for (var index = 0; index <= nameAndParameters.Length; index++)
            {
                var isEnd = index == nameAndParameters.Length;
                if (!isEnd && nameAndParameters[index] == '"')
                {
                    isInQuotes = !isInQuotes;
                }

                if (!isEnd && (nameAndParameters[index] != ';' || isInQuotes)) continue;

                if (parameterStart >= 0)
                {
                    var kind = GetParameterKind(nameAndParameters.Slice(parameterStart, index - parameterStart));
                    if (kind != ValueKind.NotFound) return kind;
                }

                parameterStart = index + 1;
            }

            return ValueKind.Other;
        }

        /// <summary>
        /// Gets the kind of value a parameter gives.
        /// </summary>
        /// <returns>The kind; or <see cref="ValueKind.NotFound"/> if the parameter isn't an encoding.</returns>
        private static ValueKind GetParameterKind(ReadOnlySpan<char> parameter)
        {
            var encoding = parameter;
            var equalsIndex = parameter.IndexOf('=');
            if (equalsIndex >= 0)
            {
                if (!VcardLegacyTokens.EqualsIgnoreCase(parameter.Slice(0, equalsIndex), VcardLegacyTokens.EncodingName)) return ValueKind.NotFound;

                encoding = GetFirstValue(parameter.Slice(equalsIndex + 1));
            }
            else if (VcardLegacyTokens.GetValuelessParameterName(parameter) != VcardLegacyTokens.EncodingName)
            {
                return ValueKind.NotFound;
            }

            if (VcardLegacyTokens.EqualsIgnoreCase(encoding, VcardLegacyTokens.QuotedPrintable)) return ValueKind.QuotedPrintable;
            if (VcardLegacyTokens.EqualsIgnoreCase(encoding, VcardLegacyTokens.Base64)) return ValueKind.Base64;

            return ValueKind.Other;
        }

        /// <summary>
        /// Gets the first value of a parameter's values, without any enclosing double quotes.
        /// </summary>
        private static ReadOnlySpan<char> GetFirstValue(ReadOnlySpan<char> values)
        {
            if (values.Length > 0 && values[0] == '"')
            {
                var quoted = values.Slice(1);
                var closingQuote = quoted.IndexOf('"');
                return closingQuote < 0 ? quoted : quoted.Slice(0, closingQuote);
            }

            var comma = values.IndexOf(',');
            return comma < 0 ? values : values.Slice(0, comma);
        }

        /// <summary>
        /// Gets whether text holds only the base64 alphabet (RFC 2045 §6.8) and white space.  Anything else, such as <c>END:VCARD</c>, shows that the empty line
        /// is missing, which would otherwise take the lines after the value into it.
        /// </summary>
        internal static bool IsBase64Text(ReadOnlySpan<char> text)
        {
            foreach (var character in text)
            {
                var isBase64 = (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') ||
                    character == '+' || character == '/' || character == '=' || character == ' ' || character == '\t';
                if (!isBase64) return false;
            }

            return true;
        }
    }
}
