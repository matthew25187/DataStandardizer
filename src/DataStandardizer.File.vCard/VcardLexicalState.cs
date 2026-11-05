using System;
using DataStandardizer.File.ContentLine;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Tracks, from the lines read or written, which card a line is in and which of vCard 4.0 or 2.1 that card is.  It decides only how RFC 6868 caret
    /// encoding and folding apply, and what an exception reports.  Whether the lines make a conforming card is checked elsewhere.
    /// </summary>
    /// <remarks>
    /// The names match with or without a group, since RFC 2426 §4 allows a group on the <c>BEGIN</c>, <c>VERSION</c> and <c>END</c> lines of a
    /// vCard 3.0 card, and whether RFC 6350 §3.3 allows it is for the conformance checks to say.
    /// </remarks>
    internal sealed class VcardLexicalState
    {
        // ref. RFC 6350 §3.3: "BEGIN:VCARD", "VERSION:4.0" and "END:VCARD".  ABNF strings are case-insensitive (RFC 5234 §2.3).
        private const string BeginName = "BEGIN";
        private const string CardValue = "VCARD";
        private const string EndName = "END";
        private const string Version21 = "2.1";
        private const string Version40 = "4.0";
        private const string VersionName = "VERSION";

        private int _cardIndex = -1;
        private bool _hasEnded;
        private bool _isInCard;

        /// <summary>
        /// Gets the 0-based index of the card the current line is in, counting each <c>BEGIN:VCARD</c>; or -1 if the line is outside a card.
        /// </summary>
        internal int CardIndexOfLine => _isInCard ? _cardIndex : -1;

        /// <summary>
        /// Gets a flag indicating if the current line is in a card that has said it is vCard 2.1.
        /// </summary>
        internal bool IsVersion21 { get; private set; }

        /// <summary>
        /// Gets a flag indicating if the current line is in a card that has said it is vCard 4.0.
        /// </summary>
        internal bool IsVersion40 { get; private set; }

        /// <summary>
        /// Takes the state of another instance.
        /// </summary>
        internal void CopyFrom(VcardLexicalState other)
        {
            _cardIndex = other._cardIndex;
            _hasEnded = other._hasEnded;
            _isInCard = other._isInCard;
            IsVersion21 = other.IsVersion21;
            IsVersion40 = other.IsVersion40;
        }

        /// <summary>
        /// Moves on from a line.  The <c>END:VCARD</c> line is the last line of its card, so the card is left here and not when that line is read.
        /// </summary>
        internal void MoveToNextLine()
        {
            if (!_hasEnded) return;

            _hasEnded = false;
            _isInCard = false;
            IsVersion21 = false;
            IsVersion40 = false;
        }

        /// <summary>
        /// Takes note of a line that has just been parsed.
        /// </summary>
        internal void ObserveLine(ContentLineScanner scanner)
        {
            ObserveLine(scanner.Name.Span, scanner.RawValue.Span);
        }

        /// <summary>
        /// Takes note of a line, from its name and its value as it appears in the content line.
        /// </summary>
        internal void ObserveLine(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            if (EqualsIgnoreCase(name, BeginName))
            {
                if (EqualsIgnoreCase(value, CardValue))
                {
                    _cardIndex++;
                    _isInCard = true;
                    IsVersion21 = false;
                    IsVersion40 = false;
                }
            }
            else if (EqualsIgnoreCase(name, VersionName))
            {
                IsVersion21 = _isInCard && value.SequenceEqual(Version21.AsSpan());
                IsVersion40 = _isInCard && value.SequenceEqual(Version40.AsSpan());
            }
            else if (EqualsIgnoreCase(name, EndName))
            {
                _hasEnded = _isInCard && EqualsIgnoreCase(value, CardValue);
            }
        }

        private static bool EqualsIgnoreCase(ReadOnlySpan<char> text, string expected)
        {
            if (text.Length != expected.Length) return false;

            // The expected text is upper case ASCII, so only ASCII letters need folding; anything else must match exactly.
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (character >= 'a' && character <= 'z')
                {
                    character = (char)(character - ('a' - 'A'));
                }

                if (character != expected[index]) return false;
            }

            return true;
        }
    }
}
