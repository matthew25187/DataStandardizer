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
    public class MimeBase64Tests
    {
        private const MimeBase64DecoderOptions Reject = MimeBase64DecoderOptions.RejectNonAlphabetCharacters;

        public static IEnumerable<object[]> Rfc4648Vectors()
        {
            // ref. RFC 4648 §10
            var vectors = new[]
            {
                ("", ""),
                ("f", "Zg=="),
                ("fo", "Zm8="),
                ("foo", "Zm9v"),
                ("foob", "Zm9vYg=="),
                ("fooba", "Zm9vYmE="),
                ("foobar", "Zm9vYmFy"),
            };

            foreach (var (data, encoded) in vectors)
            {
                yield return new object[] { data, encoded, MimeBase64Mode.Strict };
                yield return new object[] { data, encoded, MimeBase64Mode.Mime };
            }
        }

        public static IEnumerable<object[]> Modes()
        {
            yield return new object[] { MimeBase64Mode.Strict };
            yield return new object[] { MimeBase64Mode.Mime };
        }

        #region RFC 4648 test vectors

        [Theory]
        [MemberData(nameof(Rfc4648Vectors))]
        public void Encode_Rfc4648Vector_ReturnsEncoding(string data, string encoded, MimeBase64Mode mode)
        {
            var bytes = Encoding.ASCII.GetBytes(data);

            MimeBase64.Encode(bytes, mode).Should().Be(encoded);
            MimeBase64.GetEncodedLength(bytes.Length, mode).Should().Be(encoded.Length);
            EncodeToChars(bytes, mode, bytes.Length, 64).Should().Be(encoded);
            EncodeToUtf8(bytes, mode, bytes.Length, 64).Should().Be(encoded);
        }

        [Theory]
        [MemberData(nameof(Rfc4648Vectors))]
        public void Decode_Rfc4648Vector_ReturnsData(string data, string encoded, MimeBase64Mode mode)
        {
            var bytes = Encoding.ASCII.GetBytes(data);

            foreach (var options in new[] { MimeBase64DecoderOptions.None, Reject })
            {
                MimeBase64.TryDecode(encoded.AsSpan(), mode, options, out var result).Should().BeTrue();
                result.Should().Equal(bytes);
                MimeBase64.Decode(encoded.AsSpan(), mode, options).Should().Equal(bytes);
                DecodeChars(encoded, mode, options, encoded.Length, 64).Should().Equal(bytes);
                DecodeUtf8(encoded, mode, options, encoded.Length, 64).Should().Equal(bytes);
            }
        }

        #endregion

        #region Encoding

        [Theory]
        [MemberData(nameof(Modes))]
        public void Encode_EveryLength_MatchesConvert(MimeBase64Mode mode)
        {
            var formatting = mode == MimeBase64Mode.Mime ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None;
            for (var length = 0; length <= 300; length++)
            {
                var bytes = CreateData(length);
                var expected = Convert.ToBase64String(bytes, formatting);

                MimeBase64.Encode(bytes, mode).Should().Be(expected, "{0} bytes encode the same", length);
                MimeBase64.GetEncodedLength(length, mode).Should().Be(expected.Length, "{0} bytes encode the same", length);
            }
        }

        [Theory]
        [InlineData(57, 76, 0)]
        [InlineData(58, 82, 1)]
        [InlineData(114, 154, 1)]
        [InlineData(115, 160, 2)]
        public void Encode_Mime_BreaksLinesAt76Characters(int length, int expectedLength, int expectedBreaks)
        {
            var testResult = MimeBase64.Encode(CreateData(length), MimeBase64Mode.Mime);

            testResult.Length.Should().Be(expectedLength);
            var lines = testResult.Split(new[] { "\r\n" }, StringSplitOptions.None);
            lines.Length.Should().Be(expectedBreaks + 1);
            lines.Take(expectedBreaks).All(line => line.Length == 76).Should().BeTrue();
            lines.Last().Length.Should().BeInRange(1, 76);
            testResult.Replace("\r\n", string.Empty).Should().NotContain("\r").And.NotContain("\n");
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void Encode_EverySplitPoint_MatchesOneCall(MimeBase64Mode mode)
        {
            var bytes = CreateData(130);
            var expected = MimeBase64.Encode(bytes, mode);

            for (var split = 0; split <= bytes.Length; split++)
            {
                EncodeSplit(bytes, split, mode, useChars: true).Should().Be(expected, "the data is split at {0}", split);
                EncodeSplit(bytes, split, mode, useChars: false).Should().Be(expected, "the data is split at {0}", split);
            }
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void Encode_SmallBlocksAndDestinations_MatchesOneCall(MimeBase64Mode mode)
        {
            var bytes = CreateData(200);
            var expected = MimeBase64.Encode(bytes, mode);

            foreach (var block in new[] { 1, 2, 3, 4, 5, 7, 57, 58, 200 })
            {
                foreach (var destination in new[] { 6, 7, 8, 9, 11, 76, 78, 80, 1000 })
                {
                    EncodeToChars(bytes, mode, block, destination).Should().Be(expected, "blocks are {0} and destinations {1}", block, destination);
                    EncodeToUtf8(bytes, mode, block, destination).Should().Be(expected, "blocks are {0} and destinations {1}", block, destination);
                }
            }
        }

        [Fact]
        public void Encode_DestinationTooSmallForLineBreak_WritesNothing()
        {
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            var bytes = CreateData(60);
            var destination = new char[80];

            encoder.Encode(bytes.AsSpan(0, 57), destination, out var consumed, out var written, false).Should().Be(OperationStatus.Done);
            consumed.Should().Be(57);
            written.Should().Be(76);

            encoder.Encode(bytes.AsSpan(57), destination.AsSpan(0, 5), out consumed, out written, true).Should().Be(OperationStatus.DestinationTooSmall);
            consumed.Should().Be(0);
            written.Should().Be(0);

            encoder.Encode(bytes.AsSpan(57), destination, out consumed, out written, true).Should().Be(OperationStatus.Done);
            consumed.Should().Be(3);
            new string(destination, 0, written).Should().Be("\r\n" + Convert.ToBase64String(bytes, 57, 3));
        }

        [Fact]
        public void Encode_PartialBlock_HoldsBytesUntilFinalBlock()
        {
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Strict);
            var destination = new char[8];

            encoder.Encode(new byte[] { 0x66, 0x6F }, destination, out var consumed, out var written, false).Should().Be(OperationStatus.Done);
            consumed.Should().Be(2);
            written.Should().Be(0);

            encoder.Encode(ReadOnlySpan<byte>.Empty, destination, out consumed, out written, true).Should().Be(OperationStatus.Done);
            consumed.Should().Be(0);
            new string(destination, 0, written).Should().Be("Zm8=");
        }

        [Fact]
        public void Encode_AfterFinalBlock_StartsNewData()
        {
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            var bytes = CreateData(100);
            var destination = new char[200];

            encoder.Encode(bytes, destination, out _, out _, true);
            encoder.Encode(bytes, destination, out _, out var written, true).Should().Be(OperationStatus.Done);

            new string(destination, 0, written).Should().Be(MimeBase64.Encode(bytes, MimeBase64Mode.Mime));
        }

        [Fact]
        public void Reset_Encoder_DiscardsHeldBytesAndLineLength()
        {
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            var destination = new char[200];
            encoder.Encode(CreateData(58), destination, out _, out _, false);

            encoder.Reset();
            encoder.Encode(new byte[] { 0x66 }, destination, out _, out var written, true).Should().Be(OperationStatus.Done);

            new string(destination, 0, written).Should().Be("Zg==");
        }

        [Fact]
        public void TryEncode_DestinationTooShort_ReturnsFalse()
        {
            var testResult = MimeBase64.TryEncode(new byte[] { 1, 2, 3, 4 }, new char[7], out var charsWritten, MimeBase64Mode.Strict);

            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        #endregion

        #region Decoding

        [Theory]
        [InlineData("Zm9v\r\nYmFy", MimeBase64DecoderOptions.None)]
        [InlineData("Zm9v\r\nYmFy", Reject)]
        [InlineData("Zm9v\r\nYmFy\r\n", Reject)]
        [InlineData("\r\nZm\r\n9vYm\r\nFy\r\n\r\n", Reject)]
        [InlineData("Zm9v\nYmFy", MimeBase64DecoderOptions.None)]
        [InlineData("Zm9v\rYmFy", MimeBase64DecoderOptions.None)]
        [InlineData("Zm 9v\tYm-Fy!", MimeBase64DecoderOptions.None)]
        [InlineData("Zm9v\u00E9YmFy", MimeBase64DecoderOptions.None)]
        public void Decode_MimeWithOtherCharacters_IgnoresThem(string encoded, MimeBase64DecoderOptions options)
        {
            MimeBase64.Decode(encoded.AsSpan(), MimeBase64Mode.Mime, options).Should().Equal(Encoding.ASCII.GetBytes("foobar"));
        }

        [Theory]
        [InlineData("Zg==\r\n", MimeBase64DecoderOptions.None)]
        [InlineData("Zg==\r\n", Reject)]
        [InlineData("Zg=\r\n=", Reject)]
        [InlineData("Zg== -- \n", MimeBase64DecoderOptions.None)]
        public void Decode_MimeOtherCharactersAroundPadding_IgnoresThem(string encoded, MimeBase64DecoderOptions options)
        {
            MimeBase64.Decode(encoded.AsSpan(), MimeBase64Mode.Mime, options).Should().Equal(new byte[] { 0x66 });
        }

        [Theory]
        [InlineData("Zm9v\nYmFy", 4)]
        [InlineData("Zm9v\rYmFy", 5)]
        [InlineData("Zm9v\r\rYmFy", 5)]
        [InlineData("Zm9v\n\rYmFy", 4)]
        [InlineData("Zm9vYmFy\r", 9)]
        [InlineData("Zm 9vYmFy", 2)]
        [InlineData("Zm9v\tYmFy", 4)]
        [InlineData("Zm9vYmFy!", 8)]
        [InlineData("Zm9v\u00E9YmFy", 4)]
        public void Decode_MimeRejectingOtherCharacters_ReturnsInvalidDataAtCharacter(string encoded, int position)
        {
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Mime, Reject);
            var destination = new byte[16];

            decoder.Decode(encoded.AsSpan(), destination, out var consumed, out _, true).Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(position);
            MimeBase64.TryDecode(encoded.AsSpan(), MimeBase64Mode.Mime, Reject, out var result).Should().BeFalse();
            result.Should().BeNull();
        }

        [Theory]
        // ref. RFC 4648 §3.3, §4: padding and the length must be right, and nothing outside the alphabet may appear.
        [InlineData("A", 1)]
        [InlineData("AAA", 3)]
        [InlineData("Zg=", 3)]
        [InlineData("AA=A", 3)]
        [InlineData("A===", 1)]
        [InlineData("====", 0)]
        [InlineData("Zm9v=", 4)]
        [InlineData("Zg===", 4)]
        [InlineData("Zg==Zg==", 4)]
        [InlineData("Zm8=Zg==", 4)]
        [InlineData("AA AA", 2)]
        [InlineData("AA\r\nAA", 2)]
        [InlineData("Zm9v\r\n", 4)]
        [InlineData("AA-_", 2)]
        [InlineData("Zm9vYmFy\u00E9", 8)]
        public void Decode_StrictInvalid_ReturnsInvalidDataAtCharacter(string encoded, int position)
        {
            foreach (var options in new[] { MimeBase64DecoderOptions.None, Reject })
            {
                var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict, options);
                var destination = new byte[16];

                decoder.Decode(encoded.AsSpan(), destination, out var consumed, out _, true).Should().Be(OperationStatus.InvalidData);
                consumed.Should().Be(position);
                decoder.Reset();
                decoder.Decode(Encoding.Latin1.GetBytes(encoded), destination, out consumed, out _, true)
                    .Should().Be(OperationStatus.InvalidData);
                consumed.Should().Be(position);
                MimeBase64.TryDecode(encoded.AsSpan(), MimeBase64Mode.Strict, options, out var result).Should().BeFalse();
                result.Should().BeNull();
            }
        }

        [Theory]
        [InlineData("A")]
        [InlineData("AAA")]
        [InlineData("Zg=")]
        [InlineData("A===")]
        [InlineData("====")]
        [InlineData("Zg==Zg==")]
        [InlineData("Zg==\r\nZg==")]
        [InlineData("Zm9v\r\nYmF")]
        public void Decode_MimeBadPaddingOrTruncated_Fails(string encoded)
        {
            foreach (var options in new[] { MimeBase64DecoderOptions.None, Reject })
            {
                MimeBase64.TryDecode(encoded.AsSpan(), MimeBase64Mode.Mime, options, out var result).Should().BeFalse();
                result.Should().BeNull();
                FluentActions.Invoking(() => MimeBase64.Decode(encoded.AsSpan(), MimeBase64Mode.Mime, options)).Should().Throw<FormatException>();
            }
        }

        [Fact]
        public void Decode_TruncatedInFinalBlock_ConsumesAllAndReturnsInvalidData()
        {
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);
            var destination = new byte[16];

            decoder.Decode("Zm9vYm".AsSpan(), destination, out var consumed, out var written, true).Should().Be(OperationStatus.InvalidData);

            consumed.Should().Be(6);
            written.Should().Be(3);
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void Decode_NonZeroPadBits_Accepted(MimeBase64Mode mode)
        {
            // ref. RFC 4648 §3.5: decoders MAY reject non-zero pad bits; these don't, as iCal's BINARY codec doesn't.
            MimeBase64.Decode("Zh==".AsSpan(), mode, MimeBase64DecoderOptions.None).Should().Equal(new byte[] { 0x66 });
            MimeBase64.Decode("Zm9=".AsSpan(), mode, MimeBase64DecoderOptions.None).Should().Equal(new byte[] { 0x66, 0x6F });
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void Decode_EverySplitPoint_MatchesOneCall(MimeBase64Mode mode)
        {
            var bytes = CreateData(130);
            var encoded = MimeBase64.Encode(bytes, mode);
            if (mode == MimeBase64Mode.Mime)
            {
                encoded = "\r\n" + encoded.Replace("=", "\r\n=") + "\r\n";
            }

            foreach (var options in new[] { MimeBase64DecoderOptions.None, Reject })
            {
                for (var split = 0; split <= encoded.Length; split++)
                {
                    DecodeSplit(encoded, split, mode, options, useChars: true).Should().Equal(bytes, "the data is split at {0}", split);
                    DecodeSplit(encoded, split, mode, options, useChars: false).Should().Equal(bytes, "the data is split at {0}", split);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Modes))]
        public void Decode_SmallBlocksAndDestinations_MatchesOneCall(MimeBase64Mode mode)
        {
            for (var length = 197; length <= 199; length++)
            {
                var bytes = CreateData(length);
                var encoded = MimeBase64.Encode(bytes, mode);

                foreach (var block in new[] { 1, 2, 3, 4, 5, 7, 76, 78, 1000 })
                {
                    foreach (var destination in new[] { 3, 4, 5, 6, 7, 57, 1000 })
                    {
                        DecodeChars(encoded, mode, Reject, block, destination).Should().Equal(bytes, "blocks are {0} and destinations {1}", block, destination);
                        DecodeUtf8(encoded, mode, Reject, block, destination).Should().Equal(bytes, "blocks are {0} and destinations {1}", block, destination);
                    }
                }
            }
        }

        [Fact]
        public void Decode_DestinationTooSmallForPadding_WritesNothing()
        {
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);

            decoder.Decode("Zm8=".AsSpan(), new byte[1], out var consumed, out var written, true).Should().Be(OperationStatus.DestinationTooSmall);
            consumed.Should().Be(3);
            written.Should().Be(0);

            var destination = new byte[2];
            decoder.Decode("=".AsSpan(), destination, out consumed, out written, true).Should().Be(OperationStatus.Done);
            consumed.Should().Be(1);
            destination.Take(written).Should().Equal(0x66, 0x6F);
        }

        [Fact]
        public void Decode_PartialQuantum_HoldsItUntilNextBlock()
        {
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);
            var destination = new byte[8];

            decoder.Decode("Zm9vY".AsSpan(), destination, out var consumed, out var written, false).Should().Be(OperationStatus.Done);
            consumed.Should().Be(5);
            written.Should().Be(3);

            decoder.Decode("g==".AsSpan(), destination, out consumed, out written, true).Should().Be(OperationStatus.Done);
            consumed.Should().Be(3);
            destination.Take(written).Should().Equal(0x62);
        }

        [Fact]
        public void Decode_AfterFinalBlock_StartsNewData()
        {
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);
            var destination = new byte[8];

            decoder.Decode("Zg==".AsSpan(), destination, out _, out _, true).Should().Be(OperationStatus.Done);
            decoder.Decode("Zm8=".AsSpan(), destination, out _, out var written, true).Should().Be(OperationStatus.Done);

            destination.Take(written).Should().Equal(0x66, 0x6F);
        }

        [Fact]
        public void Reset_Decoder_DiscardsHeldQuantumAndPadding()
        {
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);
            var destination = new byte[8];
            decoder.Decode("Zg==".AsSpan(), destination, out _, out _, false);

            decoder.Reset();
            decoder.Decode("Zm8=".AsSpan(), destination, out _, out var written, true).Should().Be(OperationStatus.Done);

            destination.Take(written).Should().Equal(0x66, 0x6F);
        }

        #endregion

        #region iCal BINARY parity

        [Fact]
        public void Decode_IcalRfcExample_MatchesConvert()
        {
            // ref. RFC 5545 §3.3.1, as in IcalValueTests
            const string value = "AAABAAEAEBAQAAEABAAoAQAAFgAAACgAAAAQAAAAIAAAAAEABAAA";

            var testResult = MimeBase64.TryDecode(value.AsSpan(), MimeBase64Mode.Strict, MimeBase64DecoderOptions.None, out var bytes);

            testResult.Should().BeTrue();
            bytes.Should().Equal(Convert.FromBase64String(value));
            MimeBase64.Encode(bytes, MimeBase64Mode.Strict).Should().Be(value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(256)]
        public void Encode_IcalEveryLength_MatchesConvertAndRoundTrips(int length)
        {
            var bytes = Enumerable.Range(0, length).Select(index => (byte)(index * 37 + 11)).ToArray();

            var testResult = MimeBase64.Encode(bytes, MimeBase64Mode.Strict);
            MimeBase64.TryDecode(testResult.AsSpan(), MimeBase64Mode.Strict, MimeBase64DecoderOptions.None, out var parsed);

            testResult.Should().Be(Convert.ToBase64String(bytes));
            parsed.Should().Equal(bytes);
        }

        [Theory]
        [InlineData("A")]
        [InlineData("AAA")]
        [InlineData("AA=A")]
        [InlineData("A===")]
        [InlineData("====")]
        [InlineData("AA AA")]
        [InlineData("AA\r\nAA")]
        [InlineData("AA-_")]
        public void TryDecode_IcalInvalidValue_Fails(string value)
        {
            var testResult = MimeBase64.TryDecode(value.AsSpan(), MimeBase64Mode.Strict, MimeBase64DecoderOptions.None, out var bytes);

            testResult.Should().BeFalse();
            bytes.Should().BeNull();
        }

        #endregion

        #region Arguments

        [Fact]
        public void Members_UndefinedModeOrOptions_Throw()
        {
            const MimeBase64Mode badMode = (MimeBase64Mode)2;
            const MimeBase64DecoderOptions badOptions = (MimeBase64DecoderOptions)2;

            FluentActions.Invoking(() => new MimeBase64Encoder(badMode)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => new MimeBase64Decoder(badMode)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => new MimeBase64Decoder(MimeBase64Mode.Mime, badOptions)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => MimeBase64.GetEncodedLength(1, badMode)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => MimeBase64.Encode(new byte[1], badMode)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => MimeBase64.TryEncode(new byte[1], new char[4], out _, badMode)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => MimeBase64.TryDecode("Zg==".AsSpan(), badMode, MimeBase64DecoderOptions.None, out _)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => MimeBase64.TryDecode("Zg==".AsSpan(), MimeBase64Mode.Mime, badOptions, out _)).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructors_SetModeAndOptions()
        {
            new MimeBase64Encoder(MimeBase64Mode.Mime).Mode.Should().Be(MimeBase64Mode.Mime);
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Mime, Reject);
            decoder.Mode.Should().Be(MimeBase64Mode.Mime);
            decoder.Options.Should().Be(Reject);
            new MimeBase64Decoder(MimeBase64Mode.Strict).Options.Should().Be(MimeBase64DecoderOptions.None);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(3, 0)]
        [InlineData(4, 3)]
        [InlineData(7, 3)]
        [InlineData(8, 6)]
        [InlineData(int.MaxValue, int.MaxValue / 4 * 3)]
        public void GetMaxDecodedLength_ReturnsThreeBytesPerQuantum(int length, int expected)
        {
            MimeBase64.GetMaxDecodedLength(length).Should().Be(expected);
        }

        [Fact]
        public void LengthHelpers_OutOfRange_Throw()
        {
            FluentActions.Invoking(() => MimeBase64.GetMaxDecodedLength(-1)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => MimeBase64.GetEncodedLength(-1, MimeBase64Mode.Strict)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => MimeBase64.GetEncodedLength(int.MaxValue, MimeBase64Mode.Strict)).Should().Throw<ArgumentOutOfRangeException>();
            MimeBase64.GetEncodedLength(1610612733, MimeBase64Mode.Strict).Should().Be(2147483644);
        }

        #endregion

        private static byte[] CreateData(int length)
        {
            return Enumerable.Range(0, length).Select(index => (byte)(index * 151 + 7)).ToArray();
        }

        private static string EncodeSplit(byte[] bytes, int split, MimeBase64Mode mode, bool useChars)
        {
            var encoder = new MimeBase64Encoder(mode);
            var output = new StringBuilder();
            var charDestination = new char[400];
            var byteDestination = new byte[400];
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

        private static string EncodeToChars(byte[] bytes, MimeBase64Mode mode, int blockLength, int destinationLength)
        {
            var encoder = new MimeBase64Encoder(mode);
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

        private static string EncodeToUtf8(byte[] bytes, MimeBase64Mode mode, int blockLength, int destinationLength)
        {
            var encoder = new MimeBase64Encoder(mode);
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

        private static byte[] DecodeSplit(string encoded, int split, MimeBase64Mode mode, MimeBase64DecoderOptions options, bool useChars)
        {
            var decoder = new MimeBase64Decoder(mode, options);
            var output = new List<byte>();
            var destination = new byte[400];
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

        private static byte[] DecodeChars(string encoded, MimeBase64Mode mode, MimeBase64DecoderOptions options, int blockLength, int destinationLength)
        {
            var decoder = new MimeBase64Decoder(mode, options);
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

        private static byte[] DecodeUtf8(string encoded, MimeBase64Mode mode, MimeBase64DecoderOptions options, int blockLength, int destinationLength)
        {
            var decoder = new MimeBase64Decoder(mode, options);
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
