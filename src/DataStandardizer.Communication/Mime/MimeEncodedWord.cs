using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// An RFC 2047 encoded word, such as <c>=?ISO-8859-1?Q?Andr=E9?=</c>, which carries text that isn't US-ASCII in a message header: its
    /// character set, the RFC 2231 §5 language of its text, if any, its encoding, and its decoded text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A parsed word is written back as it was written, unless it is longer than <see cref="MaxLength"/> characters, as
    /// <see cref="MimeEncodedWordParseOptions.AllowLongWords"/> accepts: RFC 2047 §2 doesn't allow such a word, so its text is written as
    /// encoded words in UTF-8 that are each within the limit, separated by spaces. A word that is created is written in UTF-8. Two words are
    /// equal when their character sets, languages, encodings and texts are equal, even when they are written differently, such as
    /// <c>=41</c> and <c>A</c> in "Q"-encoded text. Character sets and languages are compared case-insensitively.
    /// </para>
    /// <para>
    /// Use <see cref="EncodeText(string, MimeEncodedWordContext)"/> to write text of any length as encoded words, and
    /// <see cref="DecodeText(ReadOnlySpan{char}, MimeEncodedWordContext, MimeEncodedWordParseOptions, IMimeCharsetResolver)"/> to read a
    /// run of text that holds encoded words.
    /// </para>
    /// <para>
    /// This type doesn't decide where an encoded word may appear: in unstructured text, a comment or a phrase, but never in a quoted-string,
    /// an addr-spec or a parameter value (RFC 2047 §5). That is up to the grammar of the header field, which the caller parses.
    /// </para>
    /// </remarks>
    public readonly struct MimeEncodedWord : IEquatable<MimeEncodedWord>
#if NET7_0_OR_GREATER
        , ISpanFormattable, ISpanParsable<MimeEncodedWord>
#elif NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        /// <summary>
        /// The most characters an encoded word may have, including its character set, encoding, encoded text and delimiters (RFC 2047 §2).
        /// </summary>
        public const int MaxLength = 75;

        // ref. RFC 2047 §2: "=?" charset "?" encoding "?" encoded-text "?=", where the encoding is one letter (RFC 2047 §4).
        private const int DelimiterLength = 7;

        // ref. RFC 2047 §2: a charset, an encoding and encoded text of one character each.
        private const int MinLength = DelimiterLength + 2;

        // The most octets of a character that MimeEncodedWordParseOptions.AllowSplitCharacters carries into the next word: a character is
        // at most 4 octets in the multi-octet character sets in use, such as UTF-8 (RFC 3629 §3), UTF-16, UTF-32 and GB18030.
        private const int MaxCarriedOctets = 3;

        // ref. ANSI X3.4 (US-ASCII): 0x41 is 'A'. EndsInAsciiMode reads it after a word's octets.
        private const byte ProbeOctet = 0x41;
        private const string ProbeCharacter = "A";

        // ref. ECMA-35 (ISO/IEC 2022): the ESC, SO and SI control characters.
        private const byte Escape = 0x1B;
        private const byte ShiftOut = 0x0E;
        private const byte ShiftIn = 0x0F;

        private const string InvalidFormatMessage = "The value is not a valid encoded word.";
        private const string UninitializedMessage = nameof(MimeEncodedWord) + " is uninitialized.";

        // AllowLowercaseHexDigits is obsolete and has no effect, but a caller that still gives it isn't refused.
#pragma warning disable CS0618
        private const MimeEncodedWordParseOptions AllOptions = MimeEncodedWordParseOptions.AllowLongWords
            | MimeEncodedWordParseOptions.AllowEmbeddedWords
            | MimeEncodedWordParseOptions.AllowUnknownCharsets
            | MimeEncodedWordParseOptions.AllowLowercaseHexDigits
            | MimeEncodedWordParseOptions.AllowInvalidOctets
            | MimeEncodedWordParseOptions.AllowSplitCharacters;
#pragma warning restore CS0618

        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        private readonly string _word;
        private readonly MimeCharset _charset;
#if NETCOREAPP3_0_OR_GREATER
        private readonly string? _language;
#else
        private readonly string _language;
#endif
        private readonly MimeEncodedWordEncoding _encoding;
        private readonly string _text;

        /// <summary>
        /// Creates an encoded word that holds text in UTF-8.
        /// </summary>
        /// <param name="text">The text, which must not be empty or contain an unpaired surrogate.</param>
        /// <param name="encoding">The encoding to write the text in.</param>
        /// <param name="context">The place in a header field where the word will appear, which sets the characters that "Q"-encoded text
        /// may hold.</param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="text"/> is empty, contains an unpaired surrogate, or is too long to fit in
        /// one encoded word of <see cref="MaxLength"/> characters.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="encoding"/> is not a <see cref="MimeEncodedWordEncoding"/>
        /// member, or <paramref name="context"/> is not a <see cref="MimeEncodedWordContext"/> member.</exception>
        /// <remarks>
        /// Use <see cref="EncodeText(string, MimeEncodedWordContext)"/> for text that may not fit in one word.
        /// </remarks>
        public MimeEncodedWord(string text, MimeEncodedWordEncoding encoding, MimeEncodedWordContext context)
        {
            this = CreateChecked(text, encoding, context, null);
        }

        /// <summary>
        /// Creates an encoded word that holds text in UTF-8, with the language of the text (RFC 2231 §5).
        /// </summary>
        /// <param name="text">The text, which must not be empty or contain an unpaired surrogate.</param>
        /// <param name="encoding">The encoding to write the text in.</param>
        /// <param name="context">The place in a header field where the word will appear, which sets the characters that "Q"-encoded text
        /// may hold.</param>
        /// <param name="language">The language of the text, which must be an RFC 1766 §2 language tag, such as <c>en-US</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="language"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="text"/> is empty, contains an unpaired surrogate, or is too long to fit in
        /// one encoded word of <see cref="MaxLength"/> characters; or <paramref name="language"/> is not an RFC 1766 language
        /// tag.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="encoding"/> is not a <see cref="MimeEncodedWordEncoding"/>
        /// member, or <paramref name="context"/> is not a <see cref="MimeEncodedWordContext"/> member.</exception>
        /// <remarks>
        /// RFC 2231 §5 calls for RFC 1766 language tags, whose subtags are 1 to 8 letters. Later BCP 47 tags with other subtags, such as
        /// <c>es-419</c>, are not accepted.
        /// </remarks>
        public MimeEncodedWord(string text, MimeEncodedWordEncoding encoding, MimeEncodedWordContext context, string language)
        {
            if (language is null) throw new ArgumentNullException(nameof(language));

            this = CreateChecked(text, encoding, context, language);
        }

#if NETCOREAPP3_0_OR_GREATER
        private MimeEncodedWord(string word, MimeCharset charset, string? language, MimeEncodedWordEncoding encoding, string text)
#else
        private MimeEncodedWord(string word, MimeCharset charset, string language, MimeEncodedWordEncoding encoding, string text)
