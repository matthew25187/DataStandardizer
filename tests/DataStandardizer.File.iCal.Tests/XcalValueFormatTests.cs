using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the <see cref="IcalValueFormat.Xml"/> lexical forms of the value types (RFC 6321 §3.6).
    /// </summary>
    public class XcalValueFormatTests
    {
        [Theory]
        [InlineData("2011-05-17", 2011, 5, 17)]
        [InlineData("0001-01-01", 1, 1, 1)]
        public void Date_Xml_RoundTrips(string xml, int year, int month, int day)
        {
            // act
            var testResult = IcalDate.Parse(xml.AsSpan(), IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(new IcalDate(year, month, day));
            testResult.ToString(IcalValueFormat.Xml).Should().Be(xml);
        }

        [Theory]
        [InlineData("20110517")]
        [InlineData("2011-5-17")]
        [InlineData("2011/05/17")]
        [InlineData("2011-02-30")]
        [InlineData(" 2011-05-17")]
        public void Date_XmlNotValid_IsRejected(string xml)
        {
            // assert
            IcalDate.TryParse(xml.AsSpan(), IcalValueFormat.Xml, out _).Should().BeFalse();
        }

        [Theory]
        [InlineData("12:00:00", "120000")]
        [InlineData("23:59:60Z", "235960Z")]
        public void Time_Xml_RoundTrips(string xml, string text)
        {
            // act
            var testResult = IcalTime.Parse(xml.AsSpan(), IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(IcalTime.Parse(text.AsSpan(), IcalValueFormat.Text));
            testResult.ToString(IcalValueFormat.Xml).Should().Be(xml);
        }

        [Theory]
        [InlineData("120000")]
        [InlineData("12:00")]
        [InlineData("12:00:00+01:00")]
        [InlineData("24:00:00")]
        public void Time_XmlNotValid_IsRejected(string xml)
        {
            // assert
            IcalTime.TryParse(xml.AsSpan(), IcalValueFormat.Xml, out _).Should().BeFalse();
        }

        [Theory]
        [InlineData("2011-05-17T12:00:00", "20110517T120000")]
        [InlineData("2011-05-17T12:00:00Z", "20110517T120000Z")]
        public void DateTime_Xml_RoundTrips(string xml, string text)
        {
            // act
            var testResult = IcalDateTime.Parse(xml.AsSpan(), IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(IcalDateTime.Parse(text.AsSpan(), IcalValueFormat.Text));
            testResult.ToString(IcalValueFormat.Xml).Should().Be(xml);
        }

        [Fact]
        public void DateTime_XmlZoned_RejectsUtc()
        {
            // assert
            IcalDateTime.Parse("2011-05-17T12:00:00".AsSpan(), "Europe/London", IcalValueFormat.Xml).TimeZoneId.Should().Be("Europe/London");
            IcalDateTime.TryParse("2011-05-17T12:00:00Z".AsSpan(), "Europe/London", IcalValueFormat.Xml, out _).Should().BeFalse();
            IcalDateTime.TryParse("2011-05-17T12:00:00.5".AsSpan(), IcalValueFormat.Xml, out _).Should().BeFalse();
            IcalDateTime.TryParse("2011-05-17T12:00:00-05:00".AsSpan(), IcalValueFormat.Xml, out _).Should().BeFalse();
        }

        [Theory]
        [InlineData("-05:00", "-0500")]
        [InlineData("+00:00", "+0000")]
        [InlineData("+01:30:45", "+013045")]
        public void UtcOffset_Xml_RoundTrips(string xml, string text)
        {
            // act
            var testResult = IcalUtcOffset.Parse(xml.AsSpan(), IcalValueFormat.Xml);

            // assert
            testResult.Should().Be(IcalUtcOffset.Parse(text.AsSpan(), IcalValueFormat.Text));
            testResult.ToString(IcalValueFormat.Xml).Should().Be(xml);
        }

        [Theory]
        [InlineData("-0500")]
        [InlineData("-00:00")]
        [InlineData("05:00")]
        [InlineData("-05:00:")]
        [InlineData("-05-00")]
        public void UtcOffset_XmlNotValid_IsRejected(string xml)
        {
            // assert
            IcalUtcOffset.TryParse(xml.AsSpan(), IcalValueFormat.Xml, out _).Should().BeFalse();
        }

        [Fact]
        public void Duration_Xml_IsTheTextForm()
        {
            // act
            var testResult = IcalDuration.Parse("P15DT5H0M20S".AsSpan(), IcalValueFormat.Xml);

            // assert
            testResult.ToString(IcalValueFormat.Xml).Should().Be(testResult.ToString(IcalValueFormat.Text));
        }

        [Fact]
        public void Period_Xml_UsesXmlDateTimes()
        {
            // act
            var withEnd = IcalPeriod.Parse("1997-01-01T18:00:00Z/1997-01-02T07:00:00Z".AsSpan(), IcalValueFormat.Xml);
            var withDuration = IcalPeriod.Parse("1997-01-01T18:00:00/PT5H30M".AsSpan(), "Europe/London", IcalValueFormat.Xml);

            // assert
            withEnd.Should().Be(IcalPeriod.Parse("19970101T180000Z/19970102T070000Z".AsSpan(), IcalValueFormat.Text));
            withEnd.ToString(IcalValueFormat.Xml).Should().Be("1997-01-01T18:00:00Z/1997-01-02T07:00:00Z");
            withDuration.Start.TimeZoneId.Should().Be("Europe/London");
            withDuration.ToString(IcalValueFormat.Xml).Should().Be("1997-01-01T18:00:00/PT5H30M");
            IcalPeriod.TryParse("19970101T180000Z/PT5H30M".AsSpan(), IcalValueFormat.Xml, out _).Should().BeFalse();
        }

        [Fact]
        public void RecurrenceRule_Xml_ThrowsNotSupported()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=DAILY".AsSpan(), IcalValueFormat.Text);

            // assert
            FluentActions.Invoking(() => IcalRecurrenceRule.TryParse("FREQ=DAILY".AsSpan(), IcalValueFormat.Xml, out _))
                .Should().Throw<NotSupportedException>().WithMessage("*recur element*");
            rule.Invoking(value => value.ToString(IcalValueFormat.Xml)).Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void Text_Xml_HasNoEscaping()
        {
            // arrange
            const string text = "a,b;c\\d\ne:f";

            // act
            IcalValue.TryParseText(text.AsSpan(), IcalValueFormat.Xml, out var parsed).Should().BeTrue();
            var formatted = IcalValue.FormatText(text, IcalValueFormat.Xml);

            // assert
            parsed.Should().Be(text);
            formatted.Should().Be(text);
            FluentActions.Invoking(() => IcalValue.FormatText("a\u0001", IcalValueFormat.Xml)).Should().Throw<ArgumentException>();
            FluentActions.Invoking(() => IcalValue.FormatText("a￾", IcalValueFormat.Xml)).Should().Throw<ArgumentException>();
            FluentActions.Invoking(() => IcalValue.FormatText("a\uD800", IcalValueFormat.Xml)).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TextList_Xml_IsOneItem()
        {
            // act
            var testResult = new List<string>();
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
        public void Boolean_Xml_AcceptsSchemaLiterals(string xml, bool expected)
        {
            // assert
            IcalValue.TryParseBoolean(xml.AsSpan(), IcalValueFormat.Xml, out var testResult).Should().BeTrue();
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("TRUE")]
        [InlineData("yes")]
        [InlineData("")]
        public void Boolean_XmlNotValid_IsRejected(string xml)
        {
            // assert
            IcalValue.TryParseBoolean(xml.AsSpan(), IcalValueFormat.Xml, out _).Should().BeFalse();
        }

        [Fact]
        public void Boolean_Xml_FormatsLowerCase()
        {
            // arrange
            var destination = new char[8];

            // act
            IcalValue.TryFormatBoolean(true, destination, out var charsWritten, IcalValueFormat.Xml).Should().BeTrue();

            // assert
            new string(destination, 0, charsWritten).Should().Be("true");
        }

        [Theory]
        [InlineData(" 42 ", 42)]
        [InlineData("+7", 7)]
        [InlineData("-2147483648", int.MinValue)]
        public void Integer_Xml_TrimsWhitespace(string xml, int expected)
        {
            // assert
            IcalValue.TryParseInteger(xml.AsSpan(), IcalValueFormat.Xml, out var testResult).Should().BeTrue();
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("1.5", 1.5)]
        [InlineData(" -0.5 ", -0.5)]
        [InlineData("1.5E3", 1500)]
        [InlineData("2e-2", 0.02)]
        [InlineData(".5", 0.5)]
        [InlineData("5.", 5)]
        public void Float_Xml_AcceptsSchemaForms(string xml, double expected)
        {
            // assert
            IcalValue.TryParseFloat(xml.AsSpan(), IcalValueFormat.Xml, out var testResult).Should().BeTrue();
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("INF")]
        [InlineData("NaN")]
        [InlineData("1E")]
        [InlineData(".")]
        [InlineData("1E400")]
        public void Float_XmlNotValid_IsRejected(string xml)
        {
            // assert
            IcalValue.TryParseFloat(xml.AsSpan(), IcalValueFormat.Xml, out _).Should().BeFalse();
        }

        [Fact]
        public void Binary_Xml_IgnoresWhitespace()
        {
            // act
            IcalValue.TryParseBinary("SGVs\r\n  bG8g\tV29y bGQh".AsSpan(), IcalValueFormat.Xml, out var testResult).Should().BeTrue();

            // assert
            testResult.Should().Equal(System.Text.Encoding.UTF8.GetBytes("Hello World!"));
            IcalValue.FormatBinary(testResult, IcalValueFormat.Xml).Should().Be("SGVsbG8gV29ybGQh");
            IcalValue.TryParseBinary("SGVs bG8=".AsSpan(), IcalValueFormat.Text, out _).Should().BeFalse();
        }

        [Fact]
        public void Uri_Xml_TrimsWhitespace()
        {
            // assert
            IcalValue.TryParseUri(" http://calendar.example.com \n".AsSpan(), IcalValueFormat.Xml, out var testResult).Should().BeTrue();
            testResult.Should().Be(new Uri("http://calendar.example.com"));
            IcalValue.TryParseCalAddress("mailto:cyrus@example.com".AsSpan(), IcalValueFormat.Xml, out _).Should().BeTrue();
        }

        // The format string reaches the value only through IFormattable, which the types implement from .NET 6.
#if NET6_0_OR_GREATER
        [Fact]
        public void SpanFormattable_XFormat_UsesXmlForm()
        {
            // arrange
            var dateTime = IcalDateTime.Parse("19980119T070000Z".AsSpan(), IcalValueFormat.Text);
            var offset = new IcalUtcOffset(TimeSpan.FromHours(-5));

            // act
            var testResult = $"{dateTime:X}|{offset:x}|{new IcalDate(1997, 7, 14):X}|{new IcalTime(7, 0, 0, true):X}";

            // assert
            testResult.Should().Be("1998-01-19T07:00:00Z|-05:00|1997-07-14|07:00:00Z");
        }
#endif
    }
}
