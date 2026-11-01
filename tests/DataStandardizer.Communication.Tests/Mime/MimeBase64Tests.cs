using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeBase64Tests
    {
        private const MimeBase64DecoderOptions Reject = MimeBase64DecoderOptions.RejectNonAlphabetCharacters;

        // ISO-8859-1. Encoding.Latin1 is .NET 5 or later, but every runtime has code page 28591 built in.
        private static readonly Encoding Latin1Encoding = Encoding.GetEncoding(28591);

        // ref. RFC 4648 §10: the test vectors.
        private const string VectorEmpty = "";
        private const string VectorF = "f";
        private const string VectorFEncoded = "Zg==";
        private const string VectorFo = "fo";
        private const string VectorFoEncoded = "Zm8=";
        private const string VectorFoo = "foo";
        private const string VectorFooEncoded = "Zm9v";
        private const string VectorFoob = "foob";
        private const string VectorFoobEncoded = "Zm9vYg==";
        private const string VectorFooba = "fooba";
        private const string VectorFoobaEncoded = "Zm9vYmE=";
        private const string VectorFoobar = "foobar";
        private const string VectorFoobarEncoded = "Zm9vYmFy";

        // The data of the "f", "fo" and "foobar" vectors.
        private static readonly byte[] VectorFBytes = { 0x66 };
        private static readonly byte[] VectorFoBytes = { 0x66, 0x6F };
        private static readonly byte[] VectorFoobarBytes = { 0x66, 0x6F, 0x6F, 0x62, 0x61, 0x72 };

        // The "foobar" vector with line breaks of each kind between its quanta; ref. RFC 2045 §6.8, a decoder ignores characters outside the
        // alphabet, unless it is told to reject them.
        private const string FoobarWithLineBreak = "Zm9v\r\nYmFy";
        private const string FoobarWithLineFeed = "Zm9v\nYmFy";
        private const string FoobarWithCarriageReturn = "Zm9v\rYmFy";
        private const string FoobarWithLatin1Letter = "Zm9v\x00E9YmFy";

        // The "f" vector with a line break after it.
        private const string FEncodedWithLineBreak = "Zg==\r\n";

        // Values that RFC 4648 §3.3 and §4 don't allow: the padding and the length must be right, and nothing outside the alphabet may
        // appear.
        private const string OneCharacter = "A";
        private const string ThreeCharacters = "AAA";
        private const string MissingPadding = "Zg=";
        private const string PaddingBeforeData = "AA=A";
        private const string ThreePaddingCharacters = "A===";
        private const string OnlyPadding = "====";
        private const string TwoPaddedQuanta = "Zg==Zg==";
        private const string SpaceInside = "AA AA";
        private const string LineBreakInside = "AA\r\nAA";
        private const string UrlSafeAlphabet = "AA-_";

        // ref. RFC 2045 §6.8: encoded lines are no more than 76 characters, which is 57 bytes of data.
        private const int MimeLineLength = 76;
        private const int BytesPerMimeLine = 57;
        private const string MimeLineBreak = "\r\n";

        // The base64 of BytesPerMimeLine zero bytes, which fills a line, and of one more zero byte.
        private const string MimeLineOfZeros = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const string OneZeroByte = "AA==";

        // A line of text one byte longer than fills an encoded line, with its encoding in each mode, and the MIME encoding with line
        // breaks around its padding, which a MIME decoder ignores.
        private const string LineOfText = "Man is distinguished, not only by his reason, but by this ";
        private const string LineOfTextStrict = "TWFuIGlzIGRpc3Rpbmd1aXNoZWQsIG5vdCBvbmx5IGJ5IGhpcyByZWFzb24sIGJ1dCBieSB0aGlzIA==";
        private const string LineOfTextMime = "TWFuIGlzIGRpc3Rpbmd1aXNoZWQsIG5vdCBvbmx5IGJ5IGhpcyByZWFzb24sIGJ1dCBieSB0aGlz\r\nIA==";
        private const string LineOfTextMimeWithLineBreaksAroundPadding =
            "\r\nTWFuIGlzIGRpc3Rpbmd1aXNoZWQsIG5vdCBvbmx5IGJ5IGhpcyByZWFzb24sIGJ1dCBieSB0aGlz\r\nIA\r\n=\r\n=\r\n";

        // ref. RFC 5545 §3.3.1, as in IcalValueTests: the example BINARY value, and its data.
        private const string IcalBinaryExample = "AAABAAEAEBAQAAEABAAoAQAAFgAAACgAAAAQAAAAIAAAAAEABAAA";
        private static readonly byte[] IcalBinaryExampleBytes =
        {
            0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x10, 0x10, 0x10, 0x00, 0x01, 0x00, 0x04, 0x00, 0x28, 0x01, 0x00, 0x00, 0x16, 0x00,
            0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x01, 0x00, 0x04, 0x00, 0x00,
        };

        // Undefined values of the enumerations.
        private const MimeBase64Mode UndefinedMode = (MimeBase64Mode)2;
        private const MimeBase64DecoderOptions UndefinedOptions = (MimeBase64DecoderOptions)2;

        // The longest data whose encoding fits in an int: 536,870,911 quanta of three bytes, which encode to four characters each.
        private const int LongestEncodableLength = 1610612733;
        private const int LongestEncodedLength = 2147483644;

        // The most data that int.MaxValue characters can decode to: 536,870,911 whole quanta of four characters, three bytes each.
        private const int MaxDecodedLengthOfLongestSource = 1610612733;

        #region Test case factories

        private static IEnumerable<(string Data, string Encoded)> Vectors()
        {
            yield return (VectorEmpty, VectorEmpty);
            yield return (VectorF, VectorFEncoded);
            yield return (VectorFo, VectorFoEncoded);
            yield return (VectorFoo, VectorFooEncoded);
            yield return (VectorFoob, VectorFoobEncoded);
            yield return (VectorFooba, VectorFoobaEncoded);
            yield return (VectorFoobar, VectorFoobarEncoded);
        }

        private static IEnumerable<MimeBase64Mode> AllModes()
        {
            yield return MimeBase64Mode.Strict;
            yield return MimeBase64Mode.Mime;
        }

        private static IEnumerable<MimeBase64DecoderOptions> AllOptions()
        {
            yield return MimeBase64DecoderOptions.None;
            yield return Reject;
        }

        public static IEnumerable<object[]> Modes()
        {
            foreach (var mode in AllModes())
            {
                yield return new object[] { mode };
            }
        }

        // Each vector's data, with its encoding, in each mode.
        public static IEnumerable<object[]> Rfc4648Vectors()
        {
            foreach (var (data, encoded) in Vectors())
            {
                foreach (var mode in AllModes())
                {
                    yield return new object[] { Encoding.ASCII.GetBytes(data), encoded, mode };
                }
            }
        }

        // Each vector's encoding, with its data, in each mode and with each decoder option.
        public static IEnumerable<object[]> Rfc4648VectorsToDecode()
        {
            foreach (var (data, encoded) in Vectors())
            {
                foreach (var mode in AllModes())
                {
                    foreach (var options in AllOptions())
                    {
                        yield return new object[] { encoded, mode, options, Encoding.ASCII.GetBytes(data) };
                    }
                }
            }
        }

        // Data of every length to 300 bytes, in each mode, with its encoding by Convert.ToBase64String, an implementation of base64 that is
        // independent of the one under test. The bytes are a sequence that reaches every value.
        public static IEnumerable<object[]> EveryLength()
        {
            foreach (var mode in AllModes())
            {
                var formatting = mode == MimeBase64Mode.Mime ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None;
                for (var length = 0; length <= 300; length++)
                {
                    var bytes = CreateData(length);
                    yield return new object[] { bytes, mode, Convert.ToBase64String(bytes, formatting) };
                }
            }
        }

        // LineOfText split into two at every point, in each mode, with its encoding.
        public static IEnumerable<object[]> LineOfTextSplits()
        {
            var bytes = Encoding.ASCII.GetBytes(LineOfText);
            foreach (var (mode, expected) in new[] { (MimeBase64Mode.Strict, LineOfTextStrict), (MimeBase64Mode.Mime, LineOfTextMime) })
            {
                for (var split = 0; split <= bytes.Length; split++)
                {
                    yield return new object[] { mode, bytes.AsSpan(0, split).ToArray(), bytes.AsSpan(split).ToArray(), expected };
                }
            }
        }

        // LineOfText, in each mode, with its encoding, and a destination too small to hold all of it.
        public static IEnumerable<object[]> LineOfTextSmallDestinations()
        {
            var bytes = Encoding.ASCII.GetBytes(LineOfText);
            foreach (var (mode, expected) in new[] { (MimeBase64Mode.Strict, LineOfTextStrict), (MimeBase64Mode.Mime, LineOfTextMime) })
            {
                foreach (var destinationLength in new[] { 0, 1, 3, 4, 5, 7, 8, 9, 11, 75, 76, 77, 78, 79 })
                {
                    yield return new object[] { mode, bytes, destinationLength, expected };
                }
            }
        }

        // The encoding of LineOfText, in each mode and with each decoder option, split into two at every point, with its data.
        public static IEnumerable<object[]> EncodedLineOfTextSplits()
        {
            var data = Encoding.ASCII.GetBytes(LineOfText);
            var encodings = new[] { (MimeBase64Mode.Strict, LineOfTextStrict), (MimeBase64Mode.Mime, LineOfTextMimeWithLineBreaksAroundPadding) };
            foreach (var (mode, encoded) in encodings)
            {
                foreach (var options in AllOptions())
                {
                    for (var split = 0; split <= encoded.Length; split++)
                    {
                        yield return new object[] { mode, options, encoded.Substring(0, split), encoded.Substring(split), data };
                    }
                }
            }
        }

        // The encoding of LineOfText, in each mode, with its data, and a destination too small to hold all of it.
        public static IEnumerable<object[]> EncodedLineOfTextSmallDestinations()
        {
            var data = Encoding.ASCII.GetBytes(LineOfText);
            foreach (var (mode, encoded) in new[] { (MimeBase64Mode.Strict, LineOfTextStrict), (MimeBase64Mode.Mime, LineOfTextMime) })
            {
                foreach (var destinationLength in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 56, 57 })
                {
                    yield return new object[] { mode, encoded, destinationLength, data };
                }
            }
        }

        // Values that a strict decoder rejects, with each decoder option, and the position of the character it rejects.
        public static IEnumerable<object[]> StrictlyInvalid()
        {
            var values = new[]
            {
                (OneCharacter, 1),
                (ThreeCharacters, 3),
                (MissingPadding, 3),
                (PaddingBeforeData, 3),
                (ThreePaddingCharacters, 1),
                (OnlyPadding, 0),
                ("Zm9v=", 4),
                ("Zg===", 4),
                (TwoPaddedQuanta, 4),
                ("Zm8=Zg==", 4),
                (SpaceInside, 2),
                (LineBreakInside, 2),
                ("Zm9v\r\n", 4),
                (UrlSafeAlphabet, 2),
                ("Zm9vYmFy\x00E9", 8),
            };

            foreach (var (encoded, position) in values)
            {
                foreach (var options in AllOptions())
                {
                    yield return new object[] { encoded, options, position };
                }
            }
        }

        // A destination longer than any of the StrictlyInvalid values could decode to.
        private const int MoreThanAnyInvalidValueDecodesTo = 16;

        // The StrictlyInvalid values as ISO-8859-1 bytes.
        public static IEnumerable<object[]> StrictlyInvalidBytes()
        {
            foreach (var row in StrictlyInvalid())
            {
                yield return new object[] { Latin1Encoding.GetBytes((string)row[0]), row[1], row[2] };
            }
        }

        // Values that a MIME decoder rejects, with each decoder option: bad padding, or data that ends inside a quantum.
        public static IEnumerable<object[]> MimeBadPaddingOrTruncated()
        {
            var values = new[]
            {
                OneCharacter,
                ThreeCharacters,
                MissingPadding,
                ThreePaddingCharacters,
                OnlyPadding,
                TwoPaddedQuanta,
                "Zg==\r\nZg==",
                "Zm9v\r\nYmF",
            };

            foreach (var encoded in values)
            {
                foreach (var options in AllOptions())
                {
                    yield return new object[] { encoded, options };
                }
            }
        }

        // ref. RFC 4648 §3.5: encodings whose pad bits aren't zero, with the data they decode to, in each mode.
        public static IEnumerable<object[]> NonZeroPadBits()
        {
            foreach (var mode in AllModes())
            {
                yield return new object[] { "Zh==", mode, VectorFBytes };
                yield return new object[] { "Zm9=", mode, VectorFoBytes };
            }
        }

        // The lengths that RFC 5545 §3.3.1 BINARY values are tested with in IcalValueTests, with data of that length and its encoding by
        // Convert.ToBase64String.
        public static IEnumerable<object[]> IcalLengths()
        {
            foreach (var length in new[] { 0, 1, 2, 3, 4, 5, 256 })
            {
                var bytes = new byte[length];
                for (var index = 0; index < length; index++)
                {
                    bytes[index] = (byte)(index * 37 + 11);
                }

                yield return new object[] { bytes, Convert.ToBase64String(bytes) };
            }
        }

        public static TheoryData<Action> MembersWithUndefinedModeOrOptions => new TheoryData<Action>
        {
            () => _ = new MimeBase64Encoder(UndefinedMode),
            () => _ = new MimeBase64Decoder(UndefinedMode),
            () => _ = new MimeBase64Decoder(MimeBase64Mode.Mime, UndefinedOptions),
            () => MimeBase64.GetEncodedLength(1, UndefinedMode),
            () => MimeBase64.Encode(new byte[1], UndefinedMode),
            () => MimeBase64.TryEncode(new byte[1], new char[4], out _, UndefinedMode),
            () => MimeBase64.TryDecode(VectorFEncoded.AsSpan(), UndefinedMode, MimeBase64DecoderOptions.None, out _),
            () => MimeBase64.TryDecode(VectorFEncoded.AsSpan(), MimeBase64Mode.Mime, UndefinedOptions, out _),
        };

        public static TheoryData<Action> LengthHelpersWithLengthOutOfRange => new TheoryData<Action>
        {
            () => MimeBase64.GetMaxDecodedLength(-1),
            () => MimeBase64.GetEncodedLength(-1, MimeBase64Mode.Strict),
            () => MimeBase64.GetEncodedLength(int.MaxValue, MimeBase64Mode.Strict),
        };

        #endregion

        #region RFC 4648 test vectors

        [Theory]
        [MemberData(nameof(Rfc4648Vectors))]
        public void Encode_Rfc4648Vector_ReturnsEncoding(byte[] data, string encoded, MimeBase64Mode mode)
        {
            // act
            var testResult = MimeBase64.Encode(data, mode);

            // assert
            testResult.Should().Be(encoded);
        }

        [Theory]
        [MemberData(nameof(Rfc4648Vectors))]
        public void GetEncodedLength_Rfc4648Vector_ReturnsLengthOfEncoding(byte[] data, string encoded, MimeBase64Mode mode)
        {
            // act
            var testResult = MimeBase64.GetEncodedLength(data.Length, mode);

            // assert
            testResult.Should().Be(encoded.Length);
        }

        [Theory]
        [MemberData(nameof(Rfc4648Vectors))]
        public void EncodeToChars_Rfc4648VectorInOneFinalBlock_WritesEncoding(byte[] data, string encoded, MimeBase64Mode mode)
        {
            // arrange
            var encoder = new MimeBase64Encoder(mode);
            var destination = new char[encoded.Length];

            // act
            var testResult = encoder.Encode(data, destination, out var consumed, out var written, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(data.Length);
            written.Should().Be(encoded.Length);
            new string(destination).Should().Be(encoded);
        }

        [Theory]
        [MemberData(nameof(Rfc4648Vectors))]
        public void EncodeToUtf8_Rfc4648VectorInOneFinalBlock_WritesEncoding(byte[] data, string encoded, MimeBase64Mode mode)
        {
            // arrange
            var encoder = new MimeBase64Encoder(mode);
            var destination = new byte[encoded.Length];

            // act
            var testResult = encoder.Encode(data, destination, out var consumed, out var written, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(data.Length);
            written.Should().Be(encoded.Length);
            Encoding.ASCII.GetString(destination).Should().Be(encoded);
        }

        [Theory]
        [MemberData(nameof(Rfc4648VectorsToDecode))]
        public void TryDecode_Rfc4648Vector_ReturnsData(string encoded, MimeBase64Mode mode, MimeBase64DecoderOptions options, byte[] data)
        {
            // act
            var testResult = MimeBase64.TryDecode(encoded.AsSpan(), mode, options, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Equal(data);
        }

        [Theory]
        [MemberData(nameof(Rfc4648VectorsToDecode))]
        public void Decode_Rfc4648Vector_ReturnsData(string encoded, MimeBase64Mode mode, MimeBase64DecoderOptions options, byte[] data)
        {
            // act
            var testResult = MimeBase64.Decode(encoded.AsSpan(), mode, options);

            // assert
            testResult.Should().Equal(data);
        }

        [Theory]
        [MemberData(nameof(Rfc4648VectorsToDecode))]
        public void DecodeChars_Rfc4648VectorInOneFinalBlock_WritesData(string encoded, MimeBase64Mode mode, MimeBase64DecoderOptions options,
            byte[] data)
        {
            // arrange
            var decoder = new MimeBase64Decoder(mode, options);
            var destination = new byte[data.Length];

            // act
            var testResult = decoder.Decode(encoded.AsSpan(), destination, out var consumed, out var written, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(encoded.Length);
            written.Should().Be(data.Length);
            destination.Should().Equal(data);
        }

        [Theory]
        [MemberData(nameof(Rfc4648VectorsToDecode))]
        public void DecodeUtf8_Rfc4648VectorInOneFinalBlock_WritesData(string encoded, MimeBase64Mode mode, MimeBase64DecoderOptions options,
            byte[] data)
        {
            // arrange
            var decoder = new MimeBase64Decoder(mode, options);
            var source = Encoding.ASCII.GetBytes(encoded);
            var destination = new byte[data.Length];

            // act
            var testResult = decoder.Decode(source, destination, out var consumed, out var written, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(source.Length);
            written.Should().Be(data.Length);
            destination.Should().Equal(data);
        }

        #endregion

        #region Encoding

        [Theory]
        [MemberData(nameof(EveryLength))]
        public void Encode_EveryLength_MatchesConvert(byte[] data, MimeBase64Mode mode, string expected)
        {
            // act
            var testResult = MimeBase64.Encode(data, mode);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EveryLength))]
        public void GetEncodedLength_EveryLength_MatchesConvert(byte[] data, MimeBase64Mode mode, string expected)
        {
            // act
            var testResult = MimeBase64.GetEncodedLength(data.Length, mode);

            // assert
            testResult.Should().Be(expected.Length);
        }

        // Zero bytes that fill one line, and one byte more, and two lines, and one byte more.
        public static TheoryData<int, string> ZeroBytesInMimeLines => new TheoryData<int, string>
        {
            { 57, MimeLineOfZeros },
            { 58, MimeLineOfZeros + MimeLineBreak + OneZeroByte },
            { 114, MimeLineOfZeros + MimeLineBreak + MimeLineOfZeros },
            { 115, MimeLineOfZeros + MimeLineBreak + MimeLineOfZeros + MimeLineBreak + OneZeroByte },
        };

        [Theory]
        [MemberData(nameof(ZeroBytesInMimeLines))]
        public void Encode_Mime_BreaksLinesAt76Characters(int length, string expected)
        {
            // arrange
            var data = new byte[length];

            // act
            var testResult = MimeBase64.Encode(data, MimeBase64Mode.Mime);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(LineOfTextSplits))]
        public void EncodeToChars_SplitAtAnyPoint_MatchesOneCall(MimeBase64Mode mode, byte[] first, byte[] second, string expected)
        {
            // arrange
            var encoder = new MimeBase64Encoder(mode);
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
        [MemberData(nameof(LineOfTextSplits))]
        public void EncodeToUtf8_SplitAtAnyPoint_MatchesOneCall(MimeBase64Mode mode, byte[] first, byte[] second, string expected)
        {
            // arrange
            var encoder = new MimeBase64Encoder(mode);
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
        [MemberData(nameof(LineOfTextSmallDestinations))]
        public void EncodeToChars_AfterDestinationTooSmall_ContinuesWhereItStopped(MimeBase64Mode mode, byte[] data, int destinationLength,
            string expected)
        {
            // arrange
            var encoder = new MimeBase64Encoder(mode);
            var destination = new char[expected.Length];
            encoder.Encode(data, destination.AsSpan(0, destinationLength), out var firstConsumed, out var firstWritten, true);

            // act
            var testResult = encoder.Encode(data.AsSpan(firstConsumed), destination.AsSpan(firstWritten), out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            new string(destination).Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(LineOfTextSmallDestinations))]
        public void EncodeToUtf8_AfterDestinationTooSmall_ContinuesWhereItStopped(MimeBase64Mode mode, byte[] data, int destinationLength,
            string expected)
        {
            // arrange
            var encoder = new MimeBase64Encoder(mode);
            var destination = new byte[expected.Length];
            encoder.Encode(data, destination.AsSpan(0, destinationLength), out var firstConsumed, out var firstWritten, true);

            // act
            var testResult = encoder.Encode(data.AsSpan(firstConsumed), destination.AsSpan(firstWritten), out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            Encoding.ASCII.GetString(destination).Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(LineOfTextSmallDestinations))]
        public void Encode_DestinationTooSmall_ReturnsDestinationTooSmall(MimeBase64Mode mode, byte[] data, int destinationLength,
            string expected)
        {
            // arrange
            var encoder = new MimeBase64Encoder(mode);
            var destination = new char[destinationLength];

            // act
            var testResult = encoder.Encode(data, destination, out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.DestinationTooSmall, "{0} characters don't hold {1}", destinationLength, expected);
        }

        // A destination too small for the line break and the four characters of the next quantum.
        private const int DestinationTooSmallForLineBreak = 5;
        private const int OneQuantum = 3;
        private const string LineBreakAndQuantumOfZeros = "\r\nAAAA";

        [Fact]
        public void Encode_DestinationTooSmallForLineBreak_WritesNothing()
        {
            // arrange
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            encoder.Encode(new byte[BytesPerMimeLine], new char[MimeLineLength], out _, out _, false);

            // act
            var testResult = encoder.Encode(new byte[OneQuantum], new char[DestinationTooSmallForLineBreak], out var consumed, out var written,
                true);

            // assert
            testResult.Should().Be(OperationStatus.DestinationTooSmall);
            consumed.Should().Be(0);
            written.Should().Be(0);
        }

        [Fact]
        public void Encode_AfterDestinationTooSmallForLineBreak_WritesLineBreakAndQuantum()
        {
            // arrange
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            encoder.Encode(new byte[BytesPerMimeLine], new char[MimeLineLength], out _, out _, false);
            encoder.Encode(new byte[OneQuantum], new char[DestinationTooSmallForLineBreak], out _, out _, true);
            var destination = new char[LineBreakAndQuantumOfZeros.Length];

            // act
            var testResult = encoder.Encode(new byte[OneQuantum], destination, out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(OneQuantum);
            new string(destination).Should().Be("\r\nAAAA");
        }

        [Fact]
        public void Encode_PartialQuantum_HoldsBytesUntilFinalBlock()
        {
            // arrange
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Strict);
            var destination = new char[VectorFoEncoded.Length];

            // act
            var testResult = encoder.Encode(VectorFoBytes, destination, out var consumed, out var written, false);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(VectorFoBytes.Length);
            written.Should().Be(0);
        }

        [Fact]
        public void Encode_FinalBlockAfterPartialQuantum_WritesHeldBytes()
        {
            // arrange
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Strict);
            var destination = new char[VectorFoEncoded.Length];
            encoder.Encode(VectorFoBytes, destination, out _, out _, false);

            // act
            var testResult = encoder.Encode(ReadOnlySpan<byte>.Empty, destination, out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(0);
            new string(destination).Should().Be(VectorFoEncoded);
        }

        [Fact]
        public void Encode_AfterFinalBlock_StartsNewData()
        {
            // arrange
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            var data = Encoding.ASCII.GetBytes(LineOfText);
            encoder.Encode(data, new char[LineOfTextMime.Length], out _, out _, true);
            var destination = new char[LineOfTextMime.Length];

            // act
            var testResult = encoder.Encode(data, destination, out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            new string(destination).Should().Be(LineOfTextMime);
        }

        [Fact]
        public void Reset_Encoder_DiscardsHeldBytesAndLineLength()
        {
            // arrange
            var encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);
            encoder.Encode(Encoding.ASCII.GetBytes(LineOfText), new char[LineOfTextMime.Length], out _, out _, false);
            encoder.Reset();
            var destination = new char[VectorFEncoded.Length];

            // act
            var testResult = encoder.Encode(VectorFBytes, destination, out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            new string(destination).Should().Be(VectorFEncoded);
        }

        // Four bytes encode to eight characters, one more than this.
        private const int OneTooFewForFourBytes = 7;

        [Fact]
        public void TryEncode_DestinationTooShort_ReturnsFalse()
        {
            // act
            var testResult = MimeBase64.TryEncode(new byte[] { 1, 2, 3, 4 }, new char[OneTooFewForFourBytes], out var charsWritten,
                MimeBase64Mode.Strict);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        #endregion

        #region Decoding

        [Theory]
        [InlineData(FoobarWithLineBreak, MimeBase64DecoderOptions.None)]
        [InlineData(FoobarWithLineBreak, Reject)]
        [InlineData("Zm9v\r\nYmFy\r\n", Reject)]
        [InlineData("\r\nZm\r\n9vYm\r\nFy\r\n\r\n", Reject)]
        [InlineData(FoobarWithLineFeed, MimeBase64DecoderOptions.None)]
        [InlineData(FoobarWithCarriageReturn, MimeBase64DecoderOptions.None)]
        [InlineData("Zm 9v\tYm-Fy!", MimeBase64DecoderOptions.None)]
        [InlineData(FoobarWithLatin1Letter, MimeBase64DecoderOptions.None)]
        public void Decode_MimeWithOtherCharacters_IgnoresThem(string encoded, MimeBase64DecoderOptions options)
        {
            // act
            var testResult = MimeBase64.Decode(encoded.AsSpan(), MimeBase64Mode.Mime, options);

            // assert
            testResult.Should().Equal(VectorFoobarBytes);
        }

        [Theory]
        [InlineData(FEncodedWithLineBreak, MimeBase64DecoderOptions.None)]
        [InlineData(FEncodedWithLineBreak, Reject)]
        [InlineData("Zg=\r\n=", Reject)]
        [InlineData("Zg== -- \n", MimeBase64DecoderOptions.None)]
        public void Decode_MimeOtherCharactersAroundPadding_IgnoresThem(string encoded, MimeBase64DecoderOptions options)
        {
            // act
            var testResult = MimeBase64.Decode(encoded.AsSpan(), MimeBase64Mode.Mime, options);

            // assert
            testResult.Should().Equal(VectorFBytes);
        }

        public static TheoryData<string, int> MimeCharactersToReject => new TheoryData<string, int>
        {
            { FoobarWithLineFeed, 4 },
            { FoobarWithCarriageReturn, 5 },
            { "Zm9v\r\rYmFy", 5 },
            { "Zm9v\n\rYmFy", 4 },
            { "Zm9vYmFy\r", 9 },
            { "Zm 9vYmFy", 2 },
            { "Zm9v\tYmFy", 4 },
            { "Zm9vYmFy!", 8 },
            { FoobarWithLatin1Letter, 4 },
        };

        [Theory]
        [MemberData(nameof(MimeCharactersToReject))]
        public void Decode_MimeRejectingOtherCharacters_ReturnsInvalidDataAtCharacter(string encoded, int position)
        {
            // arrange
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Mime, Reject);
            var destination = new byte[VectorFoobarBytes.Length];

            // act
            var testResult = decoder.Decode(encoded.AsSpan(), destination, out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(position);
        }

        [Theory]
        [MemberData(nameof(MimeCharactersToReject))]
        public void TryDecode_MimeRejectingOtherCharacters_ReturnsFalse(string encoded, int position)
        {
            // act
            var testResult = MimeBase64.TryDecode(encoded.AsSpan(), MimeBase64Mode.Mime, Reject, out var result);

            // assert
            testResult.Should().BeFalse("the character at {0} is rejected", position);
            result.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(StrictlyInvalid))]
        public void DecodeChars_StrictInvalid_ReturnsInvalidDataAtCharacter(string encoded, MimeBase64DecoderOptions options, int position)
        {
            // arrange
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict, options);
            var destination = new byte[MoreThanAnyInvalidValueDecodesTo];

            // act
            var testResult = decoder.Decode(encoded.AsSpan(), destination, out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(position);
        }

        [Theory]
        [MemberData(nameof(StrictlyInvalidBytes))]
        public void DecodeUtf8_StrictInvalid_ReturnsInvalidDataAtByte(byte[] encoded, MimeBase64DecoderOptions options, int position)
        {
            // arrange
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict, options);
            var destination = new byte[MoreThanAnyInvalidValueDecodesTo];

            // act
            var testResult = decoder.Decode(encoded, destination, out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(position);
        }

        [Theory]
        [MemberData(nameof(StrictlyInvalid))]
        public void TryDecode_StrictInvalid_ReturnsFalse(string encoded, MimeBase64DecoderOptions options, int position)
        {
            // act
            var testResult = MimeBase64.TryDecode(encoded.AsSpan(), MimeBase64Mode.Strict, options, out var result);

            // assert
            testResult.Should().BeFalse("the character at {0} is invalid", position);
            result.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(MimeBadPaddingOrTruncated))]
        public void TryDecode_MimeBadPaddingOrTruncated_ReturnsFalse(string encoded, MimeBase64DecoderOptions options)
        {
            // act
            var testResult = MimeBase64.TryDecode(encoded.AsSpan(), MimeBase64Mode.Mime, options, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(MimeBadPaddingOrTruncated))]
        public void Decode_MimeBadPaddingOrTruncated_ThrowsFormatException(string encoded, MimeBase64DecoderOptions options)
        {
            // act
            Action testAction = () => MimeBase64.Decode(encoded.AsSpan(), MimeBase64Mode.Mime, options);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        // "foo" and two characters of a quantum that never ends, which decode to three bytes.
        private const string TruncatedInSecondQuantum = "Zm9vYm";
        private const int DecodedBytesOfTruncated = 3;

        [Fact]
        public void Decode_TruncatedInFinalBlock_ConsumesAllAndReturnsInvalidData()
        {
            // arrange
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);
            var destination = new byte[VectorFoobarBytes.Length];

            // act
            var testResult = decoder.Decode(TruncatedInSecondQuantum.AsSpan(), destination, out var consumed, out var written, true);

            // assert
            testResult.Should().Be(OperationStatus.InvalidData);
            consumed.Should().Be(TruncatedInSecondQuantum.Length);
            written.Should().Be(DecodedBytesOfTruncated);
        }

        [Theory]
        [MemberData(nameof(NonZeroPadBits))]
        public void Decode_NonZeroPadBits_Accepted(string encoded, MimeBase64Mode mode, byte[] expected)
        {
            // arrange
            // ref. RFC 4648 §3.5: decoders MAY reject non-zero pad bits; these don't, as iCal's BINARY codec doesn't.

            // act
            var testResult = MimeBase64.Decode(encoded.AsSpan(), mode, MimeBase64DecoderOptions.None);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(EncodedLineOfTextSplits))]
        public void DecodeChars_SplitAtAnyPoint_MatchesOneCall(MimeBase64Mode mode, MimeBase64DecoderOptions options, string first,
            string second, byte[] expected)
        {
            // arrange
            var decoder = new MimeBase64Decoder(mode, options);
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
        [MemberData(nameof(EncodedLineOfTextSplits))]
        public void DecodeUtf8_SplitAtAnyPoint_MatchesOneCall(MimeBase64Mode mode, MimeBase64DecoderOptions options, string first,
            string second, byte[] expected)
        {
            // arrange
            var decoder = new MimeBase64Decoder(mode, options);
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
        [MemberData(nameof(EncodedLineOfTextSmallDestinations))]
        public void DecodeChars_AfterDestinationTooSmall_ContinuesWhereItStopped(MimeBase64Mode mode, string encoded, int destinationLength,
            byte[] expected)
        {
            // arrange
            var decoder = new MimeBase64Decoder(mode, Reject);
            var destination = new byte[expected.Length];
            decoder.Decode(encoded.AsSpan(), destination.AsSpan(0, destinationLength), out var firstConsumed, out var firstWritten, true);

            // act
            var testResult = decoder.Decode(encoded.AsSpan(firstConsumed), destination.AsSpan(firstWritten), out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            destination.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(EncodedLineOfTextSmallDestinations))]
        public void DecodeUtf8_AfterDestinationTooSmall_ContinuesWhereItStopped(MimeBase64Mode mode, string encoded, int destinationLength,
            byte[] expected)
        {
            // arrange
            var decoder = new MimeBase64Decoder(mode, Reject);
            var source = Encoding.ASCII.GetBytes(encoded);
            var destination = new byte[expected.Length];
            decoder.Decode(source, destination.AsSpan(0, destinationLength), out var firstConsumed, out var firstWritten, true);

            // act
            var testResult = decoder.Decode(source.AsSpan(firstConsumed), destination.AsSpan(firstWritten), out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            destination.Should().Equal(expected);
        }

        // Too small for the two bytes of "fo", which the decoder writes only once it has read the padding.
        private const int DestinationTooSmallForFo = 1;
        private const int CharactersBeforePadding = 3;

        [Fact]
        public void Decode_DestinationTooSmallForPadding_WritesNothing()
        {
            // arrange
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);

            // act
            var testResult = decoder.Decode(VectorFoEncoded.AsSpan(), new byte[DestinationTooSmallForFo], out var consumed, out var written,
                true);

            // assert
            testResult.Should().Be(OperationStatus.DestinationTooSmall);
            consumed.Should().Be(CharactersBeforePadding);
            written.Should().Be(0);
        }

        [Fact]
        public void Decode_PaddingAfterDestinationTooSmall_WritesHeldBytes()
        {
            // arrange
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);
            decoder.Decode(VectorFoEncoded.AsSpan(), new byte[DestinationTooSmallForFo], out _, out _, true);
            var destination = new byte[VectorFoBytes.Length];

            // act
            var testResult = decoder.Decode("=".AsSpan(), destination, out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(1);
            destination.Should().Equal(VectorFoBytes);
        }

        // "foo", then one character of the quantum of "b", which the decoder holds; and the rest of that quantum.
        private const string FooAndPartOfQuantum = "Zm9vY";
        private const string RestOfQuantum = "g==";
        private const int BytesOfFoo = 3;

        [Fact]
        public void Decode_PartialQuantum_HoldsItUntilNextBlock()
        {
            // arrange
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);
            var destination = new byte[VectorFoobarBytes.Length];

            // act
            var testResult = decoder.Decode(FooAndPartOfQuantum.AsSpan(), destination, out var consumed, out var written, false);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(FooAndPartOfQuantum.Length);
            written.Should().Be(BytesOfFoo);
        }

        [Fact]
        public void Decode_RestOfHeldQuantum_WritesItsData()
        {
            // arrange
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);
            decoder.Decode(FooAndPartOfQuantum.AsSpan(), new byte[BytesOfFoo], out _, out _, false);
            var destination = new byte[VectorFBytes.Length];

            // act
            var testResult = decoder.Decode(RestOfQuantum.AsSpan(), destination, out var consumed, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            consumed.Should().Be(RestOfQuantum.Length);
            destination.Should().Equal(0x62);
        }

        [Fact]
        public void Decode_AfterFinalBlock_StartsNewData()
        {
            // arrange
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);
            decoder.Decode(VectorFEncoded.AsSpan(), new byte[VectorFBytes.Length], out _, out _, true);
            var destination = new byte[VectorFoBytes.Length];

            // act
            var testResult = decoder.Decode(VectorFoEncoded.AsSpan(), destination, out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            destination.Should().Equal(VectorFoBytes);
        }

        [Fact]
        public void Reset_Decoder_DiscardsHeldQuantumAndPadding()
        {
            // arrange
            var decoder = new MimeBase64Decoder(MimeBase64Mode.Strict);
            decoder.Decode(VectorFEncoded.AsSpan(), new byte[VectorFBytes.Length], out _, out _, false);
            decoder.Reset();
            var destination = new byte[VectorFoBytes.Length];

            // act
            var testResult = decoder.Decode(VectorFoEncoded.AsSpan(), destination, out _, out _, true);

            // assert
            testResult.Should().Be(OperationStatus.Done);
            destination.Should().Equal(VectorFoBytes);
        }

        #endregion

        #region iCal BINARY parity

        [Fact]
        public void TryDecode_IcalRfcExample_ReturnsData()
        {
            // act
            var testResult = MimeBase64.TryDecode(IcalBinaryExample.AsSpan(), MimeBase64Mode.Strict, MimeBase64DecoderOptions.None,
                out var bytes);

            // assert
            testResult.Should().BeTrue();
            bytes.Should().Equal(IcalBinaryExampleBytes);
        }

        [Fact]
        public void Encode_IcalRfcExampleData_ReturnsExample()
        {
            // act
            var testResult = MimeBase64.Encode(IcalBinaryExampleBytes, MimeBase64Mode.Strict);

            // assert
            testResult.Should().Be(IcalBinaryExample);
        }

        [Theory]
        [MemberData(nameof(IcalLengths))]
        public void Encode_IcalLength_MatchesConvert(byte[] data, string expected)
        {
            // act
            var testResult = MimeBase64.Encode(data, MimeBase64Mode.Strict);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(IcalLengths))]
        public void TryDecode_IcalLength_ReturnsData(byte[] data, string encoded)
        {
            // act
            var testResult = MimeBase64.TryDecode(encoded.AsSpan(), MimeBase64Mode.Strict, MimeBase64DecoderOptions.None, out var parsed);

            // assert
            testResult.Should().BeTrue();
            parsed.Should().Equal(data);
        }

        [Theory]
        [InlineData(OneCharacter)]
        [InlineData(ThreeCharacters)]
        [InlineData(PaddingBeforeData)]
        [InlineData(ThreePaddingCharacters)]
        [InlineData(OnlyPadding)]
        [InlineData(SpaceInside)]
        [InlineData(LineBreakInside)]
        [InlineData(UrlSafeAlphabet)]
        public void TryDecode_IcalInvalidValue_Fails(string value)
        {
            // act
            var testResult = MimeBase64.TryDecode(value.AsSpan(), MimeBase64Mode.Strict, MimeBase64DecoderOptions.None, out var bytes);

            // assert
            testResult.Should().BeFalse();
            bytes.Should().BeNull();
        }

        #endregion

        #region Arguments

        [Theory]
        [MemberData(nameof(MembersWithUndefinedModeOrOptions))]
        public void Members_UndefinedModeOrOptions_ThrowArgumentOutOfRangeException(Action member)
        {
            // act
            var testAction = member;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void EncoderConstructor_Mode_SetsMode()
        {
            // act
            var testResult = new MimeBase64Encoder(MimeBase64Mode.Mime);

            // assert
            testResult.Mode.Should().Be(MimeBase64Mode.Mime);
        }

        [Fact]
        public void DecoderConstructor_ModeAndOptions_SetsModeAndOptions()
        {
            // act
            var testResult = new MimeBase64Decoder(MimeBase64Mode.Mime, Reject);

            // assert
            testResult.Mode.Should().Be(MimeBase64Mode.Mime);
            testResult.Options.Should().Be(Reject);
        }

        [Fact]
        public void DecoderConstructor_ModeOnly_SetsNoOptions()
        {
            // act
            var testResult = new MimeBase64Decoder(MimeBase64Mode.Strict);

            // assert
            testResult.Options.Should().Be(MimeBase64DecoderOptions.None);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(3, 0)]
        [InlineData(4, 3)]
        [InlineData(7, 3)]
        [InlineData(8, 6)]
        [InlineData(int.MaxValue, MaxDecodedLengthOfLongestSource)]
        public void GetMaxDecodedLength_Length_ReturnsThreeBytesPerWholeQuantum(int length, int expected)
        {
            // act
            var testResult = MimeBase64.GetMaxDecodedLength(length);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(LengthHelpersWithLengthOutOfRange))]
        public void LengthHelpers_LengthOutOfRange_ThrowArgumentOutOfRangeException(Action member)
        {
            // act
            var testAction = member;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void GetEncodedLength_LongestEncodableLength_ReturnsLongestEncodedLength()
        {
            // act
            var testResult = MimeBase64.GetEncodedLength(LongestEncodableLength, MimeBase64Mode.Strict);

            // assert
            testResult.Should().Be(LongestEncodedLength);
        }

        #endregion

        // Bytes that reach every value, for the EveryLength factory.
        private static byte[] CreateData(int length)
        {
            var bytes = new byte[length];
            for (var index = 0; index < length; index++)
            {
                bytes[index] = (byte)(index * 151 + 7);
            }

            return bytes;
        }
    }
}