#endif
        {
            _word = word;
            _charset = charset;
            _language = language;
            _encoding = encoding;
            _text = text;
        }

        /// <summary>
        /// Gets the character set of the text, as written.
        /// </summary>
        /// <remarks>
        /// The text is already decoded from the character set.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="MimeEncodedWord"/> is not initialised.</exception>
        public MimeCharset Charset => _word is null ? throw new InvalidOperationException(UninitializedMessage) : _charset;

        // TODO: Revisit the type of Language, and use Bcp47LanguageTag, if the MIME implementation is revised to a standard that calls for
        // BCP 47 language tags.
        /// <summary>
        /// Gets the language of the text, as given after the character set (RFC 2231 §5), such as <c>EN</c> in
        /// <c>=?US-ASCII*EN?Q?Keith_Moore?=</c>.
        /// </summary>
        /// <returns>The RFC 1766 §2 language tag, as written; an empty string if none was given.</returns>
        /// <remarks>
        /// RFC 2231 §5 calls for RFC 1766 language tags, not BCP 47 language tags, so the language is kept as a string.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="MimeEncodedWord"/> is not initialised.</exception>
        public string Language => _word is null ? throw new InvalidOperationException(UninitializedMessage) : _language ?? string.Empty;

        /// <summary>
        /// Gets the encoding the text is written in.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeEncodedWord"/> is not initialised.</exception>
        public MimeEncodedWordEncoding Encoding => _word is null ? throw new InvalidOperationException(UninitializedMessage) : _encoding;

        /// <summary>
        /// Gets the decoded text.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeEncodedWord"/> is not initialised.</exception>
        public string Text => _text ?? throw new InvalidOperationException(UninitializedMessage);

        /// <summary>
        /// Writes text as encoded words in UTF-8, in whichever of the "B" and "Q" encodings is shorter.
        /// </summary>
        /// <param name="text">The text, which may be empty. It must not contain an unpaired surrogate.</param>
        /// <param name="context">The place in a header field where the words will appear, which sets the characters that "Q"-encoded text
        /// may hold.</param>
        /// <returns>The words, in order; none if <paramref name="text"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// Each word is at most <see cref="MaxLength"/> characters long (RFC 2047 §2), and holds whole characters: a character, or a
        /// surrogate pair, is never split between words (RFC 2047 §5). Spaces are encoded in the words, so the text is read back whole when
        /// the words are written with whitespace between them, as a reader drops it (RFC 2047 §6.2).
        /// </para>
        /// <para>
        /// When both encodings are the same length, "Q" is used, as it leaves ASCII text readable.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="text"/> contains an unpaired surrogate.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="context"/> is not a <see cref="MimeEncodedWordContext"/>
        /// member.</exception>
        public static IReadOnlyList<MimeEncodedWord> EncodeText(string text, MimeEncodedWordContext context)
        {
            return EncodeTextChecked(text, context, null);
        }

        /// <summary>
        /// Writes text as encoded words in UTF-8, with the language of the text (RFC 2231 §5), in whichever of the "B" and "Q" encodings is
        /// shorter.
        /// </summary>
        /// <param name="text">The text, which may be empty. It must not contain an unpaired surrogate.</param>
        /// <param name="context">The place in a header field where the words will appear, which sets the characters that "Q"-encoded text
        /// may hold.</param>
        /// <param name="language">The language of the text, which must be an RFC 1766 §2 language tag, such as <c>en-US</c>. Each word
        /// carries it.</param>
        /// <returns>The words, in order; none if <paramref name="text"/> is empty.</returns>
        /// <remarks>
        /// <para>
        /// Each word is at most <see cref="MaxLength"/> characters long (RFC 2047 §2), and holds whole characters: a character, or a
        /// surrogate pair, is never split between words (RFC 2047 §5). Spaces are encoded in the words, so the text is read back whole when
        /// the words are written with whitespace between them, as a reader drops it (RFC 2047 §6.2).
        /// </para>
        /// <para>
        /// When both encodings are the same length, "Q" is used, as it leaves ASCII text readable.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="language"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="text"/> contains an unpaired surrogate, or <paramref name="language"/> is
        /// not an RFC 1766 language tag, or is so long that a word of <see cref="MaxLength"/> characters can't hold one of the characters of
        /// <paramref name="text"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="context"/> is not a <see cref="MimeEncodedWordContext"/>
        /// member.</exception>
        public static IReadOnlyList<MimeEncodedWord> EncodeText(string text, MimeEncodedWordContext context, string language)
        {
            if (language is null) throw new ArgumentNullException(nameof(language));

            return EncodeTextChecked(text, context, language);
        }

        /// <summary>
        /// Decodes the encoded words in a run of text strictly, as in an unstructured header field such as Subject.
        /// </summary>
        /// <param name="text">The text, such as the body of a header field, folded or unfolded.</param>
        /// <returns>The text, with each encoded word decoded and the whitespace between adjacent encoded words removed.</returns>
        /// <remarks>
        /// <para>
        /// Each run of characters between whitespace is read as an encoded word if it is one (RFC 2047 §6.1 (1)). Whitespace between two
        /// encoded words is dropped (RFC 2047 §6.2). Anything else, including a run that is not a valid encoded word, is kept as it is.
        /// Character sets are resolved with <see cref="MimeCharsetResolver.Default"/>.
        /// </para>
        /// </remarks>
        public static string DecodeText(ReadOnlySpan<char> text)
        {
            return DecodeTextCore(text, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default);
        }

        /// <summary>
        /// Decodes the encoded words in a run of text.
        /// </summary>
        /// <param name="text">The text, folded or unfolded: the body of an unstructured header field, a run of text in a comment, or the
        /// words of a phrase.</param>
        /// <param name="context">Where the text appears, which sets the characters that "Q"-encoded text may hold.</param>
        /// <param name="options">The accommodations to make for encoded words that don't follow the rules.</param>
        /// <param name="charsetResolver">The resolver for the character sets of the words.</param>
        /// <returns>The text, with each encoded word decoded and the whitespace between adjacent encoded words removed.</returns>
        /// <remarks>
        /// <para>
        /// Each run of characters between whitespace is read as an encoded word if it is one (RFC 2047 §6.1). Whitespace between two
        /// encoded words is dropped (RFC 2047 §6.2). Anything else, including a run that is not a valid encoded word, is kept as it is.
        /// </para>
        /// <para>
        /// Pass only text in which RFC 2047 §5 allows encoded words. In a comment, pass each run of <c>ctext</c>, without the parentheses
        /// and quoted-pairs. In a phrase, pass the words, not any quoted-strings.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="charsetResolver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="context"/> is not a <see cref="MimeEncodedWordContext"/> member, or
        /// <paramref name="options"/> is not a combination of <see cref="MimeEncodedWordParseOptions"/> members.</exception>
        public static string DecodeText(ReadOnlySpan<char> text, MimeEncodedWordContext context, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver)
        {
            CheckContext(context);
            CheckOptions(options);
            if (charsetResolver is null) throw new ArgumentNullException(nameof(charsetResolver));

            return DecodeTextCore(text, context, options, charsetResolver);
        }

        /// <summary>
        /// Decodes the encoded words in a run of text strictly, as in an unstructured header field such as Subject.
        /// </summary>
        /// <param name="text">The text, such as the body of a header field, folded or unfolded.</param>
        /// <returns>The text, with each encoded word decoded and the whitespace between adjacent encoded words removed.</returns>
        /// <remarks>
        /// <para>
        /// Each run of characters between whitespace is read as an encoded word if it is one (RFC 2047 §6.1 (1)). Whitespace between two
        /// encoded words is dropped (RFC 2047 §6.2). Anything else, including a run that is not a valid encoded word, is kept as it is.
        /// Character sets are resolved with <see cref="MimeCharsetResolver.Default"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public static string DecodeText(string text)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));

            return DecodeText(text.AsSpan());
        }

        /// <summary>
        /// Decodes the encoded words in a run of text.
        /// </summary>
        /// <param name="text">The text, folded or unfolded: the body of an unstructured header field, a run of text in a comment, or the
        /// words of a phrase.</param>
        /// <param name="context">Where the text appears, which sets the characters that "Q"-encoded text may hold.</param>
        /// <param name="options">The accommodations to make for encoded words that don't follow the rules.</param>
        /// <param name="charsetResolver">The resolver for the character sets of the words.</param>
        /// <returns>The text, with each encoded word decoded and the whitespace between adjacent encoded words removed.</returns>
        /// <remarks>
        /// <para>
        /// Each run of characters between whitespace is read as an encoded word if it is one (RFC 2047 §6.1). Whitespace between two
        /// encoded words is dropped (RFC 2047 §6.2). Anything else, including a run that is not a valid encoded word, is kept as it is.
        /// </para>
        /// <para>
        /// Pass only text in which RFC 2047 §5 allows encoded words. In a comment, pass each run of <c>ctext</c>, without the parentheses
        /// and quoted-pairs. In a phrase, pass the words, not any quoted-strings.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="charsetResolver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="context"/> is not a <see cref="MimeEncodedWordContext"/> member, or
        /// <paramref name="options"/> is not a combination of <see cref="MimeEncodedWordParseOptions"/> members.</exception>
        public static string DecodeText(string text, MimeEncodedWordContext context, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));

            return DecodeText(text.AsSpan(), context, options, charsetResolver);
        }

        /// <summary>
        /// Parses an encoded word strictly, as in unstructured text.
        /// </summary>
        /// <param name="value">The word, such as <c>=?UTF-8?Q?caf=C3=A9?=</c>, with no whitespace around it.</param>
        /// <param name="result">The word, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid encoded word; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Character sets are resolved with <see cref="MimeCharsetResolver.Default"/>.
        /// </remarks>
        public static bool TryParse(ReadOnlySpan<char> value, out MimeEncodedWord result)
        {
            return TryParseCore(value, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default, out result);
        }

        /// <summary>
        /// Parses an encoded word.
        /// </summary>
        /// <param name="value">The word, such as <c>=?UTF-8?Q?caf=C3=A9?=</c>, with no whitespace around it.</param>
        /// <param name="context">Where the word appears, which sets the characters that "Q"-encoded text may hold.</param>
        /// <param name="options">The accommodations to make for encoded words that don't follow the rules.</param>
        /// <param name="charsetResolver">The resolver for the character set of the word.</param>
        /// <param name="result">The word, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid encoded word; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="charsetResolver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="context"/> is not a <see cref="MimeEncodedWordContext"/> member, or
        /// <paramref name="options"/> is not a combination of <see cref="MimeEncodedWordParseOptions"/> members.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, MimeEncodedWordContext context, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver, out MimeEncodedWord result)
        {
            CheckContext(context);
            CheckOptions(options);
            if (charsetResolver is null) throw new ArgumentNullException(nameof(charsetResolver));

            return TryParseCore(value, context, options, charsetResolver, out result);
        }

        /// <summary>
        /// Parses an encoded word strictly, as in unstructured text.
        /// </summary>
        /// <param name="value">The word, such as <c>=?UTF-8?Q?caf=C3=A9?=</c>, with no whitespace around it.</param>
        /// <param name="result">The word, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid encoded word; otherwise <c>false</c>, including for <c>null</c>.</returns>
        /// <remarks>
        /// Character sets are resolved with <see cref="MimeCharsetResolver.Default"/>.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse([NotNullWhen(true)] string? value, out MimeEncodedWord result)
