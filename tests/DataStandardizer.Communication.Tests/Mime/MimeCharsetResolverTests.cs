using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeCharsetResolverTests
    {
        public static IEnumerable<object[]> BuiltInNames() => MimeCharsetResolverTestData.BuiltInNames();

        public static IEnumerable<object[]> ByteOrderMarks() => MimeCharsetResolverTestData.ByteOrderMarks();

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void TryGetEncoding_BuiltInNameInAnyCase_DecodesAndEncodes(string name, string text, byte[] bytes)
        {
            var encoding = Resolve(name);

            encoding.GetString(bytes).Should().Be(text);
            encoding.GetBytes(text).Should().Equal(bytes);
            encoding.GetPreamble().Should().BeEmpty();
            Resolve(name.ToUpperInvariant()).Should().BeSameAs(encoding);
            Resolve(name.ToLowerInvariant()).Should().BeSameAs(encoding);
        }

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void GetDecoder_BuiltInNameOneByteAtATime_Decodes(string name, string text, byte[] bytes)
        {
            DecodeInBlocks(Resolve(name), bytes, 1).Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(BuiltInNames))]
        public void GetEncoder_BuiltInNameOneCharacterAtATime_Encodes(string name, string text, byte[] bytes)
        {
            EncodeInBlocks(Resolve(name), text, 1).Should().Equal(bytes);
        }

        [Theory]
        [MemberData(nameof(ByteOrderMarks))]
        public void TryGetEncoding_UnmarkedSchemeWithByteOrderMark_UsesItsByteOrderAndRemovesIt(string name, byte[] mark, string text,
            byte[] bytes)
        {
            var encoding = Resolve(name);
            var marked = mark.Concat(bytes).ToArray();

            encoding.GetString(marked).Should().Be(text);
            encoding.GetCharCount(marked).Should().Be(text.Length);
            DecodeInBlocks(encoding, marked, 1).Should().Be(text);
            DecodeInBlocks(encoding, marked, 3).Should().Be(text);
        }

        [Theory]
        [InlineData("UTF-16BE", new byte[] { 0xFE, 0xFF, 0x00, 0x41 })]
        [InlineData("UTF-16LE", new byte[] { 0xFF, 0xFE, 0x41, 0x00 })]
        [InlineData("UTF-32BE", new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x41 })]
        [InlineData("UTF-32LE", new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00 })]
        public void TryGetEncoding_MarkedScheme_KeepsLeadingZeroWidthNoBreakSpace(string name, byte[] bytes)
        {
            // ref. RFC 2781 §3.2: in UTF-16BE and UTF-16LE, U+FEFF at the start is a ZERO WIDTH NO-BREAK SPACE, not a byte order mark
            Resolve(name).GetString(bytes).Should().Be("\uFEFFA");
        }

        [Theory]
        [InlineData("UTF-16", new byte[] { 0x00 }, "\uFFFD")]
        [InlineData("UTF-16", new byte[] { 0xFF }, "\uFFFD")]
        [InlineData("UTF-16", new byte[] { 0xFE, 0xFF }, "")]
        [InlineData("UTF-32", new byte[] { 0x00, 0x00, 0x00 }, "\uFFFD")]
        [InlineData("UTF-32", new byte[] { 0x00, 0x00, 0xFE, 0xFF }, "")]
        [InlineData("UTF-32", new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00 }, "\uFFFD")]
        public void GetDecoder_UnmarkedSchemeShorterThanByteOrderMark_DecodesOnFlush(string name, byte[] bytes, string expected)
        {
            DecodeInBlocks(Resolve(name), bytes, 1).Should().Be(expected);
            Resolve(name).GetString(bytes).Should().Be(expected);
        }

        [Fact]
        public void GetDecoder_UnmarkedScheme_DetectsByteOrderMarkAfterReset()
        {
            var decoder = Resolve("UTF-16").GetDecoder();
            var chars = new char[4];
            decoder.GetChars(new byte[] { 0xFF, 0xFE, 0x41, 0x00 }, 0, 4, chars, 0, false).Should().Be(1);

            decoder.Reset();

            decoder.GetChars(new byte[] { 0x00, 0x42 }, 0, 2, chars, 0, true).Should().Be(1);
            chars[0].Should().Be('B');
        }

        [Fact]
        public void TryGetEncoding_Latin1_RoundTripsEveryByte()
        {
            var encoding = Resolve("ISO-8859-1");
            var bytes = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();

            var text = encoding.GetString(bytes);

            text.Select(c => (int)c).Should().Equal(Enumerable.Range(0, 256));
            encoding.GetBytes(text).Should().Equal(bytes);
        }

        [Fact]
        public void TryGetEncoding_Latin1_WritesQuestionMarkForCharacterOutsideRange()
        {
            // On its own, Encoding.Latin1 writes a best fit, such as 'A' for U+0100.
            Resolve("ISO-8859-1").GetBytes("\u0100\uD83D\uDE00").Should().Equal((byte)'?', (byte)'?', (byte)'?');
        }

        [Fact]
        public void TryGetEncoding_UsAscii_ReplacesCharactersOutsideRange()
        {
            // On its own, the runtime's US-ASCII reads 0x80 as '?' on .NET Framework, and as U+FFFD on .NET.
            var encoding = Resolve("US-ASCII");

            encoding.GetString(new byte[] { 0x41, 0x80, 0xFF }).Should().Be("A\uFFFD\uFFFD");
            encoding.GetBytes("A\u0080\u00E9\uD83D\uDE00").Should().Equal((byte)'A', (byte)'?', (byte)'?', (byte)'?', (byte)'?');
        }

        [Fact]
        public void TryGetEncoding_Utf8_ReplacesInvalidBytes()
        {
            Resolve("UTF-8").GetString(new byte[] { 0x41, 0xFF }).Should().Be("A\uFFFD");
        }

        [Theory]
        [InlineData("utf-16")]
        [InlineData("UTF-32")]
        public void TryGetEncoding_UnmarkedScheme_IsBigEndianUnlikeRuntime(string name)
        {
            // Encoding.GetEncoding("utf-16") and ("utf-32") are little-endian; RFC 2781 §4.3 says unmarked UTF-16 is big-endian.
            Resolve(name).GetBytes("A").First().Should().Be(0x00);
        }

        [Fact]
        public void TryGetEncoding_RuntimeEncoding_IsResolved()
        {
            // UTF-7 isn't built in; .NET Framework has it, and .NET 5 and later don't.
#if NETFRAMEWORK
            Resolve("UTF-7").GetString(new byte[] { 0x2B, 0x41, 0x4F, 0x45, 0x2D }).Should().Be("\u00E1");
#else
            MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Parse("UTF-7"), out var encoding).Should().BeFalse();
            encoding.Should().BeNull();
