using System.Text;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Reads all of a tokenizer's input, so that the tests can compare the tokens with a literal.
    /// </summary>
    internal static class TokenSequences
    {
        /// <summary>
        /// The result when the tokenizer finds the input invalid.
        /// </summary>
        public const string Invalid = "invalid";

        /// <summary>
        /// Gets the name of each token, with a <c>+</c> before one that CFWS comes before, leaving out comments.
        /// </summary>
        public static string Read(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var tokenizer = new ImfTokenizer(bytes);
            var names = new StringBuilder();
            while (true)
            {
                if (!tokenizer.TryRead(out var token)) return Invalid;
                if (token.Kind == ImfTokenKind.End) return names.ToString();

                if (names.Length > 0) names.Append(' ');
                if (token.IsPrecededByCfws) names.Append('+');

                names.Append(token.Kind);
            }
        }

        /// <summary>
        /// Gets the name of each token and the bytes it covers, with comments, in the form <c>Name[bytes]</c>.
        /// </summary>
        public static string ReadWithComments(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var tokenizer = new ImfTokenizer(bytes);
            var names = new StringBuilder();
            while (true)
            {
                if (!tokenizer.TryReadWithComments(out var token)) return Invalid;
                if (token.Kind == ImfTokenKind.End) return names.ToString();

                if (names.Length > 0) names.Append(' ');

                names.Append(token.Kind).Append('[').Append(Octets.ToText(tokenizer.GetBytes(token))).Append(']');
            }
        }

        /// <summary>
        /// Gets the text of the only token in the input.
        /// </summary>
        public static string GetOnlyText(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var tokenizer = new ImfTokenizer(bytes);
            tokenizer.TryRead(out var token);
            return tokenizer.GetText(token);
        }

        /// <summary>
        /// Gets the kind of the next token twice, once by looking and once by reading, joined with a space.
        /// </summary>
        public static string PeekThenRead(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var tokenizer = new ImfTokenizer(bytes);
            tokenizer.TryPeek(out var peeked);
            tokenizer.TryRead(out var read);
            return peeked.Kind + " " + read.Kind;
        }

        /// <summary>
        /// Reads a phrase, and gets its text, whether it has an atom in the form of an encoded word, and the kind of the token after it, joined with <c>|</c>.
        /// </summary>
        public static string ReadPhrase(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var tokenizer = new ImfTokenizer(bytes);
            if (!ImfPhrase.TryRead(ref tokenizer, out var phrase)) return Invalid;

            tokenizer.TryRead(out var next);
            return phrase.Text + "|" + phrase.HasEncodedWordAtom + "|" + next.Kind;
        }
    }
}
