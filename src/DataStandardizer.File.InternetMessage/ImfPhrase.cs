using System;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A phrase (RFC 5322 §3.2.5) read by an <see cref="ImfTokenizer"/>.
    /// </summary>
    internal readonly struct ImfPhrase
    {
        internal ImfPhrase(string text, bool hasEncodedWordAtom)
        {
            Text = text;
            HasEncodedWordAtom = hasEncodedWordAtom;
        }

        /// <summary>
        /// Gets the words of the phrase, separated by a space.  A period of the obsolete form keeps its place: <c>Joe Q. Public</c> stays as it is.
        /// </summary>
        internal string Text { get; }

        /// <summary>
        /// Gets a flag indicating that a word of the phrase is an atom in the form of an RFC 2047 encoded word, which is where one may appear in a
        /// phrase (RFC 2047 §5(3)).  A quoted-string of that form is plain text.
        /// </summary>
        internal bool HasEncodedWordAtom { get; }

        /// <summary>
        /// Reads a phrase, or the obsolete form with periods.
        /// </summary>
        /// <remarks>
        /// phrase = 1*word / obs-phrase; obs-phrase = word *(word / "." / CFWS)
        /// </remarks>
        /// <returns><c>false</c> if the next token does not start a phrase, or the input is not valid.</returns>
        internal static bool TryRead(ref ImfTokenizer tokenizer, out ImfPhrase phrase)
        {
            phrase = default;
            if (!tokenizer.TryPeek(out var token) || !token.IsWord) return false;

            var text = new StringBuilder();
            var hasEncodedWordAtom = false;
            var wasWord = false;
            while (token.IsWord || token.Kind == ImfTokenKind.Period)
            {
                tokenizer.TryRead(out token);

                var isWord = token.IsWord;
                if (text.Length > 0 && (token.IsPrecededByCfws || (wasWord && isWord)))
                {
                    text.Append(' ');
                }

                if (isWord)
                {
                    text.Append(tokenizer.GetText(token));
                    hasEncodedWordAtom |= token.Kind == ImfTokenKind.Atom && HasEncodedWordForm(tokenizer.GetBytes(token));
                }
                else
                {
                    text.Append('.');
                }

                wasWord = isWord;
                if (!tokenizer.TryPeek(out token)) return false;
            }

            phrase = new ImfPhrase(text.ToString(), hasEncodedWordAtom);
            return true;
        }

        // ref. RFC 2047 §2: encoded-word = "=?" charset "?" encoding "?" encoded-text "?="
        private static bool HasEncodedWordForm(ReadOnlySpan<byte> atom)
        {
            return atom.Length >= 4 && atom[0] == '=' && atom[1] == '?' && atom[atom.Length - 2] == '?' && atom[atom.Length - 1] == '=';
        }
    }
}
