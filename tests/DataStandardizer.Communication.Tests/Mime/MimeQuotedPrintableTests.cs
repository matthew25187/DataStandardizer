using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeQuotedPrintableTests
    {
        private const MimeQuotedPrintableMode Body = MimeQuotedPrintableMode.Body;
        private const MimeQuotedPrintableEncoderOptions Text = MimeQuotedPrintableEncoderOptions.None;
        private const MimeQuotedPrintableEncoderOptions Binary = MimeQuotedPrintableEncoderOptions.Binary;
        private const MimeQuotedPrintableDecoderOptions Strict = MimeQuotedPrintableDecoderOptions.None;
        private const MimeQuotedPrintableDecoderOptions Lowercase = MimeQuotedPrintableDecoderOptions.AllowLowercaseHexDigits;
        private const MimeQuotedPrintableDecoderOptions BareLineFeeds = MimeQuotedPrintableDecoderOptions.AllowBareLineFeeds;
        private const MimeQuotedPrintableDecoderOptions LiteralEquals = MimeQuotedPrintableDecoderOptions.AllowLiteralEqualsSigns;
        private const MimeQuotedPrintableDecoderOptions EveryLeniency = Lowercase | BareLineFeeds | LiteralEquals;

        // ISO-8859-1. Encoding.Latin1 is .NET 5 or later, but every runtime has code page 28591 built in.
        private static readonly Encoding Latin1Encoding = Encoding.GetEncoding(28591);

        // ref. RFC 2045 §6.7, the example of rule 5, and its encoded lines.
        private const string Rfc2045Text = "Now's the time for all folk to come to the aid of their country.";
        private const string Rfc2045Encoded = "Now's the time =\r\nfor all folk to come=\r\n to the aid of their country.";
        private const string Rfc2045FirstLine = "Now's the time =";
        private const string Rfc2045SecondLine = "for all folk to come=";
        private const string Rfc2045LastLine = " to the aid of their country.";

        // ref. RFC 2045 §6.7, rule 5: encoded lines are no more than 76 characters.
        private const string LineOf76Letters = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string LineOf77Letters = LineOf76Letters + "a";
        private const string LineOf74Letters = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" + "aaaaaaaaaaaaaa";
        private const string LineOf75Letters = LineOf74Letters + "a";
        private const string LineOf73Letters = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" + "aaaaaaaaaaaaa";
        private const string SoftLineBreak = "=\r\n";
        private const string LineBreak = "\r\n";

        // Shorter than a full line, so the encoder writes them as they are.
        private const string LineOf70Letters = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string SevenLetters = "bcdefgh";

        // Text with line breaks, trailing whitespace, characters that need encoding and a line longer than 76 characters, as UTF-8.
        private const string SampleText = "Subject: caf\x00E9 = 100%\r\n"
            + "trailing space \r\n"
            + "trailing tab\t\r\n"
            + "\t indented\n"
            + "a long line that goes on and on, well past the seventy-six character limit of a line, so it needs soft breaks \r\n"
            + "end ";

        // SampleText as a text body decodes: the bare line feed becomes a line break.
        private const string SampleTextAsText = "Subject: caf\x00E9 = 100%\r\n"
            + "trailing space \r\n"
            + "trailing tab\t\r\n"
            + "\t indented\r\n"
            + "a long line that goes on and on, well past the seventy-six character limit of a line, so it needs soft breaks \r\n"
            + "end ";

        // SampleText encoded as a text body, as a binary body, and in each "Q" context. The text and comment contexts encode it the same,
        // as it has no parentheses or quotation marks; ref. RFC 2047 §5.
        private const string SampleTextBody = "Subject: caf=C3=A9 =3D 100%\r\n"
            + "trailing space=20\r\n"
            + "trailing tab=09\r\n"
            + "\t indented\r\n"
            + "a long line that goes on and on, well past the seventy-six character limit =\r\n"
            + "of a line, so it needs soft breaks=20\r\n"
            + "end=20";

        private const string SampleTextBinaryBody = "Subject: caf=C3=A9 =3D 100%=0D=0Atrailing space =0D=0Atrailing tab\t=0D=0A\t =\r\n"
            + "indented=0Aa long line that goes on and on, well past the seventy-six chara=\r\n"
            + "cter limit of a line, so it needs soft breaks =0D=0Aend=20";

        private const string SampleTextQ = "Subject:_caf=C3=A9_=3D_100%=0D=0Atrailing_space_=0D=0Atrailing_tab=09=0D=0A=09_indented=0A"
            + "a_long_line_that_goes_on_and_on,_well_past_the_seventy-six_character_limit_of_a_line,_so_it_needs_soft_breaks_=0D=0Aend_";

        private const string SampleTextQPhrase = "Subject=3A_caf=C3=A9_=3D_100=25=0D=0Atrailing_space_=0D=0Atrailing_tab=09=0D=0A=09_indented=0A"
            + "a_long_line_that_goes_on_and_on=2C_well_past_the_seventy-six_character_limit_of_a_line=2C_so_it_needs_soft_breaks_=0D=0Aend_";

        // Encodings that are equals signs not followed by two hexadecimal digits, or lower-case escapes; ref. RFC 2045 §6.7, rule 1.
        private const string EqualsSignAtEnd = "a=";
        private const string EqualsSignThenOneDigit = "a=4";
        private const string EqualsSignThenSpace = "a= b";
        private const string LowercaseEscapeOfEqualsSign = "=3d";
        private const string LowercaseEscapeOfLatin1Letter = "=e9";

        // ref. RFC 2047 §8: the examples of "Q" encoded text, and what they decode to.
        private const string Rfc2047Keld = "Keld_J=F8rn_Simonsen";
        private const string Rfc2047KeldText = "Keld J\x00F8rn Simonsen";
        private const string Rfc2047Andre = "Andr=E9";
        private const string Rfc2047AndreText = "Andr\x00E9";
        private const string Rfc2047Patrik = "Patrik_F=E4ltstr=F6m";
        private const string Rfc2047PatrikText = "Patrik F\x00E4ltstr\x00F6m";

        // Undefined values of the enumerations.
        private const MimeQuotedPrintableMode UndefinedMode = (MimeQuotedPrintableMode)4;
        private const MimeQuotedPrintableMode NegativeMode = (MimeQuotedPrintableMode)(-1);
        private const MimeQuotedPrintableEncoderOptions UndefinedEncoderOptions = (MimeQuotedPrintableEncoderOptions)2;
        private const MimeQuotedPrintableDecoderOptions UndefinedDecoderOptions = (MimeQuotedPrintableDecoderOptions)8;

        // The shortest data whose worst-case encoding, three characters for each byte, wouldn't fit in an int.
        private const int ShortestDataTooLongToEncode = 715827883;

        #region Test case factories

        private static IEnumerable<MimeQuotedPrintableMode> QModeValues()
        {
            yield return MimeQuotedPrintableMode.EncodedWordText;
            yield return MimeQuotedPrintableMode.EncodedWordComment;
            yield return MimeQuotedPrintableMode.EncodedWordPhrase;
        }

        public static IEnumerable<object[]> Modes()
        {
            foreach (MimeQuotedPrintableMode mode in Enum.GetValues(typeof(MimeQuotedPrintableMode)))
            {
                yield return new object[] { mode };
            }
        }

        public static IEnumerable<object[]> QModes()
        {
            foreach (var mode in QModeValues())
            {
                yield return new object[] { mode };
            }
        }

        // Each mode and encoder option, with the encoding of SampleText.
        private static IEnumerable<(MimeQuotedPrintableMode Mode, MimeQuotedPrintableEncoderOptions Options, string Encoded)> SampleEncodings()
        {
            yield return (Body, Text, SampleTextBody);
            yield return (Body, Binary, SampleTextBinaryBody);
            yield return (MimeQuotedPrintableMode.EncodedWordText, Text, SampleTextQ);
            yield return (MimeQuotedPrintableMode.EncodedWordText, Binary, SampleTextQ);
            yield return (MimeQuotedPrintableMode.EncodedWordComment, Text, SampleTextQ);
            yield return (MimeQuotedPrintableMode.EncodedWordComment, Binary, SampleTextQ);
            yield return (MimeQuotedPrintableMode.EncodedWordPhrase, Text, SampleTextQPhrase);
            yield return (MimeQuotedPrintableMode.EncodedWordPhrase, Binary, SampleTextQPhrase);
        }

        // Each mode, with the encoding of SampleText as text, and the UTF-8 that it decodes to.
        private static IEnumerable<(MimeQuotedPrintableMode Mode, string Encoded, byte[] Decoded)> SampleTextDecodings()
        {
            yield return (Body, SampleTextBody, Encoding.UTF8.GetBytes(SampleTextAsText));
            yield return (MimeQuotedPrintableMode.EncodedWordText, SampleTextQ, Encoding.UTF8.GetBytes(SampleText));
            yield return (MimeQuotedPrintableMode.EncodedWordComment, SampleTextQ, Encoding.UTF8.GetBytes(SampleText));
            yield return (MimeQuotedPrintableMode.EncodedWordPhrase, SampleTextQPhrase, Encoding.UTF8.GetBytes(SampleText));
        }

        // SampleText split into two at every point, in each mode and with each encoder option, with its encoding.
        public static IEnumerable<object[]> SampleTextSplits()
        {
            var bytes = Encoding.UTF8.GetBytes(SampleText);
            foreach (var (mode, options, encoded) in SampleEncodings())
            {
                for (var split = 0; split <= bytes.Length; split++)
                {
                    yield return new object[] { mode, options, bytes.AsSpan(0, split).ToArray(), bytes.AsSpan(split).ToArray(), encoded };
                }
            }
        }

        // SampleText, in each mode, with its encoding as text, and a destination too small to hold all of it.
        public static IEnumerable<object[]> SampleTextSmallDestinations()
        {
            var bytes = Encoding.UTF8.GetBytes(SampleText);
            foreach (var (mode, options, encoded) in SampleEncodings())
            {
                foreach (var destinationLength in new[] { 0, 1, 2, 3, 4, 5, 7, 75, 76, 77 })
                {
                    yield return new object[] { mode, options, bytes, destinationLength, encoded };
                }
            }
        }

        // The encoding of SampleText as text, in each mode, split into two at every point, with the data it decodes to.
        public static IEnumerable<object[]> EncodedSampleTextSplits()
        {
            foreach (var (mode, encoded, decoded) in SampleTextDecodings())
            {
                for (var split = 0; split <= encoded.Length; split++)
                {
                    yield return new object[] { mode, encoded.Substring(0, split), encoded.Substring(split), decoded };
                }
            }
        }

        // The encoding of SampleText as text, in each mode, with the data it decodes to, and a destination too small to hold all of it.
        public static IEnumerable<object[]> EncodedSampleTextSmallDestinations()
        {
            foreach (var (mode, encoded, decoded) in SampleTextDecodings())
            {
                foreach (var destinationLength in new[] { 0, 1, 2, 3, 5, 76 })
                {
                    yield return new object[] { mode, encoded, destinationLength, decoded };
                }
            }
        }

        // Soft line breaks and escapes, with what they decode to under every leniency, each split into two at every point.
        public static IEnumerable<object[]> SoftBreakAndEscapeSplits()
        {
            var values = new[]
            {
                ("a=\r\nb", "ab"),
                ("a= \t \r\nb", "ab"),
                ("a=\nb", "ab"),
                ("a=4Bc", "aKc"),
                ("a=4bc", "aKc"),
                ("a \t \r\nb", "a\r\nb"),
                ("a \t b", "a \t b"),
                ("a \t \nb", "a\nb"),
                ("a=4Gb", "a=4Gb"),
                (EqualsSignThenSpace, EqualsSignThenSpace),
                ("a=  \r\n", "a"),
                (EqualsSignAtEnd, EqualsSignAtEnd),
            };

            foreach (var (encoded, decoded) in values)
            {
                for (var split = 0; split <= encoded.Length; split++)
                {
                    yield return new object[] { encoded.Substring(0, split), encoded.Substring(split), Latin1Encoding.GetBytes(decoded) };
                }
            }
        }

        // A run of 76 spaces, the longest that RFC 2045 §6.7 rule 3 allows, before a letter, split into two at every point.
        public static IEnumerable<object[]> RunOf76WhitespaceSplits()
        {
            var encoded = new string(' ', 76) + "a";
            for (var split = 0; split <= encoded.Length; split++)
            {
                yield return new object[] { encoded.Substring(0, split), encoded.Substring(split), Latin1Encoding.GetBytes(encoded) };
            }
        }

        // Text-like data, from a seeded generator, so the same on every run: spaces, tabs, line breaks of every kind and octets that need
        // encoding. Each comes with each encoder option and the data it decodes to: the same for binary, and for text with each bare line
        // feed made a line break.
        public static IEnumerable<object[]> TextLikeData()
        {
            const string alphabet = "abcdefghij KLMNOP=\t\r\n.\x00E9\x0001_?";
            var random = new Random(405);
            for (var round = 0; round < 50; round++)
            {
                var length = random.Next(0, 400);
                var bytes = new byte[length];
                for (var index = 0; index < length; index++)
                {
                    bytes[index] = (byte)alphabet[random.Next(alphabet.Length)];
                }

                var canonical = new List<byte>();
                for (var index = 0; index < bytes.Length; index++)
                {
                    if (bytes[index] == '\n' && (index == 0 || bytes[index - 1] != '\r'))
                    {
                        canonical.Add((byte)'\r');
                    }

                    canonical.Add(bytes[index]);
                }

                yield return new object[] { bytes, Text, canonical.ToArray() };
                yield return new object[] { bytes, Binary, bytes };
            }
        }

        // Zero bytes of every length to 300, the worst case for the length of an encoding, in each mode.
        public static IEnumerable<object[]> WorstCaseLengths()
        {
            foreach (MimeQuotedPrintableMode mode in Enum.GetValues(typeof(MimeQuotedPrintableMode)))
            {
                for (var length = 0; length <= 300; length++)
                {
                    yield return new object[] { mode, length };
                }
            }
        }

        // Data that a decoder rejects, with the position of what it rejects, with no leniency and with every leniency.
        public static IEnumerable<object[]> InvalidData()
        {
            var values = new[]
            {
                ("a\rb", 2),
                ("a\r", 2),
                ("a=\rb", 3),
                ("a=\r", 3),
                ("a\x0001", 1),
                ("a\x007F", 1),
                ("caf\x00E9", 3),
                ("a\x20ACb", 1),
                ("a=\x0001", 2),
            };

            foreach (var (encoded, position) in values)
            {
                foreach (var options in new[] { Strict, EveryLeniency })
                {
                    yield return new object[] { encoded, options, position };
                }
            }
        }

        // Each RFC 2047 §8 example, in each "Q" context.
        public static IEnumerable<object[]> Rfc2047Examples()
        {
            var examples = new[]
            {
                (Rfc2047Keld, Rfc2047KeldText),
                (Rfc2047Andre, Rfc2047AndreText),
                (Rfc2047Patrik, Rfc2047PatrikText),
                ("a=5Fb", "a_b"),
                ("a=20b", "a b"),
            };

            foreach (var (encoded, text) in examples)
            {
                foreach (var mode in QModeValues())
                {
                    yield return new object[] { encoded, mode, text };
                }
            }
        }

        // Each "Q" context, with each encoder option.
        public static IEnumerable<object[]> QModesAndOptions()
        {
            foreach (var mode in QModeValues())
            {
                yield return new object[] { mode, Text };
                yield return new object[] { mode, Binary };
            }
        }

        // 200 letters, a line break and 100 more letters, in each "Q" context, with its encoding, which has no line breaks.
        public static IEnumerable<object[]> QLongData()
        {
            var data = new string('a', 200) + "\r\n" + new string('b', 100);
            var encoded = new string('a', 200) + "=0D=0A" + new string('b', 100);
            foreach (var mode in QModeValues())
            {
                yield return new object[] { mode, data, encoded };
            }
        }

        // Every octet, then whitespace before line breaks of each kind, in each mode.
        public static IEnumerable<object[]> EveryOctetInEachMode()
        {
            var bytes = new List<byte>();
            for (var value = 0; value <= byte.MaxValue; value++)
            {
                bytes.Add((byte)value);
            }

            bytes.AddRange(Latin1Encoding.GetBytes(" \r\n \n\r"));
            foreach (MimeQuotedPrintableMode mode in Enum.GetValues(typeof(MimeQuotedPrintableMode)))
            {
                yield return new object[] { mode, bytes.ToArray() };
            }
        }

        // Lines, as characters and as bytes, with whether each ends with a soft line break and the length of its data.
        public static IEnumerable<object[]> SoftLineBreakLines()
        {
            var values = new[]
            {
                ("abc=", true, 3),
                ("abc= \t", true, 3),
                ("abc =", true, 4),
                ("=", true, 0),
                ("abc", false, 3),
                ("abc=3D", false, 6),
                ("abc ", false, 4),
                ("", false, 0),
                // ref. RFC 2045 §6.7, the example of rule 5.
                (Rfc2045FirstLine, true, 15),
                (Rfc2045SecondLine, true, 20),
                (Rfc2045LastLine, false, 29),
            };

            foreach (var (line, endsWithSoftBreak, dataLength) in values)
            {
                yield return new object[] { line, Encoding.ASCII.GetBytes(line), endsWithSoftBreak, dataLength };
            }
        }

        public static TheoryData<Action> MembersWithInvalidArguments => new TheoryData<Action>
        {
            () => _ = new MimeQuotedPrintableEncoder(UndefinedMode),
            () => _ = new MimeQuotedPrintableEncoder(Body, UndefinedEncoderOptions),
            () => _ = new MimeQuotedPrintableDecoder(NegativeMode),
            () => _ = new MimeQuotedPrintableDecoder(Body, UndefinedDecoderOptions),
            () => MimeQuotedPrintable.Encode(ReadOnlySpan<byte>.Empty, UndefinedMode, Text),
            () => MimeQuotedPrintable.Decode(ReadOnlySpan<char>.Empty, Body, UndefinedDecoderOptions),
            () => MimeQuotedPrintable.GetMaxEncodedLength(-1, Body),
            () => MimeQuotedPrintable.GetMaxDecodedLength(-1),
        };

        #endregion

        #region RFC 2045 §6.7 examples

        [Fact]
        public void Decode_Rfc2045SoftLineBreakExample_ReturnsText()
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(Rfc2045Encoded.AsSpan(), Body, Strict);

            // assert
            testResult.Should().Equal(Latin1(Rfc2045Text));
        }

        [Fact]
        public void Encode_Rfc2045Example_WritesOneLine()
        {
            // arrange
            var data = Latin1(Rfc2045Text);

            // act
            var testResult = MimeQuotedPrintable.Encode(data, Body, Text);

            // assert
            testResult.Should().Be(Rfc2045Text);
        }

        [Theory]
        [InlineData("=3D", "=")]
        [InlineData("=0D=0A", LineBreak)]
        [InlineData("=00=FF", "\x0000\x00FF")]
        [InlineData("a=20\r\nb", "a \r\nb")]
        public void Decode_Rfc2045Rule1Escape_ReturnsOctet(string encoded, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, Strict);

            // assert
            testResult.Should().Equal(Latin1(expected));
        }

        [Theory]
        [InlineData("abc  \r\ndef", "abc\r\ndef")]
        [InlineData("abc\t \t\r\ndef", "abc\r\ndef")]
        [InlineData("abc  ", "abc")]
        [InlineData("a b\tc", "a b\tc")]
        [InlineData("a  =\r\nb", "a  b")]
        [InlineData("abc=  \r\ndef", "abcdef")]
        [InlineData("abc=\t\r\ndef", "abcdef")]
        public void Decode_Rfc2045Rule3Whitespace_RemovesTrailingWhitespace(string encoded, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, Strict);

            // assert
            testResult.Should().Equal(Latin1(expected));
        }

        #endregion

        #region Body encoding

        [Theory]
        [InlineData("a=b", "a=3Db")]
        [InlineData("caf\x00E9", "caf=E9")]
        [InlineData("tab\x01", "tab=01")]
        [InlineData("a b\tc", "a b\tc")]
        [InlineData("trailing ", "trailing=20")]
        [InlineData("trailing\t", "trailing=09")]
        [InlineData("trailing \r\nnext", "trailing=20\r\nnext")]
        [InlineData("trailing\t\nnext", "trailing=09\r\nnext")]
        [InlineData("line\nfeed", "line\r\nfeed")]
        [InlineData("bare\rreturn", "bare=0Dreturn")]
        [InlineData("bare\r", "bare=0D")]
        [InlineData("space \r", "space =0D")]
        [InlineData("\r\n\r\n", "\r\n\r\n")]
        [InlineData("", "")]
        public void Encode_Text_EncodesAsRfc2045Requires(string data, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.Encode(Latin1(data), Body, Text);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("a\r\nb", "a=0D=0Ab")]
        [InlineData("a\nb\r", "a=0Ab=0D")]
        [InlineData("a \r\n", "a =0D=0A")]
        [InlineData("end ", "end=20")]
        public void Encode_Binary_EncodesLineBreaks(string data, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.Encode(Latin1(data), Body, Binary);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(LineOf76Letters)]
        [InlineData(LineOf76Letters + "\r\nb")]
        public void Encode_LineOfExactly76Characters_HasNoSoftBreak(string data)
        {
            // act
            var testResult = MimeQuotedPrintable.Encode(Latin1(data), Body, Text);

            // assert
            testResult.Should().Be(data);
        }

        [Fact]
        public void Encode_LineOf77Characters_BreaksAfter75()
        {
            // act
            var testResult = MimeQuotedPrintable.Encode(Latin1(LineOf77Letters), Body, Text);

            // assert
            testResult.Should().Be(LineOf75Letters + SoftLineBreak + "aa");
        }

        // Lines of letters and a trailing space before a line break, with their encodings: the space is encoded as =20, which goes on the next
        // line when it wouldn't fit in 76 characters.
        public static TheoryData<string, string> TrailingSpaceAtEndOfFullLine => new TheoryData<string, string>
        {
            { LineOf73Letters + " \r\nb", LineOf73Letters + "=20\r\nb" },
            { LineOf74Letters + " \r\nb", LineOf74Letters + SoftLineBreak + "=20\r\nb" },
            { LineOf75Letters + " \r\nb", LineOf75Letters + SoftLineBreak + "=20\r\nb" },
        };

        [Theory]
        [MemberData(nameof(TrailingSpaceAtEndOfFullLine))]
        public void Encode_TrailingSpaceAtEndOfFullLine_IsEncodedWithinLimit(string data, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.Encode(Latin1(data), Body, Text);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Encode_SpaceBeforeSoftBreak_IsWrittenAsItself()
        {
            // act
            var testResult = MimeQuotedPrintable.Encode(Latin1(LineOf74Letters + " bc"), Body, Text);

            // assert
            testResult.Should().Be(LineOf74Letters + " =\r\nbc");
        }

        // 30 zero bytes: 25 escapes and a soft line break fill the first line, as an escape can't be split, and five escapes are left.
        private const int ThirtyBytes = 30;
        private const string TwentyFiveZeroEscapes = "=00=00=00=00=00=00=00=00=00=00=00=00=00=00=00=00=00=00=00=00=00=00=00=00=00";
        private const string FiveZeroEscapes = "=00=00=00=00=00";

        [Fact]
        public void Encode_EscapesAtLineEnd_DontSplitAnEscape()
        {
            // act
            var testResult = MimeQuotedPrintable.Encode(new byte[ThirtyBytes], Body, Text);

            // assert
            testResult.Should().Be(TwentyFiveZeroEscapes + SoftLineBreak + FiveZeroEscapes);
        }

        // ref. RFC 2045 §6.7: lines of no more than 76 printable characters or tabs, separated by line breaks, with no whitespace at the end.
        private const string ValidLines = @"^(?:[\t\x20-\x7E]{0,75}[\x21-\x7E])?(?:\r\n(?:[\t\x20-\x7E]{0,75}[\x21-\x7E])?)*$";

        [Theory]
        [MemberData(nameof(TextLikeData))]
        public void Encode_AnyData_WritesValidLines(byte[] data, MimeQuotedPrintableEncoderOptions options, byte[] decoded)
        {
            // act
            var testResult = MimeQuotedPrintable.Encode(data, Body, options);

            // assert
            testResult.Should().MatchRegex(ValidLines, "it decodes to {0} bytes", decoded.Length);
        }

        [Theory]
        [MemberData(nameof(TextLikeData))]
        public void Decode_EncodedAnyData_ReturnsData(byte[] data, MimeQuotedPrintableEncoderOptions options, byte[] decoded)
        {
            // arrange
            var encoded = MimeQuotedPrintable.Encode(data, Body, options);

            // act
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, Strict);

            // assert
            testResult.Should().Equal(decoded);
        }

        [Theory]
        [MemberData(nameof(TextLikeData))]
        public void GetMaxEncodedLength_AnyData_IsEnoughForTryEncode(byte[] data, MimeQuotedPrintableEncoderOptions options, byte[] decoded)
        {
            // arrange
            var destination = new char[MimeQuotedPrintable.GetMaxEncodedLength(data.Length, Body)];

            // act
            var testResult = MimeQuotedPrintable.TryEncode(data, destination, out _, Body, options);

            // assert
            testResult.Should().BeTrue("it decodes to {0} bytes", decoded.Length);
        }

        [Theory]
        [MemberData(nameof(WorstCaseLengths))]
        public void GetMaxEncodedLength_WorstCase_IsEnoughForTryEncode(MimeQuotedPrintableMode mode, int length)
        {
            // arrange
            var data = new byte[length];
            var destination = new char[MimeQuotedPrintable.GetMaxEncodedLength(length, mode)];

            // act
            var testResult = MimeQuotedPrintable.TryEncode(data, destination, out _, mode, Text);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void GetMaxEncodedLength_TooLong_ThrowsArgumentOutOfRangeException()
        {
            // act
            Action testAction = () => MimeQuotedPrintable.GetMaxEncodedLength(ShortestDataTooLongToEncode, Body);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [MemberData(nameof(SampleTextSplits))]
        public void EncodeToChars_SplitAtAnyPoint_MatchesOneCall(MimeQuotedPrintableMode mode, MimeQuotedPrintableEncoderOptions options,
            byte[] first, byte[] second, string expected)
        {
            // arrange
            var encoder = new MimeQuotedPrintableEncoder(mode, options);
            var destination = new char[expected.Length];
            encoder.Encode(first, destination, out _, out var firstWritten, false);

            // act
            var testResult = encoder.Encode(second, destination.AsSpan(firstWritten), out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(second.Length);
            new string(destination).Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(SampleTextSplits))]
        public void EncodeToUtf8_SplitAtAnyPoint_MatchesOneCall(MimeQuotedPrintableMode mode, MimeQuotedPrintableEncoderOptions options,
            byte[] first, byte[] second, string expected)
        {
            // arrange
            var encoder = new MimeQuotedPrintableEncoder(mode, options);
            var destination = new byte[expected.Length];
            encoder.Encode(first, destination, out _, out var firstWritten, false);

            // act
            var testResult = encoder.Encode(second, destination.AsSpan(firstWritten), out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(second.Length);
            Encoding.ASCII.GetString(destination).Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(SampleTextSmallDestinations))]
        public void EncodeToChars_AfterDestinationTooSmall_ContinuesWhereItStopped(MimeQuotedPrintableMode mode,
            MimeQuotedPrintableEncoderOptions options, byte[] data, int destinationLength, string expected)
        {
            // arrange
            var encoder = new MimeQuotedPrintableEncoder(mode, options);
            var destination = new char[expected.Length];
            encoder.Encode(data, destination.AsSpan(0, destinationLength), out var firstConsumed, out var firstWritten, true);

            // act
            var testResult = encoder.Encode(data.AsSpan(firstConsumed), destination.AsSpan(firstWritten), out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            new string(destination).Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(SampleTextSmallDestinations))]
        public void EncodeToUtf8_AfterDestinationTooSmall_ContinuesWhereItStopped(MimeQuotedPrintableMode mode,
            MimeQuotedPrintableEncoderOptions options, byte[] data, int destinationLength, string expected)
        {
            // arrange
            var encoder = new MimeQuotedPrintableEncoder(mode, options);
            var destination = new byte[expected.Length];
            encoder.Encode(data, destination.AsSpan(0, destinationLength), out var firstConsumed, out var firstWritten, true);

            // act
            var testResult = encoder.Encode(data.AsSpan(firstConsumed), destination.AsSpan(firstWritten), out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            Encoding.ASCII.GetString(destination).Should().Be(expected);
        }

        // Data whose end the encoder can't write until it knows what follows: a space, which is encoded only before a line break, and a
        // carriage return, which is a line break only before a line feed.
        private const string DataEndingWithSpace = "ab ";
        private const string CarriageReturn = "\r";
        private const string LineFeedAndLetter = "\nc";
        private const int DestinationLength = 16;

        // What the encoder writes of DataEndingWithSpace before it knows what follows, and what it writes of the space once a line break
        // follows it.
        private const string LettersBeforeSpace = "ab";
        private const string EncodedSpaceLineBreakAndLetter = "=20\r\nc";

        [Fact]
        public void Encode_TrailingSpace_HoldsIt()
        {
            // arrange
            var encoder = new MimeQuotedPrintableEncoder(Body);
            var destination = new char[LettersBeforeSpace.Length];

            // act
            var testResult = encoder.Encode(Latin1(DataEndingWithSpace), destination, out var consumed, out _, false);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(DataEndingWithSpace.Length);
            new string(destination).Should().Be(LettersBeforeSpace);
        }

        [Fact]
        public void Encode_CarriageReturnAfterHeldSpace_HoldsIt()
        {
            // arrange
            var encoder = new MimeQuotedPrintableEncoder(Body);
            encoder.Encode(Latin1(DataEndingWithSpace), new char[DestinationLength], out _, out _, false);

            // act
            var testResult = encoder.Encode(Latin1(CarriageReturn), new char[DestinationLength], out var consumed, out var written, false);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(CarriageReturn.Length);
            written.Should().Be(0);
        }

        [Fact]
        public void Encode_LineFeedAfterHeldSpaceAndCarriageReturn_EncodesTheSpaceBeforeTheLineBreak()
        {
            // arrange
            var encoder = new MimeQuotedPrintableEncoder(Body);
            encoder.Encode(Latin1(DataEndingWithSpace), new char[DestinationLength], out _, out _, false);
            encoder.Encode(Latin1(CarriageReturn), new char[DestinationLength], out _, out _, false);
            var destination = new char[EncodedSpaceLineBreakAndLetter.Length];

            // act
            var testResult = encoder.Encode(Latin1(LineFeedAndLetter), destination, out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(LineFeedAndLetter.Length);
            new string(destination).Should().Be(EncodedSpaceLineBreakAndLetter);
        }

        // Room for "a", but not the three characters of the escape of "=" that follows it.
        private const int RoomForOneLetterAndPartOfEscape = 2;

        [Fact]
        public void Encode_DestinationTooSmall_WritesWholeTokens()
        {
            // arrange
            var encoder = new MimeQuotedPrintableEncoder(Body);
            var destination = new char[RoomForOneLetterAndPartOfEscape];

            // act
            var testResult = encoder.Encode(Latin1(EqualsSignAtEnd), destination, out var consumed, out var written, true);

            // assert
            testResult.Should().Be(OperationStatus.DestinationTooSmall);
            consumed.Should().Be(1);
            written.Should().Be(1);
            destination[0].Should().Be('a');
        }

        [Fact]
        public void Encode_AfterFinalBlock_StartsNewData()
        {
            // arrange
            var encoder = new MimeQuotedPrintableEncoder(Body);
            var data = Latin1(LineOf70Letters);
            encoder.Encode(data, new char[LineOf70Letters.Length], out _, out _, true);
            var destination = new char[LineOf70Letters.Length];

            // act
            var testResult = encoder.Encode(data, destination, out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            new string(destination).Should().Be(LineOf70Letters);
        }

        [Fact]
        public void Reset_Encoder_DiscardsHeldBytesAndLineLength()
        {
            // arrange
            var encoder = new MimeQuotedPrintableEncoder(Body);
            encoder.Encode(Latin1(LineOf70Letters + " "), new char[LineOf70Letters.Length], out _, out _, false);
            encoder.Reset();
            var destination = new char[SevenLetters.Length];

            // act
            var testResult = encoder.Encode(Latin1(SevenLetters), destination, out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            new string(destination).Should().Be(SevenLetters);
        }

        // "a=b" encodes to the five characters "a=3Db", one more than this.
        private const int OneTooFewForEncodedEqualsSign = 4;

        [Fact]
        public void TryEncode_DestinationTooShort_ReturnsFalse()
        {
            // act
            var testResult = MimeQuotedPrintable.TryEncode(Latin1("a=b"), new char[OneTooFewForEncodedEqualsSign], out var charsWritten, Body,
                Text);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        #endregion

        #region Body decoding

        public static TheoryData<string, byte> LowercaseEscapes => new TheoryData<string, byte>
        {
            { LowercaseEscapeOfEqualsSign, 0x3D },
            { "=a0", 0xA0 },
            { "=Fe", 0xFE },
        };

        [Theory]
        [MemberData(nameof(LowercaseEscapes))]
        public void TryDecode_LowercaseHexDigitsByDefault_ReturnsFalse(string encoded, byte octet)
        {
            // act
            var testResult = MimeQuotedPrintable.TryDecode(encoded.AsSpan(), Body, Strict, out _);

            // assert
            testResult.Should().BeFalse("it would be {0}", octet);
        }

        [Theory]
        [MemberData(nameof(LowercaseEscapes))]
        public void Decode_LowercaseHexDigitsAllowed_ReturnsOctet(string encoded, byte octet)
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, Lowercase);

            // assert
            testResult.Should().Equal(octet);
        }

        public static TheoryData<string, string> BareLineFeedData => new TheoryData<string, string>
        {
            { "a\nb", "a\nb" },
            { "a \t\nb", "a\nb" },
            { "a=\nb", "ab" },
            { "a= \nb", "ab" },
            { "a\r\nb\nc", "a\r\nb\nc" },
        };

        [Theory]
        [MemberData(nameof(BareLineFeedData))]
        public void TryDecode_BareLineFeedByDefault_ReturnsFalse(string encoded, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.TryDecode(encoded.AsSpan(), Body, Strict, out _);

            // assert
            testResult.Should().BeFalse("it would be {0}", expected);
        }

        [Theory]
        [MemberData(nameof(BareLineFeedData))]
        public void Decode_BareLineFeedAllowed_TreatsItAsALineBreak(string encoded, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, BareLineFeeds);

            // assert
            testResult.Should().Equal(Latin1(expected));
        }

        public static TheoryData<string, string> EqualsSignsNotFollowedByTwoHexDigits => new TheoryData<string, string>
        {
            { "a=G1", "a=G1" },
            { "a=4G", "a=4G" },
            { "a=4=41", "a=4A" },
            { EqualsSignThenSpace, EqualsSignThenSpace },
            { "a=  b  \r\nc", "a=  b\r\nc" },
            { EqualsSignAtEnd, EqualsSignAtEnd },
            { EqualsSignThenOneDigit, EqualsSignThenOneDigit },
            { "a= ", EqualsSignAtEnd },
            { "a==41", "a=A" },
        };

        [Theory]
        [MemberData(nameof(EqualsSignsNotFollowedByTwoHexDigits))]
        public void TryDecode_EqualsSignNotFollowedByTwoHexDigitsByDefault_ReturnsFalse(string encoded, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.TryDecode(encoded.AsSpan(), Body, Strict, out _);

            // assert
            testResult.Should().BeFalse("it would be {0}", expected);
        }

        [Theory]
        [MemberData(nameof(EqualsSignsNotFollowedByTwoHexDigits))]
        public void Decode_EqualsSignNotFollowedByTwoHexDigitsAllowed_IsLiteral(string encoded, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, LiteralEquals);

            // assert
            testResult.Should().Equal(Latin1(expected));
        }

        [Fact]
        public void Decode_LowercaseWithoutItsOption_IsLiteralUnderLiteralEqualsSigns()
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(LowercaseEscapeOfEqualsSign.AsSpan(), Body, LiteralEquals);

            // assert
            testResult.Should().Equal(Latin1(LowercaseEscapeOfEqualsSign));
        }

        // More than any of the InvalidData values decode to.
        private const int AnyDestinationLength = 16;

        [Theory]
        [MemberData(nameof(InvalidData))]
        public void TryDecode_InvalidData_ReturnsFalse(string encoded, MimeQuotedPrintableDecoderOptions options, int position)
        {
            // act
            var testResult = MimeQuotedPrintable.TryDecode(encoded.AsSpan(), Body, options, out var result);

            // assert
            testResult.Should().BeFalse("the character at {0} is invalid", position);
            result.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(InvalidData))]
        public void Decode_InvalidData_ReportsPosition(string encoded, MimeQuotedPrintableDecoderOptions options, int position)
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body, options);

            // act
            var testResult = decoder.Decode(encoded.AsSpan(), new byte[AnyDestinationLength], out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(position);
        }

        [Fact]
        public void Decode_Utf8Bytes_RejectsOctetsAbove126()
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body);

            // act
            // "a" and the UTF-8 of U+00E9, whose octets are above 126, so can't appear in quoted-printable; ref. RFC 2045 §6.7, rule 2.
            var testResult = decoder.Decode(new byte[] { 0x61, 0xC3, 0xA9 }, new byte[AnyDestinationLength], out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(1);
        }

        [Theory]
        [MemberData(nameof(RunOf76WhitespaceSplits))]
        public void Decode_RunOf76WhitespaceSplitAtAnyPoint_IsAccepted(string first, string second, byte[] expected)
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body);
            var destination = new byte[expected.Length];
            decoder.Decode(first.AsSpan(), destination, out _, out var firstWritten, false);

            // act
            var testResult = decoder.Decode(second.AsSpan(), destination.AsSpan(firstWritten), out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            destination.Should().Equal(expected);
        }

        // 77 tabs, one more than RFC 2045 §6.7 rule 3 allows in a run of whitespace, before a line break; the decoder rejects the 77th.
        private const string RunOf77Tabs = "\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t"
            + "\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\r\n";
        private const int PositionOf77thTab = 76;

        [Fact]
        public void Decode_RunOfMoreThan76Whitespace_IsRejected()
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body);

            // act
            var testResult = decoder.Decode(RunOf77Tabs.AsSpan(), new byte[RunOf77Tabs.Length], out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(PositionOf77thTab);
        }

        // A line longer than 76 characters, which RFC 2045 §6.7 rule 5 doesn't allow an encoder to write, but which a decoder accepts.
        private const string LineOf200Letters = LineOf76Letters + LineOf76Letters + "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        [Fact]
        public void Decode_LongLine_IsAccepted()
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(LineOf200Letters.AsSpan(), Body, Strict);

            // assert
            testResult.Should().Equal(Latin1(LineOf200Letters));
        }

        [Theory]
        [MemberData(nameof(EncodedSampleTextSplits))]
        public void DecodeChars_SplitAtAnyPoint_MatchesOneCall(MimeQuotedPrintableMode mode, string first, string second, byte[] expected)
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(mode);
            var destination = new byte[expected.Length];
            decoder.Decode(first.AsSpan(), destination, out _, out var firstWritten, false);

            // act
            var testResult = decoder.Decode(second.AsSpan(), destination.AsSpan(firstWritten), out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(second.Length);
            destination.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(EncodedSampleTextSplits))]
        public void DecodeUtf8_SplitAtAnyPoint_MatchesOneCall(MimeQuotedPrintableMode mode, string first, string second, byte[] expected)
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(mode);
            var firstBytes = Encoding.ASCII.GetBytes(first);
            var secondBytes = Encoding.ASCII.GetBytes(second);
            var destination = new byte[expected.Length];
            decoder.Decode(firstBytes, destination, out _, out var firstWritten, false);

            // act
            var testResult = decoder.Decode(secondBytes, destination.AsSpan(firstWritten), out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(secondBytes.Length);
            destination.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(SoftBreakAndEscapeSplits))]
        public void DecodeChars_SoftBreakAndEscapeSplitAtAnyPoint_MatchesOneCall(string first, string second, byte[] expected)
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body, EveryLeniency);
            var destination = new byte[expected.Length];
            decoder.Decode(first.AsSpan(), destination, out _, out var firstWritten, false);

            // act
            var testResult = decoder.Decode(second.AsSpan(), destination.AsSpan(firstWritten), out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(second.Length);
            destination.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(SoftBreakAndEscapeSplits))]
        public void DecodeUtf8_SoftBreakAndEscapeSplitAtAnyPoint_MatchesOneCall(string first, string second, byte[] expected)
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body, EveryLeniency);
            var firstBytes = Encoding.ASCII.GetBytes(first);
            var secondBytes = Encoding.ASCII.GetBytes(second);
            var destination = new byte[expected.Length];
            decoder.Decode(firstBytes, destination, out _, out var firstWritten, false);

            // act
            var testResult = decoder.Decode(secondBytes, destination.AsSpan(firstWritten), out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(secondBytes.Length);
            destination.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(EncodedSampleTextSmallDestinations))]
        public void DecodeChars_AfterDestinationTooSmall_ContinuesWhereItStopped(MimeQuotedPrintableMode mode, string encoded,
            int destinationLength, byte[] expected)
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(mode);
            var destination = new byte[expected.Length];
            decoder.Decode(encoded.AsSpan(), destination.AsSpan(0, destinationLength), out var firstConsumed, out var firstWritten, true);

            // act
            var testResult = decoder.Decode(encoded.AsSpan(firstConsumed), destination.AsSpan(firstWritten), out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            destination.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(EncodedSampleTextSmallDestinations))]
        public void DecodeUtf8_AfterDestinationTooSmall_ContinuesWhereItStopped(MimeQuotedPrintableMode mode, string encoded,
            int destinationLength, byte[] expected)
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(mode);
            var source = Encoding.ASCII.GetBytes(encoded);
            var destination = new byte[expected.Length];
            decoder.Decode(source, destination.AsSpan(0, destinationLength), out var firstConsumed, out var firstWritten, true);

            // act
            var testResult = decoder.Decode(source.AsSpan(firstConsumed), destination.AsSpan(firstWritten), out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            destination.Should().Equal(expected);
        }

        [Fact]
        public void Decode_PartialEscape_HoldsItUntilNextCall()
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body);
            var destination = new byte[AnyDestinationLength];

            // act
            var testResult = decoder.Decode(EqualsSignThenOneDigit.AsSpan(), destination, out var consumed, out var written, false);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(EqualsSignThenOneDigit.Length);
            written.Should().Be(1);
        }

        [Fact]
        public void Decode_RestOfHeldEscape_WritesItsOctet()
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body);
            decoder.Decode(EqualsSignThenOneDigit.AsSpan(), new byte[AnyDestinationLength], out _, out _, false);
            var destination = new byte[1];

            // act
            var testResult = decoder.Decode("1".AsSpan(), destination, out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(1);
            destination.Should().Equal((byte)'A');
        }

        [Fact]
        public void Decode_TruncatedEscape_IsInvalidAtFinalBlock()
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body);
            decoder.Decode(EqualsSignThenOneDigit.AsSpan(), new byte[AnyDestinationLength], out _, out _, false);

            // act
            var testResult = decoder.Decode(ReadOnlySpan<char>.Empty, new byte[AnyDestinationLength], out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(0);
        }

        // "ab" and an equals sign and digit that are literal under LiteralEquals, which don't fit in a destination of two bytes.
        private const string LettersThenLiteralEqualsSign = "ab=4";
        private const int RoomForTwoLetters = 2;

        [Fact]
        public void Decode_DestinationTooSmallAtFinalBlock_ReturnsDestinationTooSmall()
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body, LiteralEquals);

            // act
            var testResult = decoder.Decode(LettersThenLiteralEqualsSign.AsSpan(), new byte[RoomForTwoLetters], out var consumed, out var written,
                true);

            // assert
            testResult.Should().Be(OperationStatus.DestinationTooSmall);
            consumed.Should().Be(LettersThenLiteralEqualsSign.Length);
            written.Should().Be(RoomForTwoLetters);
        }

        [Fact]
        public void Decode_AfterDestinationTooSmallAtFinalBlock_WritesTheRest()
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body, LiteralEquals);
            decoder.Decode(LettersThenLiteralEqualsSign.AsSpan(), new byte[RoomForTwoLetters], out _, out _, true);
            var destination = new byte[RoomForTwoLetters];

            // act
            var testResult = decoder.Decode(ReadOnlySpan<char>.Empty, destination, out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            destination.Should().Equal(Latin1("=4"));
        }

        [Fact]
        public void Reset_Decoder_DiscardsHeldData()
        {
            // arrange
            var decoder = new MimeQuotedPrintableDecoder(Body);
            decoder.Decode("a  =4".AsSpan(), new byte[AnyDestinationLength], out _, out _, false);
            decoder.Reset();
            var destination = new byte[1];

            // act
            var testResult = decoder.Decode("b".AsSpan(), destination, out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            destination.Should().Equal((byte)'b');
        }

        #endregion

        #region RFC 2047 "Q"

        [Theory]
        [MemberData(nameof(Rfc2047Examples))]
        public void Decode_Rfc2047Example_ReturnsText(string encoded, MimeQuotedPrintableMode mode, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), mode, Strict);

            // assert
            testResult.Should().Equal(Latin1(expected));
        }

        [Theory]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "a_b=3D=3F=5F()\".,=09=0D=0A")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordComment, "a_b=3D=3F=5F=28=29=22.,=09=0D=0A")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordPhrase, "a_b=3D=3F=5F=28=29=22=2E=2C=09=0D=0A")]
        public void Encode_QContext_EncodesWhatTheContextRequires(MimeQuotedPrintableMode mode, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.Encode(Latin1("a b=?_()\".,\t\r\n"), mode, Text);

            // assert
            testResult.Should().Be(expected);
        }

        // ref. RFC 2047 §5(3): the characters that a "Q" encoded word may hold as themselves in a phrase.
        private const string PhraseSafeCharacters = "ABCXYZabcxyz0189!*+-/";

        [Theory]
        [MemberData(nameof(QModesAndOptions))]
        public void Encode_QPhraseCharacters_AreLiteralInEveryContext(MimeQuotedPrintableMode mode, MimeQuotedPrintableEncoderOptions options)
        {
            // act
            var testResult = MimeQuotedPrintable.Encode(Latin1(PhraseSafeCharacters), mode, options);

            // assert
            testResult.Should().Be(PhraseSafeCharacters);
        }

        [Theory]
        [MemberData(nameof(QLongData))]
        public void Encode_QLongData_HasNoLineBreaks(MimeQuotedPrintableMode mode, string data, string expected)
        {
            // act
            var testResult = MimeQuotedPrintable.Encode(Latin1(data), mode, Text);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "a b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "a?b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "a\tb")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "a=\r\nb")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "a\x00E9")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordComment, "a(b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordComment, "a)b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordComment, "a\"b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordPhrase, "a.b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordPhrase, "a,b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordPhrase, "a(b")]
        public void Decode_QCharacterTheContextDoesNotAllow_IsRejected(MimeQuotedPrintableMode mode, string encoded)
        {
            // act
            var testResult = MimeQuotedPrintable.TryDecode(encoded.AsSpan(), mode, Lowercase | BareLineFeeds, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("a(b)\"c.,")]
        [InlineData("[x]<y>@;:\\")]
        public void Decode_QTextContext_AcceptsPrintableCharacters(string encoded)
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), MimeQuotedPrintableMode.EncodedWordText, Strict);

            // assert
            testResult.Should().Equal(Latin1(encoded));
        }

        [Theory]
        [MemberData(nameof(QModes))]
        public void TryDecode_QLowercaseEscapeByDefault_ReturnsFalse(MimeQuotedPrintableMode mode)
        {
            // act
            var testResult = MimeQuotedPrintable.TryDecode(LowercaseEscapeOfLatin1Letter.AsSpan(), mode, Strict, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(QModes))]
        public void Decode_QLowercaseEscapeAllowed_ReturnsOctet(MimeQuotedPrintableMode mode)
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(LowercaseEscapeOfLatin1Letter.AsSpan(), mode, Lowercase);

            // assert
            testResult.Should().Equal(0xE9);
        }

        [Theory]
        [MemberData(nameof(QModes))]
        public void TryDecode_QEqualsSignAtEndByDefault_ReturnsFalse(MimeQuotedPrintableMode mode)
        {
            // act
            var testResult = MimeQuotedPrintable.TryDecode(EqualsSignAtEnd.AsSpan(), mode, Strict, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(QModes))]
        public void Decode_QEqualsSignAtEndAllowed_IsLiteral(MimeQuotedPrintableMode mode)
        {
            // act
            var testResult = MimeQuotedPrintable.Decode(EqualsSignAtEnd.AsSpan(), mode, LiteralEquals);

            // assert
            testResult.Should().Equal(Latin1(EqualsSignAtEnd));
        }

        #endregion

        #region Round trips

        // Text in several scripts, with an emoji and characters that need encoding.
        private const string Utf8Text = "Gr\x00FC\x00DFe aus Z\x00FCrich \x2014 \x65E5\x672C\x8A9E \xD83D\xDE00 = 100% _under_score? (yes)";

        // ISO-8859-1 text, with characters that need encoding and a space at the end.
        private const string Latin1Text = "Caf\x00E9 cr\x00E8me, se\x00F1or, \x00A9 \x00BD \x00FF and plain ASCII at the end ";

        [Theory]
        [MemberData(nameof(Modes))]
        public void Decode_EncodedUtf8Text_ReturnsText(MimeQuotedPrintableMode mode)
        {
            // arrange
            var encoded = MimeQuotedPrintable.Encode(Encoding.UTF8.GetBytes(Utf8Text), mode, Text);

            // act
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), mode, Strict);

            // assert
            testResult.Should().Equal(Encoding.UTF8.GetBytes(Utf8Text));
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void Decode_EncodedLatin1Text_ReturnsText(MimeQuotedPrintableMode mode)
        {
            // arrange
            var encoded = MimeQuotedPrintable.Encode(Latin1(Latin1Text), mode, Text);

            // act
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), mode, Strict);

            // assert
            testResult.Should().Equal(Latin1(Latin1Text));
        }

        [Theory]
        [MemberData(nameof(EveryOctetInEachMode))]
        public void Decode_EveryOctetEncodedAsBinary_ReturnsData(MimeQuotedPrintableMode mode, byte[] data)
        {
            // arrange
            var encoded = MimeQuotedPrintable.Encode(data, mode, Binary);

            // act
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), mode, Strict);

            // assert
            testResult.Should().Equal(data);
        }

        #endregion

        #region Soft line breaks

        [Theory]
        [MemberData(nameof(SoftLineBreakLines))]
        public void EndsWithSoftLineBreak_LineOfCharacters_FindsSoftBreak(string line, byte[] lineBytes, bool expected, int expectedLength)
        {
            // act
            var testResult = MimeQuotedPrintable.EndsWithSoftLineBreak(line.AsSpan(), out var length);

            // assert
            testResult.Should().Be(expected, "the line is {0} bytes", lineBytes.Length);
            length.Should().Be(expectedLength);
        }

        [Theory]
        [MemberData(nameof(SoftLineBreakLines))]
        public void EndsWithSoftLineBreak_LineOfBytes_FindsSoftBreak(string line, byte[] lineBytes, bool expected, int expectedLength)
        {
            // act
            var testResult = MimeQuotedPrintable.EndsWithSoftLineBreak(lineBytes, out var length);

            // assert
            testResult.Should().Be(expected, "the line is {0}", line);
            length.Should().Be(expectedLength);
        }

        #endregion

        #region Arguments

        [Theory]
        [MemberData(nameof(MembersWithInvalidArguments))]
        public void Members_InvalidArguments_ThrowArgumentOutOfRangeException(Action member)
        {
            // act
            var testAction = member;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void EncoderConstructor_ModeAndOptions_SetsProperties()
        {
            // act
            var testResult = new MimeQuotedPrintableEncoder(MimeQuotedPrintableMode.EncodedWordPhrase, Binary);

            // assert
            testResult.Mode.Should().Be(MimeQuotedPrintableMode.EncodedWordPhrase);
            testResult.Options.Should().Be(Binary);
        }

        [Fact]
        public void DecoderConstructor_ModeAndOptions_SetsProperties()
        {
            // act
            var testResult = new MimeQuotedPrintableDecoder(Body, Lowercase | LiteralEquals);

            // assert
            testResult.Mode.Should().Be(Body);
            testResult.Options.Should().Be(Lowercase | LiteralEquals);
        }

        [Fact]
        public void Decode_InvalidData_ThrowsFormatException()
        {
            // act
            Action testAction = () => MimeQuotedPrintable.Decode("=XY".AsSpan(), Body, Strict);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        #endregion

        private static byte[] Latin1(string value)
        {
            return Latin1Encoding.GetBytes(value);
        }
    }
}
