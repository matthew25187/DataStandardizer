using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the <see cref="VcardValueFormat.Xml"/> form of the value types: the contents of the value elements of xCard (RFC 6351 Appendix A).
    /// </summary>
    public class XcardValueFormatTests
    {
        // ref. RFC 6351 Appendix A §4.3.1: value-date = "\d{8}|\d{4}-\d\d|--\d\d(\d\d)?|---\d\d", which has no year alone.
        private const string DateComplete = "19850412";
        private const string DateYearAndMonth = "1985-04";
        private const string DateMonthAndDay = "--0412";
        private const string DateMonth = "--04";
        private const string DateDay = "---12";
        private const string DateYear = "1985";

        // ref. RFC 6351 Appendix A §4.3.2 and its erratum 4247: value-time = "(\d\d(\d\d(\d\d)?)?|-\d\d(\d\d)?|--\d\d)(Z|[+\-]\d\d(\d\d)?)?".
        private const string TimeComplete = "232050";
        private const string TimeMinuteAndSecond = "-2050";
        private const string TimeSecond = "--50";
        private const string TimeUtc = "123000Z";
        private const string TimeWithOffset = "123000-0800";

        // ref. RFC 6351 §4: the ANNIVERSARY of the example, a value-date-time.
        private const string DateTimeWithOffset = "20090808T1430-0500";

        // ref. RFC 6351 §4: the BDAY of the example, a value-date in a DATE-AND-OR-TIME.
        private const string DateAndOrTimeDate = "--0203";

        // ref. RFC 6351 Appendix A §4.3.4: a time alone is a value-time, which has no "T".
        private const string DateAndOrTimeTime = "1230";

        // ref. RFC 6473 §3: the REV of its example, a value-timestamp.
        private const string TimestampUtc = "19990104T122100Z";

        // ref. RFC 6351 Appendix A §4.7: value-utc-offset = "[+\-]\d\d(\d\d)?".
        private const string UtcOffset = "-0500";

        // ref. RFC 6351 §4 and RFC 6473 §3: URIs with a semicolon and a comma, which have no escaping of vCard in xCard.
        private const string UriWithComma = "geo:42.25,-91.05";

        private const int Year1985 = 1985;
        private const int April = 4;
        private const int Day12 = 12;

        public static TheoryData<string, VcardDate> Dates() => new TheoryData<string, VcardDate>
        {
            { DateComplete, new VcardDate(Year1985, April, Day12) },
            { DateYearAndMonth, VcardDate.FromYearAndMonth(Year1985, April) },
            { DateMonthAndDay, VcardDate.FromMonthAndDay(April, Day12) },
            { DateMonth, VcardDate.FromMonth(April) },
            { DateDay, VcardDate.FromDay(Day12) },
        };

        [Theory]
        [MemberData(nameof(Dates))]
        public void Parse_XmlDate_GivesTheDate(string value, VcardDate expected)
        {
            // act
            var testResult = VcardDate.Parse(value.AsSpan(), VcardValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Dates))]
        public void ToString_DateInXmlForm_WritesTheBasicFormat(string expected, VcardDate value)
        {
            // act
            var testResult = value.ToString(VcardValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(DateYear)]
        [InlineData("1985-04-12")]
        [InlineData(" 19850412")]
        public void TryParse_DateNotInXmlForm_IsFalse(string value)
        {
            // act
            var testResult = VcardDate.TryParse(value.AsSpan(), VcardValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ToString_DateOfAYearAloneInXmlForm_ThrowsInvalidOperation()
        {
            // arrange
            var date = VcardDate.FromYear(Year1985);

            // act
            Action testAction = () => date.ToString(VcardValueFormat.Xml);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        public static TheoryData<string, VcardTime> Times() => new TheoryData<string, VcardTime>
        {
            { TimeComplete, new VcardTime(23, 20, 50) },
            { TimeMinuteAndSecond, new VcardTime(null, 20, 50) },
            { TimeSecond, new VcardTime(null, null, 50) },
            { TimeUtc, new VcardTime(12, 30, 0, true) },
            { TimeWithOffset, new VcardTime(12, 30, 0, new VcardUtcOffset(true, 8, 0)) },
        };

        [Theory]
        [MemberData(nameof(Times))]
        public void Parse_XmlTime_GivesTheTime(string value, VcardTime expected)
        {
            // act
            var testResult = VcardTime.Parse(value.AsSpan(), VcardValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Times))]
        public void ToString_TimeInXmlForm_WritesTheBasicFormat(string expected, VcardTime value)
        {
            // act
            var testResult = value.ToString(VcardValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Parse_XmlDateTime_GivesTheDateAndTime()
        {
            // act
            var testResult = VcardDateTime.Parse(DateTimeWithOffset.AsSpan(), VcardValueFormat.Xml);

            // assert
            testResult.Should().Be(new VcardDateTime(new VcardDate(2009, 8, 8), new VcardTime(14, 30, null, new VcardUtcOffset(true, 5, 0))));
        }

        public static TheoryData<string, VcardDateAndOrTime> DatesAndOrTimes() => new TheoryData<string, VcardDateAndOrTime>
        {
            { DateAndOrTimeDate, new VcardDateAndOrTime(VcardDate.FromMonthAndDay(2, 3)) },
            { DateTimeWithOffset, new VcardDateAndOrTime(new VcardDateTime(new VcardDate(2009, 8, 8), new VcardTime(14, 30, null, new VcardUtcOffset(true, 5, 0)))) },
            { DateAndOrTimeTime, new VcardDateAndOrTime(new VcardTime(12, 30, null)) },

            // ref. RFC 6351 Appendix A: "--12" is both a value-date and a value-time, and a date is tried first.
            { DateMonth, new VcardDateAndOrTime(VcardDate.FromMonth(April)) },
        };

        [Theory]
        [MemberData(nameof(DatesAndOrTimes))]
        public void Parse_XmlDateAndOrTime_GivesTheValue(string value, VcardDateAndOrTime expected)
        {
            // act
            var testResult = VcardDateAndOrTime.Parse(value.AsSpan(), VcardValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(DatesAndOrTimes))]
        public void ToString_DateAndOrTimeInXmlForm_WritesTheContentOfItsElement(string expected, VcardDateAndOrTime value)
        {
            // act
            var testResult = value.ToString(VcardValueFormat.Xml);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("T1230")]
        [InlineData(DateYear)]
        [InlineData("2013-02-14T12:30:00")]
        public void TryParse_DateAndOrTimeNotInXmlForm_IsFalse(string value)
        {
            // act
            var testResult = VcardDateAndOrTime.TryParse(value.AsSpan(), VcardValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ToString_DateAndOrTimeOfAYearAloneInXmlForm_ThrowsInvalidOperation()
        {
            // arrange
            var value = new VcardDateAndOrTime(VcardDate.FromYear(Year1985));

            // act
            Action testAction = () => value.ToString(VcardValueFormat.Xml);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Parse_XmlTimestamp_GivesTheTimestamp()
        {
            // act
            var testResult = VcardTimestamp.Parse(TimestampUtc.AsSpan(), VcardValueFormat.Xml);

            // assert
            testResult.Should().Be(new VcardTimestamp(new VcardDate(1999, 1, 4), new VcardTime(12, 21, 0, true)));
        }

        [Fact]
        public void ToString_UtcOffsetInXmlForm_WritesTheBasicFormat()
        {
            // act
            var testResult = new VcardUtcOffset(true, 5, 0).ToString(VcardValueFormat.Xml);

            // assert
            testResult.Should().Be(UtcOffset);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("1", true)]
        [InlineData("0", false)]
        [InlineData(" true\n", true)]
        public void TryParseBoolean_XsdBoolean_GivesTheBoolean(string value, bool expected)
        {
            // act
            var testResult = VcardValue.TryParseBoolean(value.AsSpan(), VcardValueFormat.Xml, out var result);

            // assert
            (testResult, result).Should().Be((true, expected));
        }

        [Theory]
        [InlineData("TRUE")]
        [InlineData("False")]
        [InlineData("yes")]
        public void TryParseBoolean_NotAnXsdBoolean_IsFalse(string value)
        {
            // act
            var testResult = VcardValue.TryParseBoolean(value.AsSpan(), VcardValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryFormatBoolean_XmlForm_WritesLowerCase()
        {
            // arrange
            var destination = new char[5];

            // act
            VcardValue.TryFormatBoolean(true, destination, out var charsWritten, VcardValueFormat.Xml);
            var testResult = new string(destination, 0, charsWritten);

            // assert
            testResult.Should().Be("true");
        }

        [Theory]
        [InlineData("42", 42L)]
        [InlineData(" +42 ", 42L)]
        [InlineData("-007", -7L)]
        public void TryParseInteger_XsdInteger_GivesTheInteger(string value, long expected)
        {
            // act
            var testResult = VcardValue.TryParseInteger(value.AsSpan(), VcardValueFormat.Xml, out var result);

            // assert
            (testResult, result).Should().Be((true, expected));
        }

        [Theory]
        [InlineData("4.2")]
        [InlineData("9223372036854775808")]
        public void TryParseInteger_NotAnXsdIntegerOrTooLarge_IsFalse(string value)
        {
            // act
            var testResult = VcardValue.TryParseInteger(value.AsSpan(), VcardValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("1.3", 1.3)]
        [InlineData("1.5E3", 1500.0)]
        [InlineData(".5", 0.5)]
        [InlineData("5.", 5.0)]
        [InlineData(" -2e-1 ", -0.2)]
        public void TryParseFloat_XsdFloat_GivesTheNumber(string value, double expected)
        {
            // act
            var testResult = VcardValue.TryParseFloat(value.AsSpan(), VcardValueFormat.Xml, out var result);

            // assert
            (testResult, result).Should().Be((true, expected));
        }

        [Theory]
        [InlineData("INF")]
        [InlineData("-INF")]
        [InlineData("NaN")]
        [InlineData(".")]
        [InlineData("1e")]
        public void TryParseFloat_NotAFiniteXsdFloat_IsFalse(string value)
        {
            // act
            var testResult = VcardValue.TryParseFloat(value.AsSpan(), VcardValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParseText_XmlForm_IsTakenAsItIs()
        {
            // act
            var testResult = VcardValue.TryParseText("a,b;c\\d".AsSpan(), VcardValueFormat.Xml, out var result);

            // assert
            (testResult, result).Should().Be((true, "a,b;c\\d"));
        }

        [Fact]
        public void TryParseText_XmlFormWithACharacterXmlCannotHold_IsFalse()
        {
            // act
            var testResult = VcardValue.TryParseText("a\xFFFE".AsSpan(), VcardValueFormat.Xml, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void FormatText_XmlForm_WritesTheTextWithNoEscaping()
        {
            // act
            var testResult = VcardValue.FormatText("a,b;c\\d\ne", VcardValueFormat.Xml, VcardVersion.V4_0);

            // assert
            testResult.Should().Be("a,b;c\\d\ne");
        }

        [Fact]
        public void FormatText_XmlFormWithACharacterXmlCannotHold_ThrowsArgumentException()
        {
            // act
            Action testAction = () => VcardValue.FormatText("a\xFFFF", VcardValueFormat.Xml, VcardVersion.V4_0);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void EnumerateList_XmlForm_GivesTheValueAsOneItem()
        {
            // act
            var testResult = EnumerateXmlListItems("a,b");

            // assert
            testResult.Should().Equal("a,b");
        }

        [Theory]
        [InlineData(UriWithComma)]
        [InlineData(" geo:42.25,-91.05\n")]
        public void TryParseUri_XsdAnyUri_GivesTheUriWithItsComma(string value)
        {
            // act
            var testResult = VcardValue.TryParseUri(value.AsSpan(), VcardValueFormat.Xml, out var result);

            // assert
            (testResult, result?.OriginalString).Should().Be((true, UriWithComma));
        }

        [Fact]
        public void TryFormatUri_XmlForm_LeavesTheCommaUnescaped()
        {
            // arrange
            var destination = new char[32];

            // act
            VcardValue.TryFormatUri(new Uri(UriWithComma), destination, out var charsWritten, VcardValueFormat.Xml);
            var testResult = new string(destination, 0, charsWritten);

            // assert
            testResult.Should().Be(UriWithComma);
        }

        [Fact]
        public void ToString_GeoInXmlForm_LeavesTheCommaUnescaped()
        {
            // arrange
            var geo = VcardGeo.Parse(UriWithComma.AsSpan(), VcardValueFormat.Xml);

            // act
            var testResult = geo.ToString(VcardValueFormat.Xml);

            // assert
            testResult.Should().Be(UriWithComma);
        }

        [Fact]
        public void TryParseLanguageTag_XmlForm_GivesTheTag()
        {
            // act
            var testResult = VcardValue.TryParseLanguageTag("fr-CA".AsSpan(), VcardValueFormat.Xml, out var result);

            // assert
            (testResult, result.ToString()).Should().Be((true, "fr-CA"));
        }

        [Fact]
        public void Parse_XmlPid_GivesTheSameValueAsTheText()
        {
            // act
            var testResult = VcardPid.Parse("1.1".AsSpan(), VcardValueFormat.Xml);

            // assert
            testResult.Should().Be(new VcardPid(1, 1));
        }

#if NET6_0_OR_GREATER
        public static TheoryData<ISpanFormattable, string> Formattables() => new TheoryData<ISpanFormattable, string>
        {
            { new VcardDate(Year1985, April, Day12), DateComplete },
            { new VcardTime(23, 20, 50), TimeComplete },
            { new VcardDateAndOrTime(new VcardTime(12, 30, null)), DateAndOrTimeTime },
            { new VcardUtcOffset(true, 5, 0), UtcOffset },
        };

        [Theory]
        [MemberData(nameof(Formattables))]
        public void ToString_XmlFormatString_WritesTheXmlForm(ISpanFormattable value, string expected)
        {
            // act
            var testResult = value.ToString("X", null);

            // assert
            testResult.Should().Be(expected);
        }
#endif

        private static List<string> EnumerateXmlListItems(string value)
        {
            var items = new List<string>();
            foreach (var item in VcardValue.EnumerateList(value.AsSpan(), VcardValueFormat.Xml))
            {
                items.Add(item.ToString());
            }

            return items;
        }
    }
}
