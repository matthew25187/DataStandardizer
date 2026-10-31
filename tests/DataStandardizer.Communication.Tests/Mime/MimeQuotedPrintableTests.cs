using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
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

        // ISO-8859-1. Encoding.Latin1 is .NET 5 or later, but every runtime has code page 28591 built in.
        private static readonly Encoding Latin1Encoding = Encoding.GetEncoding(28591);

        // ref. RFC 2045 §6.7, the example of rule 5.
        private const string Rfc2045Text = "Now's the time for all folk to come to the aid of their country.";
        private const string Rfc2045Encoded = "Now's the time =\r\nfor all folk to come=\r\n to the aid of their country.";

        // Text with line breaks, trailing whitespace, characters that need encoding and a line longer than 76 characters.
        private const string SampleText = "Subject: café = 100%\r\n"
            + "trailing space \r\n"
            + "trailing tab\t\r\n"
            + "\t indented\n"
            + "a long line that goes on and on, well past the seventy-six character limit of a line, so it needs soft breaks \r\n"
            + "end ";

        public static IEnumerable<object[]> Modes()
        {
            foreach (MimeQuotedPrintableMode mode in Enum.GetValues(typeof(MimeQuotedPrintableMode)))
            {
                yield return new object[] { mode };
            }
        }

        public static IEnumerable<object[]> QModes()
        {
            yield return new object[] { MimeQuotedPrintableMode.EncodedWordText };
            yield return new object[] { MimeQuotedPrintableMode.EncodedWordComment };
            yield return new object[] { MimeQuotedPrintableMode.EncodedWordPhrase };
        }

        #region RFC 2045 §6.7 examples

        [Fact]
        public void Decode_Rfc2045SoftLineBreakExample_ReturnsText()
        {
            var testResult = MimeQuotedPrintable.Decode(Rfc2045Encoded.AsSpan(), Body, Strict);

            Encoding.ASCII.GetString(testResult).Should().Be(Rfc2045Text);
        }

        [Fact]
        public void Encode_Rfc2045Example_WritesOneLine()
        {
            var testResult = MimeQuotedPrintable.Encode(Encoding.ASCII.GetBytes(Rfc2045Text), Body, Text);

            testResult.Should().Be(Rfc2045Text);
        }

        [Theory]
        [InlineData("=3D", "=")]
        [InlineData("=0D=0A", "\r\n")]
        [InlineData("=00=FF", "\u0000ÿ")]
        [InlineData("a=20\r\nb", "a \r\nb")]
        public void Decode_Rfc2045Rule1Escape_ReturnsOctet(string encoded, string expected)
        {
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, Strict);

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
            var testResult = MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, Strict);

            Encoding.ASCII.GetString(testResult).Should().Be(expected);
        }

        #endregion

        #region Body encoding

        [Theory]
        [InlineData("a=b", "a=3Db")]
        [InlineData("café", "caf=E9")]
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
            var testResult = MimeQuotedPrintable.Encode(Latin1(data), Body, Text);

            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("a\r\nb", "a=0D=0Ab")]
        [InlineData("a\nb\r", "a=0Ab=0D")]
        [InlineData("a \r\n", "a =0D=0A")]
        [InlineData("end ", "end=20")]
        public void Encode_Binary_EncodesLineBreaks(string data, string expected)
        {
            var testResult = MimeQuotedPrintable.Encode(Latin1(data), Body, Binary);

            testResult.Should().Be(expected);
        }

        [Fact]
        public void Encode_LineOfExactly76Characters_HasNoSoftBreak()
        {
            var line = new string('a', 76);

            MimeQuotedPrintable.Encode(Latin1(line), Body, Text).Should().Be(line);
            MimeQuotedPrintable.Encode(Latin1(line + "\r\nb"), Body, Text).Should().Be(line + "\r\nb");
        }

        [Fact]
        public void Encode_LineOf77Characters_BreaksAfter75()
        {
            var testResult = MimeQuotedPrintable.Encode(Latin1(new string('a', 77)), Body, Text);

            testResult.Should().Be(new string('a', 75) + "=\r\naa");
        }

        [Theory]
        [InlineData(73, "")]
        [InlineData(74, "=\r\n")]
        [InlineData(75, "=\r\n")]
        public void Encode_TrailingSpaceAtEndOfFullLine_IsEncodedWithinLimit(int length, string softBreak)
        {
            var testResult = MimeQuotedPrintable.Encode(Latin1(new string('a', length) + " \r\nb"), Body, Text);

            testResult.Should().Be(new string('a', length) + softBreak + "=20\r\nb");
            testResult.Split(new[] { "\r\n" }, StringSplitOptions.None).Should().OnlyContain(line => line.Length <= 76);
        }

        [Fact]
        public void Encode_SpaceBeforeSoftBreak_IsWrittenAsItself()
        {
            var testResult = MimeQuotedPrintable.Encode(Latin1(new string('a', 74) + " bc"), Body, Text);

            testResult.Should().Be(new string('a', 74) + " =\r\nbc");
        }

        [Fact]
        public void Encode_EscapesAtLineEnd_DontSplitAnEscape()
        {
            var testResult = MimeQuotedPrintable.Encode(new byte[30], Body, Text);

            var lines = testResult.Split(new[] { "\r\n" }, StringSplitOptions.None);
            lines.Should().HaveCount(2);
            lines[0].Should().Be(string.Concat(Enumerable.Repeat("=00", 25)) + "=");
            lines[1].Should().Be(string.Concat(Enumerable.Repeat("=00", 5)));
        }

        [Theory]
        [InlineData(Text)]
        [InlineData(Binary)]
        public void Encode_AnyData_WritesValidLines(MimeQuotedPrintableEncoderOptions options)
        {
            var random = new Random(405);
            for (var round = 0; round < 50; round++)
            {
                var bytes = CreateTextLikeData(random, random.Next(0, 400));

                var testResult = MimeQuotedPrintable.Encode(bytes, Body, options);

                foreach (var line in testResult.Split(new[] { "\r\n" }, StringSplitOptions.None))
                {
                    line.Length.Should().BeLessOrEqualTo(76);
                    line.Should().NotEndWith(" ").And.NotEndWith("\t").And.NotContain("\r").And.NotContain("\n");
                    line.Should().MatchRegex("^[\t\x20-\x7E]*$");
                }

                testResult.Length.Should().BeLessOrEqualTo(MimeQuotedPrintable.GetMaxEncodedLength(bytes.Length, Body));
                MimeQuotedPrintable.Decode(testResult.AsSpan(), Body, Strict).Should().Equal(options == Binary ? bytes : CanonicalLineBreaks(bytes));
            }
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void GetMaxEncodedLength_WorstCase_IsEnough(MimeQuotedPrintableMode mode)
        {
            for (var length = 0; length <= 300; length++)
            {
                var bytes = new byte[length];

                var testResult = MimeQuotedPrintable.Encode(bytes, mode, Text);

                testResult.Length.Should().BeLessOrEqualTo(MimeQuotedPrintable.GetMaxEncodedLength(length, mode), "{0} bytes fit", length);
            }
        }

        [Fact]
        public void GetMaxEncodedLength_TooLong_Throws()
        {
            Action act = () => MimeQuotedPrintable.GetMaxEncodedLength(int.MaxValue / 3 + 1, Body);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void Encode_EverySplitPoint_MatchesOneCall(MimeQuotedPrintableMode mode)
        {
            foreach (var options in new[] { Text, Binary })
            {
                var bytes = Encoding.UTF8.GetBytes(SampleText);
                var expected = MimeQuotedPrintable.Encode(bytes, mode, options);

                for (var split = 0; split <= bytes.Length; split++)
                {
                    EncodeSplit(bytes, split, mode, options, useChars: true).Should().Be(expected, "the data is split at {0}", split);
                    EncodeSplit(bytes, split, mode, options, useChars: false).Should().Be(expected, "the data is split at {0}", split);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void Encode_SmallBlocksAndDestinations_MatchesOneCall(MimeQuotedPrintableMode mode)
        {
            var bytes = Encoding.UTF8.GetBytes(SampleText);
            var expected = MimeQuotedPrintable.Encode(bytes, mode, Text);

            foreach (var block in new[] { 1, 2, 3, 5, 76, 1000 })
            {
                foreach (var destination in new[] { 3, 4, 5, 7, 76, 1000 })
                {
                    EncodeToChars(bytes, mode, block, destination).Should().Be(expected, "blocks are {0} and destinations {1}", block, destination);
                    EncodeToUtf8(bytes, mode, block, destination).Should().Be(expected, "blocks are {0} and destinations {1}", block, destination);
                }
            }
        }

        [Fact]
        public void Encode_PartialBlock_HoldsBytesThatDependOnWhatFollows()
        {
            var encoder = new MimeQuotedPrintableEncoder(Body);
            var destination = new char[16];

            encoder.Encode(Latin1("ab "), destination, out var consumed, out var written, false).Should().Be(OperationStatus.Done);
            consumed.Should().Be(3);
            new string(destination, 0, written).Should().Be("ab");

            encoder.Encode(Latin1("\r"), destination, out consumed, out written, false).Should().Be(OperationStatus.Done);
            consumed.Should().Be(1);
            written.Should().Be(0);

            encoder.Encode(Latin1("\nc"), destination, out consumed, out written, true).Should().Be(OperationStatus.Done);
            consumed.Should().Be(2);
            new string(destination, 0, written).Should().Be("=20\r\nc");
        }

        [Fact]
        public void Encode_DestinationTooSmall_WritesWholeTokens()
        {
            var encoder = new MimeQuotedPrintableEncoder(Body);
            var destination = new char[2];

            encoder.Encode(Latin1("a="), destination, out var consumed, out var written, true).Should().Be(OperationStatus.DestinationTooSmall);
            consumed.Should().Be(1);
            new string(destination, 0, written).Should().Be("a");
        }

        [Fact]
        public void Encode_AfterFinalBlock_StartsNewData()
        {
            var encoder = new MimeQuotedPrintableEncoder(Body);
            var bytes = Latin1(new string('a', 70));
            var destination = new char[200];

            encoder.Encode(bytes, destination, out _, out _, true);
            encoder.Encode(bytes, destination, out _, out var written, true).Should().Be(OperationStatus.Done);

            new string(destination, 0, written).Should().Be(new string('a', 70));
        }

        [Fact]
        public void Reset_Encoder_DiscardsHeldBytesAndLineLength()
        {
            var encoder = new MimeQuotedPrintableEncoder(Body);
            var destination = new char[200];
            encoder.Encode(Latin1(new string('a', 70) + " "), destination, out _, out _, false);

            encoder.Reset();
            encoder.Encode(Latin1("bcdefgh"), destination, out _, out var written, true).Should().Be(OperationStatus.Done);

            new string(destination, 0, written).Should().Be("bcdefgh");
        }

        [Fact]
        public void TryEncode_DestinationTooShort_ReturnsFalse()
        {
            var testResult = MimeQuotedPrintable.TryEncode(Latin1("a=b"), new char[4], out var charsWritten, Body, Text);

            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        #endregion

        #region Body decoding

        [Theory]
        [InlineData("=3d")]
        [InlineData("=a0")]
        [InlineData("=Fe")]
        public void Decode_LowercaseHexDigits_RejectedByDefault(string encoded)
        {
            MimeQuotedPrintable.TryDecode(encoded.AsSpan(), Body, Strict, out _).Should().BeFalse();

            MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, Lowercase).Should().Equal(Convert.ToByte(encoded.Substring(1), 16));
        }

        [Theory]
        [InlineData("a\nb", "a\nb")]
        [InlineData("a \t\nb", "a\nb")]
        [InlineData("a=\nb", "ab")]
        [InlineData("a= \nb", "ab")]
        [InlineData("a\r\nb\nc", "a\r\nb\nc")]
        public void Decode_BareLineFeed_RejectedByDefault(string encoded, string expected)
        {
            MimeQuotedPrintable.TryDecode(encoded.AsSpan(), Body, Strict, out _).Should().BeFalse();

            Encoding.ASCII.GetString(MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, BareLineFeeds)).Should().Be(expected);
        }

        [Theory]
        [InlineData("a=G1", "a=G1")]
        [InlineData("a=4G", "a=4G")]
        [InlineData("a=4=41", "a=4A")]
        [InlineData("a= b", "a= b")]
        [InlineData("a=  b  \r\nc", "a=  b\r\nc")]
        [InlineData("a=", "a=")]
        [InlineData("a=4", "a=4")]
        [InlineData("a= ", "a=")]
        [InlineData("a==41", "a=A")]
        public void Decode_EqualsSignNotFollowedByTwoHexDigits_RejectedByDefault(string encoded, string expected)
        {
            MimeQuotedPrintable.TryDecode(encoded.AsSpan(), Body, Strict, out _).Should().BeFalse();

            Encoding.ASCII.GetString(MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, LiteralEquals)).Should().Be(expected);
        }

        [Fact]
        public void Decode_LowercaseWithoutItsOption_IsLiteralUnderLiteralEqualsSigns()
        {
            var testResult = MimeQuotedPrintable.Decode("=3d".AsSpan(), Body, LiteralEquals);

            Encoding.ASCII.GetString(testResult).Should().Be("=3d");
        }

        [Theory]
        [InlineData("a\rb", 2)]
        [InlineData("a\r", 2)]
        [InlineData("a=\rb", 3)]
        [InlineData("a=\r", 3)]
        [InlineData("a\x01", 1)]
        [InlineData("a\x7F", 1)]
        [InlineData("café", 3)]
        [InlineData("a€b", 1)]
        [InlineData("a=\x01", 2)]
        public void Decode_InvalidData_ReportsPosition(string encoded, int position)
        {
            foreach (var options in new[] { Strict, Lowercase | BareLineFeeds | LiteralEquals })
            {
                MimeQuotedPrintable.TryDecode(encoded.AsSpan(), Body, options, out var result).Should().BeFalse();
                result.Should().BeNull();

                var decoder = new MimeQuotedPrintableDecoder(Body, options);
                var status = decoder.Decode(encoded.AsSpan(), new byte[16], out var consumed, out _, true);
                status.Should().Be(OperationStatus.InvalidData);
                consumed.Should().Be(Math.Min(position, encoded.Length));
            }
        }

        [Fact]
        public void Decode_Utf8Bytes_RejectsOctetsAbove126()
        {
            var decoder = new MimeQuotedPrintableDecoder(Body);

            decoder.Decode(new byte[] { 0x61, 0xC3, 0xA9 }, new byte[8], out var consumed, out _, true).Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(1);
        }

        [Fact]
        public void Decode_RunOf76Whitespace_IsAccepted()
        {
            var encoded = new string(' ', 76) + "a";

            Encoding.ASCII.GetString(MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, Strict)).Should().Be(encoded);
            Encoding.ASCII.GetString(DecodeChars(encoded, Body, Strict, 1, 2)).Should().Be(encoded);
        }

        [Fact]
        public void Decode_RunOfMoreThan76Whitespace_IsRejected()
        {
            var encoded = new string('\t', 77) + "\r\n";
            var decoder = new MimeQuotedPrintableDecoder(Body);

            decoder.Decode(encoded.AsSpan(), new byte[100], out var consumed, out _, true).Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(76);
        }

        [Fact]
        public void Decode_LongLine_IsAccepted()
        {
            var encoded = new string('a', 200);

            Encoding.ASCII.GetString(MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, Strict)).Should().Be(encoded);
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void Decode_EverySplitPoint_MatchesOneCall(MimeQuotedPrintableMode mode)
        {
            var bytes = Encoding.UTF8.GetBytes(SampleText);
            var encoded = MimeQuotedPrintable.Encode(bytes, mode, Text);
            var expected = MimeQuotedPrintable.Decode(encoded.AsSpan(), mode, Strict);

            for (var split = 0; split <= encoded.Length; split++)
            {
                DecodeSplit(encoded, split, mode, Strict, useChars: true).Should().Equal(expected, "the data is split at {0}", split);
                DecodeSplit(encoded, split, mode, Strict, useChars: false).Should().Equal(expected, "the data is split at {0}", split);
            }
        }

        [Theory]
        [InlineData("a=\r\nb")]
        [InlineData("a= \t \r\nb")]
        [InlineData("a=\nb")]
        [InlineData("a=4Bc")]
        [InlineData("a=4bc")]
        [InlineData("a \t \r\nb")]
        [InlineData("a \t b")]
        [InlineData("a \t \nb")]
        [InlineData("a=4Gb")]
        [InlineData("a= b")]
        [InlineData("a=  \r\n")]
        [InlineData("a=")]
        public void Decode_EverySplitPointOfSoftBreakAndEscape_MatchesOneCall(string encoded)
        {
            var options = Lowercase | BareLineFeeds | LiteralEquals;
            var expected = MimeQuotedPrintable.Decode(encoded.AsSpan(), Body, options);

            for (var split = 0; split <= encoded.Length; split++)
            {
                DecodeSplit(encoded, split, Body, options, useChars: true).Should().Equal(expected, "the data is split at {0}", split);
                DecodeSplit(encoded, split, Body, options, useChars: false).Should().Equal(expected, "the data is split at {0}", split);
            }
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void Decode_SmallBlocksAndDestinations_MatchesOneCall(MimeQuotedPrintableMode mode)
        {
            var encoded = MimeQuotedPrintable.Encode(Encoding.UTF8.GetBytes(SampleText), mode, Text);
            var expected = MimeQuotedPrintable.Decode(encoded.AsSpan(), mode, Strict);

            foreach (var block in new[] { 1, 2, 3, 5, 76, 1000 })
            {
                foreach (var destination in new[] { 2, 3, 5, 76, 1000 })
                {
                    DecodeChars(encoded, mode, Strict, block, destination).Should().Equal(expected, "blocks are {0} and destinations {1}", block, destination);
                    DecodeUtf8(encoded, mode, Strict, block, destination).Should().Equal(expected, "blocks are {0} and destinations {1}", block, destination);
                }
            }
        }

        [Fact]
        public void Decode_PartialEscape_HoldsItUntilNextCall()
        {
            var decoder = new MimeQuotedPrintableDecoder(Body);
            var destination = new byte[8];

            decoder.Decode("a=4".AsSpan(), destination, out var consumed, out var written, false).Should().Be(OperationStatus.Done);
            consumed.Should().Be(3);
            written.Should().Be(1);

            decoder.Decode("1".AsSpan(), destination, out consumed, out written, true).Should().Be(OperationStatus.Done);
            consumed.Should().Be(1);
            destination.Take(written).Should().Equal((byte)'A');
        }

        [Fact]
        public void Decode_TruncatedEscape_IsInvalidAtFinalBlock()
        {
            var decoder = new MimeQuotedPrintableDecoder(Body);
            var destination = new byte[8];

            decoder.Decode("a=4".AsSpan(), destination, out _, out _, false).Should().Be(OperationStatus.Done);
            decoder.Decode(ReadOnlySpan<char>.Empty, destination, out var consumed, out _, true).Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(0);
        }

        [Fact]
        public void Decode_DestinationTooSmallAtFinalBlock_WritesOnNextCall()
        {
            var decoder = new MimeQuotedPrintableDecoder(Body, LiteralEquals);
            var destination = new byte[2];

            decoder.Decode("ab=4".AsSpan(), destination, out var consumed, out var written, true).Should().Be(OperationStatus.DestinationTooSmall);
            consumed.Should().Be(4);
            written.Should().Be(2);

            decoder.Decode(ReadOnlySpan<char>.Empty, destination, out consumed, out written, true).Should().Be(OperationStatus.Done);
            Encoding.ASCII.GetString(destination, 0, written).Should().Be("=4");
        }

        [Fact]
        public void Reset_Decoder_DiscardsHeldData()
        {
            var decoder = new MimeQuotedPrintableDecoder(Body);
            var destination = new byte[8];
            decoder.Decode("a  =4".AsSpan(), destination, out _, out _, false);

            decoder.Reset();
            decoder.Decode("b".AsSpan(), destination, out _, out var written, true).Should().Be(OperationStatus.Done);

            Encoding.ASCII.GetString(destination, 0, written).Should().Be("b");
        }

        #endregion

        #region RFC 2047 "Q"

        [Theory]
        [InlineData("Keld_J=F8rn_Simonsen", "Keld Jørn Simonsen")]
        [InlineData("Andr=E9", "André")]
        [InlineData("Patrik_F=E4ltstr=F6m", "Patrik Fältström")]
        [InlineData("a=5Fb", "a_b")]
        [InlineData("a=20b", "a b")]
        public void Decode_Rfc2047Example_ReturnsText(string encoded, string expected)
        {
            foreach (var mode in new[] { MimeQuotedPrintableMode.EncodedWordText, MimeQuotedPrintableMode.EncodedWordComment, MimeQuotedPrintableMode.EncodedWordPhrase })
            {
                MimeQuotedPrintable.Decode(encoded.AsSpan(), mode, Strict).Should().Equal(Latin1(expected));
            }
        }

        [Theory]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "a_b=3D=3F=5F()\".,=09=0D=0A")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordComment, "a_b=3D=3F=5F=28=29=22.,=09=0D=0A")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordPhrase, "a_b=3D=3F=5F=28=29=22=2E=2C=09=0D=0A")]
        public void Encode_QContext_EncodesWhatTheContextRequires(MimeQuotedPrintableMode mode, string expected)
        {
            var testResult = MimeQuotedPrintable.Encode(Latin1("a b=?_()\".,\t\r\n"), mode, Text);

            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(QModes))]
        public void Encode_QPhraseCharacters_AreLiteralInEveryContext(MimeQuotedPrintableMode mode)
        {
            const string phraseSafe = "ABCXYZabcxyz0189!*+-/";

            MimeQuotedPrintable.Encode(Latin1(phraseSafe), mode, Text).Should().Be(phraseSafe);
            MimeQuotedPrintable.Encode(Latin1(phraseSafe), mode, Binary).Should().Be(phraseSafe);
        }

        [Theory]
        [MemberData(nameof(QModes))]
        public void Encode_QLongData_HasNoLineBreaks(MimeQuotedPrintableMode mode)
        {
            var testResult = MimeQuotedPrintable.Encode(Latin1(new string('a', 200) + "\r\n" + new string('b', 100)), mode, Text);

            testResult.Should().Be(new string('a', 200) + "=0D=0A" + new string('b', 100));
        }

        [Theory]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "a b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "a?b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "a\tb")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "a=\r\nb")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordText, "aé")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordComment, "a(b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordComment, "a)b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordComment, "a\"b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordPhrase, "a.b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordPhrase, "a,b")]
        [InlineData(MimeQuotedPrintableMode.EncodedWordPhrase, "a(b")]
        public void Decode_QCharacterTheContextDoesNotAllow_IsRejected(MimeQuotedPrintableMode mode, string encoded)
        {
            MimeQuotedPrintable.TryDecode(encoded.AsSpan(), mode, Lowercase | BareLineFeeds, out _).Should().BeFalse();
        }

        [Theory]
        [InlineData("a(b)\"c.,")]
        [InlineData("[x]<y>@;:\\")]
        public void Decode_QTextContext_AcceptsPrintableCharacters(string encoded)
        {
            MimeQuotedPrintable.Decode(encoded.AsSpan(), MimeQuotedPrintableMode.EncodedWordText, Strict).Should().Equal(Latin1(encoded));
        }

        [Theory]
        [MemberData(nameof(QModes))]
        public void Decode_QOptions_ApplyToEscapes(MimeQuotedPrintableMode mode)
        {
            MimeQuotedPrintable.TryDecode("=e9".AsSpan(), mode, Strict, out _).Should().BeFalse();
            MimeQuotedPrintable.Decode("=e9".AsSpan(), mode, Lowercase).Should().Equal(0xE9);
            MimeQuotedPrintable.TryDecode("a=".AsSpan(), mode, Strict, out _).Should().BeFalse();
            MimeQuotedPrintable.Decode("a=".AsSpan(), mode, LiteralEquals).Should().Equal(Latin1("a="));
        }

        #endregion

        #region Round trips

        [Theory]
        [MemberData(nameof(Modes))]
        public void RoundTrip_Utf8Text_ReturnsText(MimeQuotedPrintableMode mode)
        {
            const string text = "Grüße aus Zürich — 日本語 😀 = 100% _under_score? (yes)";
            var bytes = Encoding.UTF8.GetBytes(text);

            var encoded = MimeQuotedPrintable.Encode(bytes, mode, Text);

            Encoding.UTF8.GetString(MimeQuotedPrintable.Decode(encoded.AsSpan(), mode, Strict)).Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void RoundTrip_Latin1Text_ReturnsText(MimeQuotedPrintableMode mode)
        {
            const string text = "Café crème, señor, © ½ ÿ and plain ASCII at the end ";
            var bytes = Latin1(text);

            var encoded = MimeQuotedPrintable.Encode(bytes, mode, Text);

            Latin1Encoding.GetString(MimeQuotedPrintable.Decode(encoded.AsSpan(), mode, Strict)).Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void RoundTrip_EveryOctetInBinary_ReturnsData(MimeQuotedPrintableMode mode)
        {
            var bytes = Enumerable.Range(0, 256).Select(value => (byte)value).Concat(Latin1(" \r\n \n\r")).ToArray();

            var encoded = MimeQuotedPrintable.Encode(bytes, mode, Binary);

            MimeQuotedPrintable.Decode(encoded.AsSpan(), mode, Strict).Should().Equal(bytes);
        }

        #endregion

        #region Soft line breaks

        [Theory]
        [InlineData("abc=", true, 3)]
        [InlineData("abc= \t", true, 3)]
        [InlineData("abc =", true, 4)]
        [InlineData("=", true, 0)]
        [InlineData("abc", false, 3)]
        [InlineData("abc=3D", false, 6)]
        [InlineData("abc ", false, 4)]
        [InlineData("", false, 0)]
        public void EndsWithSoftLineBreak_Line_FindsSoftBreak(string line, bool expected, int expectedLength)
        {
            MimeQuotedPrintable.EndsWithSoftLineBreak(line.AsSpan(), out var length).Should().Be(expected);
            length.Should().Be(expectedLength);

            MimeQuotedPrintable.EndsWithSoftLineBreak(Encoding.ASCII.GetBytes(line), out length).Should().Be(expected);
            length.Should().Be(expectedLength);
        }

        [Fact]
        public void EndsWithSoftLineBreak_JoinedLines_DecodeAsOneValue()
        {
            var lines = Rfc2045Encoded.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var joined = new StringBuilder();
            foreach (var line in lines)
            {
                MimeQuotedPrintable.EndsWithSoftLineBreak(line.AsSpan(), out var length);
                joined.Append(line, 0, length);
            }

            Encoding.ASCII.GetString(MimeQuotedPrintable.Decode(joined.ToString().AsSpan(), Body, Strict)).Should().Be(Rfc2045Text);
        }

        #endregion

        #region Arguments

        [Fact]
        public void Constructors_InvalidArguments_Throw()
        {
            var invalidMode = (MimeQuotedPrintableMode)4;

            ((Action)(() => new MimeQuotedPrintableEncoder(invalidMode))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new MimeQuotedPrintableEncoder(Body, (MimeQuotedPrintableEncoderOptions)2))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new MimeQuotedPrintableDecoder((MimeQuotedPrintableMode)(-1)))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new MimeQuotedPrintableDecoder(Body, (MimeQuotedPrintableDecoderOptions)8))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => MimeQuotedPrintable.Encode(ReadOnlySpan<byte>.Empty, invalidMode, Text))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => MimeQuotedPrintable.Decode(ReadOnlySpan<char>.Empty, Body, (MimeQuotedPrintableDecoderOptions)8))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => MimeQuotedPrintable.GetMaxEncodedLength(-1, Body))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => MimeQuotedPrintable.GetMaxDecodedLength(-1))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructors_ValidArguments_SetProperties()
        {
            var encoder = new MimeQuotedPrintableEncoder(MimeQuotedPrintableMode.EncodedWordPhrase, Binary);
            var decoder = new MimeQuotedPrintableDecoder(Body, Lowercase | LiteralEquals);

            encoder.Mode.Should().Be(MimeQuotedPrintableMode.EncodedWordPhrase);
            encoder.Options.Should().Be(Binary);
            decoder.Mode.Should().Be(Body);
            decoder.Options.Should().Be(Lowercase | LiteralEquals);
        }

        [Fact]
        public void Decode_InvalidData_ThrowsFormatException()
        {
            Action act = () => MimeQuotedPrintable.Decode("=XY".AsSpan(), Body, Strict);

            act.Should().Throw<FormatException>();
        }

        #endregion

        private static byte[] Latin1(string value)
        {
            return Latin1Encoding.GetBytes(value);
        }

        // Text, with spaces, tabs, line breaks of every kind and octets that need encoding.
        private static byte[] CreateTextLikeData(Random random, int length)
        {
            const string alphabet = "abcdefghij KLMNOP=\t\r\n.é\u0001_?";
            var bytes = new byte[length];
            for (var index = 0; index < length; index++)
            {
                bytes[index] = (byte)alphabet[random.Next(alphabet.Length)];
            }

            return bytes;
        }

        // The data a text encoding decodes to: a bare LF becomes CRLF, and whitespace before a line break is kept.
        private static byte[] CanonicalLineBreaks(byte[] bytes)
        {
            var result = new List<byte>();
            for (var index = 0; index < bytes.Length; index++)
            {
                if (bytes[index] == '\n' && (index == 0 || bytes[index - 1] != '\r')) result.Add((byte)'\r');

                result.Add(bytes[index]);
            }

            return result.ToArray();
        }

        private static string EncodeSplit(byte[] bytes, int split, MimeQuotedPrintableMode mode, MimeQuotedPrintableEncoderOptions options, bool useChars)
        {
            var encoder = new MimeQuotedPrintableEncoder(mode, options);
            var output = new StringBuilder();
            var charDestination = new char[2000];
            var byteDestination = new byte[2000];
            foreach (var (start, length, isFinal) in new[] { (0, split, false), (split, bytes.Length - split, true) })
            {
                var source = bytes.AsSpan(start, length);
                int consumed;
                int written;
                if (useChars)
                {
                    encoder.Encode(source, charDestination, out consumed, out written, isFinal).Should().Be(OperationStatus.Done);
                    output.Append(charDestination, 0, written);
                }
                else
                {
                    encoder.Encode(source, byteDestination, out consumed, out written, isFinal).Should().Be(OperationStatus.Done);
                    output.Append(Encoding.ASCII.GetString(byteDestination, 0, written));
                }

                consumed.Should().Be(length);
            }

            return output.ToString();
        }

        private static string EncodeToChars(byte[] bytes, MimeQuotedPrintableMode mode, int blockLength, int destinationLength)
        {
            var encoder = new MimeQuotedPrintableEncoder(mode);
            var output = new StringBuilder();
            var destination = new char[destinationLength];
            var position = 0;
            while (true)
            {
                var length = Math.Min(blockLength, bytes.Length - position);
                var isFinal = position + length == bytes.Length;
                var status = encoder.Encode(bytes.AsSpan(position, length), destination, out var consumed, out var written, isFinal);
                output.Append(destination, 0, written);
                position += consumed;
                if (status == OperationStatus.Done && isFinal) return output.ToString();

                status.Should().BeOneOf(OperationStatus.Done, OperationStatus.DestinationTooSmall);
                (consumed + written).Should().BePositive("the encoder makes progress");
            }
        }

        private static string EncodeToUtf8(byte[] bytes, MimeQuotedPrintableMode mode, int blockLength, int destinationLength)
        {
            var encoder = new MimeQuotedPrintableEncoder(mode);
            var output = new StringBuilder();
            var destination = new byte[destinationLength];
            var position = 0;
            while (true)
            {
                var length = Math.Min(blockLength, bytes.Length - position);
                var isFinal = position + length == bytes.Length;
                var status = encoder.Encode(bytes.AsSpan(position, length), destination, out var consumed, out var written, isFinal);
                output.Append(Encoding.ASCII.GetString(destination, 0, written));
                position += consumed;
                if (status == OperationStatus.Done && isFinal) return output.ToString();

                status.Should().BeOneOf(OperationStatus.Done, OperationStatus.DestinationTooSmall);
                (consumed + written).Should().BePositive("the encoder makes progress");
            }
        }

        private static byte[] DecodeSplit(string encoded, int split, MimeQuotedPrintableMode mode, MimeQuotedPrintableDecoderOptions options, bool useChars)
        {
            var decoder = new MimeQuotedPrintableDecoder(mode, options);
            var output = new List<byte>();
            var destination = new byte[2000];
            var utf8 = Encoding.ASCII.GetBytes(encoded);
            foreach (var (start, length, isFinal) in new[] { (0, split, false), (split, encoded.Length - split, true) })
            {
                int consumed;
                int written;
                var status = useChars
                    ? decoder.Decode(encoded.AsSpan(start, length), destination, out consumed, out written, isFinal)
                    : decoder.Decode(utf8.AsSpan(start, length), destination, out consumed, out written, isFinal);

                status.Should().Be(OperationStatus.Done);
                consumed.Should().Be(length);
                output.AddRange(destination.Take(written));
            }

            return output.ToArray();
        }

        private static byte[] DecodeChars(string encoded, MimeQuotedPrintableMode mode, MimeQuotedPrintableDecoderOptions options, int blockLength, int destinationLength)
        {
            var decoder = new MimeQuotedPrintableDecoder(mode, options);
            var output = new List<byte>();
            var destination = new byte[destinationLength];
            var position = 0;
            while (true)
            {
                var length = Math.Min(blockLength, encoded.Length - position);
                var isFinal = position + length == encoded.Length;
                var status = decoder.Decode(encoded.AsSpan(position, length), destination, out var consumed, out var written, isFinal);
                output.AddRange(destination.Take(written));
                position += consumed;
                if (status == OperationStatus.Done && isFinal) return output.ToArray();

                status.Should().BeOneOf(OperationStatus.Done, OperationStatus.DestinationTooSmall);
                (consumed + written).Should().BePositive("the decoder makes progress");
            }
        }

        private static byte[] DecodeUtf8(string encoded, MimeQuotedPrintableMode mode, MimeQuotedPrintableDecoderOptions options, int blockLength, int destinationLength)
        {
            var decoder = new MimeQuotedPrintableDecoder(mode, options);
            var output = new List<byte>();
            var destination = new byte[destinationLength];
            var utf8 = Encoding.ASCII.GetBytes(encoded);
            var position = 0;
            while (true)
            {
                var length = Math.Min(blockLength, utf8.Length - position);
                var isFinal = position + length == utf8.Length;
                var status = decoder.Decode(utf8.AsSpan(position, length), destination, out var consumed, out var written, isFinal);
                output.AddRange(destination.Take(written));
                position += consumed;
                if (status == OperationStatus.Done && isFinal) return output.ToArray();

                status.Should().BeOneOf(OperationStatus.Done, OperationStatus.DestinationTooSmall);
                (consumed + written).Should().BePositive("the decoder makes progress");
            }
        }
    }
}
