using System;
using DataStandardizer.File.ContentLine;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Tracks, from the lines read, which card a line is in and whether that card is vCard 4.0.  It decides only whether RFC 6868 caret encoding is
    /// decoded and what an exception reports.  Whether the lines make a conforming card is checked elsewhere.
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
        /// Gets a flag indicating if the current line is in a card that has said it is vCard 4.0.
        /// </summary>
        internal bool IsVersion40 { get; private set; }

        /// <summary>
        /// Moves on from a line.  The <c>END:VCARD</c> line is the last line of its card, so the card is left here and not when that line is read.
        /// </summary>
        internal void MoveToNextLine()
        {
            if (!_hasEnded) return;

            _hasEnded = false;
            _isInCard = false;
            IsVersion40 = false;
        }

        /// <summary>
        /// Takes note of a line that has just been parsed.
        /// </summary>
        internal void ObserveLine(ContentLineScanner scanner)
        {
            var name = scanner.Name.Span;
            if (EqualsIgnoreCase(name, BeginName))
            {
                if (EqualsIgnoreCase(scanner.RawValue.Span, CardValue))
                {
                    _cardIndex++;
                    _isInCard = true;
                    IsVersion40 = false;
                }
            }
            else if (EqualsIgnoreCase(name, VersionName))
            {
                IsVersion40 = _isInCard && scanner.RawValue.Span.SequenceEqual(Version40.AsSpan());
            }
            else if (EqualsIgnoreCase(name, EndName))
            {
                _hasEnded = _isInCard && EqualsIgnoreCase(scanner.RawValue.Span, CardValue);
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
