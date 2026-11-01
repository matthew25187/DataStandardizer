using System;
using System.Collections.Generic;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;
using static DataStandardizer.Communication.Tests.Mime.MimeCharsetResolverTestData;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeCharsetResolverTests
    {
        // ref. IANA Character Sets registry
        private const string UsAsciiName = "US-ASCII";
        private const string Iso88591Name = "ISO-8859-1";
        private const string Utf8Name = "UTF-8";
        private const string Utf7Name = "UTF-7";
        private const string Windows1252Name = "windows-1252";
        private const string ShiftJisName = "Shift_JIS";

        private const string UnknownName = "x-no-such-charset";

        // ref. The Unicode Standard §23.8: U+FFFD REPLACEMENT CHARACTER, which a decoder writes for bytes it can't read.
        private const string ReplacementCharacter = "\xFFFD";

        private const int OneByte = 1;
        private const int ThreeBytes = 3;

        public static IEnumerable<object[]> BuiltInNames() => MimeCharsetResolverTestData.BuiltInNames();

        public static IEnumerable<object[]> BuiltInNameList() => MimeCharsetResolverTestData.BuiltInNameList();

        public static IEnumerable<object[]> BuiltInNamesInOtherCase() => MimeCharsetResolverTestData.BuiltInNamesInOtherCase();

        public static IEnumerable<object[]> ByteOrderMarks() => MimeCharsetResolverTestData.ByteOrderMarks();

        // ref. RFC 1345 §5; ISO/IEC 8859-1: each octet of ISO-8859-1 is the character with the same code point, U+0000 to U+00FF.
        public static TheoryData<byte, char> Latin1Octets()
        {
            var data = new TheoryData<byte, char>();
            for (var value = 0; value <= byte.MaxValue; value++)
            {
                data.Add((byte)value, (char)value);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void GetString_BuiltInName_ReturnsText(string name, string text, byte[] bytes)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = encoding.GetString(bytes);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void GetBytes_BuiltInName_ReturnsBytes(string name, string text, byte[] bytes)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = encoding.GetBytes(text);

            // assert
            testResult.Should().Equal(bytes);
        }

        [Theory]
        [MemberData(nameof(BuiltInNameList))]
        public void GetPreamble_BuiltInName_ReturnsEmpty(string name)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = encoding.GetPreamble();

            // assert
            testResult.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(BuiltInNamesInOtherCase))]
        public void TryGetEncoding_BuiltInNameInOtherCase_ReturnsSameEncoding(string name, string nameInOtherCase)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = Resolve(nameInOtherCase);

            // assert
            testResult.Should().BeSameAs(encoding);
        }

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void GetDecoder_BuiltInNameOneByteAtATime_Decodes(string name, string text, byte[] bytes)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = EncodingBlocks.Decode(encoding, bytes, OneByte);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void GetEncoder_BuiltInNameOneCharacterAtATime_Encodes(string name, string text, byte[] bytes)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = EncodingBlocks.EncodeOneCharacterAtATime(encoding, text);

            // assert
            testResult.Should().Equal(bytes);
        }

        [Theory]
        [MemberData(nameof(ByteOrderMarks))]
        public void GetString_UnmarkedSchemeWithByteOrderMark_UsesItsByteOrderAndRemovesIt(string name, byte[] markedBytes, string text)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = encoding.GetString(markedBytes);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(ByteOrderMarks))]
        public void GetCharCount_UnmarkedSchemeWithByteOrderMark_DoesNotCountTheMark(string name, byte[] markedBytes, string text)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = encoding.GetCharCount(markedBytes);

            // assert
            testResult.Should().Be(text.Length);
        }

        [Theory]
        [MemberData(nameof(ByteOrderMarks))]
        public void GetDecoder_UnmarkedSchemeWithByteOrderMarkOneByteAtATime_UsesItsByteOrderAndRemovesIt(string name, byte[] markedBytes,
            string text)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = EncodingBlocks.Decode(encoding, markedBytes, OneByte);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(ByteOrderMarks))]
        public void GetDecoder_UnmarkedSchemeWithByteOrderMarkThreeBytesAtATime_UsesItsByteOrderAndRemovesIt(string name, byte[] markedBytes,
            string text)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = EncodingBlocks.Decode(encoding, markedBytes, ThreeBytes);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [InlineData("UTF-16BE", new byte[] { 0xFE, 0xFF, 0x00, 0x41 })]
        [InlineData("UTF-16LE", new byte[] { 0xFF, 0xFE, 0x41, 0x00 })]
        [InlineData("UTF-32BE", new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x41 })]
        [InlineData("UTF-32LE", new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00 })]
        public void TryGetEncoding_MarkedScheme_KeepsLeadingZeroWidthNoBreakSpace(string name, byte[] bytes)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = encoding.GetString(bytes);

            // assert
            // ref. RFC 2781 §3.2: in UTF-16BE and UTF-16LE, U+FEFF at the start is a ZERO WIDTH NO-BREAK SPACE, not a byte order mark
            testResult.Should().Be("\xFEFFA");
        }

        // Bytes of an unmarked scheme that end before a byte order mark or a code unit could, with the text they decode to once the decoder
        // is flushed.
        public static TheoryData<string, byte[], string> BytesShorterThanByteOrderMark => new TheoryData<string, byte[], string>
        {
            { Utf16Name, new byte[] { 0x00 }, ReplacementCharacter },
            { Utf16Name, new byte[] { 0xFF }, ReplacementCharacter },
            { Utf16Name, new byte[] { 0xFE, 0xFF }, string.Empty },
            { Utf32Name, new byte[] { 0x00, 0x00, 0x00 }, ReplacementCharacter },
            { Utf32Name, new byte[] { 0x00, 0x00, 0xFE, 0xFF }, string.Empty },
            { Utf32Name, new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00 }, ReplacementCharacter },
        };

        [Theory]
        [MemberData(nameof(BytesShorterThanByteOrderMark))]
        public void GetString_UnmarkedSchemeShorterThanByteOrderMark_DecodesWhatItHas(string name, byte[] bytes, string expected)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = encoding.GetString(bytes);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(BytesShorterThanByteOrderMark))]
        public void GetDecoder_UnmarkedSchemeShorterThanByteOrderMarkFlushed_DecodesWhatItHas(string name, byte[] bytes, string expected)
        {
            // arrange
            var decoder = Resolve(name).GetDecoder();
            var chars = new char[expected.Length];

            // act
            var testResult = decoder.GetChars(bytes, 0, bytes.Length, chars, 0, true);

            // assert
            testResult.Should().Be(expected.Length);
            new string(chars).Should().Be(expected);
        }

