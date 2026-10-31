using System;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeCharsetTests
    {
        [Theory]
        [InlineData("UTF-8")]
        [InlineData("utf-8")]
        [InlineData("US-ASCII")]
        [InlineData("ISO-8859-1")]
        [InlineData("Shift_JIS")]
        [InlineData("x-mac-roman")]
        [InlineData("a")]
        // ref. RFC 2978 §2.3: mime-charset-chars
        [InlineData("aZ09!#$%&'+-^_`{}~")]
        public void Parse_ValidName_KeepsNameAsWritten(string value)
        {
            var result = MimeCharset.Parse(value);

            result.Name.Should().Be(value);
            result.ToString().Should().Be(value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" UTF-8")]
        [InlineData("UTF-8 ")]
        [InlineData("UTF 8")]
        [InlineData("\"UTF-8\"")]
        [InlineData("UTF-8*en")]
        [InlineData("ISO_8859-1:1987")]
        [InlineData("ANSI_X3.4-1968")]
        [InlineData("utf-8;")]
        [InlineData("utf=8")]
        [InlineData("utf.8")]
        [InlineData("utf/8")]
        [InlineData("utf(8)")]
        [InlineData("utf\t8")]
        [InlineData("caf\u00E9")]
        public void TryParse_InvalidName_ReturnsFalse(string value)
        {
            MimeCharset.TryParse(value, out var result).Should().BeFalse();
            result.Should().Be(default(MimeCharset));
            MimeCharset.TryParse(value.AsSpan(), out _).Should().BeFalse();
            FluentActions.Invoking(() => MimeCharset.Parse(value)).Should().Throw<FormatException>();
        }

        [Fact]
        public void TryParse_Null_ReturnsFalse()
        {
            MimeCharset.TryParse((string)null, out var result).Should().BeFalse();
            result.Should().Be(default(MimeCharset));
        }

        [Fact]
        public void Parse_Null_ThrowsArgumentNullException()
        {
            FluentActions.Invoking(() => MimeCharset.Parse((string)null)).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Parse_Span_ReturnsCharset()
        {
            MimeCharset.Parse("x UTF-8 x".AsSpan(2, 5)).Should().Be(MimeCharset.Utf8);
        }

        [Theory]
        [InlineData("utf-8", "UTF-8")]
        [InlineData("Utf-8", "UTF-8")]
        [InlineData("iso-8859-1", "ISO-8859-1")]
        [InlineData("SHIFT_JIS", "Shift_JIS")]
        public void Equals_NamesDifferingInCase_AreEqual(string left, string right)
        {
            var leftCharset = MimeCharset.Parse(left);
            var rightCharset = MimeCharset.Parse(right);

            leftCharset.Should().Be(rightCharset);
            (leftCharset == rightCharset).Should().BeTrue();
            (leftCharset != rightCharset).Should().BeFalse();
            leftCharset.Equals((object)rightCharset).Should().BeTrue();
            leftCharset.GetHashCode().Should().Be(rightCharset.GetHashCode());
        }

        [Fact]
        public void Equals_Alias_IsNotEqual()
        {
            var alias = MimeCharset.Parse("latin1");

            alias.Should().NotBe(MimeCharset.Iso88591);
            (alias != MimeCharset.Iso88591).Should().BeTrue();
            alias.Equals((object)"latin1").Should().BeFalse();
        }

        [Fact]
        public void KnownCharsets_HaveRegisteredNames()
        {
            MimeCharset.UsAscii.Name.Should().Be("US-ASCII");
            MimeCharset.Iso88591.Name.Should().Be("ISO-8859-1");
            MimeCharset.Utf8.Name.Should().Be("UTF-8");
            MimeCharset.Utf16.Name.Should().Be("UTF-16");
            MimeCharset.Utf16BigEndian.Name.Should().Be("UTF-16BE");
            MimeCharset.Utf16LittleEndian.Name.Should().Be("UTF-16LE");
            MimeCharset.Utf32.Name.Should().Be("UTF-32");
            MimeCharset.Utf32BigEndian.Name.Should().Be("UTF-32BE");
            MimeCharset.Utf32LittleEndian.Name.Should().Be("UTF-32LE");
        }

        [Fact]
        public void TryFormat_WritesName()
        {
            var destination = new char[10];

            MimeCharset.Utf8.TryFormat(destination, out var charsWritten).Should().BeTrue();

            charsWritten.Should().Be(5);
            new string(destination, 0, charsWritten).Should().Be("UTF-8");
        }

        [Fact]
        public void TryFormat_DestinationTooShort_ReturnsFalse()
        {
            MimeCharset.Utf8.TryFormat(new char[4], out var charsWritten).Should().BeFalse();

            charsWritten.Should().Be(0);
        }

        [Fact]
        public void Default_IsUninitialized()
        {
            var charset = default(MimeCharset);

            FluentActions.Invoking(() => charset.Name).Should().Throw<InvalidOperationException>();
            charset.ToString().Should().BeEmpty();
            charset.TryFormat(new char[1], out var charsWritten).Should().BeTrue();
            charsWritten.Should().Be(0);
            charset.GetHashCode().Should().Be(0);
            charset.Should().NotBe(MimeCharset.Utf8);
        }

        [Theory]
        // ref. RFC 2046 §4.1.2
        [InlineData("text/plain", "US-ASCII")]
        [InlineData("TEXT/PLAIN", "US-ASCII")]
        [InlineData("text/csv", "US-ASCII")]
        [InlineData("text/enriched", "US-ASCII")]
        [InlineData("text/x-unknown", "US-ASCII")]
        // ref. RFC 5545 §8.1; RFC 6350 §10.1
        [InlineData("text/calendar", "UTF-8")]
        [InlineData("Text/Calendar", "UTF-8")]
        [InlineData("text/vcard", "UTF-8")]
        [InlineData("text/VCARD", "UTF-8")]
        public void TryGetDefault_TextMediaType_ReturnsDefault(string mediaType, string expected)
        {
            MimeCharset.TryGetDefault(MimeMediaType.Parse(mediaType), out var charset).Should().BeTrue();

            charset.Should().Be(MimeCharset.Parse(expected));
        }

        [Fact]
        public void TryGetDefault_MediaTypeWithCharsetParameter_IgnoresParameter()
        {
            MimeCharset.TryGetDefault(MimeMediaType.Parse("text/plain;charset=UTF-8"), out var charset).Should().BeTrue();

            charset.Should().Be(MimeCharset.UsAscii);
        }

        [Theory]
        // ref. HTML Living Standard §16.1.2; RFC 7303 §3.2; RFC 7763 §2
        [InlineData("text/html")]
        [InlineData("TEXT/HTML")]
        [InlineData("text/xml")]
        [InlineData("text/markdown")]
        [InlineData("application/json")]
        [InlineData("application/xml")]
        [InlineData("image/png")]
        [InlineData("multipart/mixed")]
        [InlineData("texts/plain")]
        public void TryGetDefault_MediaTypeWithNoDefault_ReturnsFalse(string mediaType)
        {
            MimeCharset.TryGetDefault(MimeMediaType.Parse(mediaType), out var charset).Should().BeFalse();

            charset.Should().Be(default(MimeCharset));
        }

        [Fact]
        public void TryGetDefault_UninitializedMediaType_ThrowsArgumentException()
        {
            FluentActions.Invoking(() => MimeCharset.TryGetDefault(default(MimeMediaType), out _)).Should().Throw<ArgumentException>();
        }

#if NET7_0_OR_GREATER
        [Fact]
        public void ParsableAndFormattable_UseNameAndIgnoreProvider()
        {
            static T Parse<T>(string value) where T : ISpanParsable<T> => T.Parse(value.AsSpan(), null);

            var charset = Parse<MimeCharset>("UTF-8");

            charset.Should().Be(MimeCharset.Utf8);
            ((IFormattable)charset).ToString("G", null).Should().Be("UTF-8");
        }
#endif
    }
}
