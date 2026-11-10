using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Reads and writes the text of an unstructured header field or a comment, with RFC 2047 encoded words where RFC 2047 §5 allows them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Encoded words in a phrase are handled by the types that hold one: <see cref="ImfMailbox"/>, <see cref="ImfGroup"/>, <see cref="ImfAddressList"/> and
    /// <see cref="ImfPhraseList"/>.  An encoded word is never read in a quoted-string, an addr-spec, a message identifier or a parameter value.
    /// </para>
    /// <para>
    /// A word that has the form of an encoded word but can't be decoded, such as one whose character set can't be resolved, stays as ordinary text
    /// (RFC 2047 §6.1 and §6.2).  <see cref="ImfOptions.AllowMalformedEncodedWords"/> accepts some of them.
    /// </para>
    /// <para>
    /// Decoded text can hold anything that the words encode, including control characters and line breaks, so check it before using it where those
    /// matter.  A writer refuses such text, so it can't add a header field.
    /// </para>
    /// </remarks>
    public static class ImfHeaderText
    {
        internal const MimeEncodedWordParseOptions MalformedWordOptions = MimeEncodedWordParseOptions.AllowLongWords
            | MimeEncodedWordParseOptions.AllowEmbeddedWords
            | MimeEncodedWordParseOptions.AllowUnknownCharsets
            | MimeEncodedWordParseOptions.AllowLowercaseHexDigits
            | MimeEncodedWordParseOptions.AllowInvalidOctets
            | MimeEncodedWordParseOptions.AllowSplitCharacters;

        private const string InvalidBodyMessage = "The field body is not valid UTF-8.";
        private const string InvalidCommentsMessage = "The field body is not in a valid format.";

        /// <summary>
        /// Gets the options of a parse that was given none.  It is never given to a caller, so it can't change.
        /// </summary>
        internal static readonly ImfOptions DefaultOptions = new ImfOptions();

        /// <summary>
        /// Decodes the body of an unstructured header field, such as Subject (RFC 2047 §5(1)).
        /// </summary>
        /// <param name="value">The field body as UTF-8 (RFC 6532), folded or unfolded.</param>
        /// <returns>The text, without the white space at either end of the body, and with each encoded word decoded and the white space between adjacent
        /// encoded words removed (RFC 2047 §6.2).</returns>
        /// <exception cref="FormatException">The value is not valid UTF-8.</exception>
        public static string DecodeUnstructured(ReadOnlySpan<byte> value)
        {
            return DecodeUnstructured(value, DefaultOptions);
        }

        /// <summary>
        /// Decodes the body of an unstructured header field, such as Subject (RFC 2047 §5(1)).
        /// </summary>
        /// <param name="value">The field body, folded or unfolded.  Bytes that aren't valid UTF-8 are decoded with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> before any encoded word is read, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedEncodedWords"/>,
        /// <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <returns>The text, without the white space at either end of the body, and with each encoded word decoded and the white space between adjacent
        /// encoded words removed (RFC 2047 §6.2).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not valid UTF-8, and <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>false</c>.</exception>
        public static string DecodeUnstructured(ReadOnlySpan<byte> value, ImfOptions options)
        {
            CheckOptions(options);
            if (!TryGetUtf8(value, options, out var utf8)) throw new FormatException(InvalidBodyMessage);

            var bytes = utf8.ToArray();
            return DecodeUnstructuredCore(Encoding.UTF8.GetString(bytes, 0, bytes.Length), GetWordOptions(options), options.CharsetResolver);
        }

        /// <summary>
        /// Decodes the body of an unstructured header field, such as Subject (RFC 2047 §5(1)).
        /// </summary>
        /// <param name="value">The field body, folded or unfolded.</param>
        /// <returns>The text, without the white space at either end of the body, and with each encoded word decoded and the white space between adjacent
        /// encoded words removed (RFC 2047 §6.2).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        public static string DecodeUnstructured(string value)
        {
            return DecodeUnstructured(value, DefaultOptions);
        }

        /// <summary>
        /// Decodes the body of an unstructured header field, such as Subject (RFC 2047 §5(1)).
        /// </summary>
        /// <param name="value">The field body, folded or unfolded.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedEncodedWords"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <returns>The text, without the white space at either end of the body, and with each encoded word decoded and the white space between adjacent
        /// encoded words removed (RFC 2047 §6.2).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static string DecodeUnstructured(string value, ImfOptions options)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            CheckOptions(options);

            return DecodeUnstructuredCore(value, GetWordOptions(options), options.CharsetResolver);
        }

        /// <summary>
        /// Gets the text of each outermost comment in the body of a structured header field, such as <c>Mary &lt;mary@example.net&gt; (=?UTF-8?Q?caf=C3=A9?=)</c>.
        /// </summary>
        /// <param name="value">The field body as UTF-8 (RFC 6532), folded or unfolded.</param>
        /// <returns>The text of each comment, in order, as <see cref="TryGetComments(ReadOnlySpan{byte}, ImfOptions, out IReadOnlyList{string})"/> gives it.</returns>
        /// <exception cref="FormatException">The value is not valid UTF-8, or does not divide into the tokens of RFC 5322 §3.2.</exception>
        public static IReadOnlyList<string> GetComments(ReadOnlySpan<byte> value)
        {
            return GetComments(value, DefaultOptions);
        }

        /// <summary>
        /// Gets the text of each outermost comment in the body of a structured header field, such as <c>Mary &lt;mary@example.net&gt; (=?UTF-8?Q?caf=C3=A9?=)</c>.
        /// </summary>
        /// <param name="value">The field body, folded or unfolded.  Bytes that aren't valid UTF-8 are decoded with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> before any encoded word is read, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedEncodedWords"/>,
        /// <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <returns>The text of each comment, in order, as <see cref="TryGetComments(ReadOnlySpan{byte}, ImfOptions, out IReadOnlyList{string})"/> gives it.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value does not divide into the tokens of RFC 5322 §3.2, or is not valid UTF-8 and
        /// <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>false</c>.</exception>
        public static IReadOnlyList<string> GetComments(ReadOnlySpan<byte> value, ImfOptions options)
        {
            if (!TryGetComments(value, options, out var comments)) throw new FormatException(InvalidCommentsMessage);

            return comments;
        }

        /// <summary>
        /// Gets the text of each outermost comment in the body of a structured header field, such as <c>Mary &lt;mary@example.net&gt; (=?UTF-8?Q?caf=C3=A9?=)</c>.
        /// </summary>
        /// <param name="value">The field body, folded or unfolded.</param>
        /// <returns>The text of each comment, in order, as <see cref="TryGetComments(ReadOnlySpan{byte}, ImfOptions, out IReadOnlyList{string})"/> gives it.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value does not divide into the tokens of RFC 5322 §3.2.</exception>
        public static IReadOnlyList<string> GetComments(string value)
        {
            return GetComments(value, DefaultOptions);
        }

        /// <summary>
        /// Gets the text of each outermost comment in the body of a structured header field, such as <c>Mary &lt;mary@example.net&gt; (=?UTF-8?Q?caf=C3=A9?=)</c>.
        /// </summary>
        /// <param name="value">The field body, folded or unfolded.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedEncodedWords"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <returns>The text of each comment, in order, as <see cref="TryGetComments(ReadOnlySpan{byte}, ImfOptions, out IReadOnlyList{string})"/> gives it.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value does not divide into the tokens of RFC 5322 §3.2.</exception>
        public static IReadOnlyList<string> GetComments(string value, ImfOptions options)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryGetComments(value, options, out var comments)) throw new FormatException(InvalidCommentsMessage);

            return comments;
        }

        /// <summary>
        /// Gets the text of each outermost comment in the body of a structured header field.
        /// </summary>
        /// <remarks>
        /// The text of a comment has no outer parentheses, and its folds are unfolded.  Each quoted-pair stands for its character, and the parentheses of a
        /// nested comment are kept, so <c>(a \( (b))</c> gives <c>a ( (b)</c>.  An encoded word is read in each run of <c>ctext</c> and white space
        /// (RFC 2047 §5(2)), but never in a quoted-pair or another token.
        /// </remarks>
        /// <param name="value">The field body, folded or unfolded.  Bytes that aren't valid UTF-8 are decoded with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> before any encoded word is read, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedEncodedWords"/>,
        /// <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <param name="comments">The text of each comment, in order, if the value is valid.</param>
        /// <returns><c>true</c> if the value divides into the tokens of RFC 5322 §3.2; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryGetComments(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out IReadOnlyList<string> comments)
        {
            CheckOptions(options);

            comments = null;
            if (!TryGetUtf8(value, options, out var utf8)) return false;

            var tokenizer = new ImfTokenizer(utf8);
            var list = new List<string>();
            while (true)
            {
                if (!tokenizer.TryReadWithComments(out var token)) return false;

                if (token.Kind == ImfTokenKind.End) break;

                if (token.Kind == ImfTokenKind.Comment) list.Add(DecodeComment(tokenizer.GetText(token), options));
            }

            comments = new ReadOnlyCollection<string>(list);
            return true;
        }

        /// <summary>
        /// Gets the text of each outermost comment in the body of a structured header field.
        /// </summary>
        /// <remarks>
        /// The text of a comment has no outer parentheses, and its folds are unfolded.  Each quoted-pair stands for its character, and the parentheses of a
        /// nested comment are kept, so <c>(a \( (b))</c> gives <c>a ( (b)</c>.  An encoded word is read in each run of <c>ctext</c> and white space
        /// (RFC 2047 §5(2)), but never in a quoted-pair or another token.
        /// </remarks>
        /// <param name="value">The field body, folded or unfolded.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedEncodedWords"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <param name="comments">The text of each comment, in order, if the value is valid.</param>
        /// <returns><c>true</c> if the value divides into the tokens of RFC 5322 §3.2; otherwise <c>false</c>, including for <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryGetComments([AllowNull, NotNullWhen(true)] string value, ImfOptions options, [MaybeNullWhen(false)] out IReadOnlyList<string> comments)
        {
            CheckOptions(options);

            comments = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryGetComments(bytes, options, out comments);
        }

        /// <summary>
        /// Writes text as the body of an unstructured header field, such as Subject.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A word that has to be encoded becomes encoded words, together with any such words next to it and the white space between them, which would
        /// otherwise be lost (RFC 2047 §6.2).  Other words, and the white space around them, are written as they are.  So is white space at either end of the
        /// text, which a reader doesn't count as part of it.
        /// </para>
        /// <para>
        /// With <see cref="ImfHeaderEncoding.EncodedWords"/>, a word has to be encoded if it isn't US-ASCII.  With either encoding, it has to be if it holds
        /// <c>=?</c>, so that a reader doesn't take text that looks like an encoded word for one.
        /// </para>
        /// </remarks>
        /// <param name="text">The text.</param>
        /// <param name="encoding">How to write text that is not US-ASCII.</param>
        /// <returns>The field body, unfolded.  Every encoded word in it is short enough that it can be folded to fit a line of 78 characters.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="text"/> holds a control character other than tab or an unpaired surrogate.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="encoding"/> is not an <see cref="ImfHeaderEncoding"/> member.</exception>
        public static string FormatUnstructured(string text, ImfHeaderEncoding encoding)
        {
            CheckText(text);
            CheckEncoding(encoding);

            return FormatUnstructuredCore(text, encoding);
        }

        /// <summary>
        /// Writes text as a comment, for the body of a structured header field.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Words are encoded as in <see cref="FormatUnstructured"/>, and any other parenthesis or backslash is written as a quoted-pair, so
        /// <see cref="GetComments(string)"/> gives the same text back, including white space at either end.
        /// </para>
        /// </remarks>
        /// <param name="text">The text, without the outer parentheses.</param>
        /// <param name="encoding">How to write text that is not US-ASCII.</param>
        /// <returns>The comment, with its parentheses, unfolded.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="text"/> holds a control character other than tab or an unpaired surrogate.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="encoding"/> is not an <see cref="ImfHeaderEncoding"/> member.</exception>
        public static string FormatComment(string text, ImfHeaderEncoding encoding)
        {
            CheckText(text);
            CheckEncoding(encoding);

            var output = new StringBuilder(text.Length + 2);
            output.Append('(');
            AppendWords(text, MimeEncodedWordContext.Comment, encoding, output);
            output.Append(')');
            return output.ToString();
        }

        /// <summary>
        /// Checks the options of a parse.
        /// </summary>
        internal static void CheckOptions(ImfOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (options.CharsetResolver is null) throw new ArgumentException("A character set resolver is required to decode encoded words.", nameof(options));
            if (options.AllowEightBitHeaders) ImfHeaderReader.GetFallbackEncoding(options);
        }

        /// <summary>
        /// Checks that a value is a member of <see cref="ImfHeaderEncoding"/>.
        /// </summary>
        internal static void CheckEncoding(ImfHeaderEncoding encoding)
        {
            if (encoding != ImfHeaderEncoding.EncodedWords && encoding != ImfHeaderEncoding.Utf8) throw new ArgumentOutOfRangeException(nameof(encoding));
        }

        /// <summary>
        /// Gets the accommodations of RFC 2047 that some options ask for.
        /// </summary>
        internal static MimeEncodedWordParseOptions GetWordOptions(ImfOptions options)
        {
            return options.AllowMalformedEncodedWords ? MalformedWordOptions : MimeEncodedWordParseOptions.None;
        }

        /// <summary>
        /// Gets a field body as well-formed UTF-8, decoding one that isn't with the fallback character set, if the options accept that.  This comes before any
        /// encoded word is read, as those are found in the text.
        /// </summary>
        /// <returns><c>false</c> if the body is not valid UTF-8, and the options don't accept that.</returns>
        internal static bool TryGetUtf8(ReadOnlySpan<byte> value, ImfOptions options, out ReadOnlySpan<byte> utf8)
        {
            utf8 = value;
            if (ImfTokenizer.IsWellFormed(value)) return true;
            if (!options.AllowEightBitHeaders) return false;

            var bytes = value.ToArray();
            utf8 = Encoding.UTF8.GetBytes(ImfHeaderReader.GetFallbackEncoding(options).GetString(bytes, 0, bytes.Length));
            return true;
        }

        /// <summary>
        /// Decodes the encoded words in a run of text.
        /// </summary>
        /// <returns>The text; or the run as it is, if a character set resolver supplied an encoding that gave an unpaired surrogate, which can't be written.</returns>
        internal static string DecodeRun(string run, MimeEncodedWordContext context, MimeEncodedWordParseOptions wordOptions, IMimeCharsetResolver charsetResolver)
        {
            var text = MimeEncodedWord.DecodeText(run, context, wordOptions, charsetResolver);
            return ImfGrammar.HasOnlyPairedSurrogates(text) ? text : run;
        }

        /// <summary>
        /// Decodes the body of an unstructured field.
        /// </summary>
        internal static string DecodeUnstructuredCore(string value, MimeEncodedWordParseOptions wordOptions, IMimeCharsetResolver charsetResolver)
        {
            return DecodeRun(Unfold(value).Trim(' ', '\t'), MimeEncodedWordContext.Text, wordOptions, charsetResolver);
        }

        /// <summary>
        /// Writes text that has been checked as the body of an unstructured field.
        /// </summary>
        internal static string FormatUnstructuredCore(string text, ImfHeaderEncoding encoding)
        {
            var output = new StringBuilder(text.Length);
            AppendWords(text, MimeEncodedWordContext.Text, encoding, output);
            return output.ToString();
        }

        /// <summary>
        /// Writes text as encoded words, separated by a space, which a reader drops (RFC 2047 §6.2).
        /// </summary>
        internal static void AppendEncodedWords(string text, MimeEncodedWordContext context, StringBuilder output)
        {
            var words = MimeEncodedWord.EncodeText(text, context);
            for (var index = 0; index < words.Count; index++)
            {
                if (index > 0) output.Append(' ');

                output.Append(words[index].ToString());
            }
        }

        /// <summary>
        /// Determines if a string holds a character that is not US-ASCII.
        /// </summary>
        internal static bool HasNonAscii(string text)
        {
            foreach (var c in text)
            {
                if (c >= 0x80) return true;
            }

            return false;
        }

        private static void CheckText(string text)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            if (!ImfGrammar.IsWritableText(text)) throw new ArgumentException("The text holds a character that cannot be written.", nameof(text));
        }

        // Only a CRLF before a space or tab is a fold (RFC 5322 §2.2.3).
        private static string Unfold(string value)
        {
            if (value.IndexOf('\r') < 0) return value;

            var output = new StringBuilder(value.Length);
            for (var index = 0; index < value.Length; index++)
            {
                if (value[index] == '\r' && index + 2 < value.Length && value[index + 1] == '\n' && IsWhitespace(value[index + 2]))
                {
                    index++;
                }
                else
                {
                    output.Append(value[index]);
                }
            }

            return output.ToString();
        }

        // Goes through a comment with its parentheses, in a loop and not by recursion, so that hostile nesting costs nothing extra.  Decoding each run of ctext
        // by itself means that a quoted-pair or parenthesis can't be taken for part of an encoded word (RFC 2047 §5(2)).
        private static string DecodeComment(string comment, ImfOptions options)
        {
            var wordOptions = GetWordOptions(options);
            var output = new StringBuilder(comment.Length);
            var run = new StringBuilder();
            for (var index = 1; index < comment.Length - 1; index++)
            {
                var c = comment[index];
                if (c == '\\')
                {
                    FlushRun(run, wordOptions, options.CharsetResolver, output);
                    output.Append(comment[++index]);
                }
                else if (c == '(' || c == ')')
                {
                    FlushRun(run, wordOptions, options.CharsetResolver, output);
                    output.Append(c);
                }
                else if (c == '\r')
                {
                    // Only a fold has one that is not in a quoted-pair: the line feed goes as well, and the white space after it stays.
                    index++;
                }
                else
                {
                    run.Append(c);
                }
            }

            FlushRun(run, wordOptions, options.CharsetResolver, output);
            return output.ToString();
        }

        private static void FlushRun(StringBuilder run, MimeEncodedWordParseOptions wordOptions, IMimeCharsetResolver charsetResolver, StringBuilder output)
        {
            if (run.Length == 0) return;

            output.Append(DecodeRun(run.ToString(), MimeEncodedWordContext.Comment, wordOptions, charsetResolver));
            run.Length = 0;
        }

        private static void AppendWords(string text, MimeEncodedWordContext context, ImfHeaderEncoding encoding, StringBuilder output)
        {
            var index = 0;
            while (index < text.Length)
            {
                var wordStart = SkipWhitespace(text, index);
                output.Append(text, index, wordStart - index);
                if (wordStart == text.Length) return;

                var wordEnd = FindWordEnd(text, wordStart);
                if (!NeedsEncoding(text, wordStart, wordEnd, encoding))
                {
                    AppendPlainWord(text, wordStart, wordEnd, context, output);
                    index = wordEnd;
                    continue;
                }

                // ref. RFC 2047 §6.2: white space between encoded words is dropped, so what is between two of them goes in the words.
                var runEnd = wordEnd;
                while (true)
                {
                    var nextStart = SkipWhitespace(text, runEnd);
                    if (nextStart == text.Length) break;

                    var nextEnd = FindWordEnd(text, nextStart);
                    if (!NeedsEncoding(text, nextStart, nextEnd, encoding)) break;

                    runEnd = nextEnd;
                }

                AppendEncodedWords(text.Substring(wordStart, runEnd - wordStart), context, output);
                index = runEnd;
            }
        }

        private static void AppendPlainWord(string text, int start, int end, MimeEncodedWordContext context, StringBuilder output)
        {
            for (var index = start; index < end; index++)
            {
                var c = text[index];

                // ref. RFC 5322 §3.2.2: ctext has no parenthesis or backslash.
                if (context == MimeEncodedWordContext.Comment && (c == '(' || c == ')' || c == '\\')) output.Append('\\');

                output.Append(c);
            }
        }

        private static bool NeedsEncoding(string text, int start, int end, ImfHeaderEncoding encoding)
        {
            for (var index = start; index < end; index++)
            {
                if (encoding == ImfHeaderEncoding.EncodedWords && text[index] >= 0x80) return true;

                if (text[index] == '=' && index + 1 < end && text[index + 1] == '?') return true;
            }

            return false;
        }

        private static int SkipWhitespace(string text, int index)
        {
            while (index < text.Length && IsWhitespace(text[index])) index++;

            return index;
        }

        private static int FindWordEnd(string text, int index)
        {
            while (index < text.Length && !IsWhitespace(text[index])) index++;

            return index;
        }

        private static bool IsWhitespace(char c)
        {
            return c == ' ' || c == '\t';
        }
    }
}