#if NETCOREAPP
        // .NET's StreamReader flushes the decoder at the end of the stream; .NET Framework's doesn't.
        [Theory]
        [MemberData(nameof(BytesShorterThanByteOrderMark))]
        public void GetDecoder_UnmarkedSchemeShorterThanByteOrderMarkOneByteAtATime_DecodesWhatItHas(string name, byte[] bytes,
            string expected)
        {
            // arrange
            var encoding = Resolve(name);

            // act
            var testResult = EncodingBlocks.Decode(encoding, bytes, OneByte);

            // assert
            testResult.Should().Be(expected);
        }
#endif

        // The bytes of UTF-16 "A" and "B", little-endian after a byte order mark, and big-endian without one.
        private static readonly byte[] Utf16LittleEndianMarkedA = { 0xFF, 0xFE, 0x41, 0x00 };
        private static readonly byte[] Utf16BigEndianB = { 0x00, 0x42 };
        private const int UnmarkedCharacterCount = 1;

        [Fact]
        public void GetDecoder_UnmarkedScheme_DetectsByteOrderMarkAfterReset()
        {
            // arrange
            var decoder = Resolve(Utf16Name).GetDecoder();
            decoder.GetChars(Utf16LittleEndianMarkedA, 0, Utf16LittleEndianMarkedA.Length, new char[UnmarkedCharacterCount], 0, false);
            decoder.Reset();
            var chars = new char[UnmarkedCharacterCount];

            // act
            var testResult = decoder.GetChars(Utf16BigEndianB, 0, Utf16BigEndianB.Length, chars, 0, true);

            // assert
            testResult.Should().Be(UnmarkedCharacterCount);
            chars.Should().Equal('B');
        }

        [Theory]
        [MemberData(nameof(Latin1Octets))]
        public void GetString_Latin1Octet_ReturnsCharacterWithSameCodePoint(byte octet, char character)
        {
            // arrange
            var encoding = Resolve(Iso88591Name);

            // act
            var testResult = encoding.GetString(new[] { octet });

            // assert
            testResult.Should().Be(character.ToString());
        }

        [Theory]
        [MemberData(nameof(Latin1Octets))]
        public void GetBytes_Latin1Character_ReturnsOctetWithSameValue(byte octet, char character)
        {
            // arrange
            var encoding = Resolve(Iso88591Name);

            // act
            var testResult = encoding.GetBytes(new[] { character });

            // assert
            testResult.Should().Equal(octet);
        }

        [Fact]
        public void GetBytes_Latin1CharactersOutsideRange_WritesQuestionMarks()
        {
            // arrange
            // On its own, Encoding.Latin1 writes a best fit, such as 'A' for U+0100.
            var encoding = Resolve(Iso88591Name);

            // act
            var testResult = encoding.GetBytes("\x0100\xD83D\xDE00");

            // assert
            testResult.Should().Equal((byte)'?', (byte)'?', (byte)'?');
        }

        [Fact]
        public void GetString_UsAsciiBytesOutsideRange_ReturnsReplacementCharacters()
        {
            // arrange
            // On its own, the runtime's US-ASCII reads 0x80 as '?' on .NET Framework, and as U+FFFD on .NET.
            var encoding = Resolve(UsAsciiName);

            // act
            var testResult = encoding.GetString(new byte[] { 0x41, 0x80, 0xFF });

            // assert
            testResult.Should().Be("A\xFFFD\xFFFD");
        }

        [Fact]
        public void GetBytes_UsAsciiCharactersOutsideRange_WritesQuestionMarks()
        {
            // arrange
            var encoding = Resolve(UsAsciiName);

            // act
            var testResult = encoding.GetBytes("A\x0080\x00E9\xD83D\xDE00");

            // assert
            testResult.Should().Equal((byte)'A', (byte)'?', (byte)'?', (byte)'?', (byte)'?');
        }

        [Fact]
        public void GetString_Utf8InvalidByte_ReturnsReplacementCharacter()
        {
            // arrange
            var encoding = Resolve(Utf8Name);

            // act
            var testResult = encoding.GetString(new byte[] { 0x41, 0xFF });

            // assert
            testResult.Should().Be("A\xFFFD");
        }

        [Theory]
        [InlineData("utf-16")]
        [InlineData(Utf32Name)]
        public void TryGetEncoding_UnmarkedScheme_IsBigEndianUnlikeRuntime(string name)
        {
            // arrange
            // Encoding.GetEncoding("utf-16") and ("utf-32") are little-endian; RFC 2781 §4.3 says unmarked UTF-16 is big-endian.
            var encoding = Resolve(name);

            // act
            var testResult = encoding.GetBytes("A");

            // assert
            testResult[0].Should().Be(0x00);
        }

