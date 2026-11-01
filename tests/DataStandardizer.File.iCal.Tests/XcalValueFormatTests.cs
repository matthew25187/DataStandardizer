using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the <see cref="IcalValueFormat.Xml"/> lexical forms of the value types (RFC 6321 §3.6).
    /// </summary>
    public class XcalValueFormatTests
    {
        // ref. RFC 6321 §3.6.4, §3.6.5: noon on May 17, 2011, floating and in UTC.
        private const string Rfc6321FloatingDateTime = "2011-05-17T12:00:00";
        private const string Rfc6321UtcDateTime = "2011-05-17T12:00:00Z";

        // ref. RFC 6321 §3.6.9: Rfc5545Examples.ExplicitPeriod, and a floating period with a start and a duration, in the xCal form.
        private const string Rfc6321ExplicitPeriod = "1997-01-01T18:00:00Z/1997-01-02T07:00:00Z";
        private const string Rfc6321FloatingStartPeriod = "1997-01-01T18:00:00/PT5H30M";

        // ref. RFC 6321 §3.6.1: the BINARY example, "Hello World!".
        private const string Rfc6321Binary = "SGVsbG8gV29ybGQh";

        // ref. RFC 6321 §3.6.13: the URI example, written with whitespace around it.
        private const string Rfc6321Uri = "http://calendar.example.com";

        // An xCal TEXT value with each character that the iCalendar form escapes.
        private const string TextWithSpecialCharacters = "a,b;c\\d\ne:f";

        private const string LondonTimeZoneId = "Europe/London";

        public static TheoryData<string> XmlInvalidTextCharacters => new TheoryData<string>
        {
            "a\x0001",
            "a\xFFFE",
            "a\xD800",
        };

        public static TheoryData<IcalTime, string> XmlTimes => new TheoryData<IcalTime, string>
        {
            { new IcalTime(12, 0, 0), "12:00:00" },
            { new IcalTime(23, 59, 60, true), "23:59:60Z" },
        };

        public static TheoryData<IcalDateTime, string> XmlDateTimes => new TheoryData<IcalDateTime, string>
        {
            { new IcalDateTime(new IcalDate(2011, 5, 17), new IcalTime(12, 0, 0)), Rfc6321FloatingDateTime },
            { new IcalDateTime(new IcalDate(2011, 5, 17), new IcalTime(12, 0, 0, true)), Rfc6321UtcDateTime },
        };

        public static TheoryData<IcalUtcOffset, string> XmlUtcOffsets => new TheoryData<IcalUtcOffset, string>
        {
            { new IcalUtcOffset(new TimeSpan(-5, 0, 0)), "-05:00" },
            { new IcalUtcOffset(TimeSpan.Zero), "+00:00" },
            { new IcalUtcOffset(new TimeSpan(1, 30, 45)), "+01:30:45" },
        };

        public static TheoryData<Action> RecurrenceRuleXmlCalls()
        {
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily);
            return new TheoryData<Action>
            {
                () => IcalRecurrenceRule.TryParse("FREQ=DAILY".AsSpan(), IcalValueFormat.Xml, out _),
                () => rule.ToString(IcalValueFormat.Xml),
            };
        }

        [Theory]
        [InlineData("2011-05-17", 2011, 5, 17)]
        [InlineData("0001-01-01", 1, 1, 1)]
        public void IcalDateParse_XmlForm_IsTheDate(string xml, int year, int month, int day)
        {
            // act
            var testResult = IcalDate.Parse(xml.AsSpan(), IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(new IcalDate(year, month, day));
        }

        [Theory]
        [InlineData(2011, 5, 17, "2011-05-17")]
        [InlineData(1, 1, 1, "0001-01-01")]
        public void IcalDateToString_XmlForm_WritesXmlDate(int year, int month, int day, string expected)
        {
            // arrange
            var date = new IcalDate(year, month, day);

            // act
            var testResult = date.ToString(IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("20110517")]
        [InlineData("2011-5-17")]
        [InlineData("2011/05/17")]
        [InlineData("2011-02-30")]
        [InlineData(" 2011-05-17")]
        public void IcalDateTryParse_XmlFormNotValid_Fails(string xml)
        {
            // act
            var testResult = IcalDate.TryParse(xml.AsSpan(), IcalValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(XmlTimes), DisableDiscoveryEnumeration = true)]
        public void IcalTimeParse_XmlForm_IsTheTime(IcalTime expected, string xml)
        {
            // act
            var testResult = IcalTime.Parse(xml.AsSpan(), IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(XmlTimes), DisableDiscoveryEnumeration = true)]
        public void IcalTimeToString_XmlForm_WritesXmlTime(IcalTime time, string expected)
        {
            // act
            var testResult = time.ToString(IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("120000")]
        [InlineData("12:00")]
        [InlineData("12:00:00+01:00")]
        [InlineData("24:00:00")]
        public void IcalTimeTryParse_XmlFormNotValid_Fails(string xml)
        {
            // act
            var testResult = IcalTime.TryParse(xml.AsSpan(), IcalValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(XmlDateTimes), DisableDiscoveryEnumeration = true)]
        public void IcalDateTimeParse_XmlForm_IsTheDateTime(IcalDateTime expected, string xml)
        {
            // act
            var testResult = IcalDateTime.Parse(xml.AsSpan(), IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(XmlDateTimes), DisableDiscoveryEnumeration = true)]
        public void IcalDateTimeToString_XmlForm_WritesXmlDateTime(IcalDateTime dateTime, string expected)
        {
            // act
            var testResult = dateTime.ToString(IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void IcalDateTimeParse_XmlFormWithTimeZoneId_IsZoned()
        {
            // act
            var testResult = IcalDateTime.Parse(Rfc6321FloatingDateTime.AsSpan(), LondonTimeZoneId, IcalValueFormat.Xml);

            // assert
            testResult.TimeZoneId.Should().Be(LondonTimeZoneId);
        }

        [Fact]
        public void IcalDateTimeTryParse_XmlFormUtcWithTimeZoneId_Fails()
        {
            // act
            var testResult = IcalDateTime.TryParse(Rfc6321UtcDateTime.AsSpan(), LondonTimeZoneId, IcalValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("2011-05-17T12:00:00.5")]
        [InlineData("2011-05-17T12:00:00-05:00")]
        public void IcalDateTimeTryParse_XmlFormWithFractionOrOffset_Fails(string xml)
        {
            // act
            var testResult = IcalDateTime.TryParse(xml.AsSpan(), IcalValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(XmlUtcOffsets), DisableDiscoveryEnumeration = true)]
        public void IcalUtcOffsetParse_XmlForm_IsTheOffset(IcalUtcOffset expected, string xml)
        {
            // act
            var testResult = IcalUtcOffset.Parse(xml.AsSpan(), IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(XmlUtcOffsets), DisableDiscoveryEnumeration = true)]
        public void IcalUtcOffsetToString_XmlForm_WritesXmlOffset(IcalUtcOffset offset, string expected)
        {
            // act
            var testResult = offset.ToString(IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(Rfc5545Examples.NegativeUtcOffset)]
        [InlineData("-00:00")]
        [InlineData("05:00")]
        [InlineData("-05:00:")]
        [InlineData("-05-00")]
        public void IcalUtcOffsetTryParse_XmlFormNotValid_Fails(string xml)
        {
            // act
            var testResult = IcalUtcOffset.TryParse(xml.AsSpan(), IcalValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalDurationToString_XmlForm_IsTheTextForm()
        {
            // arrange
            var duration = IcalDuration.Parse(Rfc5545Examples.Duration.AsSpan(), IcalValueFormat.Xml);

            // act
            var testResult = duration.ToString(IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(Rfc5545Examples.Duration);
        }

        [Fact]
        public void IcalPeriodParse_XmlFormWithEnd_UsesXmlDateTimes()
        {
            // act
            var testResult = IcalPeriod.Parse(Rfc6321ExplicitPeriod.AsSpan(), IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(new IcalPeriod(new IcalDateTime(new IcalDate(1997, 1, 1), new IcalTime(18, 0, 0, true)),
                new IcalDateTime(new IcalDate(1997, 1, 2), new IcalTime(7, 0, 0, true))));
        }

        [Fact]
        public void IcalPeriodToString_XmlFormWithEnd_UsesXmlDateTimes()
        {
            // arrange
            var period = new IcalPeriod(new IcalDateTime(new IcalDate(1997, 1, 1), new IcalTime(18, 0, 0, true)),
                new IcalDateTime(new IcalDate(1997, 1, 2), new IcalTime(7, 0, 0, true)));

            // act
            var testResult = period.ToString(IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(Rfc6321ExplicitPeriod);
        }

        [Fact]
        public void IcalPeriodParse_XmlFormWithTimeZoneId_IsZoned()
        {
            // act
            var testResult = IcalPeriod.Parse(Rfc6321FloatingStartPeriod.AsSpan(), LondonTimeZoneId, IcalValueFormat.Xml);

            // assert
            testResult.Start.TimeZoneId.Should().Be(LondonTimeZoneId);
        }

        [Fact]
        public void IcalPeriodToString_XmlFormZonedWithDuration_UsesXmlDateTime()
        {
            // arrange
            var period = new IcalPeriod(new IcalDateTime(new IcalDate(1997, 1, 1), new IcalTime(18, 0, 0), LondonTimeZoneId),
                new IcalDuration(0, new TimeSpan(5, 30, 0)));

            // act
            var testResult = period.ToString(IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(Rfc6321FloatingStartPeriod);
        }

        [Fact]
        public void IcalPeriodTryParse_TextFormAsXml_Fails()
        {
            // act
            var testResult = IcalPeriod.TryParse(Rfc5545Examples.StartPeriod.AsSpan(), IcalValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(RecurrenceRuleXmlCalls), DisableDiscoveryEnumeration = true)]
        public void IcalRecurrenceRule_XmlForm_ThrowsNotSupported(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<NotSupportedException>().WithMessage("*recur element*");
        }

        [Fact]
        public void TryParseText_XmlForm_HasNoEscaping()
        {
            // act
            var testResult = IcalValue.TryParseText(TextWithSpecialCharacters.AsSpan(), IcalValueFormat.Xml, out var parsed);

            // assert
            testResult.Should().BeTrue();
            parsed.Should().Be(TextWithSpecialCharacters);
        }

        [Fact]
        public void FormatText_XmlForm_HasNoEscaping()
        {
            // act
            var testResult = IcalValue.FormatText(TextWithSpecialCharacters, IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(TextWithSpecialCharacters);
        }

        [Theory]
        [MemberData(nameof(XmlInvalidTextCharacters), DisableDiscoveryEnumeration = true)]
        public void FormatText_XmlFormCharacterXmlCannotHold_ThrowsArgumentException(string value)
        {
            // act
            Action testAction = () => IcalValue.FormatText(value, IcalValueFormat.Xml);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void EnumerateTextList_XmlForm_IsOneItem()
        {
            // arrange
            var testResult = new List<string>();

            // act
            foreach (var item in IcalValue.EnumerateTextList("a,b\\,c".AsSpan(), IcalValueFormat.Xml))
            {
                testResult.Add(item.ToString());
            }

            // assert
            testResult.Should().Equal("a,b\\,c");
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("1", true)]
        [InlineData(" 0\n", false)]
        public void TryParseBoolean_XmlSchemaLiteral_Parses(string xml, bool expected)
        {
            // act
            var testResult = IcalValue.TryParseBoolean(xml.AsSpan(), IcalValueFormat.Xml, out var parsed);

            // assert
            testResult.Should().BeTrue();
            parsed.Should().Be(expected);
        }

        [Theory]
        [InlineData("TRUE")]
        [InlineData("yes")]
        [InlineData("")]
        public void TryParseBoolean_XmlFormNotValid_Fails(string xml)
        {
            // act
            var testResult = IcalValue.TryParseBoolean(xml.AsSpan(), IcalValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryFormatBoolean_XmlForm_WritesLowerCase()
        {
            // arrange
            var destination = new char[8];

            // act
            var testResult = IcalValue.TryFormatBoolean(true, destination, out var charsWritten, IcalValueFormat.Xml);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be("true");
        }

        [Theory]
        [InlineData(" 42 ", 42)]
        [InlineData("+7", 7)]
        [InlineData("-2147483648", int.MinValue)]
        public void TryParseInteger_XmlForm_TrimsWhitespace(string xml, int expected)
        {
            // act
            var testResult = IcalValue.TryParseInteger(xml.AsSpan(), IcalValueFormat.Xml, out var parsed);

            // assert
            testResult.Should().BeTrue();
            parsed.Should().Be(expected);
        }

        [Theory]
        [InlineData("1.5", 1.5)]
        [InlineData(" -0.5 ", -0.5)]
        [InlineData("1.5E3", 1500)]
        [InlineData("2e-2", 0.02)]
        [InlineData(".5", 0.5)]
        [InlineData("5.", 5)]
        public void TryParseFloat_XmlSchemaForm_Parses(string xml, double expected)
        {
            // act
            var testResult = IcalValue.TryParseFloat(xml.AsSpan(), IcalValueFormat.Xml, out var parsed);

            // assert
            testResult.Should().BeTrue();
            parsed.Should().Be(expected);
        }

        [Theory]
        [InlineData("INF")]
        [InlineData("NaN")]
        [InlineData("1E")]
        [InlineData(".")]
        [InlineData("1E400")]
        public void TryParseFloat_XmlFormNotValid_Fails(string xml)
        {
            // act
            var testResult = IcalValue.TryParseFloat(xml.AsSpan(), IcalValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParseBinary_XmlFormWithWhitespace_IgnoresWhitespace()
        {
            // act
            var testResult = IcalValue.TryParseBinary("SGVs\r\n  bG8g\tV29y bGQh".AsSpan(), IcalValueFormat.Xml, out var bytes);

            // assert
            testResult.Should().BeTrue();
            bytes.Should().Equal(0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21);
        }

        [Fact]
        public void FormatBinary_XmlForm_IsTheTextForm()
        {
            // arrange
            var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21 };

            // act
            var testResult = IcalValue.FormatBinary(bytes, IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(Rfc6321Binary);
        }

        [Fact]
        public void TryParseBinary_TextFormWithSpace_Fails()
        {
            // act
            var testResult = IcalValue.TryParseBinary("SGVs bG8=".AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParseUri_XmlFormWithWhitespace_TrimsWhitespace()
        {
            // act
            var testResult = IcalValue.TryParseUri(" http://calendar.example.com \n".AsSpan(), IcalValueFormat.Xml, out var uri);

            // assert
            testResult.Should().BeTrue();
            uri.Should().Be(new Uri(Rfc6321Uri));
        }

        [Fact]
        public void TryParseCalAddress_XmlForm_Parses()
        {
            // act
            var testResult = IcalValue.TryParseCalAddress("mailto:cyrus@example.com".AsSpan(), IcalValueFormat.Xml, out _);

            // assert
            testResult.Should().BeTrue();
        }

        // The format string reaches the value only through IFormattable, which the types implement from .NET 6.
#if NET6_0_OR_GREATER
        [Fact]
        public void ToString_InterpolatedStringWithXFormat_UsesXmlForm()
        {
            // arrange
            var dateTime = IcalDateTime.Parse(Rfc5545Examples.UtcDateTime.AsSpan(), IcalValueFormat.Text);
            var offset = new IcalUtcOffset(TimeSpan.FromHours(-5));
            var date = new IcalDate(1997, 7, 14);
            var time = new IcalTime(7, 0, 0, true);

            // act
            var testResult = $"{dateTime:X}|{offset:x}|{date:X}|{time:X}";

            // assert
            testResult.Should().Be("1998-01-19T07:00:00Z|-05:00|1997-07-14|07:00:00Z");
        }
#endif
    }
}
