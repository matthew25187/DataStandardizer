// AssemblyLoadContext is only on .NET, and the netstandard1.1 build behaves the same whatever runtime loads it.
#if NETCOREAPP
using System.Collections.Generic;
using System.Runtime.Versioning;
using DataStandardizer.Communication.Mime;
using DataStandardizer.Communication.Tests.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.IntegrationTests.Mime
{
    /// <summary>
    /// Tests of the built-in encodings of the netstandard1.1 build of the library, which hand-codes US-ASCII, ISO-8859-1 and UTF-32, as
    /// netstandard1.1 has no <c>Encoding.ASCII</c>, <c>Encoding.Latin1</c> or <c>UTF32Encoding</c>.
    /// </summary>
    /// <remarks>
    /// They load that build from the file system, so they are integration tests; see <see cref="NetStandard11Build"/>.
    /// </remarks>
    public class NetStandard11CharsetTests
    {
        private const string NetStandard11FrameworkName = ".NETStandard,Version=v1.1";

        // ref. IANA Character Sets registry
        private const string UsAsciiName = "US-ASCII";
        private const string Latin1Name = "latin1";
        private const string Utf32BigEndianName = "UTF-32BE";
        private const string Utf32LittleEndianName = "UTF-32LE";

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

        [Fact]
        public void Load_NetStandard11Build_IsNotTheReferencedBuild()
        {
            // act
            var testResult = NetStandard11Build.Assembly;

            // assert
            testResult.Should().NotBeSameAs(typeof(MimeCharset).Assembly);
        }

        [Fact]
        public void Load_NetStandard11Build_TargetsNetStandard11()
        {
            // act
            var testResult = NetStandard11Build.Assembly.GetCustomAttributes(typeof(TargetFrameworkAttribute), false);

            // assert
            testResult.Should().ContainSingle().Which.Should().BeOfType<TargetFrameworkAttribute>()
                .Which.FrameworkName.Should().Be(NetStandard11FrameworkName);
        }

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void GetString_BuiltInName_ReturnsText(string name, string text, byte[] bytes)
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(name);

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
            var encoding = NetStandard11Build.GetEncoding(name);

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
            var encoding = NetStandard11Build.GetEncoding(name);

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
            var encoding = NetStandard11Build.GetEncoding(name);

            // act
            var testResult = NetStandard11Build.GetEncoding(nameInOtherCase);

            // assert
            testResult.Should().BeSameAs(encoding);
        }

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void GetDecoder_BuiltInNameOneByteAtATime_Decodes(string name, string text, byte[] bytes)
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(name);

            // act
            var testResult = EncodingBlocks.Decode(encoding, bytes, OneByte);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void GetDecoder_BuiltInNameThreeBytesAtATime_Decodes(string name, string text, byte[] bytes)
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(name);

            // act
            var testResult = EncodingBlocks.Decode(encoding, bytes, ThreeBytes);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void GetEncoder_BuiltInNameOneCharacterAtATime_Encodes(string name, string text, byte[] bytes)
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(name);

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
            var encoding = NetStandard11Build.GetEncoding(name);

            // act
            var testResult = encoding.GetString(markedBytes);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(ByteOrderMarks))]
        public void GetDecoder_UnmarkedSchemeWithByteOrderMarkOneByteAtATime_UsesItsByteOrderAndRemovesIt(string name, byte[] markedBytes,
            string text)
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(name);

            // act
            var testResult = EncodingBlocks.Decode(encoding, markedBytes, OneByte);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(Latin1Octets))]
        public void GetString_Latin1Octet_ReturnsCharacterWithSameCodePoint(byte octet, char character)
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(Latin1Name);

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
            var encoding = NetStandard11Build.GetEncoding(Latin1Name);

            // act
            var testResult = encoding.GetBytes(new[] { character });

            // assert
            testResult.Should().Equal(octet);
        }

        [Fact]
        public void GetBytes_Latin1CharactersOutsideRange_WritesQuestionMarks()
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(Latin1Name);

            // act
            // U+0100, a surrogate pair, and an unpaired high surrogate.
            var testResult = encoding.GetBytes("\x0100\xD83D\xDE00\xD83D");

            // assert
            testResult.Should().Equal((byte)'?', (byte)'?', (byte)'?', (byte)'?');
        }

        [Fact]
        public void GetString_UsAsciiBytesOutsideRange_ReturnsReplacementCharacters()
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(UsAsciiName);

            // act
            var testResult = encoding.GetString(new byte[] { 0x41, 0x80, 0xFF });

            // assert
            testResult.Should().Be("A\xFFFD\xFFFD");
        }

        [Fact]
        public void GetBytes_UsAsciiCharactersOutsideRange_WritesQuestionMarks()
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(UsAsciiName);

            // act
            var testResult = encoding.GetBytes("A\x0080\xD83D\xDE00");

            // assert
            testResult.Should().Equal((byte)'A', (byte)'?', (byte)'?', (byte)'?');
        }

        // ref. The Unicode Standard §3.9, D76: code units above U+10FFFF, and surrogates, aren't Unicode scalar values; nor are four bytes
        // that are negative as a signed integer. An incomplete code unit at the end can't be read either.
        public static TheoryData<string, byte[]> InvalidUtf32CodeUnits => new TheoryData<string, byte[]>
        {
            { Utf32BigEndianName, new byte[] { 0x00, 0x11, 0x00, 0x00 } },
            { Utf32BigEndianName, new byte[] { 0x00, 0x00, 0xD8, 0x00 } },
            { Utf32BigEndianName, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF } },
            { Utf32LittleEndianName, new byte[] { 0x00, 0xDC, 0x00, 0x00 } },
            { Utf32BigEndianName, new byte[] { 0x00, 0x00 } },
            { Utf32LittleEndianName, new byte[] { 0x41 } },
        };

        [Theory]
        [MemberData(nameof(InvalidUtf32CodeUnits))]
        public void GetString_Utf32InvalidCodeUnit_ReturnsReplacementCharacter(string name, byte[] bytes)
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(name);

            // act
            var testResult = encoding.GetString(bytes);

            // assert
            testResult.Should().Be(ReplacementCharacter);
        }

        [Theory]
        [MemberData(nameof(InvalidUtf32CodeUnits))]
        public void GetDecoder_Utf32InvalidCodeUnitOneByteAtATime_ReturnsReplacementCharacter(string name, byte[] bytes)
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(name);

            // act
            var testResult = EncodingBlocks.Decode(encoding, bytes, OneByte);

            // assert
            testResult.Should().Be(ReplacementCharacter);
        }

        // The UTF-32BE bytes of U+FFFD REPLACEMENT CHARACTER, which the encoder writes for an unpaired surrogate (The Unicode Standard §3.9,
        // D76), and of "A". An attribute can't hold the unpaired surrogates, as it keeps strings as UTF-8.
        public static TheoryData<string, byte[]> UnpairedSurrogates => new TheoryData<string, byte[]>
        {
            { "\xD83D", new byte[] { 0x00, 0x00, 0xFF, 0xFD } },
            { "\xDE00A", new byte[] { 0x00, 0x00, 0xFF, 0xFD, 0x00, 0x00, 0x00, 0x41 } },
            { "\xD83DA", new byte[] { 0x00, 0x00, 0xFF, 0xFD, 0x00, 0x00, 0x00, 0x41 } },
            { "A\xDE00\xD83D", new byte[] { 0x00, 0x00, 0x00, 0x41, 0x00, 0x00, 0xFF, 0xFD, 0x00, 0x00, 0xFF, 0xFD } },
        };

        [Theory]
        [MemberData(nameof(UnpairedSurrogates), DisableDiscoveryEnumeration = true)]
        public void GetBytes_Utf32UnpairedSurrogate_WritesReplacementCharacter(string text, byte[] bytes)
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(Utf32BigEndianName);

            // act
            var testResult = encoding.GetBytes(text);

            // assert
            testResult.Should().Equal(bytes);
        }

        [Theory]
        [MemberData(nameof(UnpairedSurrogates), DisableDiscoveryEnumeration = true)]
        public void GetEncoder_Utf32UnpairedSurrogateOneCharacterAtATime_WritesReplacementCharacter(string text, byte[] bytes)
        {
            // arrange
            var encoding = NetStandard11Build.GetEncoding(Utf32BigEndianName);

            // act
            var testResult = EncodingBlocks.EncodeOneCharacterAtATime(encoding, text);

            // assert
            testResult.Should().Equal(bytes);
        }

        [Fact]
        public void TryGetEncoding_UnknownName_ReturnsFalse()
        {
            // act
            var testResult = NetStandard11Build.TryGetEncoding(UnknownName, out var encoding);

            // assert
            testResult.Should().BeFalse();
            encoding.Should().BeNull();
        }
    }
}
#endif
