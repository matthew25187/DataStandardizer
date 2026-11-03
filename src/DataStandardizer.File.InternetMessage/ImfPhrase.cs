using System;
using System.Text;
using DataStandardizer.Communication.Mime;

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
        /// Gets the words of the phrase, separated by a space, with each encoded word decoded and the white space between adjacent encoded words removed
        /// (RFC 2047 §6.2).  A period of the obsolete form keeps its place: <c>Joe Q. Public</c> stays as it is.
        /// </summary>
        internal string Text { get; }

        /// <summary>
        /// Gets a flag indicating that a word of the phrase is an atom in the form of an RFC 2047 encoded word, which is where one may appear in a
        /// phrase (RFC 2047 §5(3)).  A quoted-string of that form is plain text.
        /// </summary>
        internal bool HasEncodedWordAtom => _encodedWordForm.Length > 0;

        /// <summary>
        /// Reads a phrase, or the obsolete form with periods, with the default options.
        /// </summary>
        /// <returns><c>false</c> if the next token does not start a phrase, or the input is not valid.</returns>
        internal static bool TryRead(ref ImfTokenizer tokenizer, out ImfPhrase phrase)
        {
            return TryRead(ref tokenizer, ImfHeaderText.DefaultOptions, out phrase);
        }

        /// <summary>
        /// Reads a phrase, or the obsolete form with periods.
        /// </summary>
        /// <remarks>
        /// phrase = 1*word / obs-phrase; obs-phrase = word *(word / "." / CFWS)
        /// </remarks>
        /// <returns><c>false</c> if the next token does not start a phrase, or the input is not valid.</returns>
        internal static bool TryRead(ref ImfTokenizer tokenizer, ImfOptions options, out ImfPhrase phrase)
        {
            phrase = default;
            if (!tokenizer.TryPeek(out var token) || !token.IsWord) return false;

            var start = tokenizer;
            var text = new StringBuilder();

            // ref. RFC 2047 §5(3), §6.2: an atom may be an encoded word, but a quoted-string may not.  The atoms next to each other are decoded together, so that
            // the white space between encoded words goes.  A comment isn't white space, so it ends the run.
            var atoms = new StringBuilder();
            var hasEncodedWordAtom = false;
            var wasWord = false;
            while (token.IsWord || token.Kind == ImfTokenKind.Period)
            {
                tokenizer.TryRead(out token);

                var isWord = token.IsWord;
                if (token.Kind == ImfTokenKind.Atom && atoms.Length > 0 && !token.IsPrecededByComment)
                {
                    atoms.Append(' ').Append(tokenizer.GetText(token));
                }
                else
                {
                    AppendDecodedAtoms(atoms, options, text);
                    if (text.Length > 0 && (token.IsPrecededByCfws || (wasWord && isWord)))
                    {
                        text.Append(' ');
                    }

                    if (token.Kind == ImfTokenKind.Atom)
                    {
                        atoms.Append(tokenizer.GetText(token));
                    }
                    else if (isWord)
                    {
                        text.Append(tokenizer.GetText(token));
                    }
                    else
                    {
                        text.Append('.');
                    }
                }

                hasEncodedWordAtom |= IsEncodedWordAtom(tokenizer, token);
                wasWord = isWord;
                if (!tokenizer.TryPeek(out token)) return false;
            }

            AppendDecodedAtoms(atoms, options, text);
            var phraseText = text.ToString();
            phrase = new ImfPhrase(phraseText, hasEncodedWordAtom ? BuildEncodedWordForm(start, phraseText) : string.Empty);
            return true;
        }

        /// <summary>
        /// Indicates whether this phrase equals another: it has the same text, compared case-sensitively, whichever form it was written in.
        /// </summary>
        internal bool ValueEquals(ImfPhrase other)
        {
            return string.Equals(Text, other.Text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Writes the phrase in the modern form.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="encoding">How to write text that is not US-ASCII; or <c>null</c> to write as the phrase holds it, with each encoded word as it was read and any
        /// other text as UTF-8.</param>
        internal void AppendTo(StringBuilder output, ImfHeaderEncoding? encoding)
        {
            if (encoding is null && HasEncodedWordAtom)
            {
                output.Append(_encodedWordForm);
                return;
            }

            // ref. RFC 2047 §5(3): a phrase that has to be quoted as well as encoded is written as encoded words, as they can't be in a quoted-string.
            if (encoding == ImfHeaderEncoding.EncodedWords && ImfHeaderText.HasNonAscii(Text))
            {
                ImfHeaderText.AppendEncodedWords(RemoveControlCharacters(Text), MimeEncodedWordContext.Phrase, output);
            }
            else
            {
                ImfGrammar.AppendPhrase(Text, output);
            }
        }

        private static void AppendDecodedAtoms(StringBuilder atoms, ImfOptions options, StringBuilder text)
        {
            if (atoms.Length == 0) return;

            text.Append(ImfHeaderText.DecodeRun(atoms.ToString(), MimeEncodedWordContext.Phrase, ImfHeaderText.GetWordOptions(options), options.CharsetResolver));
            atoms.Length = 0;
        }

        // Decoded text, or the obsolete syntax, can hold control characters, which have no modern form, and a line break must never get out.  A quoted-string
        // leaves them out too (see ImfGrammar.AppendQuotedString).
        private static string RemoveControlCharacters(string text)
        {
            var output = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (c == '\t' || (c >= ' ' && c != 127)) output.Append(c);
            }

            return output.ToString();
        }

        // Quoting an encoded word would make it plain text, and writing plain text as an atom could make it an encoded word.  So each encoded word is
        // written as it is, and the plain text between them is written as a phrase of its own, which quotes anything of that form.
        //
        // A period right next to an encoded word is the exception.  Only the obsolete form can write one with no white space, which is never written, and a quoted
        // period after a space would read back as different text.  So the whole text is written as encoded words, which hold the period.
        private static string BuildEncodedWordForm(ImfTokenizer tokenizer, string text)
        {
            var output = new StringBuilder();
            var plainText = new StringBuilder();
            var hasPlainText = false;
            var wasWord = false;
            var wasEncodedWord = false;
            var wasPeriod = false;
            while (tokenizer.TryRead(out var token) && (token.IsWord || token.Kind == ImfTokenKind.Period))
            {
                var isEncodedWord = IsEncodedWordAtom(tokenizer, token);
                var isPeriod = token.Kind == ImfTokenKind.Period;
                if (!token.IsPrecededByCfws && ((isEncodedWord && wasPeriod) || (isPeriod && wasEncodedWord)))
                {
                    var encodedWords = new StringBuilder();
                    ImfHeaderText.AppendEncodedWords(RemoveControlCharacters(text), MimeEncodedWordContext.Phrase, encodedWords);
                    return encodedWords.ToString();
                }

                wasEncodedWord = isEncodedWord;
                wasPeriod = isPeriod;
                if (isEncodedWord)
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