#endif
        }

        [Fact]
        public void TryGetEncoding_CodePages_AreResolvedOnlyWhereTheRuntimeHasThem()
        {
            // The only test that registers an encoding provider, which can't be undone, so it checks both sides in order.
#if !NETFRAMEWORK
            MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Parse("windows-1252"), out var unregistered).Should().BeFalse();
            unregistered.Should().BeNull();
            MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Parse("Shift_JIS"), out unregistered).Should().BeFalse();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif

            Resolve("windows-1252").GetString(new byte[] { 0x80, 0x93, 0x94 }).Should().Be("\u20AC\u201C\u201D");
            Resolve("WINDOWS-1252").GetBytes("\u20AC").Should().Equal(0x80);
            Resolve("Shift_JIS").GetString(new byte[] { 0x93, 0xFA, 0x96, 0x7B }).Should().Be("\u65E5\u672C");
            Resolve("shift_jis").GetBytes("\u65E5\u672C").Should().Equal(0x93, 0xFA, 0x96, 0x7B);
        }

        [Fact]
        public void TryGetEncoding_UnknownName_ReturnsFalse()
        {
            MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Parse("x-no-such-charset"), out var encoding).Should().BeFalse();

            encoding.Should().BeNull();
        }

        [Fact]
        public void TryGetEncoding_UnknownNameWithFallback_UsesFallback()
        {
            var fallback = new StubResolver(Encoding.UTF8);
            var resolver = new MimeCharsetResolver(fallback);

            resolver.TryGetEncoding(MimeCharset.Parse("x-no-such-charset"), out var encoding).Should().BeTrue();

            encoding.Should().BeSameAs(Encoding.UTF8);
            fallback.Requested.Should().Equal(MimeCharset.Parse("x-no-such-charset"));
        }

        [Fact]
        public void TryGetEncoding_UnknownNameWithFallbackThatDoesNotResolve_ReturnsFalse()
        {
            var resolver = new MimeCharsetResolver(new StubResolver(null));

            resolver.TryGetEncoding(MimeCharset.Parse("x-no-such-charset"), out var encoding).Should().BeFalse();

            encoding.Should().BeNull();
        }

        [Theory]
        [InlineData("UTF-8")]
        [InlineData("latin1")]
        [InlineData("utf-7")]
        public void TryGetEncoding_KnownNameWithFallback_DoesNotUseFallback(string name)
        {
#if !NETFRAMEWORK
            // UTF-7 is not a runtime encoding on .NET 5 and later.
            if (name == "utf-7") return;
#endif
            var fallback = new StubResolver(Encoding.UTF8);
            var resolver = new MimeCharsetResolver(fallback);

            resolver.TryGetEncoding(MimeCharset.Parse(name), out var encoding).Should().BeTrue();

            encoding.WebName.Should().Be(Resolve(name).WebName);
            fallback.Requested.Should().BeEmpty();
        }

        [Fact]
        public void TryGetEncoding_UninitializedCharset_ThrowsArgumentException()
        {
            FluentActions.Invoking(() => MimeCharsetResolver.Default.TryGetEncoding(default(MimeCharset), out _))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullFallback_ThrowsArgumentNullException()
        {
            FluentActions.Invoking(() => new MimeCharsetResolver(null)).Should().Throw<ArgumentNullException>();
        }

        private static Encoding Resolve(string name)
        {
            MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Parse(name), out var encoding).Should().BeTrue("{0} should resolve", name);
            return encoding;
        }

        internal static string DecodeInBlocks(Encoding encoding, byte[] bytes, int blockSize)
        {
            var decoder = encoding.GetDecoder();
            var text = new StringBuilder();
            var index = 0;
            do
            {
                var count = Math.Min(blockSize, bytes.Length - index);
                var flush = index + count == bytes.Length;
                var chars = new char[decoder.GetCharCount(bytes, index, count, flush)];
                decoder.GetChars(bytes, index, count, chars, 0, flush).Should().Be(chars.Length);
                text.Append(chars);
                index += count;
            }
            while (index < bytes.Length);

            return text.ToString();
        }

        internal static byte[] EncodeInBlocks(Encoding encoding, string text, int blockSize)
        {
            var encoder = encoding.GetEncoder();
            var chars = text.ToCharArray();
            var bytes = new List<byte>();
            var index = 0;
            do
            {
                var count = Math.Min(blockSize, chars.Length - index);
                var flush = index + count == chars.Length;
                var block = new byte[encoder.GetByteCount(chars, index, count, flush)];
                encoder.GetBytes(chars, index, count, block, 0, flush).Should().Be(block.Length);
                bytes.AddRange(block);
                index += count;
            }
            while (index < chars.Length);

            return bytes.ToArray();
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