#else
        public static bool TryParse([NotNullWhen(true)] string value, out MimeEncodedWord result)
#endif
        {
            if (value is null)
            {
                result = default(MimeEncodedWord);
                return false;
            }

            return TryParse(value.AsSpan(), out result);
        }

        /// <summary>
        /// Parses an encoded word.
        /// </summary>
        /// <param name="value">The word, such as <c>=?UTF-8?Q?caf=C3=A9?=</c>, with no whitespace around it.</param>
        /// <param name="context">Where the word appears, which sets the characters that "Q"-encoded text may hold.</param>
        /// <param name="options">The accommodations to make for encoded words that don't follow the rules.</param>
        /// <param name="charsetResolver">The resolver for the character set of the word.</param>
        /// <param name="result">The word, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid encoded word; otherwise <c>false</c>, including for <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="charsetResolver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="context"/> is not a <see cref="MimeEncodedWordContext"/> member, or
        /// <paramref name="options"/> is not a combination of <see cref="MimeEncodedWordParseOptions"/> members.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse([NotNullWhen(true)] string? value, MimeEncodedWordContext context, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver, out MimeEncodedWord result)
#else
        public static bool TryParse([NotNullWhen(true)] string value, MimeEncodedWordContext context, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver, out MimeEncodedWord result)
