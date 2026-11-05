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
        private readonly VcardLexicalState _state;

        // How far the search for the value has got, kept so that a line with many folds before its value is searched once.
        private bool _isInQuotes;
        private ValueKind _kind;
        private int _searchPosition;
        private int _valueOffset;

        internal VcardLineContinuation(VcardLexicalState state, bool acceptLegacySyntax)
        {
            _state = state;
            _acceptLegacySyntax = acceptLegacySyntax;
        }

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
            }

            if (_kind == ValueKind.NotFound)
            {
                FindValue(line);
            }

            switch (_kind)
            {
                case ValueKind.QuotedPrintable:
                    // ref. vCard 2.1 §2.1.3 and RFC 2045 §6.7 rule 5.  What the next line starts with is data, so no fold can follow.
                    int quotedPrintableLength;
                    if (!MimeQuotedPrintable.EndsWithSoftLineBreak(line.Slice(_valueOffset), out quotedPrintableLength)) return ContentLineContinuationKind.None;

                    dataLength = _valueOffset + quotedPrintableLength;
                    return ContentLineContinuationKind.Join;
                case ValueKind.Base64:
                    // ref. vCard 2.1 §2.9: "the end of the text is marked with two CRLF sequences".
                    if (physicalLineStart > 0 && physicalLineStart == line.Length) return ContentLineContinuationKind.End;

                    return IsBase64Text(line.Slice(Math.Max(physicalLineStart, _valueOffset)))
                        ? ContentLineContinuationKind.JoinUntilEnd
                        : ContentLineContinuationKind.Invalid;
                default:
                    return ContentLineContinuationKind.None;
            }
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
        /// Finds the encoding from the first parameter that gives one.
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
                    if (kind != ValueKind.Other) return kind;
                }

                parameterStart = index + 1;
            }

            return ValueKind.Other;
        }

        private static ValueKind GetParameterKind(ReadOnlySpan<char> parameter)
        {
            var equalsIndex = parameter.IndexOf('=');
            if (equalsIndex >= 0)
            {
                if (!VcardLegacyTokens.EqualsIgnoreCase(parameter.Slice(0, equalsIndex), VcardLegacyTokens.EncodingName)) return ValueKind.Other;

                parameter = parameter.Slice(equalsIndex + 1);
            }

            if (VcardLegacyTokens.EqualsIgnoreCase(parameter, VcardLegacyTokens.QuotedPrintable)) return ValueKind.QuotedPrintable;
            if (VcardLegacyTokens.EqualsIgnoreCase(parameter, VcardLegacyTokens.Base64)) return ValueKind.Base64;

            return ValueKind.Other;
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
