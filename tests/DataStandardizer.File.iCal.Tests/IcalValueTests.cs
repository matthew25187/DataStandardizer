using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalValueTests
    {
        // A TEXT value with an escaped COMMA, and the text it stands for.
        private const string EscapedComma = "a\\,b";
        private const string UnescapedComma = "a,b";

        // A backslash, semicolon and comma, unescaped and escaped as RFC 5545 §3.3.11 requires.
        private const string SpecialCharacters = "a\\b;c,d";
        private const string EscapedSpecialCharacters = "a\\\\b\\;c\\,d";

        private const string PlainColonAndQuotes = "Colon: and \"quotes\" are plain";
        private const string TabColonAndQuote = "tab\tand: \"quote\"";

        // Text with a letter, CJK characters and an emoji outside the BMP: "Caf\xE9 \x4F1A\x8B70 \xD83D\xDE00" is "Cafe, meeting, grinning face".
        private const string NonAsciiText = "Caf\xE9 \x4F1A\x8B70 \xD83D\xDE00";
        private const string NonAsciiTextWithEmoji = "Caf\xE9 \xD83D\xDE00";

        // A TEXT list item that is a backslash, an escaped backslash and an escaped comma.
        private const string EscapedBackslashThenComma = "a\\\\\\,b";

        // ref. RFC 5545 §3.3.8: the examples of INTEGER values.
        private const string Rfc5545Integer = "1234567890";
        private const int Rfc5545IntegerValue = 1234567890;
        private const string Rfc5545NegativeInteger = Rfc5545Examples.NegativeInteger;
        private const int Rfc5545NegativeIntegerValue = -1234567890;
        private const string Rfc5545PositiveSignedInteger = "+1234567890";
        private const string Rfc5545OtherInteger = "432109876";
        private const int Rfc5545OtherIntegerValue = 432109876;

        // The limits of a 32-bit INTEGER.
        private const string MaximumInteger = "2147483647";
        private const string MinimumInteger = "-2147483648";

        // ref. RFC 5545 §3.3.7: one of the examples of FLOAT values.
        private const string Rfc5545NegativeFloat = "-3.14";
        private const double Rfc5545NegativeFloatValue = -3.14;

        // ref. RFC 5545 §3.3.1: an inline binary value, the start of an icon file.
        private const string Rfc5545Binary = "AAABAAEAEBAQAAEABAAoAQAAFgAAACgAAAAQAAAAIAAAAAEABAAA";

        // The base64 encoding of the octets 0 to 255, in order.
        private const string EveryOctetBase64 =
            "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3eHl6e3x9fn+AgYKDhIWGh4iJiouMjY6PkJGSk5SVlpeYmZqbnJ2en6ChoqOkpaanqKmqq6ytrq+wsbKztLW2t7i5uru8vb6/wMHCw8TFxsfIycrLzM3Oz9DR0tPU1dbX2Nna29zd3t/g4eLj5OXm5+jp6uvs7e7v8PHy8/T19vf4+fr7/P3+/w==";

        // ref. RFC 5545 §3.3.3: the example of a CAL-ADDRESS value.
        private const string Rfc5545CalAddress = "mailto:jane_doe@example.com";

        private static readonly byte[] Rfc5545BinaryOctets =
        {
            0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x10, 0x10, 0x10, 0x00, 0x01, 0x00, 0x04, 0x00, 0x28, 0x01, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x28,
            0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x01, 0x00, 0x04, 0x00, 0x00,
        };

        // xunit replaces a lone surrogate in serialized theory data with U+FFFD, so these cases are not enumerated at discovery.
        public static TheoryData<string> InvalidTextCharacters => new TheoryData<string>
        {
            "bell\a",
            "delete\x7F",
            "lone \xD800 surrogate",
            "lone \xDC00 surrogate",
            "reversed \xDC00\xD800 pair",
        };

        public static TheoryData<byte[], string> BinaryValues()
        {
            var everyOctet = new byte[256];
            for (var index = 0; index < everyOctet.Length; index++)
            {
                everyOctet[index] = (byte)index;
            }

            return new TheoryData<byte[], string>
            {
                { new byte[0], string.Empty },
                { new byte[] { 0x0B }, "Cw==" },
                { new byte[] { 0x0B, 0x30 }, "CzA=" },
                { new byte[] { 0x0B, 0x30, 0x55 }, "CzBV" },
                { new byte[] { 0x0B, 0x30, 0x55, 0x7A }, "CzBVeg==" },
                { new byte[] { 0x0B, 0x30, 0x55, 0x7A, 0x9F }, "CzBVep8=" },
                { everyOctet, EveryOctetBase64 },
            };
        }

        public static TheoryData<double, string> FloatsWithoutExponents => new TheoryData<double, string>
        {
            { 1e20, "100000000000000000000" },
            { 1.5e-7, "0.00000015" },
            { -2.5e-5, "-0.000025" },
            { 1.25e21, "1250000000000000000000" },
            { 0.0, "0" },
            { Rfc5545NegativeFloatValue, Rfc5545NegativeFloat },
        };

        // ref. RFC 5545 §3.3.13, §3.3.3: the examples of URI and CAL-ADDRESS values.
        public static TheoryData<string> Rfc5545Uris => new TheoryData<string>
        {
            "http://example.com/my-report.txt",
            "ftp://example.com/pub/docs/agenda.doc",
            Rfc5545CalAddress,
            "urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6",
        };

        #region TEXT

        [Theory]
        [InlineData("Project XYZ Final Review\\nConference Room - 3B\\nCome Prepared.", "Project XYZ Final Review\nConference Room - 3B\nCome Prepared.")]
        [InlineData(EscapedSpecialCharacters, SpecialCharacters)]
        [InlineData("Line\\NBreak", "Line\nBreak")]
        [InlineData(PlainColonAndQuotes, PlainColonAndQuotes)]
        [InlineData(NonAsciiText, NonAsciiText)]
        [InlineData("", "")]
        public void TryParseText_ValidValue_Unescapes(string value, string expected)
        {
            // act
            var testResult = IcalValue.TryParseText(value.AsSpan(), IcalValueFormat.Text, out var text);

            // assert
            testResult.Should().BeTrue();
            text.Should().Be(expected);
        }

        [Theory]
        [InlineData("trailing\\")]
        [InlineData("bad \\x escape")]
        [InlineData("colon \\: escape")]
        public void TryParseText_InvalidEscape_Fails(string value)
        {
            // act
            var testResult = IcalValue.TryParseText(value.AsSpan(), IcalValueFormat.Text, out var text);

            // assert
            testResult.Should().BeFalse();
            text.Should().BeNull();
        }

        [Fact]
        public void TryParseText_IntoSpan_WritesUnescapedText()
        {
            // arrange
            var destination = new char[10];

            // act
            var testResult = IcalValue.TryParseText(EscapedComma.AsSpan(), IcalValueFormat.Text, destination, out var charsWritten);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be(UnescapedComma);
        }

        [Fact]
        public void TryParseText_DestinationShorterThanValue_Throws()
        {
            // act
            Action testAction = () => IcalValue.TryParseText(EscapedComma.AsSpan(), IcalValueFormat.Text, new char[3], out _);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("destination");
        }

        [Theory]
        [InlineData(SpecialCharacters, EscapedSpecialCharacters)]
        [InlineData("one\ntwo\r\nthree\rfour", "one\\ntwo\\nthree\\nfour")]
        [InlineData(TabColonAndQuote, TabColonAndQuote)]
        [InlineData(NonAsciiTextWithEmoji, NonAsciiTextWithEmoji)]
        [InlineData("", "")]
        public void FormatText_Value_Escapes(string value, string expected)
        {
            // act
            var testResult = IcalValue.FormatText(value, IcalValueFormat.Text);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(InvalidTextCharacters), DisableDiscoveryEnumeration = true)]
        public void FormatText_InvalidCharacter_Throws(string value)
        {
            // act
            Action testAction = () => IcalValue.FormatText(value, IcalValueFormat.Text);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("value");
        }

        [Theory]
        [InlineData("a\\\\b;c,d\nTabbed\there")]
        [InlineData("\\\\\\;,,;\\")]
        [InlineData("Caf\xE9 \x4F1A\x8B70 \xD83D\xDE00, more")]
        public void TryParseText_FormattedText_GivesTheText(string value)
        {
            // arrange
            var formatted = IcalValue.FormatText(value, IcalValueFormat.Text);

            // act
            IcalValue.TryParseText(formatted.AsSpan(), IcalValueFormat.Text, out var testResult);

            // assert
            testResult.Should().Be(value);
        }

        [Fact]
        public void TryFormatText_DestinationTooShort_ReturnsFalse()
        {
            // act
            var testResult = IcalValue.TryFormatText(UnescapedComma.AsSpan(), new char[3], out var charsWritten, IcalValueFormat.Text);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Theory]
        [InlineData("APPOINTMENT,EDUCATION", new[] { "APPOINTMENT", "EDUCATION" })]
        [InlineData("MEETING", new[] { "MEETING" })]
        [InlineData("a\\,b,c", new[] { EscapedComma, "c" })]
        [InlineData("a\\\\,b", new[] { "a\\\\", "b" })]
        [InlineData(EscapedBackslashThenComma, new[] { EscapedBackslashThenComma })]
        [InlineData(",", new[] { "", "" })]
        [InlineData("", new[] { "" })]
        [InlineData("a,,b,", new[] { "a", "", "b", "" })]
        public void EnumerateTextList_Value_SplitsOnUnescapedCommas(string value, string[] expected)
        {
            // act
            var testResult = IcalTestData.TextListItems(value, IcalValueFormat.Text);

            // assert
            testResult.Should().Equal(expected);
        }

        #endregion

        #region BOOLEAN

        [Theory]
        [InlineData("TRUE", true)]
        [InlineData("FALSE", false)]
        [InlineData("true", true)]
        [InlineData("False", false)]
        public void TryParseBoolean_ValidValue_Parses(string value, bool expected)
        {
            // act
            var testResult = IcalValue.TryParseBoolean(value.AsSpan(), IcalValueFormat.Text, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("YES")]
        [InlineData("1")]
        [InlineData("TRUE ")]
        public void TryParseBoolean_InvalidValue_Fails(string value)
        {
            // act
            var testResult = IcalValue.TryParseBoolean(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(true, "TRUE")]
        [InlineData(false, "FALSE")]
        public void TryFormatBoolean_Value_WritesUpperCase(bool value, string expected)
        {
            // arrange
            var destination = new char[5];

            // act
            var testResult = IcalValue.TryFormatBoolean(value, destination, out var charsWritten, IcalValueFormat.Text);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be(expected);
        }

        #endregion

        #region INTEGER

        [Theory]
        [InlineData(Rfc5545Integer, Rfc5545IntegerValue)]
        [InlineData(Rfc5545NegativeInteger, Rfc5545NegativeIntegerValue)]
        [InlineData(Rfc5545PositiveSignedInteger, Rfc5545IntegerValue)]
        [InlineData(Rfc5545OtherInteger, Rfc5545OtherIntegerValue)]
        [InlineData(MaximumInteger, int.MaxValue)]
        [InlineData(MinimumInteger, int.MinValue)]
        [InlineData("007", 7)]
        [InlineData("-0", 0)]
        public void TryParseInteger_RfcExampleOrLimit_Parses(string value, int expected)
        {
            // act
            var testResult = IcalValue.TryParseInteger(value.AsSpan(), IcalValueFormat.Text, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(Rfc5545IntegerValue, Rfc5545Integer)]
        [InlineData(Rfc5545NegativeIntegerValue, Rfc5545NegativeInteger)]
        [InlineData(Rfc5545OtherIntegerValue, Rfc5545OtherInteger)]
        [InlineData(int.MaxValue, MaximumInteger)]
        [InlineData(int.MinValue, MinimumInteger)]
        [InlineData(7, "7")]
        [InlineData(0, "0")]
        public void TryFormatInteger_RfcExampleOrLimit_WritesItWithoutPlusSignOrLeadingZeros(int value, string expected)
        {
            // arrange
            var destination = new char[11];

            // act
            var testResult = IcalValue.TryFormatInteger(value, destination, out var charsWritten, IcalValueFormat.Text);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("2147483648")]
        [InlineData("-2147483649")]
        [InlineData("1.0")]
        [InlineData("1e3")]
        [InlineData(" 1")]
        public void TryParseInteger_InvalidValue_Fails(string value)
        {
            // act
            var testResult = IcalValue.TryParseInteger(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        #endregion

        #region FLOAT

        [Theory]
        [InlineData("1000000.0000001", 1000000.0000001)]
        [InlineData("1.333", 1.333)]
        [InlineData(Rfc5545NegativeFloat, Rfc5545NegativeFloatValue)]
        [InlineData("+5", 5.0)]
        [InlineData("37.386013", 37.386013)]
        [InlineData("-122.082932", -122.082932)]
        public void TryParseFloat_RfcExample_Parses(string value, double expected)
        {
            // act
            var testResult = IcalValue.TryParseFloat(value.AsSpan(), IcalValueFormat.Text, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData(".5")]
        [InlineData("5.")]
        [InlineData("1e10")]
        [InlineData("1,5")]
        [InlineData("--1")]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        [InlineData("1.5 ")]
        public void TryParseFloat_InvalidValue_Fails(string value)
        {
            // act
            var testResult = IcalValue.TryParseFloat(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParseFloat_TooLarge_Fails()
        {
            // arrange: four hundred nines, which is larger than the largest double.
            var value = new string('9', 400);

            // act
            var testResult = IcalValue.TryParseFloat(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(FloatsWithoutExponents))]
        public void FormatFloat_Value_IsWrittenWithoutExponent(double value, string expected)
        {
            // act
            var testResult = IcalValue.FormatFloat(value, IcalValueFormat.Text);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(FloatsWithoutExponents))]
        public void TryParseFloat_ValueWrittenWithoutExponent_GivesTheValue(double expected, string value)
        {
            // act
            var testResult = IcalValue.TryParseFloat(value.AsSpan(), IcalValueFormat.Text, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void FormatFloat_NotFinite_Throws(double value)
        {
            // act
            Action testAction = () => IcalValue.FormatFloat(value, IcalValueFormat.Text);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        #endregion

        #region BINARY

        [Fact]
        public void TryParseBinary_RfcExample_GivesItsOctets()
        {
            // act
            var testResult = IcalValue.TryParseBinary(Rfc5545Binary.AsSpan(), IcalValueFormat.Text, out var bytes);

            // assert
            testResult.Should().BeTrue();
            bytes.Should().Equal(Rfc5545BinaryOctets);
        }

        [Fact]
        public void FormatBinary_RfcExampleOctets_WritesTheRfcExample()
        {
            // act
            var testResult = IcalValue.FormatBinary(Rfc5545BinaryOctets, IcalValueFormat.Text);

            // assert
            testResult.Should().Be(Rfc5545Binary);
        }

        [Theory]
        [MemberData(nameof(BinaryValues))]
        public void FormatBinary_OctetsOfEachLength_WritesBase64(byte[] bytes, string expected)
        {
            // act
            var testResult = IcalValue.FormatBinary(bytes, IcalValueFormat.Text);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(BinaryValues))]
        public void TryParseBinary_Base64OfEachLength_GivesTheOctets(byte[] expected, string value)
        {
            // act
            var testResult = IcalValue.TryParseBinary(value.AsSpan(), IcalValueFormat.Text, out var bytes);

            // assert
            testResult.Should().BeTrue();
            bytes.Should().Equal(expected);
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
        public void TryParseBinary_InvalidValue_Fails(string value)
        {
            // act
            var testResult = IcalValue.TryParseBinary(value.AsSpan(), IcalValueFormat.Text, out var bytes);

            // assert
            testResult.Should().BeFalse();
            bytes.Should().BeNull();
        }

        [Fact]
        public void TryFormatBinary_DestinationTooShort_ReturnsFalse()
        {
            // act
            var testResult = IcalValue.TryFormatBinary(new byte[] { 1, 2, 3, 4 }, new char[7], out var charsWritten, IcalValueFormat.Text);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        #endregion

        #region URI and CAL-ADDRESS

        [Theory]
        [MemberData(nameof(Rfc5545Uris))]
        public void TryParseUri_RfcExample_Parses(string value)
        {
            // act
            var testResult = IcalValue.TryParseUri(value.AsSpan(), IcalValueFormat.Text, out var uri);

            // assert
            testResult.Should().BeTrue();
            uri.Should().Be(new Uri(value));
        }

        [Theory]
        [MemberData(nameof(Rfc5545Uris))]
        public void TryFormatUri_RfcExample_WritesItAsItIs(string value)
        {
            // arrange
            var uri = new Uri(value);
            var destination = new char[64];

            // act
            var testResult = IcalValue.TryFormatUri(uri, destination, out var charsWritten, IcalValueFormat.Text);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be(value);
        }

        [Fact]
        public void TryParseCalAddress_RfcExample_IsMailto()
        {
            // act
            var testResult = IcalValue.TryParseCalAddress(Rfc5545CalAddress.AsSpan(), IcalValueFormat.Text, out var address);

            // assert
            testResult.Should().BeTrue();
            address.Scheme.Should().Be("mailto");
        }

        [Theory]
        [InlineData("")]
        [InlineData("/relative/path")]
        [InlineData("example.com")]
        [InlineData("1http://example.com")]
        [InlineData("C:\\temp\\file.txt")]
        public void TryParseUri_NotAbsoluteUri_Fails(string value)
        {
            // act
            var testResult = IcalValue.TryParseUri(value.AsSpan(), IcalValueFormat.Text, out var uri);

            // assert
            testResult.Should().BeFalse();
            uri.Should().BeNull();
        }

        [Fact]
        public void TryFormatUri_RelativeUri_Throws()
        {
            // act
            Action testAction = () => IcalValue.TryFormatUri(new Uri("relative", UriKind.Relative), new char[16], out _, IcalValueFormat.Text);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("value");
        }

        #endregion
    }
}
