using System;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeCharsetTests
    {
        // ref. IANA Character Sets registry: the names of the character sets MimeCharset has members for, and an alias.
        private const string UsAsciiName = "US-ASCII";
        private const string Iso88591Name = "ISO-8859-1";
        private const string Utf8Name = "UTF-8";
        private const string ShiftJisName = "Shift_JIS";
        private const string Latin1Alias = "latin1";

        // A name of a registered character set in another case; ref. RFC 2978 §2.3, names are not case-sensitive.
        private const string Utf8InLowerCase = "utf-8";

        // "UTF-8" in the middle of other text, at the span from Utf8Start of Utf8Name.Length characters.
        private const string Utf8AmongOtherText = "x UTF-8 x";
        private const int Utf8Start = 2;

        // A destination one character too short for "UTF-8".
        private const int OneTooFewForUtf8 = 4;

        // Room for "UTF-8" and more.
        private const int RoomForUtf8 = 10;

        // ref. RFC 2046 §4.1.2: text/plain without a charset parameter is US-ASCII.
        private const string TextPlain = "text/plain";

        [Theory]
        [InlineData(Utf8Name)]
        [InlineData(Utf8InLowerCase)]
        [InlineData(UsAsciiName)]
        [InlineData(Iso88591Name)]
        [InlineData(ShiftJisName)]
        [InlineData("x-mac-roman")]
        [InlineData("a")]
        // ref. RFC 2978 §2.3: mime-charset-chars
        [InlineData("aZ09!#$%&'+-^_`{}~")]
        public void Parse_ValidName_KeepsNameAsWritten(string value)
        {
            // act
            var testResult = MimeCharset.Parse(value);

            // assert
            testResult.Name.Should().Be(value);
            testResult.ToString().Should().Be(value);
        }

        public static TheoryData<string> InvalidNames => new TheoryData<string>
        {
            "",
            " UTF-8",
            "UTF-8 ",
            "UTF 8",
            "\"UTF-8\"",
            "UTF-8*en",
            "ISO_8859-1:1987",
            "ANSI_X3.4-1968",
            "utf-8;",
            "utf=8",
            "utf.8",
            "utf/8",
            "utf(8)",
            "utf\t8",
            "caf\x00E9",
        };

        [Theory]
        [MemberData(nameof(InvalidNames))]
        public void TryParse_InvalidName_ReturnsFalse(string value)
        {
            // act
            var testResult = MimeCharset.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(default(MimeCharset));
        }

        [Theory]
        [MemberData(nameof(InvalidNames))]
        public void TryParse_InvalidNameAsSpan_ReturnsFalse(string value)
        {
            // act
            var testResult = MimeCharset.TryParse(value.AsSpan(), out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(default(MimeCharset));
        }

        [Theory]
        [MemberData(nameof(InvalidNames))]
        public void Parse_InvalidName_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => MimeCharset.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void TryParse_Null_ReturnsFalse()
        {
            // act
            var testResult = MimeCharset.TryParse((string)null, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(default(MimeCharset));
        }

        [Fact]
        public void Parse_Null_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => MimeCharset.Parse((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Parse_Span_ReturnsCharset()
        {
            // arrange
            var value = Utf8AmongOtherText.AsSpan(Utf8Start, Utf8Name.Length);

            // act
            var testResult = MimeCharset.Parse(value);

            // assert
            testResult.Should().Be(MimeCharset.Utf8);
        }

        public static TheoryData<string, string> NamesDifferingInCase => new TheoryData<string, string>
        {
            { Utf8InLowerCase, Utf8Name },
            { "Utf-8", Utf8Name },
            { "iso-8859-1", Iso88591Name },
            { "SHIFT_JIS", ShiftJisName },
        };

        [Theory]
        [MemberData(nameof(NamesDifferingInCase))]
        public void Equals_NamesDifferingInCase_ReturnsTrue(string left, string right)
        {
            // arrange
            var leftCharset = MimeCharset.Parse(left);
            var rightCharset = MimeCharset.Parse(right);

            // act
            var testResult = leftCharset.Equals(rightCharset);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(NamesDifferingInCase))]
        public void EqualsObject_NamesDifferingInCase_ReturnsTrue(string left, string right)
        {
            // arrange
            var leftCharset = MimeCharset.Parse(left);
            object rightCharset = MimeCharset.Parse(right);

            // act
            var testResult = leftCharset.Equals(rightCharset);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(NamesDifferingInCase))]
        public void EqualityOperator_NamesDifferingInCase_ReturnsTrue(string left, string right)
        {
            // arrange
            var leftCharset = MimeCharset.Parse(left);
            var rightCharset = MimeCharset.Parse(right);

            // act
            var testResult = leftCharset == rightCharset;

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(NamesDifferingInCase))]
        public void InequalityOperator_NamesDifferingInCase_ReturnsFalse(string left, string right)
        {
            // arrange
            var leftCharset = MimeCharset.Parse(left);
            var rightCharset = MimeCharset.Parse(right);

            // act
            var testResult = leftCharset != rightCharset;

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(NamesDifferingInCase))]
        public void GetHashCode_NamesDifferingInCase_ReturnsSameHashCode(string left, string right)
        {
            // arrange
            var leftCharset = MimeCharset.Parse(left);
            var rightHashCode = MimeCharset.Parse(right).GetHashCode();

            // act
            var testResult = leftCharset.GetHashCode();

            // assert
            testResult.Should().Be(rightHashCode);
        }

        [Fact]
        public void Equals_Alias_ReturnsFalse()
        {
            // arrange
            var alias = MimeCharset.Parse(Latin1Alias);

            // act
            var testResult = alias.Equals(MimeCharset.Iso88591);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void InequalityOperator_Alias_ReturnsTrue()
        {
            // arrange
            var alias = MimeCharset.Parse(Latin1Alias);

            // act
            var testResult = alias != MimeCharset.Iso88591;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void EqualsObject_StringOfSameName_ReturnsFalse()
        {
            // arrange
            var alias = MimeCharset.Parse(Latin1Alias);

            // act
            var testResult = alias.Equals((object)Latin1Alias);

            // assert
            testResult.Should().BeFalse();
        }

        public static TheoryData<MimeCharset, string> KnownCharsets => new TheoryData<MimeCharset, string>
        {
            { MimeCharset.UsAscii, UsAsciiName },
            { MimeCharset.Iso88591, Iso88591Name },
            { MimeCharset.Utf8, Utf8Name },
            { MimeCharset.Utf16, "UTF-16" },
            { MimeCharset.Utf16BigEndian, "UTF-16BE" },
            { MimeCharset.Utf16LittleEndian, "UTF-16LE" },
            { MimeCharset.Utf32, "UTF-32" },
            { MimeCharset.Utf32BigEndian, "UTF-32BE" },
            { MimeCharset.Utf32LittleEndian, "UTF-32LE" },
        };

        [Theory]
        [MemberData(nameof(KnownCharsets))]
        public void Name_KnownCharset_ReturnsRegisteredName(MimeCharset charset, string expected)
        {
            // act
            var testResult = charset.Name;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void TryFormat_DestinationLongEnough_WritesName()
        {
            // arrange
            var destination = new char[RoomForUtf8];

            // act
            var testResult = MimeCharset.Utf8.TryFormat(destination, out var charsWritten);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(Utf8Name.Length);
            new string(destination, 0, charsWritten).Should().Be(Utf8Name);
        }

        [Fact]
        public void TryFormat_DestinationTooShort_ReturnsFalse()
        {
            // act
            var testResult = MimeCharset.Utf8.TryFormat(new char[OneTooFewForUtf8], out var charsWritten);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void Name_Uninitialized_ThrowsInvalidOperationException()
        {
            // arrange
            var charset = default(MimeCharset);

            // act
            Action testAction = () => _ = charset.Name;

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ToString_Uninitialized_ReturnsEmpty()
        {
            // act
            var testResult = default(MimeCharset).ToString();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void TryFormat_Uninitialized_WritesNothing()
        {
            // act
            var testResult = default(MimeCharset).TryFormat(new char[1], out var charsWritten);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void GetHashCode_Uninitialized_ReturnsZero()
        {
            // act
            var testResult = default(MimeCharset).GetHashCode();

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void Equals_UninitializedAndInitialized_ReturnsFalse()
        {
            // act
            var testResult = default(MimeCharset).Equals(MimeCharset.Utf8);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        // ref. RFC 2046 §4.1.2
        [InlineData(TextPlain, UsAsciiName)]
        [InlineData("TEXT/PLAIN", UsAsciiName)]
        [InlineData("text/csv", UsAsciiName)]
        [InlineData("text/enriched", UsAsciiName)]
        [InlineData("text/x-unknown", UsAsciiName)]
        // ref. RFC 5545 §8.1; RFC 6350 §10.1
        [InlineData("text/calendar", Utf8Name)]
        [InlineData("Text/Calendar", Utf8Name)]
        [InlineData("text/vcard", Utf8Name)]
        [InlineData("text/VCARD", Utf8Name)]
        public void TryGetDefault_TextMediaType_ReturnsDefault(string mediaType, string expected)
        {
            // arrange
            var parsedMediaType = MimeMediaType.Parse(mediaType);
            var expectedCharset = MimeCharset.Parse(expected);

            // act
            var testResult = MimeCharset.TryGetDefault(parsedMediaType, out var charset);

            // assert
            testResult.Should().BeTrue();
            charset.Should().Be(expectedCharset);
        }

        [Fact]
        public void TryGetDefault_MediaTypeWithCharsetParameter_IgnoresParameter()
        {
            // arrange
            var mediaType = MimeMediaType.Parse("text/plain;charset=UTF-8");

            // act
            var testResult = MimeCharset.TryGetDefault(mediaType, out var charset);

            // assert
            testResult.Should().BeTrue();
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
            // arrange
            var parsedMediaType = MimeMediaType.Parse(mediaType);

            // act
            var testResult = MimeCharset.TryGetDefault(parsedMediaType, out var charset);

            // assert
            testResult.Should().BeFalse();
            charset.Should().Be(default(MimeCharset));
        }

        [Fact]
        public void TryGetDefault_UninitializedMediaType_ThrowsArgumentException()
        {
            // act
            Action testAction = () => MimeCharset.TryGetDefault(default(MimeMediaType), out _);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

#if NET7_0_OR_GREATER
        [Fact]
        public void ISpanParsableParse_NameAndNoProvider_ReturnsCharset()
        {
            // act
            var testResult = ParseGeneric<MimeCharset>(Utf8Name);

            // assert
            testResult.Should().Be(MimeCharset.Utf8);
        }

        [Fact]
        public void IFormattableToString_FormatAndNoProvider_ReturnsName()
        {
            // arrange
            IFormattable charset = MimeCharset.Utf8;

            // act
            var testResult = charset.ToString("G", null);

            // assert
            testResult.Should().Be(Utf8Name);
        }

        private static T ParseGeneric<T>(string value) where T : ISpanParsable<T> => T.Parse(value.AsSpan(), null);
#endif
    }
}
