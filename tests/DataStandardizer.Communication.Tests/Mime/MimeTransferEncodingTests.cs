using System;
using System.Collections.Generic;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeTransferEncodingTests
    {
        public static IEnumerable<object[]> KnownMechanisms()
        {
            // ref. RFC 2045 §6.1, §6.2
            yield return new object[] { "7bit", MimeTransferEncoding.SevenBit, true, MimeDataDomain.SevenBit };
            yield return new object[] { "8bit", MimeTransferEncoding.EightBit, true, MimeDataDomain.EightBit };
            yield return new object[] { "binary", MimeTransferEncoding.Binary, true, MimeDataDomain.Binary };
            yield return new object[] { "quoted-printable", MimeTransferEncoding.QuotedPrintable, false, MimeDataDomain.SevenBit };
            yield return new object[] { "base64", MimeTransferEncoding.Base64, false, MimeDataDomain.SevenBit };
        }

        [Theory]
        [MemberData(nameof(KnownMechanisms))]
        public void Parse_KnownMechanism_ReturnsKnownValue(string value, MimeTransferEncoding expected, bool isIdentity, MimeDataDomain domain)
        {
            var result = MimeTransferEncoding.Parse(value);

            result.Should().Be(expected);
            result.Name.Should().Be(value);
            result.IsKnown.Should().BeTrue();
            result.IsIdentity.Should().Be(isIdentity);
            result.Domain.Should().Be(domain);
        }

        [Theory]
        [InlineData("7BIT", "7bit")]
        [InlineData("8Bit", "8bit")]
        [InlineData("BINARY", "binary")]
        [InlineData("Binary", "binary")]
        [InlineData("Quoted-Printable", "quoted-printable")]
        [InlineData("QUOTED-PRINTABLE", "quoted-printable")]
        [InlineData("BASE64", "base64")]
        [InlineData("Base64", "base64")]
        public void Parse_KnownMechanismInOtherCase_ReturnsLowerCaseName(string value, string name)
        {
            var result = MimeTransferEncoding.Parse(value);

            result.IsKnown.Should().BeTrue();
            result.Name.Should().Be(name);
            result.ToString().Should().Be(name);
            result.Should().Be(MimeTransferEncoding.Parse(name));
        }

        [Theory]
        // ref. RFC 2045 §6.1: x-token; RFC 4289 §3: a registered mechanism is an ietf-token
        [InlineData("x-uuencode")]
        [InlineData("X-UUENCODE")]
        [InlineData("x-gzip64")]
        [InlineData("X-")]
        [InlineData("uuencode")]
        [InlineData("b")]
        [InlineData("7bit-ish")]
        [InlineData("base-64")]
        [InlineData("base64x")]
        [InlineData("quoted-printable!")]
        public void Parse_ExtensionToken_IsKeptAsWrittenAndUnknown(string value)
        {
            var result = MimeTransferEncoding.Parse(value);

            result.Name.Should().Be(value);
            result.ToString().Should().Be(value);
            result.IsKnown.Should().BeFalse();
            result.IsIdentity.Should().BeFalse();
            result.Domain.Should().Be(MimeDataDomain.Unknown);
        }

        [Fact]
        public void Parse_Span_ReturnsTransferEncoding()
        {
            MimeTransferEncoding.Parse("xx base64 xx".AsSpan(3, 6)).Should().Be(MimeTransferEncoding.Base64);
            MimeTransferEncoding.Parse("xx x-foo xx".AsSpan(3, 5)).Name.Should().Be("x-foo");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(" base64")]
        [InlineData("base64 ")]
        [InlineData("base 64")]
        [InlineData("base64;")]
        [InlineData("base64 (comment)")]
        [InlineData("\"base64\"")]
        [InlineData("x/y")]
        [InlineData("x=y")]
        [InlineData("base64\t")]
        [InlineData("bäse64")]
        public void TryParse_InvalidMechanism_ReturnsFalse(string value)
        {
            MimeTransferEncoding result;
            MimeTransferEncoding.TryParse(value, out result).Should().BeFalse();
            MimeTransferEncoding.TryParse(value.AsSpan(), out result).Should().BeFalse();
            result.Should().Be(default(MimeTransferEncoding));
        }

        [Fact]
        public void TryParse_Null_ReturnsFalse()
        {
            MimeTransferEncoding result;
            MimeTransferEncoding.TryParse((string)null, out result).Should().BeFalse();
        }

        [Fact]
        public void Parse_Invalid_ThrowsFormatException()
        {
            Action act = () => MimeTransferEncoding.Parse("base 64");

            act.Should().Throw<FormatException>();
            ((Action)(() => MimeTransferEncoding.Parse(ReadOnlySpan<char>.Empty))).Should().Throw<FormatException>();
        }

        [Fact]
        public void Parse_Null_ThrowsArgumentNullException()
        {
            Action act = () => MimeTransferEncoding.Parse((string)null);

            act.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("base64", "BASE64")]
        [InlineData("x-uuencode", "X-UUEncode")]
        [InlineData("uuencode", "UUENCODE")]
        public void Equals_DifferentCase_ReturnsTrue(string left, string right)
        {
            var leftEncoding = MimeTransferEncoding.Parse(left);
            var rightEncoding = MimeTransferEncoding.Parse(right);

            leftEncoding.Equals(rightEncoding).Should().BeTrue();
            (leftEncoding == rightEncoding).Should().BeTrue();
            (leftEncoding != rightEncoding).Should().BeFalse();
            leftEncoding.GetHashCode().Should().Be(rightEncoding.GetHashCode());
        }

        [Theory]
        [InlineData("base64", "quoted-printable")]
        [InlineData("7bit", "8bit")]
        [InlineData("x-uuencode", "uuencode")]
        [InlineData("base64", "x-base64")]
        public void Equals_DifferentMechanisms_ReturnsFalse(string left, string right)
        {
            var leftEncoding = MimeTransferEncoding.Parse(left);
            var rightEncoding = MimeTransferEncoding.Parse(right);

            leftEncoding.Equals(rightEncoding).Should().BeFalse();
            (leftEncoding == rightEncoding).Should().BeFalse();
            (leftEncoding != rightEncoding).Should().BeTrue();
        }

        [Fact]
        public void Equals_DefaultInstances_ReturnsTrue()
        {
            default(MimeTransferEncoding).Equals(default(MimeTransferEncoding)).Should().BeTrue();
            default(MimeTransferEncoding).Equals(MimeTransferEncoding.SevenBit).Should().BeFalse();
            default(MimeTransferEncoding).GetHashCode().Should().Be(0);
        }

        [Fact]
        public void Equals_OtherObject_ReturnsFalse()
        {
            MimeTransferEncoding.Base64.Equals("base64").Should().BeFalse();
            MimeTransferEncoding.Base64.Equals(null).Should().BeFalse();
            MimeTransferEncoding.Base64.Equals((object)MimeTransferEncoding.Parse("BASE64")).Should().BeTrue();
        }

        [Theory]
        [InlineData("quoted-printable")]
        [InlineData("x-uuencode")]
        public void TryFormat_WritesName(string value)
        {
            var encoding = MimeTransferEncoding.Parse(value);
            var buffer = new char[value.Length];

            int charsWritten;
            encoding.TryFormat(buffer, out charsWritten).Should().BeTrue();

            new string(buffer, 0, charsWritten).Should().Be(value);
            MimeTransferEncoding.Parse(encoding.ToString()).Should().Be(encoding);
        }

        [Fact]
        public void TryFormat_DestinationTooShort_ReturnsFalse()
        {
            var encoding = MimeTransferEncoding.QuotedPrintable;

            for (var length = 0; length < encoding.Name.Length; length++)
            {
                int charsWritten;
                encoding.TryFormat(new char[length], out charsWritten).Should().BeFalse();
                charsWritten.Should().Be(0);
            }
        }

        [Fact]
        public void TryFormat_DefaultInstance_WritesNothing()
        {
            int charsWritten;
            default(MimeTransferEncoding).TryFormat(new char[0], out charsWritten).Should().BeTrue();

            charsWritten.Should().Be(0);
            default(MimeTransferEncoding).ToString().Should().BeEmpty();
        }

        [Fact]
        public void Properties_DefaultInstance_ThrowInvalidOperationException()
        {
            var encoding = default(MimeTransferEncoding);

            ((Action)(() => _ = encoding.Name)).Should().Throw<InvalidOperationException>();
            ((Action)(() => _ = encoding.IsKnown)).Should().Throw<InvalidOperationException>();
            ((Action)(() => _ = encoding.IsIdentity)).Should().Throw<InvalidOperationException>();
            ((Action)(() => _ = encoding.Domain)).Should().Throw<InvalidOperationException>();
        }

#if NET7_0_OR_GREATER
        [Fact]
        public void ISpanParsable_ParsesMechanism()
        {
            ParseGeneric<MimeTransferEncoding>("Base64").Should().Be(MimeTransferEncoding.Base64);
            TryParseGeneric<MimeTransferEncoding>(" base64").Should().BeFalse();
        }

        [Fact]
        public void ISpanFormattable_FormatsMechanism()
        {
            var encoding = MimeTransferEncoding.Parse("X-UUEncode");

            $"{encoding}".Should().Be("X-UUEncode");
            ((IFormattable)encoding).ToString(null, null).Should().Be("X-UUEncode");
        }

        private static T ParseGeneric<T>(string value) where T : ISpanParsable<T>
        {
            return T.Parse(value.AsSpan(), null);
        }

        private static bool TryParseGeneric<T>(string value) where T : IParsable<T>
        {
            return T.TryParse(value, null, out _);
        }
#endif
    }
}
