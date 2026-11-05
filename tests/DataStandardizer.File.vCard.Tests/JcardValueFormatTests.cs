using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the <see cref="VcardValueFormat.Json"/> form of the value types: the values of jCard (RFC 7095 §3.5).
    /// </summary>
    public class JcardValueFormatTests
    {
        // ref. RFC 7095 §3.5.3: the examples and the table of DATE values.
        private const string DateComplete = "1985-04-12";
        private const string DateYearAndMonth = "1985-04";
        private const string DateYear = "1985";
        private const string DateMonthAndDay = "--04-12";
        private const string DateMonth = "--04";
        private const string DateDay = "---12";

        // ref. RFC 7095 §3.5.4: the examples and the table of TIME values.
        private const string TimeComplete = "23:20:50";
        private const string TimeHourAndMinute = "23:20";
        private const string TimeHour = "23";
        private const string TimeMinuteAndSecond = "-20:50";
        private const string TimeMinute = "-20";
        private const string TimeSecond = "--50";
        private const string TimeUtc = "12:30:00Z";
        private const string TimeWithOffset = "12:30:00-08:00";

        // ref. RFC 7095 §3.5.4: "all combinations may have any zone designator appended", and an offset of hours alone is "+04" in §3.5.5's table.
        private const string TimeWithHourOffset = "23:20:50+04";

        // ref. RFC 7095 §3.5.5: the examples and the table of DATE-TIME values.
        private const string DateTimeComplete = "1985-04-12T23:20:50";
        private const string DateTimeUtc = "1985-04-12T23:20:50Z";
        private const string DateTimeWithOffset = "1985-04-12T23:20:50+04:00";
        private const string DateTimeWithHourOffset = "1985-04-12T23:20:50+04";
        private const string DateTimeHourAndMinute = "1985-04-12T23:20";
        private const string DateTimeHour = "1985-04-12T23";
        private const string DateTimeMonthAndDay = "--04-12T23:20";
        private const string DateTimeDay = "---12T23:20";
        private const string DateTimeDayWithOffset = "---15T09:45:00+01:00";

        // ref. RFC 7095 §3.5.6: the examples of DATE-AND-OR-TIME values.
        private const string DateAndOrTimeDateTime = "2013-02-14T12:30:00";
        private const string DateAndOrTimeTruncatedDateTime = "---22T14:00";
        private const string DateAndOrTimeYear = "1985";
        private const string DateAndOrTimeTime = "T12:30";

        // ref. RFC 7095 §3.5.7: the examples of TIMESTAMP values.
        private const string TimestampLocal = "2013-02-14T12:30:00";
        private const string TimestampUtc = "2013-02-14T12:30:00Z";
        private const string TimestampWithHourOffset = "2013-02-14T12:30:00-05";
        private const string TimestampWithOffset = "2013-02-14T12:30:00-05:00";

        // ref. RFC 7095 §3.5.11: the example of a UTC-OFFSET value.  RFC 6350 §4 has an offset of hours alone, which has no minutes to separate.
        private const string UtcOffset = "-05:00";
        private const string UtcOffsetHours = "-05";

        // ref. RFC 7095 §3.5.8, §3.5.9 and §3.5.10: the examples of BOOLEAN, INTEGER and FLOAT values.
        private const string BooleanTrue = "true";
        private const string BooleanFalse = "false";
        private const string Integer = "42";
        private const string Float = "1.3";

        // ref. RFC 7095 §3.5.9: decimals and exponents "MUST be eliminated" from an INTEGER, and §3.5.10: exponents from a FLOAT.
        private const string IntegerWithDecimals = "3.14";
        private const string IntegerWithExponent = "2e10";
        private const string FloatWithExponent = "2e10";

        // ref. RFC 7095 Appendix B.1.2: a GEO URI, with no escaping of its comma.
        private const string GeoUri = "geo:46.772673,-71.282945";

        // ref. RFC 7095 §3.5.12: the example of a LANGUAGE-TAG value.
        private const string LanguageTag = "de";

        // ref. RFC 7095 §3.4: the value of the PREF parameter in Appendix B.1.2; a PID value of RFC 6350 §5.5 has the same form.
        private const string Pid = "1.1";

        private const int Year1985 = 1985;
        private const int April = 4;
        private const int Day12 = 12;

        public static TheoryData<string, VcardDate> Dates() => new TheoryData<string, VcardDate>
        {
            { DateComplete, new VcardDate(Year1985, April, Day12) },
            { DateYearAndMonth, VcardDate.FromYearAndMonth(Year1985, April) },
            { DateYear, VcardDate.FromYear(Year1985) },
            { DateMonthAndDay, VcardDate.FromMonthAndDay(April, Day12) },
            { DateMonth, VcardDate.FromMonth(April) },
            { DateDay, VcardDate.FromDay(Day12) },
        };

        [Theory]
        [MemberData(nameof(Dates))]
        public void Parse_JsonDate_GivesTheDate(string value, VcardDate expected)
        {
            // act
            var testResult = VcardDate.Parse(value.AsSpan(), VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Dates))]
        public void ToString_DateInJsonForm_WritesTheExtendedFormat(string expected, VcardDate value)
        {
            // act
            var testResult = value.ToString(VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("19850412")]
        [InlineData("--0412")]
        [InlineData("1985-4-12")]
        [InlineData("1985-13-01")]
        [InlineData("1985-04-1")]
        [InlineData("1985--04")]
        public void TryParse_DateNotInJsonForm_IsFalse(string value)
        {
            // act
            var testResult = VcardDate.TryParse(value.AsSpan(), VcardValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        public static TheoryData<string, VcardTime> Times() => new TheoryData<string, VcardTime>
        {
            { TimeComplete, new VcardTime(23, 20, 50) },
            { TimeHourAndMinute, new VcardTime(23, 20, null) },
            { TimeHour, new VcardTime(23, null, null) },
            { TimeMinuteAndSecond, new VcardTime(null, 20, 50) },
            { TimeMinute, new VcardTime(null, 20, null) },
            { TimeSecond, new VcardTime(null, null, 50) },
            { TimeUtc, new VcardTime(12, 30, 0, true) },
            { TimeWithOffset, new VcardTime(12, 30, 0, new VcardUtcOffset(true, 8, 0)) },
            { TimeWithHourOffset, new VcardTime(23, 20, 50, new VcardUtcOffset(false, 4, null)) },
        };

        [Theory]
        [MemberData(nameof(Times))]
        public void Parse_JsonTime_GivesTheTime(string value, VcardTime expected)
        {
            // act
            var testResult = VcardTime.Parse(value.AsSpan(), VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Times))]
        public void ToString_TimeInJsonForm_WritesTheExtendedFormat(string expected, VcardTime value)
        {
            // act
            var testResult = value.ToString(VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("232050")]
        [InlineData("23:2050")]
        [InlineData("24:00:00")]
        [InlineData("12:30:00-0800")]
        [InlineData("12:30:00z")]
        [InlineData("12:30:00,5")]
        [InlineData("-2050")]
        public void TryParse_TimeNotInJsonForm_IsFalse(string value)
        {
            // act
            var testResult = VcardTime.TryParse(value.AsSpan(), VcardValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ToString_TimeWithAFractionInJsonForm_Throws()
        {
            // arrange
            var value = VcardTime.Parse("10:22:00,5".AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0);

            // act
            Action testAction = () => value.ToString(VcardValueFormat.Json);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        public static TheoryData<string, VcardDateTime> DateTimes() => new TheoryData<string, VcardDateTime>
        {
            { DateTimeComplete, new VcardDateTime(new VcardDate(Year1985, April, Day12), new VcardTime(23, 20, 50)) },
            { DateTimeUtc, new VcardDateTime(new VcardDate(Year1985, April, Day12), new VcardTime(23, 20, 50, true)) },
            { DateTimeWithOffset, new VcardDateTime(new VcardDate(Year1985, April, Day12), new VcardTime(23, 20, 50, new VcardUtcOffset(false, 4, 0))) },
            { DateTimeWithHourOffset, new VcardDateTime(new VcardDate(Year1985, April, Day12), new VcardTime(23, 20, 50, new VcardUtcOffset(false, 4, null))) },
            { DateTimeHourAndMinute, new VcardDateTime(new VcardDate(Year1985, April, Day12), new VcardTime(23, 20, null)) },
            { DateTimeHour, new VcardDateTime(new VcardDate(Year1985, April, Day12), new VcardTime(23, null, null)) },
            { DateTimeMonthAndDay, new VcardDateTime(VcardDate.FromMonthAndDay(April, Day12), new VcardTime(23, 20, null)) },
            { DateTimeDay, new VcardDateTime(VcardDate.FromDay(Day12), new VcardTime(23, 20, null)) },
            { DateTimeDayWithOffset, new VcardDateTime(VcardDate.FromDay(15), new VcardTime(9, 45, 0, new VcardUtcOffset(false, 1, 0))) },
        };

        [Theory]
        [MemberData(nameof(DateTimes))]
        public void Parse_JsonDateTime_GivesTheDateAndTime(string value, VcardDateTime expected)
        {
            // act
            var testResult = VcardDateTime.Parse(value.AsSpan(), VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(DateTimes))]
        public void ToString_DateTimeInJsonForm_WritesTheExtendedFormat(string expected, VcardDateTime value)
        {
            // act
            var testResult = value.ToString(VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        /// <summary>
        /// The table of RFC 7095 §3.5.5 has <c>--04T23:20</c> and <c>--04T23</c>, a month alone before a time.  That table is "not a formal specification", and §3.5.5 says the
        /// restrictions of RFC 6350 apply, whose <c>date-noreduc</c> (§4) has no month alone, so they aren't valid.
        /// </summary>
        [Theory]
        [InlineData("--04T23:20")]
        [InlineData("--04T23")]
        [InlineData("1985-04T23:20")]
        [InlineData("1985-04-12T-20")]
        [InlineData("19850412T232050")]
        [InlineData("1985-04-12t23:20:50")]
        public void TryParse_DateTimeNotInJsonForm_IsFalse(string value)
        {
            // act
            var testResult = VcardDateTime.TryParse(value.AsSpan(), VcardValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        public static TheoryData<string, VcardDateAndOrTime> DatesAndOrTimes() => new TheoryData<string, VcardDateAndOrTime>
        {
            { DateAndOrTimeDateTime, new VcardDateAndOrTime(new VcardDateTime(new VcardDate(2013, 2, 14), new VcardTime(12, 30, 0))) },
            { DateAndOrTimeTruncatedDateTime, new VcardDateAndOrTime(new VcardDateTime(VcardDate.FromDay(22), new VcardTime(14, 0, null))) },
            { DateAndOrTimeYear, new VcardDateAndOrTime(VcardDate.FromYear(Year1985)) },
            { DateAndOrTimeTime, new VcardDateAndOrTime(new VcardTime(12, 30, null)) },
        };

        [Theory]
        [MemberData(nameof(DatesAndOrTimes))]
        public void Parse_JsonDateAndOrTime_GivesTheValue(string value, VcardDateAndOrTime expected)
        {
            // act
            var testResult = VcardDateAndOrTime.Parse(value.AsSpan(), VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(DatesAndOrTimes))]
        public void ToString_DateAndOrTimeInJsonForm_WritesTheExtendedFormat(string expected, VcardDateAndOrTime value)
        {
            // act
            var testResult = value.ToString(VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("12:30")]
        [InlineData("T1230")]
        [InlineData("19850412")]
        public void TryParse_DateAndOrTimeNotInJsonForm_IsFalse(string value)
        {
            // act
            var testResult = VcardDateAndOrTime.TryParse(value.AsSpan(), VcardValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        public static TheoryData<string, VcardTimestamp> Timestamps() => new TheoryData<string, VcardTimestamp>
        {
            { TimestampLocal, new VcardTimestamp(new VcardDate(2013, 2, 14), new VcardTime(12, 30, 0)) },
            { TimestampUtc, new VcardTimestamp(new VcardDate(2013, 2, 14), new VcardTime(12, 30, 0, true)) },
            { TimestampWithHourOffset, new VcardTimestamp(new VcardDate(2013, 2, 14), new VcardTime(12, 30, 0, new VcardUtcOffset(true, 5, null))) },
            { TimestampWithOffset, new VcardTimestamp(new VcardDate(2013, 2, 14), new VcardTime(12, 30, 0, new VcardUtcOffset(true, 5, 0))) },
        };

        [Theory]
        [MemberData(nameof(Timestamps))]
        public void Parse_JsonTimestamp_GivesTheTimestamp(string value, VcardTimestamp expected)
        {
            // act
            var testResult = VcardTimestamp.Parse(value.AsSpan(), VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Timestamps))]
        public void ToString_TimestampInJsonForm_WritesTheExtendedFormat(string expected, VcardTimestamp value)
        {
            // act
            var testResult = value.ToString(VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("2013-02-14T12:30")]
        [InlineData("--02-14T12:30:00")]
        [InlineData("20130214T123000")]
        public void TryParse_TimestampNotCompleteOrNotInJsonForm_IsFalse(string value)
        {
            // act
            var testResult = VcardTimestamp.TryParse(value.AsSpan(), VcardValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        public static TheoryData<string, VcardUtcOffset> Offsets() => new TheoryData<string, VcardUtcOffset>
        {
            { UtcOffset, new VcardUtcOffset(true, 5, 0) },
            { UtcOffsetHours, new VcardUtcOffset(true, 5, null) },
        };

        [Theory]
        [MemberData(nameof(Offsets))]
        public void Parse_JsonUtcOffset_GivesTheOffset(string value, VcardUtcOffset expected)
        {
            // act
            var testResult = VcardUtcOffset.Parse(value.AsSpan(), VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Offsets))]
        public void ToString_UtcOffsetInJsonForm_SeparatesTheHoursAndMinutes(string expected, VcardUtcOffset value)
        {
            // act
            var testResult = value.ToString(VcardValueFormat.Json);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("-0500")]
        [InlineData("-5:00")]
        [InlineData("-24:00")]
        public void TryParse_UtcOffsetNotInJsonForm_IsFalse(string value)
        {
            // act
            var testResult = VcardUtcOffset.TryParse(value.AsSpan(), VcardValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(BooleanTrue, true)]
        [InlineData(BooleanFalse, false)]
        public void TryParseBoolean_JsonLiteral_GivesTheBoolean(string value, bool expected)
        {
            // act
            bool result;
            var testResult = VcardValue.TryParseBoolean(value.AsSpan(), VcardValueFormat.Json, out result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("TRUE")]
        [InlineData("False")]
        [InlineData("1")]
        public void TryParseBoolean_NotAJsonLiteral_IsFalse(string value)
        {
            // act
            var testResult = VcardValue.TryParseBoolean(value.AsSpan(), VcardValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(true, BooleanTrue)]
        [InlineData(false, BooleanFalse)]
        public void TryFormatBoolean_JsonForm_WritesTheJsonLiteral(bool value, string expected)
        {
            // arrange
            var destination = new char[5];

            // act
            VcardValue.TryFormatBoolean(value, destination, out var charsWritten, VcardValueFormat.Json);

            // assert
            new string(destination, 0, charsWritten).Should().Be(expected);
        }

        [Theory]
        [InlineData(Integer, 42L)]
        [InlineData(IntegerWithDecimals, 3L)]
        [InlineData("-3.99", -3L)]
        [InlineData(IntegerWithExponent, 20000000000L)]
        [InlineData("-0", 0L)]
        [InlineData("9223372036854775807", long.MaxValue)]
        public void TryParseInteger_JsonNumber_GivesItWithoutDecimalsOrExponent(string value, long expected)
        {
            // act
            long result;
            var testResult = VcardValue.TryParseInteger(value.AsSpan(), VcardValueFormat.Json, out result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("+42")]
        [InlineData("042")]
        [InlineData("4.")]
        [InlineData("9223372036854775808")]
        [InlineData("1e400")]
        public void TryParseInteger_NotAJsonNumberOrTooLarge_IsFalse(string value)
        {
            // act
            var testResult = VcardValue.TryParseInteger(value.AsSpan(), VcardValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(Float, 1.3)]
        [InlineData(FloatWithExponent, 2e10)]
        [InlineData("-0.5", -0.5)]
        public void TryParseFloat_JsonNumber_GivesTheNumber(string value, double expected)
        {
            // act
            double result;
            var testResult = VcardValue.TryParseFloat(value.AsSpan(), VcardValueFormat.Json, out result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("+1.3")]
        [InlineData(".5")]
        [InlineData("01.5")]
        [InlineData("1e999")]
        public void TryParseFloat_NotAJsonNumberOrInfinite_IsFalse(string value)
        {
            // act
            var testResult = VcardValue.TryParseFloat(value.AsSpan(), VcardValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryFormatInteger_JsonForm_WritesAJsonNumber()
        {
            // arrange
            var destination = new char[20];

            // act
            VcardValue.TryFormatInteger(42, destination, out var charsWritten, VcardValueFormat.Json);

            // assert
            new string(destination, 0, charsWritten).Should().Be(Integer);
        }

        [Fact]
        public void TryFormatFloat_JsonForm_WritesAJsonNumber()
        {
            // arrange
            var destination = new char[20];

            // act
            VcardValue.TryFormatFloat(1.3, destination, out var charsWritten, VcardValueFormat.Json);

            // assert
            new string(destination, 0, charsWritten).Should().Be(Float);
        }

        [Fact]
        public void TryParseText_JsonString_IsTakenAsItIs()
        {
            // arrange
            const string value = "Stenophylla;Guinea\\,Africa";
            var destination = new char[value.Length];

            // act
            var testResult = VcardValue.TryParseText(value.AsSpan(), VcardValueFormat.Json, destination, out var charsWritten);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be(value);
        }

        [Fact]
        public void TryParseText_JsonStringWithAControlCharacter_IsFalse()
        {
            // arrange
            const string value = "a\x0001b";
            var destination = new char[value.Length];

            // act
            var testResult = VcardValue.TryParseText(value.AsSpan(), VcardValueFormat.Json, destination, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void FormatText_JsonForm_WritesTheTextWithNoEscaping()
        {
            // arrange
            const string value = "Mr. John Q. Public, Esq.;\\\n";

            // act
            var testResult = VcardValue.FormatText(value, VcardValueFormat.Json, VcardVersion.V4_0);

            // assert
            testResult.Should().Be(value);
        }

        [Fact]
        public void EnumerateList_JsonForm_GivesTheValueAsOneItem()
        {
            // act
            var testResult = EnumerateJsonListItems("computers,cameras");

            // assert
            testResult.Should().Equal("computers,cameras");
        }

        [Fact]
        public void TryParseUri_JsonStringWithAComma_GivesTheUri()
        {
            // act
            Uri result;
            var testResult = VcardValue.TryParseUri(GeoUri.AsSpan(), VcardValueFormat.Json, out result);

            // assert
            testResult.Should().BeTrue();
            result.OriginalString.Should().Be(GeoUri);
        }

        [Fact]
        public void TryParseUri_JsonStringWithAnEscapedComma_IsFalse()
        {
            // act
            var testResult = VcardValue.TryParseUri("geo:46.772673\\,-71.282945".AsSpan(), VcardValueFormat.Json, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryFormatUri_JsonForm_LeavesTheCommaUnescaped()
        {
            // arrange
            var destination = new char[64];

            // act
            VcardValue.TryFormatUri(new Uri(GeoUri), destination, out var charsWritten, VcardValueFormat.Json);

            // assert
            new string(destination, 0, charsWritten).Should().Be(GeoUri);
        }

        [Fact]
        public void Parse_JsonGeo_GivesTheCoordinates()
        {
            // act
            var testResult = VcardGeo.Parse(GeoUri.AsSpan(), VcardValueFormat.Json);

            // assert
            testResult.Latitude.Should().Be(46.772673);
        }

        [Fact]
        public void ToString_GeoInJsonForm_LeavesTheCommaUnescaped()
        {
            // arrange
            var value = VcardGeo.Parse(GeoUri.AsSpan(), VcardValueFormat.Json);

            // act
            var testResult = value.ToString(VcardValueFormat.Json);

            // assert
            testResult.Should().Be(GeoUri);
        }

        [Fact]
        public void TryParseLanguageTag_JsonString_GivesTheTag()
        {
            // act
            var testResult = VcardValue.TryParseLanguageTag(LanguageTag.AsSpan(), VcardValueFormat.Json, out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Parse_JsonPid_GivesTheSameValueAsTheText()
        {
            // act
            var testResult = VcardPid.Parse(Pid.AsSpan(), VcardValueFormat.Json);

            // assert
            testResult.Should().Be(new VcardPid(1, 1));
        }

#if NET6_0_OR_GREATER
        public static TheoryData<ISpanFormattable, string> Formattables() => new TheoryData<ISpanFormattable, string>
        {
            { new VcardDate(Year1985, April, Day12), DateComplete },
            { new VcardTime(23, 20, 50), TimeComplete },
            { new VcardDateTime(new VcardDate(Year1985, April, Day12), new VcardTime(23, 20, 50)), DateTimeComplete },
            { new VcardTimestamp(new VcardDate(2013, 2, 14), new VcardTime(12, 30, 0)), TimestampLocal },
            { new VcardDateAndOrTime(new VcardTime(12, 30, null)), DateAndOrTimeTime },
            { new VcardUtcOffset(true, 5, 0), UtcOffset },
        };

        [Theory]
        [MemberData(nameof(Formattables))]
        public void ToString_JsonFormatString_WritesTheJsonForm(ISpanFormattable value, string expected)
        {
            // act
            var testResult = value.ToString("J", null);

            // assert
            testResult.Should().Be(expected);
        }
#endif

        private static List<string> EnumerateJsonListItems(string value)
        {
            var items = new List<string>();
            foreach (var item in VcardValue.EnumerateList(value.AsSpan(), VcardValueFormat.Json))
            {
                items.Add(item.ToString());
            }

            return items;
        }
    }
}
