using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// The field bodies of the examples in RFC 2047 §8, what each one reads as, and the character sets they need.
    /// </summary>
    internal static class Rfc2047Examples
    {
        // ref. RFC 2047 §8: the first group of headers.
        public const string KeithMooreFrom = "=?US-ASCII?Q?Keith_Moore?= <moore@cs.utk.edu>";
        public const string KeithMooreName = "Keith Moore";
        public const string KeldTo = "=?ISO-8859-1?Q?Keld_J=F8rn_Simonsen?= <keld@dkuug.dk>";
        public const string KeldName = "Keld J\x00F8rn Simonsen";
        public const string AndreCc = "=?ISO-8859-1?Q?Andr=E9?= Pirard <PIRARD@vm1.ulg.ac.be>";
        public const string AndreName = "Andr\x00E9 Pirard";
        public const string ExampleSubject = "=?ISO-8859-1?B?SWYgeW91IGNhbiByZWFkIHRoaXMgeW8=?=\r\n =?ISO-8859-2?B?dSB1bmRlcnN0YW5kIHRoZSBleGFtcGxlLg==?=";
        public const string ExampleSubjectText = "If you can read this you understand the example.";

        // ref. RFC 2047 §8: the second group of headers.
        public const string OlleFrom = "=?ISO-8859-1?Q?Olle_J=E4rnefors?= <ojarnef@admin.kth.se>";
        public const string OlleName = "Olle J\x00E4rnefors";
        public const string OlleSubject = "Time for ISO 10646?";

        // ref. RFC 2047 §8: the third group of headers.
        public const string PatrikFrom = "=?ISO-8859-1?Q?Patrik_F=E4ltstr=F6m?= <paf@nada.kth.se>";
        public const string PatrikName = "Patrik F\x00E4ltstr\x00F6m";
        public const string PatrikSubject = "Re: RFC-HDR care and feeding";

        // ref. RFC 2047 §8: the fourth group of headers, with a comment in ISO-8859-8.
        public const string NathanielFrom = "Nathaniel Borenstein <nsb@thumper.bellcore.com>\r\n      (=?iso-8859-8?b?7eXs+SDv4SDp7Oj08A==?=)";
        public const string NathanielName = "Nathaniel Borenstein";
        public const string NathanielComment = "\x05DD\x05D5\x05DC\x05E9 \x05DF\x05D1 \x05D9\x05DC\x05D8\x05E4\x05E0";

        // ref. RFC 2047 §8: the table of encoded forms, each a comment in a structured field.
        public const string TableOneWord = "(=?ISO-8859-1?Q?a?=)";
        public const string TableWordAndText = "(=?ISO-8859-1?Q?a?= b)";
        public const string TableTwoWords = "(=?ISO-8859-1?Q?a?= =?ISO-8859-1?Q?b?=)";
        public const string TableTwoSpaces = "(=?ISO-8859-1?Q?a?=  =?ISO-8859-1?Q?b?=)";
        public const string TableFolded = "(=?ISO-8859-1?Q?a?=\r\n    =?ISO-8859-1?Q?b?=)";
        public const string TableEncodedSpace = "(=?ISO-8859-1?Q?a_b?=)";
        public const string TableSpaceInSecondWord = "(=?ISO-8859-1?Q?a?= =?ISO-8859-2?Q?_b?=)";

        /// <summary>
        /// Gets options whose character set resolver knows the character sets of the examples, whatever encodings the runtime has.
        /// </summary>
        public static ImfOptions Options => new ImfOptions { CharsetResolver = new MimeCharsetResolver(new ExampleCharsetResolver()) };

        /// <summary>
        /// Resolves ISO-8859-8 to the part of it that the examples use, and ISO-8859-2 to ISO-8859-1, as they agree on US-ASCII, the only text the examples have in it.
        /// </summary>
        private sealed class ExampleCharsetResolver : IMimeCharsetResolver
        {
#if NETCOREAPP3_0_OR_GREATER
            public bool TryGetEncoding(MimeCharset charset, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Encoding encoding)
#else
            public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
#endif
            {
                encoding = charset.Name == "iso-8859-8" ? new HebrewLettersEncoding() : Encoding.GetEncoding("ISO-8859-1");
                return true;
            }
        }

        /// <summary>
        /// The ISO-8859-8 letters 0xE0 to 0xFA, which are U+05D0 to U+05EA, and US-ASCII.
        /// </summary>
        private sealed class HebrewLettersEncoding : Encoding
        {
            private const int FirstLetterOctet = 0xE0;
            private const int FirstLetter = 0x05D0;

            public override int GetByteCount(char[] chars, int index, int count)
            {
                return count;
            }

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
            {
                for (var offset = 0; offset < charCount; offset++)
                {
                    var c = chars[charIndex + offset];
                    bytes[byteIndex + offset] = (byte)(c >= FirstLetter ? c - FirstLetter + FirstLetterOctet : c);
                }

                return charCount;
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                return count;
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                for (var offset = 0; offset < byteCount; offset++)
                {
                    var octet = bytes[byteIndex + offset];
                    chars[charIndex + offset] = (char)(octet >= FirstLetterOctet ? octet - FirstLetterOctet + FirstLetter : octet);
                }

                return byteCount;
            }

            public override int GetMaxByteCount(int charCount)
            {
                return charCount;
            }

            public override int GetMaxCharCount(int byteCount)
            {
                return byteCount;
            }
        }
    }
}
