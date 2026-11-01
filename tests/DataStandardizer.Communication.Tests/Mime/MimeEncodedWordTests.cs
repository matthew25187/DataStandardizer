using System;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeEncodedWordTests
    {
        // ref. RFC 2047 §8: an encoded word in a From display name.
        private const string KeithMooreWord = "=?US-ASCII?Q?Keith_Moore?=";
        private const string KeithMoore = "Keith Moore";
        private const string UsAscii = "US-ASCII";

        // ref. RFC 2047 §8: an encoded word followed by an ordinary word in a CC display name.
        private const string AndreWord = "=?ISO-8859-1?Q?Andr=E9?=";
        private const string AndrePirardPhrase = "=?ISO-8859-1?Q?Andr=E9?= Pirard";
        private const string Andre = "Andr\x00E9";
        private const string AndrePirard = "Andr\x00E9 Pirard";
        private const string Iso88591 = "ISO-8859-1";

        // ref. RFC 2047 §8: a Subject of two "B" encoded words in different character sets, folded between them.
        private const string SubjectExample = "=?ISO-8859-1?B?SWYgeW91IGNhbiByZWFkIHRoaXMgeW8=?=\r\n =?ISO-8859-2?B?dSB1bmRlcnN0YW5kIHRoZSBleGFtcGxlLg==?=";
        private const string SubjectExampleFirstWord = "=?ISO-8859-1?B?SWYgeW91IGNhbiByZWFkIHRoaXMgeW8=?=";
        private const string SubjectExampleFirstText = "If you can read this yo";
        private const string SubjectExampleText = "If you can read this you understand the example.";

        // ref. RFC 2047 §8: a "B" encoded word in a comment, in ISO-8859-8, and its octets read as ISO-8859-1. The Hebrew letters of
        // ISO-8859-8 are not resolved on every target, so the test resolver reads every character set as ISO-8859-1.
        private const string HebrewCommentWord = "=?iso-8859-8?b?7eXs+SDv4SDp7Oj08A==?=";
        private const string HebrewCommentOctets = "\x00ED\x00E5\x00EC\x00F9 \x00EF\x00E1 \x00E9\x00EC\x00E8\x00F4\x00F0";

        // ref. RFC 2047 §8: the text of the "(=?ISO-8859-1?Q?a?=)" examples, which a *text field doesn't decode.
        private const string ParenthesisedWord = "(=?ISO-8859-1?Q?a?=)";

        // ref. RFC 2231 §5: an encoded word with a language.
        private const string Rfc2231Section5Word = "=?US-ASCII*EN?Q?Keith_Moore?=";
        private const string Rfc2231Section5Language = "EN";

        // ref. RFC 2047 §2: an encoded word is at most 75 characters long. These have 63 and 64 characters of encoded text.
        private const string MaxLengthWord = "=?UTF-8?Q?aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa?=";
        private const string LongWord = "=?UTF-8?Q?aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa?=";
        private const string MaxLengthText = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string LongText = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        // Text that is 70 characters of encoded text in "Q", so it is split into a word of 75 characters and one of the rest.
        private const string SeventyLetters = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string SevenLettersWord = "=?UTF-8?Q?aaaaaaa?=";

        // Thirty e-acutes, of 2 octets each in UTF-8. A "B" word holds 63 characters of encoded text, which is 47 octets, so it holds at
        // most 22 whole e-acutes, not 23. 22 and 8 e-acutes (44 and 16 octets) take 60 and 24 characters, but 21 and 9 (42 and 18 octets)
        // take only 56 and 24.
        private const string ThirtyEAcutes = "\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9";
        private const string TwentyOneEAcutes = "\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9";
        private const string NineEAcutes = "\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9\x00E9";
        private const string TwentyOneEAcutesWord = "=?UTF-8?B?w6nDqcOpw6nDqcOpw6nDqcOpw6nDqcOpw6nDqcOpw6nDqcOpw6nDqcOp?=";
        private const string NineEAcutesWord = "=?UTF-8?B?w6nDqcOpw6nDqcOpw6nDqcOp?=";

        // Twelve emoji, each a surrogate pair of 4 octets in UTF-8. A "B" word holds at most 11 of them, in 44 octets. 11 and 1 emoji take
        // 60 and 8 characters, but 9 and 3 (36 and 12 octets) take only 48 and 16.
        private const string TwelveEmoji = "\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00";
        private const string NineEmoji = "\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00";
        private const string Emoji = "\xD83D\xDE00";
        private const string NineEmojiWord = "=?UTF-8?B?8J+YgPCfmIDwn5iA8J+YgPCfmIDwn5iA8J+YgPCfmIDwn5iA?=";
        private const string ThreeEmoji = "\xD83D\xDE00\xD83D\xDE00\xD83D\xDE00";
        private const string ThreeEmojiWord = "=?UTF-8?B?8J+YgPCfmIDwn5iA?=";
        private const string EmojiWord = "=?UTF-8?B?8J+YgA==?=";

        // 38 letters and 5 e-acutes, 48 octets in UTF-8. "Q" writes them as 68 characters, which take two words of 74 and 18 characters.
        // "B" fills a word of 63 characters of encoded text with 45 octets, so it splits them into 44 and 4 octets (60 and 8 characters,
        // in words of 72 and 20), but a split into 42 and 6 octets needs only 56 and 8 characters, in words of 68 and 20, and is shortest.
        private const string LettersAndFiveEAcutes = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\x00E9\x00E9\x00E9\x00E9\x00E9";
        private const string LettersAndTwoEAcutes = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\x00E9\x00E9";
        private const string ThreeEAcutes = "\x00E9\x00E9\x00E9";
        private const string LettersAndTwoEAcutesWord = "=?UTF-8?B?YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWHDqcOp?=";
        private const string ThreeEAcutesWord = "=?UTF-8?B?w6nDqcOp?=";

        // A language of 62 characters, which leaves no room in a word of 75 characters for any encoded text.
        private const string TooLongLanguage = "abcdefgh-abcdefgh-abcdefgh-abcdefgh-abcdefgh-abcdefgh-abcdefgh";

        private const string Cafe = "caf\x00E9";
        private const string CafeQWord = "=?UTF-8?Q?caf=C3=A9?=";
        private const string CafeBWord = "=?UTF-8?B?Y2Fmw6k=?=";
        private const string English = "en";
        private const string Utf8 = "UTF-8";

        // ref. RFC 3629 §3: an e-acute is C3 A9 in UTF-8, so each of these words holds half of one.
        private const string SplitEAcute = "=?UTF-8?B?ww==?= =?UTF-8?B?qQ==?=";
        private const string EAcute = "\x00E9";

        // A split e-acute, and a word whose octet is not UTF-8 (RFC 3629 §3), which stays as it is, with the whitespace next to it.
        private const string SplitEAcuteThenInvalidOctet = "=?UTF-8?B?ww==?= =?UTF-8?B?qQ==?= =?UTF-8?Q?=FF?=";
        private const string EAcuteThenInvalidOctet = "\x00E9 =?UTF-8?Q?=FF?=";
        private const string InvalidOctetThenSplitEAcute = "=?UTF-8?Q?=FF?= =?UTF-8?B?ww==?= =?UTF-8?B?qQ==?=";
        private const string InvalidOctetThenEAcute = "=?UTF-8?Q?=FF?= \x00E9";

        // ref. RFC 3629 §3: each word but the first finishes an e-acute (C3 A9) and each but the last starts one, so no word holds whole
        // characters.
        private const string EAcutesSplitInEveryWord = "=?UTF-8?Q?=C3?= =?UTF-8?Q?=A9=C3?= =?UTF-8?Q?=A9=C3?= =?UTF-8?Q?=A9=C3?= =?UTF-8?Q?=A9?=";
        private const string FourEAcutes = "\x00E9\x00E9\x00E9\x00E9";

        // ref. RFC 3629 §3: letters around an e-acute (C3 A9) that is split between the words.
        private const string LettersAroundSplitEAcute = "=?UTF-8?Q?a=C3?= =?UTF-8?Q?=A9b?=";
        private const string LettersAroundEAcute = "a\x00E9b";

        // ref. RFC 3629 §3: C3 starts an e-acute that no word finishes, so the word stays as it is.
        private const string UnfinishedEAcute = "=?UTF-8?Q?a=C3?= b";

        // ref. RFC 3629 §3: U+1F600 is F0 9F 98 80 in UTF-8, so each of these words holds a quarter of it.
        private const string SplitEmoji = "=?UTF-8?Q?=F0?= =?UTF-8?Q?=9F?= =?UTF-8?Q?=98?= =?UTF-8?Q?=80?=";

        // ref. RFC 2781 §4.3: each word is FE FF, a byte order mark, then a letter in UTF-16, so each word holds whole characters.
        private const string Utf16WordsWithByteOrderMarks = "=?UTF-16?B?/v8AYQ==?= =?UTF-16?B?/v8AYg==?=";

        // ref. The Unicode Standard §23.8: U+FFFD REPLACEMENT CHARACTER, which an invalid octet sequence is read as.
        private const string ReplacementCharacter = "\xFFFD";
        private const string TwoReplacementCharacters = "\xFFFD\xFFFD";

        // An encoded word in a character set that no resolver knows.
        private const string UnknownCharsetWord = "=?x-unknown?Q?caf=E9?=";
        private const string UnknownCharset = "x-unknown";
        private const string CafeInLatin1 = "caf\x00E9";

        // ref. RFC 2047 §4.2 (1): hexadecimal digits are written in upper case.
        private const string LowercaseHexWord = "=?ISO-8859-1?Q?caf=e9?=";

        // ref. RFC 3629 §3: 0xFF never appears in UTF-8.
        private const string InvalidOctetWord = "=?UTF-8?Q?=FF?=";

        // ref. RFC 2047 §5 (2) and (3): "(" can't be in "Q" text in a comment, and "." can't be in "Q" text in a phrase.
        private const string ParenthesisWord = "=?UTF-8?Q?a(b?=";
        private const string ParenthesisText = "a(b";
        private const string FullStopWord = "=?UTF-8?Q?a.b?=";
        private const string FullStopBWord = "=?UTF-8?B?YS5i?=";
        private const string FullStopText = "a.b";

        // ref. RFC 2047 §5 (2): a comment's "Q" text may hold "\", although a writer encodes it.
        private const string Backslash = "a\\b";
        private const string BackslashWord = "=?UTF-8?Q?a\\b?=";

        // ref. RFC 822 §3.3: linear-white-space = 1*([CRLF] LWSP-char), so a bare LF, or a CRLF with no SPACE or HTAB after it, is not
        // whitespace, and the words around it are one run of text.
        private const string WordsAroundBareLineFeed = "=?UTF-8?Q?a?=\n=?UTF-8?Q?b?=";
        private const string WordsAroundUnfoldedLineBreak = "=?UTF-8?Q?a?=\r\n=?UTF-8?Q?b?=";

        private const string TextWithoutWords = "Re: hello";
        private const string EmbeddedWord = "Re:=?UTF-8?Q?caf=C3=A9?=";
        private const string EmbeddedWordText = "Re:caf\x00E9";
        private const string AdjacentWords = "=?UTF-8?Q?a?==?UTF-8?Q?b?=";
        private const string AB = "ab";
        private const string ASpaceB = "a b";

        // A word in a character set that no resolver knows, then whitespace and a word that decodes.
        private const string UnknownThenKnownWords = "=?x-unknown?Q?a?= =?UTF-8?Q?b?=";

        private const MimeEncodedWordContext InvalidContext = (MimeEncodedWordContext)3;
        private const MimeEncodedWordEncoding InvalidEncoding = (MimeEncodedWordEncoding)2;
        private const MimeEncodedWordParseOptions InvalidOptions = (MimeEncodedWordParseOptions)(1 << 6);

        public delegate bool TryParseCall(out MimeEncodedWord result);

        // ref. RFC 2047 §8: the encoded words of the examples, with their character sets, encodings and texts.
        public static TheoryData<string, string, MimeEncodedWordEncoding, string> Rfc2047Section8Words =>
            new TheoryData<string, string, MimeEncodedWordEncoding, string>
            {
                { KeithMooreWord, UsAscii, MimeEncodedWordEncoding.Q, KeithMoore },
                { "=?ISO-8859-1?Q?Keld_J=F8rn_Simonsen?=", Iso88591, MimeEncodedWordEncoding.Q, "Keld J\x00F8rn Simonsen" },
                { AndreWord, Iso88591, MimeEncodedWordEncoding.Q, Andre },
                { SubjectExampleFirstWord, Iso88591, MimeEncodedWordEncoding.B, SubjectExampleFirstText },
                { "=?ISO-8859-1?Q?Olle_J=E4rnefors?=", Iso88591, MimeEncodedWordEncoding.Q, "Olle J\x00E4rnefors" },
                { "=?ISO-8859-1?Q?Patrik_F=E4ltstr=F6m?=", Iso88591, MimeEncodedWordEncoding.Q, "Patrik F\x00E4ltstr\x00F6m" },
            };

        // ref. RFC 2047 §8: the text of each comment in the examples, without its parentheses, and how it is displayed.
        public static TheoryData<string, string> Rfc2047Section8Comments => new TheoryData<string, string>
        {
            { "=?ISO-8859-1?Q?a?=", "a" },
            { "=?ISO-8859-1?Q?a?= b", ASpaceB },
            { "=?ISO-8859-1?Q?a?= =?ISO-8859-1?Q?b?=", AB },
            { "=?ISO-8859-1?Q?a?=  =?ISO-8859-1?Q?b?=", AB },
            { "=?ISO-8859-1?Q?a?=\r\n    =?ISO-8859-1?Q?b?=", AB },
            { "=?ISO-8859-1?Q?a_b?=", ASpaceB },
            { "=?ISO-8859-1?Q?a?= =?ISO-8859-2?Q?_b?=", ASpaceB },
            { HebrewCommentWord, HebrewCommentOctets },
        };

        public static TheoryData<string> InvalidWords => new TheoryData<string>
        {
            // ref. RFC 2047 §2: encoded-word = "=?" charset "?" encoding "?" encoded-text "?="
            "",
            "=?UTF-8?Q?a?",
            "UTF-8?Q?a?=",
            " =?UTF-8?Q?a?=",
            "=?UTF-8?Q?a?= ",
            "=?UTF-8?Q=",
            "=?UTF-8?Q??=",
            "=??Q?a?=",
            "=?UTF-8??a?=",
            "=?UTF-8?Q?a?b?=",
            // ref. RFC 2047 §2: encoded-text has no SPACE, and only printable ASCII.
            "=?UTF-8?Q?a b?=",
            "=?UTF-8?Q?a\tb?=",
            "=?UTF-8?Q?caf\x00E9?=",
            // ref. RFC 2047 §3, RFC 2978 §2.3: the character set is a mime-charset.
            "=?UTF.8?Q?a?=",
            // ref. RFC 2047 §4: the only encodings are "B" and "Q".
            "=?UTF-8?X?a?=",
            "=?UTF-8?QQ?a?=",
            // ref. RFC 2047 §4.1, §5: "B" text is base64 in whole groups of 4 characters.
            "=?UTF-8?B?YQ?=",
            "=?UTF-8?B?Y-E=?=",
            // ref. RFC 2047 §4.2, §5: each "=" in "Q" text is followed by two hexadecimal digits, and "=" and "_" are not literal.
            "=?UTF-8?Q?=4?=",
            "=?UTF-8?Q?a=?=",
            // ref. RFC 2231 §5, RFC 1766 §2: a language follows "*", and its subtags are 1 to 8 letters.
            "=?UTF-8*?Q?a?=",
            "=?UTF-8*e1?Q?a?=",
            "=?UTF-8*en-?Q?a?=",
            "=?UTF-8*abcdefghi?Q?a?=",
        };

        public static TheoryData<string, MimeEncodedWordParseOptions, string> ParseDeviations =>
            new TheoryData<string, MimeEncodedWordParseOptions, string>
            {
                { LongWord, MimeEncodedWordParseOptions.AllowLongWords, LongText },
                { UnknownCharsetWord, MimeEncodedWordParseOptions.AllowUnknownCharsets, CafeInLatin1 },
                { LowercaseHexWord, MimeEncodedWordParseOptions.AllowLowercaseHexDigits, CafeInLatin1 },
                { InvalidOctetWord, MimeEncodedWordParseOptions.AllowInvalidOctets, ReplacementCharacter },
            };

        public static TheoryData<string> ParseDeviationValues => new TheoryData<string>
        {
            LongWord,
            UnknownCharsetWord,
            LowercaseHexWord,
            InvalidOctetWord,
        };

        public static TheoryData<string, MimeEncodedWordParseOptions, string> DecodeDeviations =>
            new TheoryData<string, MimeEncodedWordParseOptions, string>
            {
                { LongWord, MimeEncodedWordParseOptions.AllowLongWords, LongText },
                { EmbeddedWord, MimeEncodedWordParseOptions.AllowEmbeddedWords, EmbeddedWordText },
                { AdjacentWords, MimeEncodedWordParseOptions.AllowEmbeddedWords, AB },
                { "=?UTF-8?Q?a?=x=?UTF-8?Q?b?=", MimeEncodedWordParseOptions.AllowEmbeddedWords, "axb" },
                { "=?UTF-8?Q?a?=x =?UTF-8?Q?b?=", MimeEncodedWordParseOptions.AllowEmbeddedWords, "ax b" },
                { "a=?UTF-8?Q?b?b=?UTF-8?Q?c?=", MimeEncodedWordParseOptions.AllowEmbeddedWords, "a=?UTF-8?Q?b?bc" },
                { UnknownThenKnownWords, MimeEncodedWordParseOptions.AllowUnknownCharsets, AB },
                { LowercaseHexWord, MimeEncodedWordParseOptions.AllowLowercaseHexDigits, CafeInLatin1 },
                { InvalidOctetWord, MimeEncodedWordParseOptions.AllowInvalidOctets, ReplacementCharacter },
                { SplitEAcute, MimeEncodedWordParseOptions.AllowInvalidOctets, TwoReplacementCharacters },
                { SplitEAcute, MimeEncodedWordParseOptions.AllowSplitCharacters, EAcute },
                { "x =?UTF-8?B?ww==?=\t=?UTF-8?Q?=A9?= y", MimeEncodedWordParseOptions.AllowSplitCharacters, "x \x00E9 y" },
                { SplitEAcute, MimeEncodedWordParseOptions.AllowSplitCharacters | MimeEncodedWordParseOptions.AllowInvalidOctets, EAcute },
                { SplitEAcuteThenInvalidOctet, MimeEncodedWordParseOptions.AllowSplitCharacters, EAcuteThenInvalidOctet },
                { InvalidOctetThenSplitEAcute, MimeEncodedWordParseOptions.AllowSplitCharacters, InvalidOctetThenEAcute },
                { SplitEmoji, MimeEncodedWordParseOptions.AllowSplitCharacters, Emoji },
                { EAcutesSplitInEveryWord, MimeEncodedWordParseOptions.AllowSplitCharacters, FourEAcutes },
                { LettersAroundSplitEAcute, MimeEncodedWordParseOptions.AllowSplitCharacters, LettersAroundEAcute },
                { UnfinishedEAcute, MimeEncodedWordParseOptions.AllowSplitCharacters, UnfinishedEAcute },
                { Utf16WordsWithByteOrderMarks, MimeEncodedWordParseOptions.AllowSplitCharacters, AB },
            };

        public static TheoryData<string> DecodeDeviationValues => new TheoryData<string>
        {
            LongWord,
            EmbeddedWord,
            AdjacentWords,
            LowercaseHexWord,
            InvalidOctetWord,
            SplitEAcute,
        };

        public static TheoryData<string, string> TextContextValues => new TheoryData<string, string>
        {
            { TextWithoutWords, TextWithoutWords },
            { ParenthesisedWord, ParenthesisedWord },
            { "Hello  =?UTF-8?Q?w=C3=B6rld?=", "Hello  w\x00F6rld" },
            { "=?UTF-8?Q?a?=\t=?UTF-8?Q?b?= c", "ab c" },
            { "=?UTF-8?Q?a?=\r\n\t=?UTF-8?Q?b?=", AB },
            { WordsAroundBareLineFeed, WordsAroundBareLineFeed },
            { WordsAroundUnfoldedLineBreak, WordsAroundUnfoldedLineBreak },
            { UnknownThenKnownWords, "=?x-unknown?Q?a?= b" },
            { "=?UTF-8?B?ww==?= =?ISO-8859-1?Q?a?=", "=?UTF-8?B?ww==?= a" },
            { MaxLengthWord, MaxLengthText },
            { FullStopWord, FullStopText },
            { "", "" },
        };

        public static TheoryData<Func<MimeEncodedWord>> ParseCalls => new TheoryData<Func<MimeEncodedWord>>
        {
            () => MimeEncodedWord.Parse(AndreWord),
            () => MimeEncodedWord.Parse(AndreWord.AsSpan()),
            () => MimeEncodedWord.Parse(AndreWord, MimeEncodedWordContext.Phrase, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default),
            () => MimeEncodedWord.Parse(AndreWord.AsSpan(), MimeEncodedWordContext.Phrase, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default),
        };

        public static TheoryData<TryParseCall> TryParseCalls => new TheoryData<TryParseCall>
        {
            (out MimeEncodedWord result) => MimeEncodedWord.TryParse(AndreWord, out result),
            (out MimeEncodedWord result) => MimeEncodedWord.TryParse(AndreWord.AsSpan(), out result),
            (out MimeEncodedWord result) => MimeEncodedWord.TryParse(AndreWord, MimeEncodedWordContext.Phrase, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default, out result),
            (out MimeEncodedWord result) => MimeEncodedWord.TryParse(AndreWord.AsSpan(), MimeEncodedWordContext.Phrase, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default, out result),
        };

        public static TheoryData<Action> InvalidParseCalls => new TheoryData<Action>
        {
            () => MimeEncodedWord.Parse(LongWord),
            () => MimeEncodedWord.Parse(LongWord.AsSpan()),
            () => MimeEncodedWord.Parse(LongWord, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default),
            () => MimeEncodedWord.Parse(LongWord.AsSpan(), MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default),
        };

        public static TheoryData<Func<string>> DecodeTextCalls => new TheoryData<Func<string>>
        {
            () => MimeEncodedWord.DecodeText(AndrePirardPhrase),
            () => MimeEncodedWord.DecodeText(AndrePirardPhrase.AsSpan()),
            () => MimeEncodedWord.DecodeText(AndrePirardPhrase, MimeEncodedWordContext.Phrase, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default),
            () => MimeEncodedWord.DecodeText(AndrePirardPhrase.AsSpan(), MimeEncodedWordContext.Phrase, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default),
        };

        public static TheoryData<Action> CallsWithNullArgument => new TheoryData<Action>
        {
            () => MimeEncodedWord.Parse((string)null),
            () => MimeEncodedWord.Parse((string)null, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default),
            () => MimeEncodedWord.Parse(AndreWord, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, null),
            () => MimeEncodedWord.Parse(AndreWord.AsSpan(), MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, null),
            () => MimeEncodedWord.TryParse(AndreWord, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, null, out _),
            () => MimeEncodedWord.TryParse(AndreWord.AsSpan(), MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, null, out _),
            () => MimeEncodedWord.DecodeText((string)null),
            () => MimeEncodedWord.DecodeText((string)null, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default),
            () => MimeEncodedWord.DecodeText(AndreWord, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, null),
            () => MimeEncodedWord.DecodeText(AndreWord.AsSpan(), MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, null),
            () => new MimeEncodedWord((string)null, MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text),
            () => new MimeEncodedWord((string)null, MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text, English),
            () => new MimeEncodedWord(Cafe, MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text, null),
            () => MimeEncodedWord.EncodeText((string)null, MimeEncodedWordContext.Text),
            () => MimeEncodedWord.EncodeText((string)null, MimeEncodedWordContext.Text, English),
            () => MimeEncodedWord.EncodeText(Cafe, MimeEncodedWordContext.Text, null),
        };

        public static TheoryData<Action> CallsWithArgumentOutOfRange => new TheoryData<Action>
        {
            () => MimeEncodedWord.Parse(AndreWord, InvalidContext, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default),
            () => MimeEncodedWord.Parse(AndreWord, MimeEncodedWordContext.Text, InvalidOptions, MimeCharsetResolver.Default),
            () => MimeEncodedWord.TryParse(AndreWord.AsSpan(), InvalidContext, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default, out _),
            () => MimeEncodedWord.TryParse(AndreWord.AsSpan(), MimeEncodedWordContext.Text, InvalidOptions, MimeCharsetResolver.Default, out _),
            () => MimeEncodedWord.DecodeText(AndreWord, InvalidContext, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default),
            () => MimeEncodedWord.DecodeText(AndreWord, MimeEncodedWordContext.Text, InvalidOptions, MimeCharsetResolver.Default),
            () => new MimeEncodedWord(Cafe, InvalidEncoding, MimeEncodedWordContext.Text),
            () => new MimeEncodedWord(Cafe, MimeEncodedWordEncoding.Q, InvalidContext),
            () => MimeEncodedWord.EncodeText(Cafe, InvalidContext),
        };

        public static TheoryData<Action> CallsWithInvalidArgument => new TheoryData<Action>
        {
            () => new MimeEncodedWord("", MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text),
            () => new MimeEncodedWord("a\xD83D", MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text),
            () => new MimeEncodedWord("\xDE00a", MimeEncodedWordEncoding.B, MimeEncodedWordContext.Text),
            () => new MimeEncodedWord(LongText, MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text),
            () => new MimeEncodedWord(Cafe, MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text, "e1"),
            () => new MimeEncodedWord(Cafe, MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text, TooLongLanguage),
            () => MimeEncodedWord.EncodeText("a\xD83D", MimeEncodedWordContext.Text),
            () => MimeEncodedWord.EncodeText(Cafe, MimeEncodedWordContext.Text, "es-419"),
            () => MimeEncodedWord.EncodeText(Cafe, MimeEncodedWordContext.Text, TooLongLanguage),
        };

        public static TheoryData<Func<object>> UninitializedProperties => new TheoryData<Func<object>>
        {
            () => default(MimeEncodedWord).Charset,
            () => default(MimeEncodedWord).Language,
            () => default(MimeEncodedWord).Encoding,
            () => default(MimeEncodedWord).Text,
        };

        // A text, the encoding and context to write it in, and the word that is written.
        public static TheoryData<string, MimeEncodedWordEncoding, MimeEncodedWordContext, string> CreatedWords =>
            new TheoryData<string, MimeEncodedWordEncoding, MimeEncodedWordContext, string>
            {
                { Cafe, MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text, CafeQWord },
                { Cafe, MimeEncodedWordEncoding.B, MimeEncodedWordContext.Text, CafeBWord },
                // ref. RFC 2047 §4.2: a space is written as "_", and "=", "?" and "_" are always encoded.
                { "a b=?_", MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text, "=?UTF-8?Q?a_b=3D=3F=5F?=" },
                // ref. RFC 2047 §5 (1), (2) and (3): the characters that "Q" text may hold in each context.
                { "a(b.c", MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text, "=?UTF-8?Q?a(b.c?=" },
                { "a(b.c", MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Comment, "=?UTF-8?Q?a=28b.c?=" },
                { "a(b.c", MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Phrase, "=?UTF-8?Q?a=28b=2Ec?=" },
                // ref. RFC 5322 §3.2.2: "\" starts a quoted-pair in a comment, so it is encoded there.
                { Backslash, MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text, BackslashWord },
                { Backslash, MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Comment, "=?UTF-8?Q?a=5Cb?=" },
                { MaxLengthText, MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text, MaxLengthWord },
                { Emoji, MimeEncodedWordEncoding.B, MimeEncodedWordContext.Phrase, EmojiWord },
            };

        [Theory]
        [MemberData(nameof(Rfc2047Section8Words))]
        public void Parse_Rfc2047Section8Word_DecodesText(string value, string charset, MimeEncodedWordEncoding encoding, string text)
        {
            // act
            var result = MimeEncodedWord.Parse(value);

            // assert
            result.Charset.Name.Should().Be(charset);
            result.Language.Should().BeEmpty();
            result.Encoding.Should().Be(encoding);
            result.Text.Should().Be(text);
        }

        [Fact]
        public void Parse_Rfc2231Section5Example_ReadsLanguage()
        {
            // act
            var result = MimeEncodedWord.Parse(Rfc2231Section5Word);

            // assert
            result.Charset.Name.Should().Be(UsAscii);
            result.Language.Should().Be(Rfc2231Section5Language);
            result.Text.Should().Be(KeithMoore);
        }

        [Fact]
        public void Parse_LowerCaseEncodingAndCharset_DecodesText()
        {
            // act
            var result = MimeEncodedWord.Parse("=?utf-8?b?YS5i?=");

            // assert
            result.Charset.Name.Should().Be("utf-8");
            result.Encoding.Should().Be(MimeEncodedWordEncoding.B);
            result.Text.Should().Be(FullStopText);
        }

        [Fact]
        public void Parse_MaxLengthWord_DecodesText()
        {
            // act
            var result = MimeEncodedWord.Parse(MaxLengthWord);

            // assert
            result.Text.Should().Be(MaxLengthText);
        }

        [Theory]
        [MemberData(nameof(ParseCalls))]
        public void Parse_EachOverload_DecodesText(Func<MimeEncodedWord> parse)
        {
            // act
            var result = parse();

            // assert
            result.Text.Should().Be(Andre);
        }

        [Theory]
        [MemberData(nameof(TryParseCalls))]
        public void TryParse_EachOverload_DecodesText(TryParseCall tryParse)
        {
            // act
            var success = tryParse(out var result);

            // assert
            success.Should().BeTrue();
            result.Text.Should().Be(Andre);
        }

        [Theory]
        [MemberData(nameof(InvalidWords))]
        public void TryParse_InvalidWord_ReturnsFalse(string value)
        {
            // act
            var success = MimeEncodedWord.TryParse(value, out var result);

            // assert
            success.Should().BeFalse();
            result.Should().Be(default(MimeEncodedWord));
        }

        [Fact]
        public void TryParse_NullString_ReturnsFalse()
        {
            // act
            var success = MimeEncodedWord.TryParse((string)null, out _);

            // assert
            success.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(InvalidParseCalls))]
        public void Parse_InvalidWord_ThrowsFormatException(Action parse)
        {
            // act
            var testAction = parse;

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(ParseDeviationValues))]
        public void TryParse_DeviationWithoutOption_ReturnsFalse(string value)
        {
            // act
            var success = MimeEncodedWord.TryParse(value, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default, out _);

            // assert
            success.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(ParseDeviations))]
        public void TryParse_DeviationWithOption_DecodesText(string value, MimeEncodedWordParseOptions options, string text)
        {
            // act
            var success = MimeEncodedWord.TryParse(value, MimeEncodedWordContext.Text, options, MimeCharsetResolver.Default, out var result);

            // assert
            success.Should().BeTrue();
            result.Text.Should().Be(text);
        }

        [Fact]
        public void Parse_UnknownCharsetWithOption_KeepsCharsetName()
        {
            // act
            var result = MimeEncodedWord.Parse(UnknownCharsetWord, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.AllowUnknownCharsets,
                MimeCharsetResolver.Default);

            // assert
            result.Charset.Name.Should().Be(UnknownCharset);
        }

        [Fact]
        public void Parse_CharsetOfResolver_DecodesWithResolver()
        {
            // arrange
            var resolver = new Latin1CharsetResolver();

            // act
            var result = MimeEncodedWord.Parse(UnknownCharsetWord, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, resolver);

            // assert
            result.Text.Should().Be(CafeInLatin1);
        }

        [Theory]
        [InlineData(MimeEncodedWordContext.Text, true)]
        [InlineData(MimeEncodedWordContext.Comment, false)]
        [InlineData(MimeEncodedWordContext.Phrase, false)]
        public void TryParse_QWordWithParenthesis_AcceptedOnlyInText(MimeEncodedWordContext context, bool expected)
        {
            // act
            var success = MimeEncodedWord.TryParse(ParenthesisWord, context, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default, out _);

            // assert
            success.Should().Be(expected);
        }

        [Theory]
        [InlineData(MimeEncodedWordContext.Text, true)]
        [InlineData(MimeEncodedWordContext.Comment, true)]
        [InlineData(MimeEncodedWordContext.Phrase, false)]
        public void TryParse_QWordWithFullStop_RejectedInPhrase(MimeEncodedWordContext context, bool expected)
        {
            // act
            var success = MimeEncodedWord.TryParse(FullStopWord, context, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default, out _);

            // assert
            success.Should().Be(expected);
        }

        [Fact]
        public void Parse_QWordWithBackslashInComment_DecodesText()
        {
            // act
            var result = MimeEncodedWord.Parse(BackslashWord, MimeEncodedWordContext.Comment, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default);

            // assert
            result.Text.Should().Be(Backslash);
        }

        [Fact]
        public void Parse_BWordInPhrase_DecodesText()
        {
            // act
            var result = MimeEncodedWord.Parse(FullStopBWord, MimeEncodedWordContext.Phrase, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default);

            // assert
            result.Text.Should().Be(FullStopText);
        }

        [Fact]
        public void ToString_ParsedWord_WritesWordAsWritten()
        {
            // arrange
            var word = MimeEncodedWord.Parse("=?iso-8859-1?q?Andr=E9?=");

            // act
            var result = word.ToString();

            // assert
            result.Should().Be("=?iso-8859-1?q?Andr=E9?=");
        }

        [Fact]
        public void TryFormat_LongEnoughDestination_WritesWord()
        {
            // arrange
            var word = MimeEncodedWord.Parse(AndreWord);
            var destination = new char[AndreWord.Length];

            // act
            var success = word.TryFormat(destination, out var charsWritten);

            // assert
            success.Should().BeTrue();
            charsWritten.Should().Be(AndreWord.Length);
            new string(destination).Should().Be(AndreWord);
        }

        [Fact]
        public void TryFormat_ShortDestination_ReturnsFalse()
        {
            // arrange
            var word = MimeEncodedWord.Parse(AndreWord);
            var destination = new char[1];

            // act
            var success = word.TryFormat(destination, out var charsWritten);

            // assert
            success.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void TryFormat_Uninitialized_WritesNothing()
        {
            // arrange
            var destination = new char[1];

            // act
            var success = default(MimeEncodedWord).TryFormat(destination, out var charsWritten);

            // assert
            success.Should().BeTrue();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void ToString_Uninitialized_ReturnsEmptyString()
        {
            // act
            var result = default(MimeEncodedWord).ToString();

            // assert
            result.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(UninitializedProperties))]
        public void Property_Uninitialized_ThrowsInvalidOperationException(Func<object> property)
        {
            // act
            Action testAction = () => property();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData("=?UTF-8?Q?A?=", "=?utf-8?Q?=41?=")]
        [InlineData("=?UTF-8*en?Q?A?=", "=?UTF-8*EN?q?A?=")]
        public void Equals_SameTextWrittenDifferently_ReturnsTrue(string left, string right)
        {
            // arrange
            var leftWord = MimeEncodedWord.Parse(left);
            var rightWord = MimeEncodedWord.Parse(right);

            // act
            var result = leftWord == rightWord;

            // assert
            result.Should().BeTrue();
            leftWord.GetHashCode().Should().Be(rightWord.GetHashCode());
        }

        [Theory]
        [InlineData("=?UTF-8?Q?A?=", "=?UTF-8?B?QQ==?=")]
        [InlineData("=?UTF-8?Q?A?=", "=?US-ASCII?Q?A?=")]
        [InlineData("=?UTF-8?Q?A?=", "=?UTF-8*en?Q?A?=")]
        [InlineData("=?UTF-8?Q?A?=", "=?UTF-8?Q?a?=")]
        public void Equals_DifferentWords_ReturnsFalse(string left, string right)
        {
            // arrange
            var leftWord = MimeEncodedWord.Parse(left);
            var rightWord = MimeEncodedWord.Parse(right);

            // act
            var result = leftWord != rightWord;

            // assert
            result.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(CreatedWords))]
        public void Constructor_Text_WritesWordInUtf8(string text, MimeEncodedWordEncoding encoding, MimeEncodedWordContext context, string expected)
        {
            // act
            var result = new MimeEncodedWord(text, encoding, context);

            // assert
            result.ToString().Should().Be(expected);
            result.Charset.Name.Should().Be(Utf8);
            result.Encoding.Should().Be(encoding);
            result.Text.Should().Be(text);
        }

        [Fact]
        public void Constructor_Language_WritesLanguageAfterCharset()
        {
            // act
            var result = new MimeEncodedWord(Cafe, MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text, English);

            // assert
            result.ToString().Should().Be("=?UTF-8*en?Q?caf=C3=A9?=");
            result.Language.Should().Be(English);
        }

        [Fact]
        public void Parse_CreatedWord_EqualsCreatedWord()
        {
            // arrange
            var word = new MimeEncodedWord(Cafe, MimeEncodedWordEncoding.B, MimeEncodedWordContext.Text, English);

            // act
            var result = MimeEncodedWord.Parse(word.ToString());

            // assert
            result.Should().Be(word);
        }

        [Theory]
        [MemberData(nameof(CallsWithNullArgument))]
        public void Call_NullArgument_ThrowsArgumentNullException(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(CallsWithArgumentOutOfRange))]
        public void Call_ArgumentOutOfRange_ThrowsArgumentOutOfRangeException(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [MemberData(nameof(CallsWithInvalidArgument))]
        public void Call_InvalidArgument_ThrowsArgumentException(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void EncodeText_EmptyText_ReturnsNoWords()
        {
            // act
            var result = MimeEncodedWord.EncodeText("", MimeEncodedWordContext.Text);

            // assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void EncodeText_MostlyAsciiText_UsesQ()
        {
            // act
            var result = MimeEncodedWord.EncodeText(AndrePirard, MimeEncodedWordContext.Text);

            // assert
            result.Should().HaveCount(1);
            result[0].ToString().Should().Be("=?UTF-8?Q?Andr=C3=A9_Pirard?=");
        }

        [Fact]
        public void EncodeText_MostlyNonAsciiText_UsesB()
        {
            // act
            var result = MimeEncodedWord.EncodeText("\x65E5\x672C\x8A9E", MimeEncodedWordContext.Text);

            // assert
            result.Should().HaveCount(1);
            result[0].ToString().Should().Be("=?UTF-8?B?5pel5pys6Kqe?=");
        }

        [Fact]
        public void EncodeText_SameLengthInBothEncodings_UsesQ()
        {
            // act
            var result = MimeEncodedWord.EncodeText("ab\x00E9", MimeEncodedWordContext.Text);

            // assert
            result.Should().HaveCount(1);
            result[0].ToString().Should().Be("=?UTF-8?Q?ab=C3=A9?=");
        }

        [Fact]
        public void EncodeText_LongerThanOneWord_SplitsAtMaxLength()
        {
            // act
            var result = MimeEncodedWord.EncodeText(SeventyLetters, MimeEncodedWordContext.Text);

            // assert
            result.Should().HaveCount(2);
            result[0].ToString().Should().Be(MaxLengthWord);
            result[1].ToString().Should().Be(SevenLettersWord);
        }

        [Fact]
        public void EncodeText_TwoOctetCharacters_NeverSplitsCharacter()
        {
            // act
            var result = MimeEncodedWord.EncodeText(ThirtyEAcutes, MimeEncodedWordContext.Text);

            // assert
            result.Should().HaveCount(2);
            result[0].ToString().Should().Be(TwentyOneEAcutesWord);
            result[0].Text.Should().Be(TwentyOneEAcutes);
            result[1].ToString().Should().Be(NineEAcutesWord);
            result[1].Text.Should().Be(NineEAcutes);
        }

        [Fact]
        public void EncodeText_FullBWordNeedsMorePadding_SplitsWhereShortest()
        {
            // act
            var result = MimeEncodedWord.EncodeText(LettersAndFiveEAcutes, MimeEncodedWordContext.Text);

            // assert
            result.Should().HaveCount(2);
            result[0].ToString().Should().Be(LettersAndTwoEAcutesWord);
            result[0].Text.Should().Be(LettersAndTwoEAcutes);
            result[1].ToString().Should().Be(ThreeEAcutesWord);
            result[1].Text.Should().Be(ThreeEAcutes);
        }

        [Fact]
        public void EncodeText_SurrogatePairs_NeverSplitsPair()
        {
            // act
            var result = MimeEncodedWord.EncodeText(TwelveEmoji, MimeEncodedWordContext.Phrase);

            // assert
            result.Should().HaveCount(2);
            result[0].ToString().Should().Be(NineEmojiWord);
            result[0].Text.Should().Be(NineEmoji);
            result[1].ToString().Should().Be(ThreeEmojiWord);
            result[1].Text.Should().Be(ThreeEmoji);
        }

        [Fact]
        public void EncodeText_Language_WritesLanguageInEachWord()
        {
            // act
            var result = MimeEncodedWord.EncodeText(Cafe, MimeEncodedWordContext.Text, English);

            // assert
            result.Should().HaveCount(1);
            result[0].ToString().Should().Be("=?UTF-8*en?B?Y2Fmw6k=?=");
            result[0].Language.Should().Be(English);
        }

        [Fact]
        public void EncodeText_Phrase_EncodesPhraseSpecials()
        {
            // act
            var result = MimeEncodedWord.EncodeText("Smith, J\x00F6rgensen", MimeEncodedWordContext.Phrase);

            // assert
            result.Should().HaveCount(1);
            result[0].ToString().Should().Be("=?UTF-8?Q?Smith=2C_J=C3=B6rgensen?=");
        }

        [Fact]
        public void DecodeText_EncodedWords_DecodesTextOfEachWord()
        {
            // act
            var result = MimeEncodedWord.DecodeText("=?UTF-8?Q?Smith=2C_J=C3=B6rg?= =?UTF-8?B?w6nDqQ==?=");

            // assert
            result.Should().Be("Smith, J\x00F6rg\x00E9\x00E9");
        }

        [Theory]
        [MemberData(nameof(Rfc2047Section8Comments))]
        public void DecodeText_Rfc2047Section8Comment_DisplaysText(string value, string expected)
        {
            // arrange
            var resolver = new Latin1CharsetResolver();

            // act
            var result = MimeEncodedWord.DecodeText(value, MimeEncodedWordContext.Comment, MimeEncodedWordParseOptions.None, resolver);

            // assert
            result.Should().Be(expected);
        }

        [Fact]
        public void DecodeText_Rfc2047Section8Subject_JoinsWords()
        {
            // arrange
            var resolver = new Latin1CharsetResolver();

            // act
            var result = MimeEncodedWord.DecodeText(SubjectExample, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, resolver);

            // assert
            result.Should().Be(SubjectExampleText);
        }

        [Theory]
        [MemberData(nameof(DecodeTextCalls))]
        public void DecodeText_EachOverload_DecodesWordAndKeepsOrdinaryWord(Func<string> decodeText)
        {
            // act
            var result = decodeText();

            // assert
            result.Should().Be(AndrePirard);
        }

        [Theory]
        [MemberData(nameof(TextContextValues))]
        public void DecodeText_Text_DecodesOnlyWordsBetweenWhitespace(string value, string expected)
        {
            // act
            var result = MimeEncodedWord.DecodeText(value);

            // assert
            result.Should().Be(expected);
        }

        [Fact]
        public void DecodeText_QWordWithFullStopInPhrase_KeepsWord()
        {
            // act
            var result = MimeEncodedWord.DecodeText(FullStopWord, MimeEncodedWordContext.Phrase, MimeEncodedWordParseOptions.None,
                MimeCharsetResolver.Default);

            // assert
            result.Should().Be(FullStopWord);
        }

        [Theory]
        [MemberData(nameof(DecodeDeviationValues))]
        public void DecodeText_DeviationWithoutOption_KeepsText(string value)
        {
            // act
            var result = MimeEncodedWord.DecodeText(value, MimeEncodedWordContext.Text, MimeEncodedWordParseOptions.None, MimeCharsetResolver.Default);

            // assert
            result.Should().Be(value);
        }

        [Theory]
        [MemberData(nameof(DecodeDeviations))]
        public void DecodeText_DeviationWithOption_DecodesText(string value, MimeEncodedWordParseOptions options, string expected)
        {
            // act
            var result = MimeEncodedWord.DecodeText(value, MimeEncodedWordContext.Text, options, MimeCharsetResolver.Default);

            // assert
            result.Should().Be(expected);
        }

        // Resolves every character set as ISO-8859-1, so that the octets of a word are the same on every target.
        private sealed class Latin1CharsetResolver : IMimeCharsetResolver
        {
            public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
            {
                return MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Iso88591, out encoding);
            }
        }
    }
}