#if NETFRAMEWORK
        // ref. RFC 2152: "+AOE-" is U+00E1 in UTF-7.
        private static readonly byte[] Utf7Bytes = { 0x2B, 0x41, 0x4F, 0x45, 0x2D };
        private const string Utf7Text = "\x00E1";

        [Fact]
        public void TryGetEncoding_RuntimeEncodingOnNetFramework_IsResolved()
        {
            // arrange
            // UTF-7 isn't built in; .NET Framework has it.
            var encoding = Resolve(Utf7Name);

            // act
            var testResult = encoding.GetString(Utf7Bytes);

            // assert
            testResult.Should().Be(Utf7Text);
        }
#else
        [Theory]
        // UTF-7 isn't built in, and .NET 5 and later don't have it; nor do they have code pages until a provider is registered, which only
        // the integration tests do.
        [InlineData(Utf7Name)]
        [InlineData(Windows1252Name)]
        [InlineData(ShiftJisName)]
        public void TryGetEncoding_EncodingTheRuntimeDoesNotHave_ReturnsFalse(string name)
        {
            // arrange
            var charset = MimeCharset.Parse(name);

            // act
            var testResult = MimeCharsetResolver.Default.TryGetEncoding(charset, out var encoding);

            // assert
            testResult.Should().BeFalse();
            encoding.Should().BeNull();
        }
