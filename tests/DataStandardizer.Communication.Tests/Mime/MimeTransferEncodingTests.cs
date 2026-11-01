using System;
using System.Collections.Generic;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeTransferEncodingTests
    {
        // ref. RFC 2045 §6.1: the mechanisms that RFC 2045 defines.
        private const string SevenBit = "7bit";
        private const string EightBit = "8bit";
        private const string Binary = "binary";
        private const string QuotedPrintable = "quoted-printable";
        private const string Base64 = "base64";

        // ref. RFC 2045 §6.1: x-token, an extension mechanism; and uuencode, an unregistered ietf-token (RFC 4289 §3).
        private const string XUuencode = "x-uuencode";
        private const string XUuencodeInMixedCase = "X-UUEncode";
        private const string Uuencode = "uuencode";

        // ref. RFC 2045 §6.1: mechanism names are not case-sensitive.
        private const string Base64InUpperCase = "BASE64";
        private const string Base64Capitalised = "Base64";

        // Not a token, as it has a space in it.
        private const string Base64WithSpace = "base 64";
        private const string Base64AfterSpace = " base64";

        // Names in the middle of other text, at the spans from MechanismStart of Base64.Length and XFoo.Length characters.
        private const string Base64AmongOtherText = "xx base64 xx";
        private const string XFooAmongOtherText = "xx x-foo xx";
        private const string XFoo = "x-foo";
        private const int MechanismStart = 3;

        public static IEnumerable<object[]> KnownMechanisms()
        {
            // ref. RFC 2045 §6.1, §6.2
            yield return new object[] { SevenBit, MimeTransferEncoding.SevenBit, true, MimeDataDomain.SevenBit };
            yield return new object[] { EightBit, MimeTransferEncoding.EightBit, true, MimeDataDomain.EightBit };
            yield return new object[] { Binary, MimeTransferEncoding.Binary, true, MimeDataDomain.Binary };
            yield return new object[] { QuotedPrintable, MimeTransferEncoding.QuotedPrintable, false, MimeDataDomain.SevenBit };
            yield return new object[] { Base64, MimeTransferEncoding.Base64, false, MimeDataDomain.SevenBit };
        }

        [Theory]
        [MemberData(nameof(KnownMechanisms))]
        public void Parse_KnownMechanism_ReturnsKnownValue(string value, MimeTransferEncoding expected, bool isIdentity, MimeDataDomain domain)
        {
            // act
            var testResult = MimeTransferEncoding.Parse(value);

            // assert
            testResult.Should().Be(expected);
            testResult.Name.Should().Be(value);
            testResult.IsKnown.Should().BeTrue();
            testResult.IsIdentity.Should().Be(isIdentity);
            testResult.Domain.Should().Be(domain);
        }

        public static TheoryData<string, string> KnownMechanismsInOtherCase => new TheoryData<string, string>
        {
            { "7BIT", SevenBit },
            { "8Bit", EightBit },
            { "BINARY", Binary },
            { "Binary", Binary },
            { "Quoted-Printable", QuotedPrintable },
            { "QUOTED-PRINTABLE", QuotedPrintable },
            { Base64InUpperCase, Base64 },
            { Base64Capitalised, Base64 },
        };

        [Theory]
        [MemberData(nameof(KnownMechanismsInOtherCase))]
        public void Parse_KnownMechanismInOtherCase_ReturnsLowerCaseName(string value, string name)
        {
            // act
            var testResult = MimeTransferEncoding.Parse(value);

            // assert
            testResult.IsKnown.Should().BeTrue();
            testResult.Name.Should().Be(name);
            testResult.ToString().Should().Be(name);
        }

        [Theory]
        [MemberData(nameof(KnownMechanismsInOtherCase))]
        public void Equals_KnownMechanismInOtherCase_EqualsLowerCaseName(string value, string name)
        {
            // arrange
            var lowerCase = MimeTransferEncoding.Parse(name);
            var otherCase = MimeTransferEncoding.Parse(value);

            // act
            var testResult = otherCase.Equals(lowerCase);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        // ref. RFC 2045 §6.1: x-token; RFC 4289 §3: a registered mechanism is an ietf-token
        [InlineData(XUuencode)]
        [InlineData("X-UUENCODE")]
        [InlineData("x-gzip64")]
        [InlineData("X-")]
        [InlineData(Uuencode)]
        [InlineData("b")]
        [InlineData("7bit-ish")]
        [InlineData("base-64")]
        [InlineData("base64x")]
        [InlineData("quoted-printable!")]
        public void Parse_ExtensionToken_IsKeptAsWrittenAndUnknown(string value)
        {
            // act
            var testResult = MimeTransferEncoding.Parse(value);

            // assert
            testResult.Name.Should().Be(value);
            testResult.ToString().Should().Be(value);
            testResult.IsKnown.Should().BeFalse();
            testResult.IsIdentity.Should().BeFalse();
            testResult.Domain.Should().Be(MimeDataDomain.Unknown);
        }

        [Fact]
        public void Parse_SpanOfKnownMechanism_ReturnsKnownValue()
        {
            // arrange
            var value = Base64AmongOtherText.AsSpan(MechanismStart, Base64.Length);

            // act
            var testResult = MimeTransferEncoding.Parse(value);

            // assert
            testResult.Should().Be(MimeTransferEncoding.Base64);
        }

        [Fact]
        public void Parse_SpanOfExtensionToken_KeepsName()
        {
            // arrange
            var value = XFooAmongOtherText.AsSpan(MechanismStart, XFoo.Length);

            // act
            var testResult = MimeTransferEncoding.Parse(value);

            // assert
            testResult.Name.Should().Be(XFoo);
        }

        public static TheoryData<string> InvalidMechanisms => new TheoryData<string>
        {
            "",
            " ",
            Base64AfterSpace,
            "base64 ",
            Base64WithSpace,
            "base64;",
            "base64 (comment)",
            "\"base64\"",
            "x/y",
            "x=y",
            "base64\t",
            "b\x00E4se64",
        };

        [Theory]
        [MemberData(nameof(InvalidMechanisms))]
        public void TryParse_InvalidMechanism_ReturnsFalse(string value)
        {
            // act
            var testResult = MimeTransferEncoding.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(default(MimeTransferEncoding));
        }

        [Theory]
        [MemberData(nameof(InvalidMechanisms))]
        public void TryParse_InvalidMechanismAsSpan_ReturnsFalse(string value)
        {
            // act
            var testResult = MimeTransferEncoding.TryParse(value.AsSpan(), out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(default(MimeTransferEncoding));
        }

        [Fact]
        public void TryParse_Null_ReturnsFalse()
        {
            // act
            var testResult = MimeTransferEncoding.TryParse((string)null, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Parse_Invalid_ThrowsFormatException()
        {
            // act
            Action testAction = () => MimeTransferEncoding.Parse(Base64WithSpace);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void Parse_EmptySpan_ThrowsFormatException()
        {
            // act
            Action testAction = () => MimeTransferEncoding.Parse(ReadOnlySpan<char>.Empty);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void Parse_Null_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => MimeTransferEncoding.Parse((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        public static TheoryData<string, string> MechanismsDifferingInCase => new TheoryData<string, string>
        {
            { Base64, Base64InUpperCase },
            { XUuencode, XUuencodeInMixedCase },
            { Uuencode, "UUENCODE" },
        };

        public static TheoryData<string, string> DifferentMechanisms => new TheoryData<string, string>
        {
            { Base64, QuotedPrintable },
            { SevenBit, EightBit },
            { XUuencode, Uuencode },
            { Base64, "x-base64" },
        };

        [Theory]
        [MemberData(nameof(MechanismsDifferingInCase))]
        public void Equals_DifferentCase_ReturnsTrue(string left, string right)
        {
            // arrange
            var leftEncoding = MimeTransferEncoding.Parse(left);
            var rightEncoding = MimeTransferEncoding.Parse(right);

            // act
            var testResult = leftEncoding.Equals(rightEncoding);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(MechanismsDifferingInCase))]
        public void EqualityOperator_DifferentCase_ReturnsTrue(string left, string right)
        {
            // arrange
            var leftEncoding = MimeTransferEncoding.Parse(left);
            var rightEncoding = MimeTransferEncoding.Parse(right);

            // act
            var testResult = leftEncoding == rightEncoding;

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(MechanismsDifferingInCase))]
        public void InequalityOperator_DifferentCase_ReturnsFalse(string left, string right)
        {
            // arrange
            var leftEncoding = MimeTransferEncoding.Parse(left);
            var rightEncoding = MimeTransferEncoding.Parse(right);

            // act
            var testResult = leftEncoding != rightEncoding;

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(MechanismsDifferingInCase))]
        public void GetHashCode_DifferentCase_ReturnsSameHashCode(string left, string right)
        {
            // arrange
            var leftEncoding = MimeTransferEncoding.Parse(left);
            var rightHashCode = MimeTransferEncoding.Parse(right).GetHashCode();

            // act
            var testResult = leftEncoding.GetHashCode();

            // assert
            testResult.Should().Be(rightHashCode);
        }

        [Theory]
        [MemberData(nameof(DifferentMechanisms))]
        public void Equals_DifferentMechanisms_ReturnsFalse(string left, string right)
        {
            // arrange
            var leftEncoding = MimeTransferEncoding.Parse(left);
            var rightEncoding = MimeTransferEncoding.Parse(right);

            // act
            var testResult = leftEncoding.Equals(rightEncoding);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(DifferentMechanisms))]
        public void EqualityOperator_DifferentMechanisms_ReturnsFalse(string left, string right)
        {
            // arrange
            var leftEncoding = MimeTransferEncoding.Parse(left);
            var rightEncoding = MimeTransferEncoding.Parse(right);

            // act
            var testResult = leftEncoding == rightEncoding;

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(DifferentMechanisms))]
        public void InequalityOperator_DifferentMechanisms_ReturnsTrue(string left, string right)
        {
            // arrange
            var leftEncoding = MimeTransferEncoding.Parse(left);
            var rightEncoding = MimeTransferEncoding.Parse(right);

            // act
            var testResult = leftEncoding != rightEncoding;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_DefaultInstances_ReturnsTrue()
        {
            // act
            var testResult = default(MimeTransferEncoding).Equals(default(MimeTransferEncoding));

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_DefaultInstanceAndKnownMechanism_ReturnsFalse()
        {
            // act
            var testResult = default(MimeTransferEncoding).Equals(MimeTransferEncoding.SevenBit);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_DefaultInstance_ReturnsZero()
        {
            // act
            var testResult = default(MimeTransferEncoding).GetHashCode();

            // assert
            testResult.Should().Be(0);
        }

        public static TheoryData<object> OtherObjects => new TheoryData<object> { Base64, null };

        [Theory]
        [MemberData(nameof(OtherObjects))]
        public void EqualsObject_OtherObject_ReturnsFalse(object other)
        {
            // act
            var testResult = MimeTransferEncoding.Base64.Equals(other);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void EqualsObject_SameMechanismInOtherCase_ReturnsTrue()
        {
            // arrange
            object other = MimeTransferEncoding.Parse(Base64InUpperCase);

            // act
            var testResult = MimeTransferEncoding.Base64.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [InlineData(QuotedPrintable)]
        [InlineData(XUuencode)]
        public void TryFormat_DestinationLongEnough_WritesName(string value)
        {
            // arrange
            var encoding = MimeTransferEncoding.Parse(value);
            var destination = new char[value.Length];

            // act
            var testResult = encoding.TryFormat(destination, out var charsWritten);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(value.Length);
            new string(destination).Should().Be(value);
        }

        // Each length of destination too short for "quoted-printable", which is 16 characters.
        public static IEnumerable<object[]> DestinationsTooShortForQuotedPrintable()
        {
            for (var length = 0; length < QuotedPrintable.Length; length++)
            {
                yield return new object[] { length };
            }
        }

        [Theory]
        [MemberData(nameof(DestinationsTooShortForQuotedPrintable))]
        public void TryFormat_DestinationTooShort_ReturnsFalse(int length)
        {
            // arrange
            var destination = new char[length];

            // act
            var testResult = MimeTransferEncoding.QuotedPrintable.TryFormat(destination, out var charsWritten);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void TryFormat_DefaultInstance_WritesNothing()
        {
            // act
            var testResult = default(MimeTransferEncoding).TryFormat(new char[0], out var charsWritten);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void ToString_DefaultInstance_ReturnsEmpty()
        {
            // act
            var testResult = default(MimeTransferEncoding).ToString();

            // assert
            testResult.Should().BeEmpty();
        }

        public static TheoryData<Action> PropertiesOfDefaultInstance => new TheoryData<Action>
        {
            () => _ = default(MimeTransferEncoding).Name,
            () => _ = default(MimeTransferEncoding).IsKnown,
            () => _ = default(MimeTransferEncoding).IsIdentity,
            () => _ = default(MimeTransferEncoding).Domain,
        };

        [Theory]
        [MemberData(nameof(PropertiesOfDefaultInstance))]
        public void Properties_DefaultInstance_ThrowInvalidOperationException(Action property)
        {
            // act
            var testAction = property;

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

#if NET7_0_OR_GREATER
        [Fact]
        public void ISpanParsableParse_KnownMechanismInOtherCase_ReturnsKnownValue()
        {
            // act
            var testResult = ParseGeneric<MimeTransferEncoding>(Base64Capitalised);

            // assert
            testResult.Should().Be(MimeTransferEncoding.Base64);
        }

        [Fact]
        public void IParsableTryParse_Invalid_ReturnsFalse()
        {
            // act
            var testResult = TryParseGeneric<MimeTransferEncoding>(Base64AfterSpace);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ISpanFormattableTryFormat_ExtensionToken_KeepsNameAsWritten()
        {
            // arrange
            ISpanFormattable encoding = MimeTransferEncoding.Parse(XUuencodeInMixedCase);
            var destination = new char[XUuencodeInMixedCase.Length];

            // act
            var testResult = encoding.TryFormat(destination, out var charsWritten, default, null);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(XUuencodeInMixedCase.Length);
            new string(destination).Should().Be(XUuencodeInMixedCase);
        }

        [Fact]
        public void IFormattableToString_ExtensionToken_KeepsNameAsWritten()
        {
            // arrange
            IFormattable encoding = MimeTransferEncoding.Parse(XUuencodeInMixedCase);

            // act
            var testResult = encoding.ToString(null, null);

            // assert
            testResult.Should().Be(XUuencodeInMixedCase);
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