#endif
        {
            CheckContext(context);
            CheckOptions(options);
            if (charsetResolver is null) throw new ArgumentNullException(nameof(charsetResolver));

            if (value is null)
            {
                result = default(MimeEncodedWord);
                return false;
            }

            return TryParseCore(value.AsSpan(), context, options, charsetResolver, out result);
        }

        /// <summary>
        /// Parses an encoded word strictly, as in unstructured text.
        /// </summary>
        /// <param name="value">The word, such as <c>=?UTF-8?Q?caf=C3=A9?=</c>, with no whitespace around it.</param>
        /// <returns>The word.</returns>
        /// <remarks>
        /// Character sets are resolved with <see cref="MimeCharsetResolver.Default"/>.
        /// </remarks>
        /// <exception cref="FormatException">The value is not a valid encoded word.</exception>
        public static MimeEncodedWord Parse(ReadOnlySpan<char> value)
        {
            MimeEncodedWord result;
            if (!TryParse(value, out result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses an encoded word.
        /// </summary>
        /// <param name="value">The word, such as <c>=?UTF-8?Q?caf=C3=A9?=</c>, with no whitespace around it.</param>
        /// <param name="context">Where the word appears, which sets the characters that "Q"-encoded text may hold.</param>
        /// <param name="options">The accommodations to make for encoded words that don't follow the rules.</param>
        /// <param name="charsetResolver">The resolver for the character set of the word.</param>
        /// <returns>The word.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="charsetResolver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="context"/> is not a <see cref="MimeEncodedWordContext"/> member, or
        /// <paramref name="options"/> is not a combination of <see cref="MimeEncodedWordParseOptions"/> members.</exception>
        /// <exception cref="FormatException">The value is not a valid encoded word.</exception>
        public static MimeEncodedWord Parse(ReadOnlySpan<char> value, MimeEncodedWordContext context, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver)
        {
            MimeEncodedWord result;
            if (!TryParse(value, context, options, charsetResolver, out result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses an encoded word strictly, as in unstructured text.
        /// </summary>
        /// <param name="value">The word, such as <c>=?UTF-8?Q?caf=C3=A9?=</c>, with no whitespace around it.</param>
        /// <returns>The word.</returns>
        /// <remarks>
        /// Character sets are resolved with <see cref="MimeCharsetResolver.Default"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid encoded word.</exception>
        public static MimeEncodedWord Parse(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            return Parse(value.AsSpan());
        }

        /// <summary>
        /// Parses an encoded word.
        /// </summary>
        /// <param name="value">The word, such as <c>=?UTF-8?Q?caf=C3=A9?=</c>, with no whitespace around it.</param>
        /// <param name="context">Where the word appears, which sets the characters that "Q"-encoded text may hold.</param>
        /// <param name="options">The accommodations to make for encoded words that don't follow the rules.</param>
        /// <param name="charsetResolver">The resolver for the character set of the word.</param>
        /// <returns>The word.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="charsetResolver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="context"/> is not a <see cref="MimeEncodedWordContext"/> member, or
        /// <paramref name="options"/> is not a combination of <see cref="MimeEncodedWordParseOptions"/> members.</exception>
        /// <exception cref="FormatException">The value is not a valid encoded word.</exception>
        public static MimeEncodedWord Parse(string value, MimeEncodedWordContext context, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            return Parse(value.AsSpan(), context, options, charsetResolver);
        }

#if NET7_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The word is parsed strictly, as in unstructured text. The provider is ignored.</remarks>
        static MimeEncodedWord IParsable<MimeEncodedWord>.Parse(string s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <inheritdoc />
        /// <remarks>The word is parsed strictly, as in unstructured text. The provider is ignored.</remarks>
        static bool IParsable<MimeEncodedWord>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out MimeEncodedWord result)
        {
            return TryParse(s, out result);
        }

        /// <inheritdoc />
        /// <remarks>The word is parsed strictly, as in unstructured text. The provider is ignored.</remarks>
        static MimeEncodedWord ISpanParsable<MimeEncodedWord>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <inheritdoc />
        /// <remarks>The word is parsed strictly, as in unstructured text. The provider is ignored.</remarks>
        static bool ISpanParsable<MimeEncodedWord>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out MimeEncodedWord result)
        {
            return TryParse(s, out result);
        }
#endif

        /// <summary>
        /// Formats the encoded word into a span: as it was written, if it was parsed and is at most <see cref="MaxLength"/> characters
        /// long.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <returns><c>true</c> if the word was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <remarks>
        /// Nothing is written if the <see cref="MimeEncodedWord"/> is not initialised.
        /// </remarks>
        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            var position = 0;
            if (_word != null && !MimeGrammar.TryWrite(_word, destination, ref position))
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = position;
            return true;
        }

        /// <summary>
        /// Formats the encoded word: as it was written, if it was parsed and is at most <see cref="MaxLength"/> characters long.
        /// </summary>
        /// <returns>The word; an empty string if the <see cref="MimeEncodedWord"/> is not initialised.</returns>
        public override string ToString()
        {
            return _word ?? string.Empty;
        }

#if NET6_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The format and provider are ignored, as an encoded word has only one form.</remarks>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return TryFormat(destination, out charsWritten);
        }

        /// <inheritdoc />
        /// <remarks>The format and provider are ignored, as an encoded word has only one form.</remarks>
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }
#endif

        /// <summary>
        /// Indicates whether this encoded word equals another.
        /// </summary>
        /// <remarks>
        /// Character sets and languages are compared case-insensitively, and texts case-sensitively. How the text was encoded is not
        /// compared, other than its encoding.
        /// </remarks>
        public bool Equals(MimeEncodedWord other)
        {
            return _charset.Equals(other._charset) &&
                   string.Equals(_language, other._language, StringComparison.OrdinalIgnoreCase) &&
                   _encoding == other._encoding &&
                   string.Equals(_text, other._text, StringComparison.Ordinal);
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is MimeEncodedWord other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Must agree with Equals, which compares character sets and languages case-insensitively.
            if (_word is null) return 0;

            unchecked
            {
                var hash = _charset.GetHashCode() * 31 + (int)_encoding;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(_text);
                return _language is null ? hash : hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(_language);
            }
        }

        /// <summary>
        /// Indicates whether two encoded words are equal.
        /// </summary>
        public static bool operator ==(MimeEncodedWord left, MimeEncodedWord right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two encoded words differ.
        /// </summary>
        public static bool operator !=(MimeEncodedWord left, MimeEncodedWord right)
        {
            return !left.Equals(right);
        }

#if NETCOREAPP3_0_OR_GREATER
        private static MimeEncodedWord CreateChecked(string text, MimeEncodedWordEncoding encoding, MimeEncodedWordContext context, string? language)
#else
        private static MimeEncodedWord CreateChecked(string text, MimeEncodedWordEncoding encoding, MimeEncodedWordContext context, string language)
#endif
        {
            CheckText(text);
            if (text.Length == 0) throw new ArgumentException("The text is empty, and an encoded word holds at least one character.", nameof(text));
            if (encoding != MimeEncodedWordEncoding.B && encoding != MimeEncodedWordEncoding.Q) throw new ArgumentOutOfRangeException(nameof(encoding));
            CheckContext(context);
            CheckLanguage(language);

            var word = Create(text, 0, text.Length, encoding, context, language);
            if (word._word.Length > MaxLength) throw new ArgumentException("The text doesn't fit in one encoded word of 75 characters.", nameof(text));

            return word;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static IReadOnlyList<MimeEncodedWord> EncodeTextChecked(string text, MimeEncodedWordContext context, string? language)
#else
        private static IReadOnlyList<MimeEncodedWord> EncodeTextChecked(string text, MimeEncodedWordContext context, string language)
#endif
        {
            CheckText(text);
            CheckContext(context);
            CheckLanguage(language);

            if (text.Length == 0) return new ReadOnlyCollection<MimeEncodedWord>(new List<MimeEncodedWord>());

            var words = TryEncodeShortestWords(text, context, language);
            if (words is null)
            {
                throw new ArgumentException("The language is too long for an encoded word of 75 characters to hold all of a character.", nameof(language));
            }

            return new ReadOnlyCollection<MimeEncodedWord>(words);
        }

        // Splits non-empty text into words in whichever of the "B" and "Q" encodings is shorter, "Q" when they are the same length, or
        // returns null if one character doesn't fit in a word in either encoding.
#if NETCOREAPP3_0_OR_GREATER
        private static List<MimeEncodedWord>? TryEncodeShortestWords(string text, MimeEncodedWordContext context, string? language)
#else
        private static List<MimeEncodedWord> TryEncodeShortestWords(string text, MimeEncodedWordContext context, string language)
#endif
        {
            var qWords = TryEncodeWords(text, MimeEncodedWordEncoding.Q, context, language);
            var bWords = TryEncodeWords(text, MimeEncodedWordEncoding.B, context, language);
            return qWords != null && (bWords is null || GetLength(qWords) <= GetLength(bWords)) ? qWords : bWords;
        }

        // Writes the text of a word longer than MaxLength, accepted with MimeEncodedWordParseOptions.AllowLongWords, as UTF-8 words that
        // are each within MaxLength, separated by spaces, or returns null if the text is empty or its language leaves no room for a
        // character. The "Q" mode of a phrase allows the fewest characters, so the words are valid wherever the word was. An unpaired
        // surrogate, which a character set such as UTF-7 can decode to, is written as U+FFFD, as UTF-8 can't hold it.
#if NETCOREAPP3_0_OR_GREATER
        private static string? TryRewriteLongWord(string text, string? language)
#else
        private static string TryRewriteLongWord(string text, string language)
#endif
        {
            if (text.Length == 0) return null;

            var words = TryEncodeShortestWords(ReplaceUnpairedSurrogates(text), MimeEncodedWordContext.Phrase, language);
            if (words is null) return null;

            // ref. RFC 2047 §6.2: whitespace between adjacent encoded words is dropped when they are read, so the text is read back whole.
            var builder = new StringBuilder(GetLength(words) + words.Count - 1);
            for (var index = 0; index < words.Count; index++)
            {
                if (index > 0) builder.Append(' ');
                builder.Append(words[index]._word);
            }

            return builder.ToString();
        }

        // Splits text into words of whole characters that each fit in MaxLength, with the shortest total length, or returns null if one
        // character doesn't fit in a word.
#if NETCOREAPP3_0_OR_GREATER
        private static List<MimeEncodedWord>? TryEncodeWords(string text, MimeEncodedWordEncoding encoding, MimeEncodedWordContext context, string? language)
#else
        private static List<MimeEncodedWord> TryEncodeWords(string text, MimeEncodedWordEncoding encoding, MimeEncodedWordContext context, string language)
#endif
        {
            var wordOverhead = DelimiterLength + GetCharsetLength(language);
            var maxEncodedLength = MaxLength - wordOverhead;
            var mode = GetMode(context);

            // The index of each character in the text, followed by the length of the text, and the octets and "Q" length of each one.
            var starts = new List<int>();
            var octetCounts = new List<int>();
            var qLengths = new List<int>();
            var octets = new byte[4];
            for (var index = 0; index < text.Length;)
            {
                int charCount;
                var count = GetUtf8Octets(text, index, octets, out charCount);
                starts.Add(index);
                octetCounts.Add(count);
                qLengths.Add(GetQEncodedLength(octets, count, mode));
                index += charCount;
            }

            starts.Add(text.Length);

            // ref. RFC 2047 §5: each word encodes whole octets, and whole characters, so "B" text is in groups of 4 characters, and filling
            // each word isn't always shortest. Working back from the end of the text, find for each character the shortest length of the
            // words from it to the end, and the end of the longest first word that gives it.
            var characterCount = octetCounts.Count;
            var shortestLengths = new int[characterCount + 1];
            var wordEnds = new int[characterCount];
            for (var first = characterCount - 1; first >= 0; first--)
            {
                shortestLengths[first] = int.MaxValue;
                var encodedLength = 0;
                var octetCount = 0;
                for (var end = first + 1; end <= characterCount; end++)
                {
                    octetCount += octetCounts[end - 1];
                    encodedLength = encoding == MimeEncodedWordEncoding.B ? (octetCount + 2) / 3 * 4 : encodedLength + qLengths[end - 1];
                    if (encodedLength > maxEncodedLength) break;

                    var length = wordOverhead + encodedLength + shortestLengths[end];
                    if (length > shortestLengths[first]) continue;

                    shortestLengths[first] = length;
                    wordEnds[first] = end;
                }

                if (shortestLengths[first] == int.MaxValue) return null;
            }

            var words = new List<MimeEncodedWord>();
            for (var first = 0; first < characterCount; first = wordEnds[first])
            {
                words.Add(Create(text, starts[first], starts[wordEnds[first]] - starts[first], encoding, context, language));
            }

            return words;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static MimeEncodedWord Create(string text, int start, int length, MimeEncodedWordEncoding encoding, MimeEncodedWordContext context, string? language)
#else
        private static MimeEncodedWord Create(string text, int start, int length, MimeEncodedWordEncoding encoding, MimeEncodedWordContext context, string language)
#endif
        {
            var value = text.Substring(start, length);
            var octets = Utf8.GetBytes(value);
            var encodedText = encoding == MimeEncodedWordEncoding.B
                ? MimeBase64.Encode(octets, MimeBase64Mode.Strict)
                : MimeQuotedPrintable.Encode(octets, GetMode(context), MimeQuotedPrintableEncoderOptions.None);

            // ref. RFC 2047 §2: "=?" charset "?" encoding "?" encoded-text "?="; RFC 2231 §5: charset ["*" language]
            var builder = new StringBuilder(DelimiterLength + GetCharsetLength(language) + encodedText.Length);
            builder.Append("=?").Append(MimeCharset.Utf8.Name);
            if (language != null) builder.Append('*').Append(language);
            builder.Append('?').Append(encoding == MimeEncodedWordEncoding.B ? 'B' : 'Q').Append('?').Append(encodedText).Append("?=");

            return new MimeEncodedWord(builder.ToString(), MimeCharset.Utf8, language, encoding, value);
        }

        private static string ReplaceUnpairedSurrogates(string text)
        {
#if NETCOREAPP3_0_OR_GREATER
            StringBuilder? builder = null;
#else
            StringBuilder builder = null;
#endif
            for (var index = 0; index < text.Length; index++)
            {
                var c = text[index];
                if (char.IsHighSurrogate(c) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                {
                    index++;
                    continue;
                }

                if (!char.IsSurrogate(c)) continue;

                if (builder is null) builder = new StringBuilder(text);
                builder[index] = MimeDecoderFallbacks.ReplacementString[0];
            }

            return builder is null ? text : builder.ToString();
        }

        private static int GetLength(List<MimeEncodedWord> words)
        {
            var length = 0;
            foreach (var word in words)
            {
                length += word._word.Length;
            }

            return length;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static int GetCharsetLength(string? language)
#else
        private static int GetCharsetLength(string language)
#endif
        {
            return MimeCharset.Utf8.Name.Length + (language is null ? 0 : language.Length + 1);
        }

        private static int GetQEncodedLength(byte[] octets, int count, MimeQuotedPrintableMode mode)
        {
            // ref. RFC 2047 §4.2: an octet is written as itself, a space as "_", and any other octet as "=" and two hexadecimal digits.
            var length = 0;
            for (var index = 0; index < count; index++)
            {
                var octet = octets[index];
                length += octet == 0x20 || MimeQuotedPrintable.IsLiteral(octet, mode) ? 1 : 3;
            }

            return length;
        }

        // Writes one character, or one surrogate pair, of well-formed text as UTF-8, and returns the number of octets.
        private static int GetUtf8Octets(string text, int index, byte[] octets, out int charCount)
        {
            int codePoint = text[index];
            charCount = 1;
            if (char.IsHighSurrogate(text[index]))
            {
                codePoint = char.ConvertToUtf32(text[index], text[index + 1]);
                charCount = 2;
            }

            // ref. RFC 3629 §3: UTF-8 definition.
            if (codePoint < 0x80)
            {
                octets[0] = (byte)codePoint;
                return 1;
            }

            if (codePoint < 0x800)
            {
                octets[0] = (byte)(0xC0 | (codePoint >> 6));
                octets[1] = (byte)(0x80 | (codePoint & 0x3F));
                return 2;
            }

            if (codePoint < 0x10000)
            {
                octets[0] = (byte)(0xE0 | (codePoint >> 12));
                octets[1] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
                octets[2] = (byte)(0x80 | (codePoint & 0x3F));
                return 3;
            }

            octets[0] = (byte)(0xF0 | (codePoint >> 18));
            octets[1] = (byte)(0x80 | ((codePoint >> 12) & 0x3F));
            octets[2] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
            octets[3] = (byte)(0x80 | (codePoint & 0x3F));
            return 4;
        }

        private static bool TryParseCore(ReadOnlySpan<char> value, MimeEncodedWordContext context, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver, out MimeEncodedWord result)
        {
            RawWord word;
            string text;
            if (!TryRead(value, context, options, out word) || !TryDecodeOctets(word.Charset, word.Octets, options, charsetResolver, out text))
            {
                result = default(MimeEncodedWord);
                return false;
            }

            // ref. RFC 2047 §2: "An 'encoded-word' may not be more than 75 characters long". A word that is longer is written back as
            // words that aren't. The text of a word whose character set isn't resolved holds its octets, not its characters, so it can't be
            // written in another character set.
#if NETCOREAPP3_0_OR_GREATER
            string? written;
#else
            string written;
#endif
            if (value.Length <= MaxLength)
            {
                written = value.ToString();
            }
            else
            {
                written = charsetResolver.TryGetEncoding(word.Charset, out _) ? TryRewriteLongWord(text, word.Language) : null;
            }

            if (written is null)
            {
                result = default(MimeEncodedWord);
                return false;
            }

            result = new MimeEncodedWord(written, word.Charset, word.Language, word.Encoding, text);
            return true;
        }

        // Reads the parts of an encoded word, and decodes its encoded text to octets.
        private static bool TryRead(ReadOnlySpan<char> value, MimeEncodedWordContext context, MimeEncodedWordParseOptions options, out RawWord word)
        {
            word = default(RawWord);

            // ref. RFC 2047 §2: encoded-word = "=?" charset "?" encoding "?" encoded-text "?=", of at most 75 characters.
            if (value.Length < MinLength) return false;
            if (value.Length > MaxLength && (options & MimeEncodedWordParseOptions.AllowLongWords) == 0) return false;
            if (value[0] != '=' || value[1] != '?' || value[value.Length - 2] != '?' || value[value.Length - 1] != '=') return false;

            var content = value.Slice(2, value.Length - 4);
            var charsetEnd = content.IndexOf('?');
            if (charsetEnd < 0) return false;

            var charsetPart = content.Slice(0, charsetEnd);
            var rest = content.Slice(charsetEnd + 1);
            var encodingEnd = rest.IndexOf('?');
            if (encodingEnd < 0) return false;

            var encodingPart = rest.Slice(0, encodingEnd);
            var encodedText = rest.Slice(encodingEnd + 1);

            // ref. RFC 2047 §2: encoded-text = 1*<Any printable ASCII character other than "?" or SPACE>
            if (encodedText.IsEmpty) return false;
            foreach (var c in encodedText)
            {
                if (c <= ' ' || c >= '\x7F' || c == '?') return false;
            }

            // ref. RFC 2231 §5: the character set may be followed by "*" and a language. RFC 2047 §3: the character set is one that a MIME
            // charset parameter allows, so it is a mime-charset (RFC 2978 §2.3), which can't hold a "*".
            var languageStart = charsetPart.IndexOf('*');
            var charsetName = languageStart < 0 ? charsetPart : charsetPart.Slice(0, languageStart);
            if (!MimeCharset.TryParse(charsetName, out word.Charset)) return false;

            if (languageStart >= 0)
            {
                var language = charsetPart.Slice(languageStart + 1);
                if (!MimeExtendedParameter.IsLanguage(language)) return false;

                word.Language = language.ToString();
            }

            // ref. RFC 2047 §4: the encodings are "B" and "Q", and RFC 2047 §2: their names are case-independent.
            if (encodingPart.Length != 1) return false;

#if NETCOREAPP3_0_OR_GREATER
            byte[]? octets;
#else
            byte[] octets;
#endif
            switch (encodingPart[0])
            {
                case 'B':
                case 'b':
                    // ref. RFC 2047 §4.1, §5: base64 in whole groups of 4 characters.
                    word.Encoding = MimeEncodedWordEncoding.B;
                    if (!MimeBase64.TryDecode(encodedText, MimeBase64Mode.Strict, MimeBase64DecoderOptions.None, out octets)) return false;
                    break;
                case 'Q':
                case 'q':
                    // ref. RFC 2047 §4.2, §5: each "=" is followed by two hexadecimal digits in the same word.
                    word.Encoding = MimeEncodedWordEncoding.Q;
                    // The "Q" modes accept lower-case hexadecimal digits, which RFC 2047 §4.2 (1) allows.
                    if (!MimeQuotedPrintable.TryDecode(encodedText, GetMode(context), MimeQuotedPrintableDecoderOptions.None, out octets))
                    {
                        return false;
                    }

                    break;
                default:
                    return false;
            }

            word.Octets = octets;
            return true;
        }

        private static bool TryDecodeOctets(MimeCharset charset, byte[] octets, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver, out string text)
        {
            return TryDecodeOctets(charset, octets, options, charsetResolver, true, out text);
        }

        // Decodes octets in a character set. Unless requireAsciiMode is false, which only the first part of a character split between words
        // asks for, the octets must also leave the character set in ASCII mode.
        private static bool TryDecodeOctets(MimeCharset charset, byte[] octets, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver, bool requireAsciiMode, out string text)
        {
#if NETCOREAPP3_0_OR_GREATER
            Encoding? encoding;
#else
            Encoding encoding;
#endif
            if (charsetResolver.TryGetEncoding(charset, out encoding))
            {
                // ref. RFC 2047 §6.3 (2): a word whose octets are not whole characters in its character set is incorrectly formed.
                var allowInvalidOctets = (options & MimeEncodedWordParseOptions.AllowInvalidOctets) != 0;
                if (!MimeExtendedParameter.TryGetString(encoding, octets, octets.Length, allowInvalidOctets, out text)) return false;

                // ref. RFC 2047 §3: a word that switches out of ASCII mode must switch back to it by its end, whatever its octets.
                return !requireAsciiMode || EndsInAsciiMode(charset, encoding, octets, allowInvalidOctets ? null : text);
            }

            if ((options & MimeEncodedWordParseOptions.AllowUnknownCharsets) == 0)
            {
                text = string.Empty;
                return false;
            }

            text = MimeExtendedParameter.DecodeLatin1(octets, octets.Length);
            return true;
        }

        // Indicates whether a word's octets leave its character set in ASCII mode (ref. RFC 2047 §3).
        //
        // A character set of the ISO/IEC 2022 family, such as ISO-2022-JP (RFC 1468), is in ASCII mode when ASCII is designated to G0 and
        // G0 is shifted in, which its octets show whether or not they are valid. Any other character set that switches modes, such as
        // HZ-GB-2312, is in the mode it starts in if an ASCII octet after the word's octets is read as the ASCII character, as it is at the
        // start; this can only be asked of octets that are valid. A character set that doesn't read the octet alone as that character, such
        // as UTF-16, has no ASCII mode, and the Unicode encoding forms have no modes at all.
#if NETCOREAPP3_0_OR_GREATER
        private static bool EndsInAsciiMode(MimeCharset charset, Encoding encoding, byte[] octets, string? validText)
#else
        private static bool EndsInAsciiMode(MimeCharset charset, Encoding encoding, byte[] octets, string validText)
#endif
        {
            if (IsIso2022(charset)) return EndsWithIso2022Ascii(octets);
            if (encoding is UTF8Encoding || encoding is UnicodeEncoding || encoding is MimeUnicodeSchemeEncoding) return true;
            if (validText is null && !MimeExtendedParameter.TryGetString(encoding, octets, octets.Length, false, out validText)) return true;

            string probeText;
            if (!MimeExtendedParameter.TryGetString(encoding, new[] { ProbeOctet }, 1, false, out probeText) ||
                probeText != ProbeCharacter)
            {
                return true;
            }

            var probed = new byte[octets.Length + 1];
            Array.Copy(octets, probed, octets.Length);
            probed[octets.Length] = ProbeOctet;
            return MimeExtendedParameter.TryGetString(encoding, probed, probed.Length, false, out probeText) &&
                string.Equals(probeText, validText + ProbeCharacter, StringComparison.Ordinal);
        }

        // ref. IANA Character Sets registry: the ISO-2022-* names, such as ISO-2022-JP and ISO-2022-KR, and their csISO2022* aliases.
        // The encoding's WebName isn't asked, as the base class throws for an encoding with no code page, as a caller's may be.
        private static bool IsIso2022(MimeCharset charset)
        {
            return charset.Name.StartsWith("ISO-2022-", StringComparison.OrdinalIgnoreCase) ||
                charset.Name.StartsWith("csISO2022", StringComparison.OrdinalIgnoreCase);
        }

        // Indicates whether ISO/IEC 2022 octets end with ASCII designated to G0 and G0 shifted in, as they start.
        private static bool EndsWithIso2022Ascii(byte[] octets)
        {
            var mode = Iso2022Mode.Initial;
            return mode.TryRead(octets, 0, octets.Length) && mode.IsAscii;
        }

        private static string DecodeTextCore(ReadOnlySpan<char> text, MimeEncodedWordContext context, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver)
        {
            var source = text.ToString();
            if (source.IndexOf("=?", StringComparison.Ordinal) < 0) return source;

            var segments = Split(text, context, options);
            DecodeWords(segments, options, charsetResolver);

            var builder = new StringBuilder(source.Length);
            for (var index = 0; index < segments.Count; index++)
            {
                var segment = segments[index];
                if (segment.Kind == SegmentKind.Word)
                {
                    builder.Append(segment.Text);
                    continue;
                }

                // ref. RFC 2047 §6.2: whitespace that separates two encoded words is ignored.
                if (segment.Kind == SegmentKind.Whitespace && index > 0 && index + 1 < segments.Count &&
                    segments[index - 1].Kind == SegmentKind.Word && segments[index + 1].Kind == SegmentKind.Word)
                {
                    continue;
                }

                builder.Append(source, segment.Start, segment.Length);
            }

            return builder.ToString();
        }

        // Splits text into runs of whitespace, ordinary text and encoded words, whose octets are decoded but whose text is not.
        private static List<Segment> Split(ReadOnlySpan<char> text, MimeEncodedWordContext context, MimeEncodedWordParseOptions options)
        {
            var segments = new List<Segment>();
            for (var index = 0; index < text.Length;)
            {
                var start = index;
                var isWhitespace = GetLinearWhitespaceLength(text, index) > 0;
                while (index < text.Length)
                {
                    var whitespaceLength = GetLinearWhitespaceLength(text, index);
                    if ((whitespaceLength > 0) != isWhitespace) break;

                    index += isWhitespace ? whitespaceLength : 1;
                }

                if (isWhitespace)
                {
                    segments.Add(new Segment(SegmentKind.Whitespace, start, index - start));
                }
                else
                {
                    AddWords(text, start, index - start, context, options, segments);
                }
            }

            return segments;
        }

        // Adds a run of characters with no whitespace, as an encoded word if it is one, or, as an opt-in, as the encoded words within it.
        private static void AddWords(ReadOnlySpan<char> text, int start, int length, MimeEncodedWordContext context,
            MimeEncodedWordParseOptions options, List<Segment> segments)
        {
            // ref. RFC 2047 §6.1: each sequence of printable characters between whitespace is examined to see if it is an encoded word.
            var run = text.Slice(start, length);
            RawWord word;
            if (TryRead(run, context, options, out word))
            {
                segments.Add(new Segment(start, length, word));
                return;
            }

            if ((options & MimeEncodedWordParseOptions.AllowEmbeddedWords) == 0)
            {
                segments.Add(new Segment(SegmentKind.Literal, start, length));
                return;
            }

            var literalStart = 0;
            var position = 0;
            while (true)
            {
                var wordStart = IndexOf(run, position, '=', '?');
                if (wordStart < 0) break;

                var wordEnd = FindWordEnd(run, wordStart);
                if (wordEnd < 0 || !TryRead(run.Slice(wordStart, wordEnd - wordStart), context, options, out word))
                {
                    position = wordStart + 1;
                    continue;
                }

                if (wordStart > literalStart) segments.Add(new Segment(SegmentKind.Literal, start + literalStart, wordStart - literalStart));
                segments.Add(new Segment(start + wordStart, wordEnd - wordStart, word));
                literalStart = position = wordEnd;
            }

            if (literalStart < run.Length) segments.Add(new Segment(SegmentKind.Literal, start + literalStart, run.Length - literalStart));
        }

        // Finds the end of the "?=" that would close an encoded word starting at a "=?": the one after the third "?" that follows it.
        private static int FindWordEnd(ReadOnlySpan<char> run, int wordStart)
        {
            var position = wordStart + 2;
            for (var count = 0; count < 3; count++)
            {
                var question = IndexOf(run, position, '?');
                if (question < 0) return -1;

                position = question + 1;
            }

            return position < run.Length && run[position] == '=' ? position + 1 : -1;
        }

        private static int IndexOf(ReadOnlySpan<char> run, int start, char value)
        {
            var index = run.Slice(start).IndexOf(value);
            return index < 0 ? -1 : start + index;
        }

        private static int IndexOf(ReadOnlySpan<char> run, int start, char first, char second)
        {
            for (var index = start; index + 1 < run.Length; index++)
            {
                if (run[index] == first && run[index + 1] == second) return index;
            }

            return -1;
        }

        // Decodes the text of each encoded word. A word whose text can't be decoded becomes ordinary text.
        private static void DecodeWords(List<Segment> segments, MimeEncodedWordParseOptions options, IMimeCharsetResolver charsetResolver)
        {
            if ((options & MimeEncodedWordParseOptions.AllowSplitCharacters) != 0)
            {
                DecodeSplitCharacters(segments, options, charsetResolver);
                return;
            }

            foreach (var segment in segments)
            {
                if (segment.Kind == SegmentKind.Word) DecodeWord(segment, options, charsetResolver);
            }
        }

        // Decodes the text of each encoded word. A word that ends partway through a character starts a chain of words, separated only by
        // whitespace and with the same character set and language, each of which carries the octets of the unfinished character into the
        // next. Each word is read in the state that the words before it in the chain leave the decoder in, such as the byte order given by a
        // byte order mark or the mode of ISO-2022-JP, so that its octets are split into characters the same way as when the chain is read
        // whole. Once a word finishes the last character, the octets of the whole chain are decoded together.
        private static void DecodeSplitCharacters(List<Segment> segments, MimeEncodedWordParseOptions options, IMimeCharsetResolver charsetResolver)
        {
            var strictOptions = options & ~MimeEncodedWordParseOptions.AllowInvalidOctets;
            var chainOctets = new List<byte>();
            var carried = new List<byte>(MaxCarriedOctets);
            var state = new SplitCharacterState();

            // The first word of the chain, or -1 if there is none, and the last word in it.
            var chainStart = -1;
            var previous = -1;
            var index = 0;
            string text;
            while (index < segments.Count || chainStart >= 0)
            {
#if NETCOREAPP3_0_OR_GREATER
                Segment? segment = index < segments.Count ? segments[index] : null;
#else
                Segment segment = index < segments.Count ? segments[index] : null;
#endif
                if (segment != null && segment.Kind == SegmentKind.Whitespace)
                {
                    index++;
                    continue;
                }

                if (chainStart < 0)
                {
                    if (segment != null && segment.Kind == SegmentKind.Word)
                    {
                        state.Reset();
                        if (!TryCarry(segment, carried, state, strictOptions, charsetResolver, out text))
                        {
                            DecodeWord(segment, options, charsetResolver);
                        }
                        else if (carried.Count == 0)
                        {
                            segment.Text = text;
                        }
                        else
                        {
                            chainStart = previous = index;
                            chainOctets.AddRange(segment.Word.Octets);
                        }
                    }

                    index++;
                    continue;
                }

                if (segment != null && segment.Kind == SegmentKind.Word && segment.Word.HasSameCharsetAndLanguage(segments[previous].Word) &&
                    TryCarry(segment, carried, state, strictOptions, charsetResolver, out text))
                {
                    chainOctets.AddRange(segment.Word.Octets);
                    previous = index;
                    if (carried.Count > 0)
                    {
                        index++;
                        continue;
                    }

                    if (TryDecodeOctets(segment.Word.Charset, chainOctets.ToArray(), strictOptions, charsetResolver, out text))
                    {
                        segments[chainStart].Text = text;
                        for (var word = chainStart + 1; word <= index; word++)
                        {
                            segments[word].Text = string.Empty;
                        }

                        chainStart = -1;
                        chainOctets.Clear();
                        index++;
                        continue;
                    }
                }

                // The chain doesn't finish its last character, or its octets aren't valid together: the word that starts it is decoded by
                // itself, and decoding starts again after it.
                DecodeWord(segments[chainStart], options, charsetResolver);
                index = chainStart + 1;
                chainStart = -1;
                chainOctets.Clear();
                carried.Clear();
            }
        }

        // Decodes the carried octets and those of a word, but for up to MaxCarriedOctets at the end, which become the carried octets. They
        // are decoded after the octets of the state that the chain so far leaves the decoder in, which read as no text, and the state is
        // moved on past the octets that are decoded.
        private static bool TryCarry(Segment segment, List<byte> carried, SplitCharacterState state, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver, out string text)
        {
            var charset = segment.Word.Charset;
            var stateOctets = state.Octets;
            var wordOctets = segment.Word.Octets;
            var octets = new byte[stateOctets.Length + carried.Count + wordOctets.Length];
            Array.Copy(stateOctets, octets, stateOctets.Length);
            carried.CopyTo(octets, stateOctets.Length);
            Array.Copy(wordOctets, 0, octets, stateOctets.Length + carried.Count, wordOctets.Length);

            // ref. RFC 2047 §3: octets that end partway through a character or an escape sequence can't have switched back to ASCII mode. In
            // ISO/IEC 2022, the mode they leave carries into the next word with the rest of the character; the chain must still end in
            // ASCII mode. A word of whole characters must end in ASCII mode, as it must without AllowSplitCharacters.
            var allowOtherMode = false;
            for (var carryLength = 0; carryLength <= MaxCarriedOctets && carryLength <= carried.Count + wordOctets.Length; carryLength++)
            {
                var decoded = new byte[octets.Length - carryLength];
                Array.Copy(octets, decoded, decoded.Length);
                if (!TryDecodeOctets(charset, decoded, options, charsetResolver, carryLength == 0 || !allowOtherMode, out text))
                {
                    if (carryLength == 0)
                    {
                        allowOtherMode = IsIso2022(charset) && EndsPartwayThrough(charset, octets, stateOctets.Length, options, charsetResolver);
                    }

                    continue;
                }

                // A chain that goes on needs the state its octets leave the decoder in, which octets that end partway through an escape
                // sequence leave unknown.
                if (carryLength > 0 && !state.TryRead(charset, decoded, stateOctets.Length, decoded.Length - stateOctets.Length, charsetResolver))
                {
                    continue;
                }

                carried.Clear();
                for (var position = decoded.Length; position < octets.Length; position++)
                {
                    carried.Add(octets[position]);
                }

                return true;
            }

            text = string.Empty;
            return false;
        }

        // Indicates whether ISO/IEC 2022 octets, after the octets of the state they are read in, end partway through a character, so that
        // they aren't valid even outside ASCII mode, or partway through an escape sequence.
        private static bool EndsPartwayThrough(MimeCharset charset, byte[] octets, int stateLength, MimeEncodedWordParseOptions options,
            IMimeCharsetResolver charsetResolver)
        {
            string text;
            if (!TryDecodeOctets(charset, octets, options, charsetResolver, false, out text)) return true;

            var mode = Iso2022Mode.Initial;
            return !mode.TryRead(octets, stateLength, octets.Length - stateLength);
        }

        private static void DecodeWord(Segment segment, MimeEncodedWordParseOptions options, IMimeCharsetResolver charsetResolver)
        {
            string text;
            if (TryDecodeOctets(segment.Word.Charset, segment.Word.Octets, options, charsetResolver, out text))
            {
                segment.Text = text;
            }
            else
            {
                segment.Kind = SegmentKind.Literal;
            }
        }

        // Gets the length of one ([CRLF] LWSP-char) at a position, or 0 if there is none (ref. RFC 822 §3.3: linear-white-space =
        // 1*([CRLF] LWSP-char), where LWSP-char = SPACE / HTAB). A bare CR or LF, or a CRLF that isn't a fold, is not whitespace.
        private static int GetLinearWhitespaceLength(ReadOnlySpan<char> text, int index)
        {
            if (MimeGrammar.IsWhitespace(text[index])) return 1;

            return text[index] == '\r' && index + 2 < text.Length && text[index + 1] == '\n' && MimeGrammar.IsWhitespace(text[index + 2]) ? 3 : 0;
        }

        private static MimeQuotedPrintableMode GetMode(MimeEncodedWordContext context)
        {
            switch (context)
            {
                case MimeEncodedWordContext.Comment:
                    return MimeQuotedPrintableMode.EncodedWordComment;
                case MimeEncodedWordContext.Phrase:
                    return MimeQuotedPrintableMode.EncodedWordPhrase;
                default:
                    return MimeQuotedPrintableMode.EncodedWordText;
            }
        }

        private static void CheckText(string text)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));

            for (var index = 0; index < text.Length; index++)
            {
                var c = text[index];
                if (char.IsHighSurrogate(c) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                {
                    index++;
                }
                else if (char.IsSurrogate(c))
                {
                    throw new ArgumentException("The text contains an unpaired surrogate.", nameof(text));
                }
            }
        }

#if NETCOREAPP3_0_OR_GREATER
        private static void CheckLanguage(string? language)
#else
        private static void CheckLanguage(string language)
#endif
        {
            if (language != null && !MimeExtendedParameter.IsLanguage(language.AsSpan()))
            {
                throw new ArgumentException("The language is not an RFC 1766 language tag.", nameof(language));
            }
        }

        private static void CheckContext(MimeEncodedWordContext context)
        {
            if (context < MimeEncodedWordContext.Text || context > MimeEncodedWordContext.Phrase) throw new ArgumentOutOfRangeException(nameof(context));
        }

        private static void CheckOptions(MimeEncodedWordParseOptions options)
        {
            if ((options & ~AllOptions) != 0) throw new ArgumentOutOfRangeException(nameof(options));
        }

        /// <summary>
        /// The parts of an encoded word, with its encoded text decoded to octets.
        /// </summary>
        private struct RawWord
        {
            internal MimeCharset Charset;
#if NETCOREAPP3_0_OR_GREATER
            internal string? Language;
#else
            internal string Language;
#endif
            internal MimeEncodedWordEncoding Encoding;
            internal byte[] Octets;

            internal bool HasSameCharsetAndLanguage(RawWord other)
            {
                return Charset.Equals(other.Charset) && string.Equals(Language, other.Language, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// The state that the octets read so far of a chain of words that a character is split between leave a decoder in, as octets that
        /// put a decoder in the same state from the start and read as no text.
        /// </summary>
        /// <remarks>
        /// UTF-16 and UTF-32 read with a byte order mark keep the byte order the chain starts with (RFC 2781 §3.2: a byte order mark is
        /// only read at the start of the text), so their state is the mark of that byte order. A character set of the ISO/IEC 2022 family,
        /// such as ISO-2022-JP (RFC 1468), keeps the character sets designated and invoked, so its state is the escape sequences and the
        /// shift that set them. Any other character set reads a character the same way wherever it starts, so it needs no octets.
        /// </remarks>
        private sealed class SplitCharacterState
        {
            private static readonly byte[] NoOctets = new byte[0];

            private bool _hasOctets;
            private Iso2022Mode _mode = Iso2022Mode.Initial;

            /// <summary>Gets the octets that put a decoder in the state.</summary>
            internal byte[] Octets { get; private set; } = NoOctets;

            /// <summary>Returns to the state at the start of a chain.</summary>
            internal void Reset()
            {
                _hasOctets = false;
                _mode = Iso2022Mode.Initial;
                Octets = NoOctets;
            }

            /// <summary>
            /// Moves the state on past octets of the chain that are valid in the state, or leaves it as it is if they leave it unknown.
            /// </summary>
            /// <param name="charset">The character set of the chain.</param>
            /// <param name="octets">The octets.</param>
            /// <param name="start">The position of the first octet to read.</param>
            /// <param name="count">The number of octets to read.</param>
            /// <param name="charsetResolver">Finds the encoding of the character set.</param>
            /// <returns><c>true</c> if the state is known after the octets; otherwise, <c>false</c>.</returns>
            internal bool TryRead(MimeCharset charset, byte[] octets, int start, int count, IMimeCharsetResolver charsetResolver)
            {
                if (IsIso2022(charset))
                {
                    var mode = _mode;
                    if (!mode.TryRead(octets, start, count)) return false;

                    _mode = mode;
                    Octets = mode.GetOctets();
                    return true;
                }

                if (_hasOctets || count == 0) return true;

                // ref. RFC 2781 §4.3: the first octets of the chain give the byte order, by their byte order mark or by having none.
                _hasOctets = true;
#if NETCOREAPP3_0_OR_GREATER
                Encoding? encoding;
#else
                Encoding encoding;
#endif
                var scheme = charsetResolver.TryGetEncoding(charset, out encoding) ? encoding as MimeUnicodeSchemeEncoding : null;
                if (scheme != null) Octets = scheme.GetByteOrderMark(octets, start, count);

                return true;
            }
        }

        /// <summary>
        /// The mode of ISO/IEC 2022 octets: the character sets designated to G0 and G1, and which of them is invoked.
        /// </summary>
        /// <remarks>
        /// ref. ECMA-35, which is ISO/IEC 2022: an escape sequence is ESC, any intermediate bytes 0x20 to 0x2F, and a final byte 0x30 to
        /// 0x7E. ESC ( F designates a 94-character set to G0, and ESC $ F and ESC $ ( F a multiple-byte one; ESC ) F and ESC $ ) F designate
        /// them to G1. SO invokes G1 and SI invokes G0. RFC 1468: in ISO-2022-JP, ESC ( B designates ASCII, and ESC ( J, ESC $ @ and ESC $ B
        /// the other sets. The octets start with ASCII designated to G0, nothing designated to G1, and G0 invoked. The G2 and G3 designations
        /// and single shifts of ISO-2022-JP-2 (RFC 1554) and ISO-2022-CN (RFC 1922) aren't kept, so a character split between words after
        /// one of them isn't joined; the words are then decoded by themselves.
        /// </remarks>
        private struct Iso2022Mode
        {
            private static readonly byte[] NoOctets = new byte[0];

            // The escape sequences that designate G0, or none for ASCII, and G1, or none.
            private byte[] _g0Designation;
            private byte[] _g1Designation;
            private bool _isShiftedOut;

            internal static Iso2022Mode Initial => new Iso2022Mode { _g0Designation = NoOctets, _g1Designation = NoOctets };

            /// <summary>Gets a value indicating whether ASCII is designated to G0 and G0 is invoked.</summary>
            internal bool IsAscii => _g0Designation.Length == 0 && !_isShiftedOut;

            /// <summary>
            /// Moves the mode on past octets, or leaves it as it is if they end partway through an escape sequence, which leaves the mode
            /// unknown.
            /// </summary>
            internal bool TryRead(byte[] octets, int start, int count)
            {
                var g0Designation = _g0Designation;
                var g1Designation = _g1Designation;
                var isShiftedOut = _isShiftedOut;
                var end = start + count;
                for (var index = start; index < end; index++)
                {
                    switch (octets[index])
                    {
                        case ShiftOut:
                            isShiftedOut = true;
                            break;
                        case ShiftIn:
                            isShiftedOut = false;
                            break;
                        case Escape:
                            var final = index + 1;
                            while (final < end && octets[final] >= 0x20 && octets[final] <= 0x2F) final++;

                            if (final >= end) return false;

                            var intermediateCount = final - index - 1;
                            var first = intermediateCount > 0 ? octets[index + 1] : (byte)0;
                            var second = intermediateCount > 1 ? octets[index + 2] : (byte)0;
                            if (intermediateCount == 1 && first == (byte)'(')
                            {
                                g0Designation = octets[final] == (byte)'B' ? NoOctets : Copy(octets, index, final + 1 - index);
                            }
                            else if ((intermediateCount == 1 && first == (byte)'$') || (intermediateCount == 2 && first == (byte)'$' && second == (byte)'('))
                            {
                                g0Designation = Copy(octets, index, final + 1 - index);
                            }
                            else if ((intermediateCount == 1 && first == (byte)')') || (intermediateCount == 2 && first == (byte)'$' && second == (byte)')'))
                            {
                                g1Designation = Copy(octets, index, final + 1 - index);
                            }

                            index = final;
                            break;
                    }
                }

                _g0Designation = g0Designation;
                _g1Designation = g1Designation;
                _isShiftedOut = isShiftedOut;
                return true;
            }

            /// <summary>Gets octets that put ISO/IEC 2022 octets in this mode from the start, and read as no text.</summary>
            internal byte[] GetOctets()
            {
                var octets = new byte[_g0Designation.Length + _g1Designation.Length + (_isShiftedOut ? 1 : 0)];
                Array.Copy(_g0Designation, octets, _g0Designation.Length);
                Array.Copy(_g1Designation, 0, octets, _g0Designation.Length, _g1Designation.Length);
                if (_isShiftedOut) octets[octets.Length - 1] = ShiftOut;

                return octets;
            }

            private static byte[] Copy(byte[] octets, int start, int count)
            {
                var copy = new byte[count];
                Array.Copy(octets, start, copy, 0, count);
                return copy;
            }
        }

        private enum SegmentKind
        {
            Whitespace,
            Literal,
            Word,
        }

        /// <summary>
        /// A run of text to decode: whitespace, ordinary text, or an encoded word.
        /// </summary>
        private sealed class Segment
        {
            internal Segment(SegmentKind kind, int start, int length)
            {
                Kind = kind;
                Start = start;
                Length = length;
                Text = string.Empty;
            }

            internal Segment(int start, int length, RawWord word)
                : this(SegmentKind.Word, start, length)
            {
                Word = word;
            }

            internal SegmentKind Kind { get; set; }

            internal int Start { get; }

            internal int Length { get; }

            internal RawWord Word { get; }

            /// <summary>The decoded text of a word.</summary>
            internal string Text { get; set; }
        }
    }
}
