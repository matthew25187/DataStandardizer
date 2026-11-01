using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the <see cref="IcalValueFormat.Json"/> lexical forms of the value types (RFC 7265 §3.6).
    /// </summary>
    public class JcalValueFormatTests
    {
        // ref. RFC 7265 §3.6.1: the example DATE, May 17, 2011.
        private const string Rfc7265Date = "2011-05-17";

        // ref. RFC 7265 §3.6.4: the DATE-TIME examples, noon on October 17, 2012, floating and in UTC.
        private const string Rfc7265FloatingDateTime = "2012-10-17T12:00:00";
        private const string Rfc7265UtcDateTime = "2012-10-17T12:00:00Z";

        // ref. RFC 7265 §3.6.9: the PERIOD example, a day from 4 PM UTC on March 8, 1997.
        private const string Rfc7265Period = "1997-03-08T16:00:00Z/P1D";

        // ref. RFC 7265 §3.6.1: the BINARY example, "Hello World!".
        private const string Rfc7265Binary = "SGVsbG8gV29ybGQh";

        // ref. RFC 7265 §3.6.3: the CAL-ADDRESS example.
        private const string Rfc7265CalAddress = "mailto:kewisch@example.com";

        private const string BerlinTimeZoneId = "Europe/Berlin";
        private const string OneDay = "P1D";

        // Text with an emoji outside the BMP: "pair" and U+1F600 GRINNING FACE.
        private const string TextWithSurrogatePair = "pair\xD83D\xDE00";

        public static TheoryData<string> JsonInvalidTextCharacters => new TheoryData<string>
        {
            "bell\a",
            "lone\xD800",
        };

        public static TheoryData<IcalDate, string> JsonDates => new TheoryData<IcalDate, string>
        {
            { new IcalDate(2011, 5, 17), Rfc7265Date },
            { new IcalDate(1, 1, 1), "0001-01-01" },
        };

        public static TheoryData<IcalDateTime, string> JsonDateTimes => new TheoryData<IcalDateTime, string>
        {
            { new IcalDateTime(new IcalDate(2012, 10, 17), new IcalTime(12, 0, 0)), Rfc7265FloatingDateTime },
            { new IcalDateTime(new IcalDate(2012, 10, 17), new IcalTime(12, 0, 0, true)), Rfc7265UtcDateTime },
        };

        public static TheoryData<IcalTime, string> JsonTimes => new TheoryData<IcalTime, string>
        {
            { new IcalTime(12, 30, 0), "12:30:00" },
            { new IcalTime(12, 30, 0, true), "12:30:00Z" },
        };

        public static TheoryData<IcalUtcOffset, string> JsonUtcOffsets => new TheoryData<IcalUtcOffset, string>
        {
            { new IcalUtcOffset(new TimeSpan(-5, 0, 0)), "-05:00" },
            { new IcalUtcOffset(new TimeSpan(12, 45, 0)), "+12:45" },
            { new IcalUtcOffset(new TimeSpan(1, 30, 45)), "+01:30:45" },
        };

        public static TheoryData<Action> RecurrenceRuleJsonCalls => new TheoryData<Action>
        {
            () => IcalRecurrenceRule.Parse("FREQ=DAILY".AsSpan(), IcalValueFormat.Json),
            () => new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).ToString(IcalValueFormat.Json),
        };

        [Theory]
        [MemberData(nameof(JsonDates), DisableDiscoveryEnumeration = true)]
        public void IcalDateParse_JsonForm_IsTheDate(IcalDate expected, string json)
        {
            // act
            var testResult = IcalDate.Parse(json.AsSpan(), IcalValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(JsonDates), DisableDiscoveryEnumeration = true)]
        public void IcalDateToString_JsonForm_WritesJsonDate(IcalDate date, string expected)
        {
            // act
            var testResult = date.ToString(IcalValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("20110517")]
        [InlineData("2011-05")]
        [InlineData("2011-02-30")]
        [InlineData(" 2011-05-17")]
        public void IcalDateTryParse_JsonFormNotValid_Fails(string json)
        {
            // act
            var testResult = IcalDate.TryParse(json.AsSpan(), IcalValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(JsonDateTimes), DisableDiscoveryEnumeration = true)]
        public void IcalDateTimeParse_JsonForm_IsTheDateTime(IcalDateTime expected, string json)
        {
            // act
            var testResult = IcalDateTime.Parse(json.AsSpan(), IcalValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimes), DisableDiscoveryEnumeration = true)]
        public void IcalDateTimeToString_JsonForm_WritesJsonDateTime(IcalDateTime dateTime, string expected)
        {
            // act
            var testResult = dateTime.ToString(IcalValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void IcalDateTimeParse_JsonFormWithTimeZoneId_IsZoned()
        {
            // act
            var testResult = IcalDateTime.Parse(Rfc7265FloatingDateTime.AsSpan(), BerlinTimeZoneId, IcalValueFormat.Json);

            // assert
            testResult.TimeZoneId.Should().Be(BerlinTimeZoneId);
        }

        [Theory]
        [MemberData(nameof(JsonTimes), DisableDiscoveryEnumeration = true)]
        public void IcalTimeParse_JsonForm_IsTheTime(IcalTime expected, string json)
        {
            // act
            var testResult = IcalTime.Parse(json.AsSpan(), IcalValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(JsonTimes), DisableDiscoveryEnumeration = true)]
        public void IcalTimeToString_JsonForm_WritesJsonTime(IcalTime time, string expected)
        {
            // act
            var testResult = time.ToString(IcalValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(JsonUtcOffsets), DisableDiscoveryEnumeration = true)]
        public void IcalUtcOffsetParse_JsonForm_IsTheOffset(IcalUtcOffset expected, string json)
        {
            // act
            var testResult = IcalUtcOffset.Parse(json.AsSpan(), IcalValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(JsonUtcOffsets), DisableDiscoveryEnumeration = true)]
        public void IcalUtcOffsetToString_JsonForm_WritesJsonOffset(IcalUtcOffset offset, string expected)
        {
            // act
            var testResult = offset.ToString(IcalValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(Rfc5545Examples.NegativeUtcOffset)]
        [InlineData("+1245")]
        [InlineData("+013045")]
        public void IcalUtcOffsetTryParse_TextFormAsJson_Fails(string text)
        {
            // act
            var testResult = IcalUtcOffset.TryParse(text.AsSpan(), IcalValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalDurationToString_JsonForm_IsTheTextForm()
        {
            // arrange
            var duration = new IcalDuration(1, TimeSpan.Zero);

            // act
            var testResult = duration.ToString(IcalValueFormat.Json);

            // assert
            testResult.Should().Be(OneDay);
        }

        [Fact]
        public void IcalDurationParse_JsonForm_IsTheTextForm()
        {
            // act
            var testResult = IcalDuration.Parse(OneDay.AsSpan(), IcalValueFormat.Json);

            // assert
            testResult.Should().Be(new IcalDuration(1, TimeSpan.Zero));
        }

        [Fact]
        public void IcalPeriodParse_JsonForm_JoinsTheJsonPartsWithASolidus()
        {
            // act
            var testResult = IcalPeriod.Parse(Rfc7265Period.AsSpan(), IcalValueFormat.Json);

            // assert
            testResult.Should().Be(new IcalPeriod(new IcalDateTime(new IcalDate(1997, 3, 8), new IcalTime(16, 0, 0, true)), new IcalDuration(1, TimeSpan.Zero)));
        }

        [Fact]
        public void IcalPeriodToString_JsonForm_JoinsTheJsonPartsWithASolidus()
        {
            // arrange
            var period = new IcalPeriod(new IcalDateTime(new IcalDate(1997, 3, 8), new IcalTime(16, 0, 0, true)), new IcalDuration(1, TimeSpan.Zero));

            // act
            var testResult = period.ToString(IcalValueFormat.Json);

            // assert
            testResult.Should().Be(Rfc7265Period);
        }

        [Theory]
        [MemberData(nameof(RecurrenceRuleJsonCalls), DisableDiscoveryEnumeration = true)]
        public void IcalRecurrenceRule_JsonForm_ThrowsNotSupported(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<NotSupportedException>().WithMessage("*jCal*");
        }

        [Fact]
        public void TryParseText_JsonForm_HasNoEscaping()
        {
            // act
            var testResult = IcalValue.TryParseText(@"a\,b;c".AsSpan(), IcalValueFormat.Json, out var parsed);

            // assert
            testResult.Should().BeTrue();
            parsed.Should().Be(@"a\,b;c");
        }

        [Theory]
        [InlineData("hello, world\nnext; line")]
        [InlineData(TextWithSurrogatePair)]
        public void FormatText_JsonForm_HasNoEscaping(string value)
        {
            // act
            var testResult = IcalValue.FormatText(value, IcalValueFormat.Json);

            // assert
            testResult.Should().Be(value);
        }

        [Theory]
        [MemberData(nameof(JsonInvalidTextCharacters), DisableDiscoveryEnumeration = true)]
        public void FormatText_JsonFormInvalidCharacter_ThrowsArgumentException(string value)
        {
            // act
            Action testAction = () => IcalValue.FormatText(value, IcalValueFormat.Json);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void EnumerateTextList_JsonForm_IsOneItem()
        {
            // act
            var testResult = IcalTestData.TextListItems("a,b", IcalValueFormat.Json);

            // assert
            testResult.Should().Equal("a,b");
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void TryParseBoolean_JsonLiteral_Parses(string json, bool expected)
        {
            // act
            var testResult = IcalValue.TryParseBoolean(json.AsSpan(), IcalValueFormat.Json, out var parsed);

            // assert
            testResult.Should().BeTrue();
            parsed.Should().Be(expected);
        }

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public void TryFormatBoolean_JsonForm_WritesJsonLiteral(bool value, string expected)
        {
            // arrange
            var buffer = new char[8];

            // act
            var testResult = IcalValue.TryFormatBoolean(value, buffer, out var charsWritten, IcalValueFormat.Json);

            // assert
            testResult.Should().BeTrue();
            new string(buffer, 0, charsWritten).Should().Be(expected);
        }

        [Theory]
        [InlineData("TRUE")]
        [InlineData("True")]
        [InlineData("1")]
        [InlineData(" true")]
        public void TryParseBoolean_JsonFormNotValid_Fails(string json)
        {
            // act
            var testResult = IcalValue.TryParseBoolean(json.AsSpan(), IcalValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("42", 42)]
        [InlineData("-7", -7)]
        [InlineData("0", 0)]
        [InlineData("1.0", 1)]
        [InlineData("1e2", 100)]
        [InlineData("-2147483648", int.MinValue)]
        public void TryParseInteger_WholeJsonNumber_Parses(string json, int expected)
        {
            // act
            var testResult = IcalValue.TryParseInteger(json.AsSpan(), IcalValueFormat.Json, out var parsed);

            // assert
            testResult.Should().BeTrue();
            parsed.Should().Be(expected);
        }

        [Theory]
        [InlineData("+42")]
        [InlineData("042")]
        [InlineData("1.5")]
        [InlineData("2147483648")]
        [InlineData("1.")]
        [InlineData(" 1")]
        public void TryParseInteger_JsonFormNotValid_Fails(string json)
        {
            // act
            var testResult = IcalValue.TryParseInteger(json.AsSpan(), IcalValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("1.3", 1.3)]
        [InlineData("-122.082932", -122.082932)]
        [InlineData("1.5E3", 1500)]
        [InlineData("0", 0)]
        public void TryParseFloat_JsonNumber_Parses(string json, double expected)
        {
            // act
            var testResult = IcalValue.TryParseFloat(json.AsSpan(), IcalValueFormat.Json, out var parsed);

            // assert
            testResult.Should().BeTrue();
            parsed.Should().Be(expected);
        }

        [Theory]
        [InlineData(1.3, "1.3")]
        [InlineData(-122.082932, "-122.082932")]
        [InlineData(1500, "1500")]
        [InlineData(0, "0")]
        public void FormatFloat_JsonForm_WritesItWithoutExponent(double value, string expected)
        {
            // act
            var testResult = IcalValue.FormatFloat(value, IcalValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("+1.3")]
        [InlineData("01.3")]
        [InlineData(".5")]
        [InlineData("NaN")]
        [InlineData("1e999")]
        public void TryParseFloat_JsonFormNotValid_Fails(string json)
        {
            // act
            var testResult = IcalValue.TryParseFloat(json.AsSpan(), IcalValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParseBinary_JsonForm_IsTheTextForm()
        {
            // act
            var testResult = IcalValue.TryParseBinary(Rfc7265Binary.AsSpan(), IcalValueFormat.Json, out var bytes);

            // assert
            testResult.Should().BeTrue();
            bytes.Should().Equal(0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21);
        }

        [Fact]
        public void FormatBinary_JsonForm_IsTheTextForm()
        {
            // arrange
            var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21 };

            // act
            var testResult = IcalValue.FormatBinary(bytes, IcalValueFormat.Json);

            // assert
            testResult.Should().Be(Rfc7265Binary);
        }

        [Fact]
        public void TryParseBinary_JsonFormWithSpace_Fails()
        {
            // act
            var testResult = IcalValue.TryParseBinary("SGVs bG8=".AsSpan(), IcalValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParseCalAddress_JsonForm_IsTheTextForm()
        {
            // act
            var testResult = IcalValue.TryParseCalAddress(Rfc7265CalAddress.AsSpan(), IcalValueFormat.Json, out var uri);

            // assert
            testResult.Should().BeTrue();
            uri.OriginalString.Should().Be(Rfc7265CalAddress);
        }

        [Fact]
        public void TryParseUri_JsonFormWithSpace_Fails()
        {
            // act
            var testResult = IcalValue.TryParseUri(" http://example.org/".AsSpan(), IcalValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }
    }
}