#endif

        [Fact]
        public void TryGetEncoding_UnknownName_ReturnsFalse()
        {
            // arrange
            var charset = MimeCharset.Parse(UnknownName);

            // act
            var testResult = MimeCharsetResolver.Default.TryGetEncoding(charset, out var encoding);

            // assert
            testResult.Should().BeFalse();
            encoding.Should().BeNull();
        }

        [Fact]
        public void TryGetEncoding_UnknownNameWithFallback_UsesFallback()
        {
            // arrange
            var fallback = new StubResolver(Encoding.UTF8);
            var resolver = new MimeCharsetResolver(fallback);
            var charset = MimeCharset.Parse(UnknownName);

            // act
            var testResult = resolver.TryGetEncoding(charset, out var encoding);

            // assert
            testResult.Should().BeTrue();
            encoding.Should().BeSameAs(Encoding.UTF8);
            fallback.Requested.Should().Equal(charset);
        }

        [Fact]
        public void TryGetEncoding_UnknownNameWithFallbackThatDoesNotResolve_ReturnsFalse()
        {
            // arrange
            var resolver = new MimeCharsetResolver(new StubResolver(null));
            var charset = MimeCharset.Parse(UnknownName);

            // act
            var testResult = resolver.TryGetEncoding(charset, out var encoding);

            // assert
            testResult.Should().BeFalse();
            encoding.Should().BeNull();
        }

        [Theory]
        [InlineData(Utf8Name, "utf-8")]
        [InlineData("latin1", "iso-8859-1")]
#if NETFRAMEWORK
        // UTF-7 is a runtime encoding only on .NET Framework.
        [InlineData("utf-7", "utf-7")]
