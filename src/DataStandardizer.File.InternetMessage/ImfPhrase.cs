using System;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A phrase (RFC 5322 §3.2.5) read by an <see cref="ImfTokenizer"/>, or given as plain text.
    /// </summary>
    internal readonly struct ImfPhrase
    {
        private readonly string _encodedWordForm;

        internal ImfPhrase(string text)
            : this(text, string.Empty)
        {
        }

        private ImfPhrase(string text, string encodedWordForm)
        {
            Text = text;
            _encodedWordForm = encodedWordForm;
        }

        /// <summary>
        /// Gets the words of the phrase, separated by a space.  A period of the obsolete form keeps its place: <c>Joe Q. Public</c> stays as it is.
        /// </summary>
        internal string Text { get; }

        /// <summary>
        /// Gets a flag indicating that a word of the phrase is an atom in the form of an RFC 2047 encoded word, which is where one may appear in a
        /// phrase (RFC 2047 §5(3)).  A quoted-string of that form is plain text.
        /// </summary>
        internal bool HasEncodedWordAtom => _encodedWordForm.Length > 0;

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

            var start = tokenizer;
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
                    hasEncodedWordAtom |= IsEncodedWordAtom(tokenizer, token);
                }
                else
                {
                    text.Append('.');
                }

                wasWord = isWord;
                if (!tokenizer.TryPeek(out token)) return false;
            }

            phrase = new ImfPhrase(text.ToString(), hasEncodedWordAtom ? BuildEncodedWordForm(start) : string.Empty);
            return true;
        }

        /// <summary>
        /// Indicates whether this phrase equals another: it has the same text, compared case-sensitively, and the same encoded words.
        /// </summary>
        internal bool ValueEquals(ImfPhrase other)
        {
            return string.Equals(Text, other.Text, StringComparison.Ordinal) && string.Equals(_encodedWordForm, other._encodedWordForm, StringComparison.Ordinal);
        }

        /// <summary>
        /// Writes the phrase in the modern form.
        /// </summary>
        internal void AppendTo(StringBuilder output)
        {
            if (HasEncodedWordAtom)
            {
                output.Append(_encodedWordForm);
            }
            else
            {
                ImfGrammar.AppendPhrase(Text, output);
            }
        }

        // Quoting an encoded word would make it plain text, and writing plain text as an atom could make it an encoded word.  So each encoded word is
        // written as it is, and the plain text between them is written as a phrase of its own, which quotes anything of that form.
        private static string BuildEncodedWordForm(ImfTokenizer tokenizer)
        {
            var output = new StringBuilder();
            var plainText = new StringBuilder();
            var hasPlainText = false;
            var wasWord = false;
            while (tokenizer.TryRead(out var token) && (token.IsWord || token.Kind == ImfTokenKind.Period))
            {
                if (IsEncodedWordAtom(tokenizer, token))
                {
                    AppendPlainText(plainText, ref hasPlainText, output);
                    if (output.Length > 0) output.Append(' ');

                    output.Append(tokenizer.GetText(token));
                    continue;
                }

                var isWord = token.IsWord;
                if (hasPlainText && (token.IsPrecededByCfws || (wasWord && isWord)))
                {
                    plainText.Append(' ');
                }

                if (isWord)
                {
                    plainText.Append(tokenizer.GetText(token));
                }
                else
                {
                    plainText.Append('.');
                }

                hasPlainText = true;
                wasWord = isWord;
            }

            AppendPlainText(plainText, ref hasPlainText, output);
            return output.ToString();
        }

        private static void AppendPlainText(StringBuilder plainText, ref bool hasPlainText, StringBuilder output)
        {
            if (!hasPlainText) return;

            if (output.Length > 0) output.Append(' ');

            ImfGrammar.AppendPhrase(plainText.ToString(), output);
            plainText.Length = 0;
            hasPlainText = false;
        }

        private static bool IsEncodedWordAtom(ImfTokenizer tokenizer, ImfToken token)
        {
            return token.Kind == ImfTokenKind.Atom && HasEncodedWordForm(tokenizer.GetBytes(token));
        }

        // ref. RFC 2047 §2: encoded-word = "=?" charset "?" encoding "?" encoded-text "?="
        private static bool HasEncodedWordForm(ReadOnlySpan<byte> atom)
        {
            return atom.Length >= 4 && atom[0] == '=' && atom[1] == '?' && atom[atom.Length - 2] == '?' && atom[atom.Length - 1] == '=';
        }
    }
}
