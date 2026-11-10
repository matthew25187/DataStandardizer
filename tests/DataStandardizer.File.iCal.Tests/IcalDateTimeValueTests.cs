using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalDateTimeValueTests
    {
        // The DATE value of default(IcalDate), January 1 of year 1.
        private const string FirstDate = "00010101";

        // ref. RFC 5545 §3.3.12: a positive leap second, here on June 30, 1997.
        private const string LeapSecondDateTime = "19970630T235960Z";

        private const string LondonTimeZoneId = "Europe/London";
        private const string AucklandTimeZoneId = "Pacific/Auckland";

        // The length of a UTC DATE-TIME value, such as Rfc5545Examples.UtcDateTime.
        private const int UtcDateTimeLength = 16;

        #region IcalDate

        [Theory]
        [InlineData(Rfc5545Examples.Date, 1997, 7, 14)]
        [InlineData(FirstDate, 1, 1, 1)]
        [InlineData("99991231", 9999, 12, 31)]
        [InlineData("20240229", 2024, 2, 29)]
        public void IcalDate_TryParse_ValidValue_Parses(string value, int year, int month, int day)
        {
            // act
            var testResult = IcalDate.TryParse(value.AsSpan(), IcalValueFormat.Text, out var date);

            // assert
            testResult.Should().BeTrue();
            date.Year.Should().Be(year);
            date.Month.Should().Be(month);
            date.Day.Should().Be(day);
        }

        [Theory]
        [InlineData(1997, 7, 14, Rfc5545Examples.Date)]
        [InlineData(1, 1, 1, FirstDate)]
        [InlineData(9999, 12, 31, "99991231")]
        [InlineData(2024, 2, 29, "20240229")]
        public void IcalDate_ToString_ValidDate_WritesDateValue(int year, int month, int day, string expected)
        {
            // arrange
            var date = new IcalDate(year, month, day);

            // act
            var testResult = date.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("20260230")]
        [InlineData("20250229")]
        [InlineData("20261301")]
        [InlineData("20260001")]
        [InlineData("20260100")]
        [InlineData("00000101")]
        [InlineData("2026011")]
        [InlineData("202601011")]
        [InlineData("2026-01-01")]
        [InlineData("2026O101")]
        [InlineData("")]
        public void IcalDate_TryParse_InvalidValue_Fails(string value)
        {
            // act
            var testResult = IcalDate.TryParse(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalDate_Constructor_InvalidDay_Throws()
        {
            // act
            Action testAction = () => new IcalDate(2026, 2, 30);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("day");
        }

        [Fact]
        public void IcalDate_ToString_Default_Is00010101()
        {
            // act
            var testResult = default(IcalDate).ToString();

            // assert
            testResult.Should().Be(FirstDate);
        }

        [Theory]
        [InlineData(1, DayOfWeek.Thursday)]
        [InlineData(2, DayOfWeek.Friday)]
        [InlineData(3, DayOfWeek.Saturday)]
        [InlineData(4, DayOfWeek.Sunday)]
        [InlineData(5, DayOfWeek.Monday)]
        [InlineData(6, DayOfWeek.Tuesday)]
        [InlineData(7, DayOfWeek.Wednesday)]
        public void IcalDate_DayOfWeek_DayInJanuary2026_IsItsDayOfTheWeek(int day, DayOfWeek expected)
        {
            // arrange
            var date = IcalDate.FromDateTime(new DateTime(2026, 1, day));

            // act
            var testResult = date.DayOfWeek;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void IcalDate_LessThan_EarlierDate_IsTrue()
        {
            // arrange
            var earlier = new IcalDate(2026, 1, 31);
            var later = new IcalDate(2026, 2, 1);

            // act
            var testResult = earlier < later;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IcalDate_GreaterThanOrEqual_LaterDate_IsTrue()
        {
            // arrange
            var earlier = new IcalDate(2026, 1, 31);
            var later = new IcalDate(2026, 2, 1);

            // act
            var testResult = later >= earlier;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IcalDate_CompareTo_LaterDate_IsNegative()
        {
            // arrange
            var earlier = new IcalDate(2026, 1, 31);
            var later = new IcalDate(2026, 2, 1);

            // act
            var testResult = earlier.CompareTo(later);

            // assert
            testResult.Should().BeNegative();
        }

        [Fact]
        public void IcalDate_Equals_SameDate_IsTrue()
        {
            // arrange
            var date = new IcalDate(2026, 1, 31);
            var other = new IcalDate(2026, 1, 31);

            // act
            var testResult = date.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        #endregion

        #region IcalTime

        [Theory]
        [InlineData("230000", 23, 0, 0, false)]
        [InlineData(Rfc5545Examples.UtcTime, 7, 0, 0, true)]
        [InlineData("083000", 8, 30, 0, false)]
        [InlineData("235960Z", 23, 59, 60, true)]
        public void IcalTime_TryParse_ValidValue_Parses(string value, int hour, int minute, int second, bool isUtc)
        {
            // act
            var testResult = IcalTime.TryParse(value.AsSpan(), IcalValueFormat.Text, out var time);

            // assert
            testResult.Should().BeTrue();
            time.Should().Be(new IcalTime(hour, minute, second, isUtc));
        }

        [Theory]
        [InlineData(23, 0, 0, false, "230000")]
        [InlineData(7, 0, 0, true, Rfc5545Examples.UtcTime)]
        [InlineData(8, 30, 0, false, "083000")]
        [InlineData(23, 59, 60, true, "235960Z")]
        public void IcalTime_ToString_ValidTime_WritesTimeValue(int hour, int minute, int second, bool isUtc, string expected)
        {
            // arrange
            var time = new IcalTime(hour, minute, second, isUtc);

            // act
            var testResult = time.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("T250000")]
        [InlineData("250000")]
        [InlineData("236000")]
        [InlineData("235961")]
        [InlineData("230000-0800")]
        [InlineData("230000X")]
        [InlineData("2300")]
        [InlineData("")]
        public void IcalTime_TryParse_InvalidValue_Fails(string value)
        {
            // act
            var testResult = IcalTime.TryParse(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalTime_Parse_LowerCaseZ_IsUtc()
        {
            // act
            var testResult = IcalTime.Parse("070000z".AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.Should().Be(new IcalTime(7, 0, 0, true));
        }

        [Fact]
        public void IcalTime_Equals_FloatingAndUtc_AreDifferent()
        {
            // arrange
            var floating = new IcalTime(7, 0, 0);
            var utc = new IcalTime(7, 0, 0, true);

            // act
            var testResult = floating.Equals(utc);

            // assert
            testResult.Should().BeFalse();
        }

        #endregion

        #region IcalDateTime

        [Theory]
        [InlineData(Rfc5545Examples.FloatingDateTime, IcalDateTimeKind.Floating)]
        [InlineData(Rfc5545Examples.UtcDateTime, IcalDateTimeKind.Utc)]
        [InlineData("19970714T133000", IcalDateTimeKind.Floating)]
        [InlineData("19970714T173000Z", IcalDateTimeKind.Utc)]
        [InlineData(LeapSecondDateTime, IcalDateTimeKind.Utc)]
        public void IcalDateTime_TryParse_RfcExample_Parses(string value, IcalDateTimeKind kind)
        {
            // act
            var testResult = IcalDateTime.TryParse(value.AsSpan(), IcalValueFormat.Text, out var dateTime);

            // assert
            testResult.Should().BeTrue();
            dateTime.Kind.Should().Be(kind);
            dateTime.TimeZoneId.Should().BeNull();
        }

        [Theory]
        [InlineData(Rfc5545Examples.FloatingDateTime)]
        [InlineData(Rfc5545Examples.UtcDateTime)]
        [InlineData("19970714T133000")]
        [InlineData("19970714T173000Z")]
        [InlineData(LeapSecondDateTime)]
        public void IcalDateTime_ToString_ParsedRfcExample_WritesItAsItWasRead(string value)
        {
            // arrange
            var dateTime = IcalDateTime.Parse(value.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = dateTime.ToString();

            // assert
            testResult.Should().Be(value);
        }

        [Fact]
        public void IcalDateTime_TryParse_WithTimeZoneId_IsZoned()
        {
            // act
            var testResult = IcalDateTime.TryParse(Rfc5545Examples.NewYorkLocalDateTime.AsSpan(), Rfc5545Examples.NewYorkTimeZoneId,
                IcalValueFormat.Text, out var dateTime);

            // assert
            testResult.Should().BeTrue();
            dateTime.Kind.Should().Be(IcalDateTimeKind.Zoned);
            dateTime.TimeZoneId.Should().Be(Rfc5545Examples.NewYorkTimeZoneId);
            dateTime.Date.Should().Be(new IcalDate(1998, 1, 19));
            dateTime.Time.Should().Be(new IcalTime(2, 0, 0));
        }

        [Fact]
        public void IcalDateTime_ToString_Zoned_WritesLocalTimeWithoutTimeZone()
        {
            // arrange
            var dateTime = new IcalDateTime(new IcalDate(1998, 1, 19), new IcalTime(2, 0, 0), Rfc5545Examples.NewYorkTimeZoneId);

            // act
            var testResult = dateTime.ToString();

            // assert
            testResult.Should().Be(Rfc5545Examples.NewYorkLocalDateTime);
        }

        [Fact]
        public void IcalDateTime_TryParse_UtcWithTimeZoneId_Fails()
        {
            // act
            var testResult = IcalDateTime.TryParse(Rfc5545Examples.UtcDateTime.AsSpan(), Rfc5545Examples.NewYorkTimeZoneId, IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalDateTime_Parse_NullTimeZoneId_IsFloating()
        {
            // act
            var testResult = IcalDateTime.Parse(Rfc5545Examples.NewYorkLocalDateTime.AsSpan(), null, IcalValueFormat.Text);

            // assert
            testResult.Kind.Should().Be(IcalDateTimeKind.Floating);
        }

        [Fact]
        public void IcalDateTime_TryParse_EmptyTimeZoneId_Throws()
        {
            // act
            Action testAction = () => IcalDateTime.TryParse(Rfc5545Examples.NewYorkLocalDateTime.AsSpan(), string.Empty, IcalValueFormat.Text, out _);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("timeZoneId");
        }

        [Theory]
        [InlineData("19980119T230000-0800")]
        [InlineData("20260230T120000")]
        [InlineData("20260101T250000")]
        [InlineData("20260101 120000")]
        [InlineData("20260101T")]
        [InlineData("20260101")]
        [InlineData("2026-01-01T12:00:00Z")]
        [InlineData("")]
        public void IcalDateTime_TryParse_InvalidValue_Fails(string value)
        {
            // act
            var testResult = IcalDateTime.TryParse(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalDateTime_Equals_DifferentTimeZoneIds_IsFalse()
        {
            // arrange
            var london = new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 0, 0), LondonTimeZoneId);
            var paris = new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 0, 0), "Europe/Paris");

            // act
            var testResult = london.Equals(paris);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalDateTime_Equals_ZonedAndFloating_IsFalse()
        {
            // arrange
            var zoned = new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 0, 0), LondonTimeZoneId);
            var floating = new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 0, 0));

            // act
            var testResult = zoned.Equals(floating);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalDateTime_Equals_SameTimeZoneId_IsTrue()
        {
            // arrange
            var dateTime = new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 0, 0), LondonTimeZoneId);
            var other = new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 0, 0), LondonTimeZoneId);

            // act
            var testResult = dateTime.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void IcalDateTime_Constructor_UtcTimeWithTimeZoneId_Throws()
        {
            // act
            Action testAction = () => new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 0, 0, true), LondonTimeZoneId);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("time");
        }

        [Fact]
        public void IcalDateTime_FromDateTime_UtcWithFraction_IsUtcWithoutFraction()
        {
            // arrange
            var utc = new DateTime(2026, 3, 1, 9, 30, 15, 500, DateTimeKind.Utc);

            // act
            var testResult = IcalDateTime.FromDateTime(utc);

            // assert
            testResult.Should().Be(new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 30, 15, true)));
        }

        [Fact]
        public void IcalDateTime_FromDateTime_Local_IsFloating()
        {
            // arrange
            var local = new DateTime(2026, 3, 1, 9, 30, 15, DateTimeKind.Local);

            // act
            var testResult = IcalDateTime.FromDateTime(local);

            // assert
            testResult.Should().Be(new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 30, 15)));
        }

        [Fact]
        public void IcalDateTime_FromDateTime_WithTimeZoneId_IsZoned()
        {
            // arrange
            var local = new DateTime(2026, 3, 1, 9, 30, 15, DateTimeKind.Local);

            // act
            var testResult = IcalDateTime.FromDateTime(local, AucklandTimeZoneId);

            // assert
            testResult.Kind.Should().Be(IcalDateTimeKind.Zoned);
            testResult.TimeZoneId.Should().Be(AucklandTimeZoneId);
        }

        [Fact]
        public void IcalDateTime_ToDateTime_Utc_IsUtcDateTime()
        {
            // arrange
            var dateTime = new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 30, 15, true));

            // act
            var testResult = dateTime.ToDateTime();

            // assert
            testResult.Should().Be(new DateTime(2026, 3, 1, 9, 30, 15, DateTimeKind.Utc));
            testResult.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void IcalDateTime_ToDateTime_Floating_IsUnspecifiedDateTime()
        {
            // arrange
            var dateTime = new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 30, 15));

            // act
            var testResult = dateTime.ToDateTime();

            // assert
            testResult.Kind.Should().Be(DateTimeKind.Unspecified);
        }

        [Fact]
        public void IcalDateTime_ToDateTime_LeapSecond_Throws()
        {
            // arrange
            var leapSecond = IcalDateTime.Parse(LeapSecondDateTime.AsSpan(), IcalValueFormat.Text);

            // act
            Action testAction = () => leapSecond.ToDateTime();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void IcalDateTime_TryFormat_DestinationTooShort_ReturnsFalse()
        {
            // arrange
            var dateTime = IcalDateTime.Parse(Rfc5545Examples.UtcDateTime.AsSpan(), IcalValueFormat.Text);
            var destination = new char[15];

            // act
            var testResult = dateTime.TryFormat(destination, out var charsWritten, IcalValueFormat.Text);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void IcalDateTime_TryFormat_ExactLength_Succeeds()
        {
            // arrange
            var dateTime = IcalDateTime.Parse(Rfc5545Examples.UtcDateTime.AsSpan(), IcalValueFormat.Text);
            var destination = new char[UtcDateTimeLength];

            // act
            var testResult = dateTime.TryFormat(destination, out var charsWritten, IcalValueFormat.Text);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(UtcDateTimeLength);
            destination.Should().Equal(Rfc5545Examples.UtcDateTime.ToCharArray());
        }

        #endregion

        #region IcalUtcOffset

        [Theory]
        [InlineData(Rfc5545Examples.NegativeUtcOffset, -18000)]
        [InlineData(Rfc5545Examples.PositiveUtcOffset, 3600)]
        [InlineData("+0000", 0)]
        [InlineData("+1345", 49500)]
        [InlineData("-000001", -1)]
        [InlineData("+023045", 9045)]
        public void IcalUtcOffset_TryParse_ValidValue_Parses(string value, int totalSeconds)
        {
            // act
            var testResult = IcalUtcOffset.TryParse(value.AsSpan(), IcalValueFormat.Text, out var offset);

            // assert
            testResult.Should().BeTrue();
            offset.Offset.Should().Be(TimeSpan.FromSeconds(totalSeconds));
        }

        [Theory]
        [InlineData(-18000, Rfc5545Examples.NegativeUtcOffset)]
        [InlineData(3600, Rfc5545Examples.PositiveUtcOffset)]
        [InlineData(0, "+0000")]
        [InlineData(49500, "+1345")]
        [InlineData(-1, "-000001")]
        [InlineData(9045, "+023045")]
        public void IcalUtcOffset_ToString_ValidOffset_WritesUtcOffsetValue(int totalSeconds, string expected)
        {
            // arrange
            var offset = new IcalUtcOffset(TimeSpan.FromSeconds(totalSeconds));

            // act
            var testResult = offset.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("-0000")]
        [InlineData("-000000")]
        [InlineData("0500")]
        [InlineData("+2400")]
        [InlineData("+0060")]
        [InlineData("+000060")]
        [InlineData("+05")]
        [InlineData("+050")]
        [InlineData("-05:00")]
        [InlineData("")]
        public void IcalUtcOffset_TryParse_InvalidValue_Fails(string value)
        {
            // act
            var testResult = IcalUtcOffset.TryParse(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalUtcOffset_ToString_ParsedWithZeroSeconds_IsWrittenWithoutSeconds()
        {
            // arrange
            var offset = IcalUtcOffset.Parse("+010000".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = offset.ToString();

            // assert
            testResult.Should().Be(Rfc5545Examples.PositiveUtcOffset);
        }

        [Fact]
        public void IcalUtcOffset_Constructor_OneDay_ThrowsArgumentOutOfRange()
        {
            // act
            Action testAction = () => new IcalUtcOffset(TimeSpan.FromHours(24));

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void IcalUtcOffset_Constructor_FractionOfASecond_ThrowsArgumentException()
        {
            // act
            Action testAction = () => new IcalUtcOffset(TimeSpan.FromMilliseconds(500));

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        #endregion
    }
}