#endif
        public void TryGetEncoding_KnownNameWithFallback_DoesNotUseFallback(string name, string expectedWebName)
        {
            // arrange
            var fallback = new StubResolver(Encoding.UTF8);
            var resolver = new MimeCharsetResolver(fallback);
            var charset = MimeCharset.Parse(name);

            // act
            var testResult = resolver.TryGetEncoding(charset, out var encoding);

            // assert
            testResult.Should().BeTrue();
            encoding.WebName.Should().Be(expectedWebName);
            fallback.Requested.Should().BeEmpty();
        }

        // Bytes that are not valid in an unmarked Unicode scheme, which is read as big-endian.
        public static TheoryData<string, byte[]> InvalidSchemeBytes => new TheoryData<string, byte[]>
        {
            // ref. RFC 2781 §2.2: 0xD800 is a high surrogate, which must be followed by a low surrogate.
            { Utf16Name, new byte[] { 0xD8, 0x00 } },
            // ref. The Unicode Standard §3.9, D76: 0x00110000 is above U+10FFFF, so it is not a Unicode scalar value.
            { Utf32Name, new byte[] { 0x00, 0x11, 0x00, 0x00 } },
        };

        [Theory]
        [MemberData(nameof(InvalidSchemeBytes))]
        public void GetString_UnmarkedSchemeWithExceptionFallback_ThrowsDecoderFallbackException(string name, byte[] bytes)
        {
            // arrange
            var encoding = (Encoding)Resolve(name).Clone();
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;

            // act
            Action testAction = () => encoding.GetString(bytes);

            // assert
            testAction.Should().Throw<DecoderFallbackException>();
        }

        [Theory]
        [MemberData(nameof(InvalidSchemeBytes))]
        public void GetDecoder_UnmarkedSchemeWithExceptionFallback_ThrowsDecoderFallbackException(string name, byte[] bytes)
        {
            // arrange
            var encoding = (Encoding)Resolve(name).Clone();
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
            var decoder = encoding.GetDecoder();
            var chars = new char[bytes.Length];

            // act
            Action testAction = () => decoder.GetChars(bytes, 0, bytes.Length, chars, 0, true);

            // assert
            testAction.Should().Throw<DecoderFallbackException>();
        }

        // ref. RFC 2781 §2.1: an unpaired surrogate can't be written in UTF-16, and has no Unicode scalar value to write in UTF-32.
        private const string UnpairedHighSurrogate = "\xD800";

        [Theory]
        [InlineData(Utf16Name)]
        [InlineData(Utf32Name)]
        public void GetBytes_UnmarkedSchemeWithExceptionFallback_ThrowsEncoderFallbackException(string name)
        {
            // arrange
            var encoding = (Encoding)Resolve(name).Clone();
            encoding.EncoderFallback = EncoderFallback.ExceptionFallback;

            // act
            Action testAction = () => encoding.GetBytes(UnpairedHighSurrogate);

            // assert
            testAction.Should().Throw<EncoderFallbackException>();
        }

        // A replacement longer than the one character that the default fallbacks write.
        private const string LongReplacement = "[invalid]";

        [Theory]
        [MemberData(nameof(InvalidSchemeBytes))]
        public void GetMaxCharCount_UnmarkedSchemeWithLongReplacement_IsEnoughForGetChars(string name, byte[] bytes)
        {
            // arrange
            var encoding = (Encoding)Resolve(name).Clone();
            encoding.DecoderFallback = new DecoderReplacementFallback(LongReplacement);
            var chars = new char[encoding.GetMaxCharCount(bytes.Length)];

            // act
            Action testAction = () => encoding.GetChars(bytes, 0, bytes.Length, chars, 0);

            // assert
            testAction.Should().NotThrow();
        }

        [Theory]
        [InlineData(Utf16Name)]
        [InlineData(Utf32Name)]
        public void GetMaxByteCount_UnmarkedSchemeWithLongReplacement_IsEnoughForGetBytes(string name)
        {
            // arrange
            var encoding = (Encoding)Resolve(name).Clone();
            encoding.EncoderFallback = new EncoderReplacementFallback(LongReplacement);
            var chars = UnpairedHighSurrogate.ToCharArray();
            var bytes = new byte[encoding.GetMaxByteCount(chars.Length)];

            // act
            Action testAction = () => encoding.GetBytes(chars, 0, chars.Length, bytes, 0);

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void TryGetEncoding_UninitializedCharset_ThrowsArgumentException()
        {
            // act
            Action testAction = () => MimeCharsetResolver.Default.TryGetEncoding(default(MimeCharset), out _);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullFallback_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => _ = new MimeCharsetResolver(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        private static Encoding Resolve(string name)
        {
            MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Parse(name), out var encoding);
            return encoding;
        }

        private sealed class StubResolver : IMimeCharsetResolver
        {
            private readonly Encoding _encoding;

            public StubResolver(Encoding encoding)
            {
                _encoding = encoding;
            }

            public List<MimeCharset> Requested { get; } = new List<MimeCharset>();

            public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
            {
                Requested.Add(charset);
                encoding = _encoding;
                return encoding != null;
            }
        }
    }
}
