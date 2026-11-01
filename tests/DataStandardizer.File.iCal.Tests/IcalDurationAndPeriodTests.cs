using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalDurationAndPeriodTests
    {
        private const string OneDay = "P1D";
        private const string TwentyFourHours = "PT24H";
        private const string OneHour = "PT1H";
        private const string NinetyMinutes = "PT1H30M";
        private const string FiveSeconds = "PT5S";
        private const string ZeroSeconds = "PT0S";
        private const string LondonTimeZoneId = "Europe/London";

        // ref. RFC 5545 §3.3.6: seven weeks, here negative.
        private const string MinusSevenWeeks = "-P7W";

        public static TheoryData<string> CanonicalDurations => new TheoryData<string>
        {
            Rfc5545Examples.Duration,
            "P7W",
            "PT15M",
            "-PT15M",
            OneDay,
            TwentyFourHours,
            OneHour,
            NinetyMinutes,
            FiveSeconds,
            "PT1M5S",
            "P2DT1H",
            "-P1W",
            "P0W",
            ZeroSeconds,
        };

        public static TheoryData<string> RfcExamplePeriods => new TheoryData<string>
        {
            Rfc5545Examples.ExplicitPeriod,
            Rfc5545Examples.StartPeriod,
        };

        #region IcalDuration

        [Theory]
        [MemberData(nameof(CanonicalDurations))]
        public void IcalDuration_TryParse_CanonicalValue_Succeeds(string value)
        {
            // act
            var testResult = IcalDuration.TryParse(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(CanonicalDurations))]
        public void IcalDuration_ToString_ParsedCanonicalValue_IsWrittenExactly(string value)
        {
            // arrange
            var duration = IcalDuration.Parse(value.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = duration.ToString();

            // assert
            testResult.Should().Be(value);
        }

        [Theory]
        [InlineData("+P1D", OneDay)]
        [InlineData("PT90M", NinetyMinutes)]
        [InlineData("PT0H0M5S", FiveSeconds)]
        [InlineData("PT1H0M0S", OneHour)]
        [InlineData("P0D", ZeroSeconds)]
        [InlineData("-PT0S", ZeroSeconds)]
        [InlineData("p1dt2h", "P1DT2H")]
        public void IcalDuration_ToString_ParsedNonCanonicalValue_IsWrittenCanonically(string value, string expected)
        {
            // arrange
            var duration = IcalDuration.Parse(value.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = duration.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void IcalDuration_Parse_RfcExample_HasNominalAndExactParts()
        {
            // act
            var testResult = IcalDuration.Parse(Rfc5545Examples.Duration.AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.IsNegative.Should().BeFalse();
            testResult.IsWeeks.Should().BeFalse();
            testResult.Days.Should().Be(15);
            testResult.Time.Should().Be(new TimeSpan(5, 0, 20));
        }

        [Fact]
        public void IcalDuration_Parse_OneDay_IsOneNominalDay()
        {
            // act
            var testResult = IcalDuration.Parse(OneDay.AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.Days.Should().Be(1);
            testResult.Time.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void IcalDuration_Parse_TwentyFourHours_IsExactTime()
        {
            // act
            var testResult = IcalDuration.Parse(TwentyFourHours.AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.Days.Should().Be(0);
            testResult.Time.Should().Be(TimeSpan.FromHours(24));
        }

        [Fact]
        public void IcalDuration_Equals_OneDayAnd24Hours_IsFalse()
        {
            // arrange
            var day = IcalDuration.Parse(OneDay.AsSpan(), IcalValueFormat.Text);
            var hours = IcalDuration.Parse(TwentyFourHours.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = day.Equals(hours);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalDuration_Parse_Weeks_AreNominalWeeks()
        {
            // act
            var testResult = IcalDuration.Parse(MinusSevenWeeks.AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.IsWeeks.Should().BeTrue();
            testResult.Weeks.Should().Be(-7);
            testResult.Days.Should().Be(0);
        }

        [Fact]
        public void IcalDuration_Equals_OneWeekAndSevenDays_IsFalse()
        {
            // arrange
            var week = IcalDuration.FromWeeks(1);
            var days = new IcalDuration(7, TimeSpan.Zero);

            // act
            var testResult = week.Equals(days);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("")]
        [InlineData("P")]
        [InlineData("PT")]
        [InlineData("1D")]
        [InlineData("P1")]
        [InlineData("P1DT")]
        [InlineData("P1W2D")]
        [InlineData("P1WT1H")]
        [InlineData("PT1H1S")]
        [InlineData("PT1M1H")]
        [InlineData("PT1S1M")]
        [InlineData("PT1H1H")]
        [InlineData("P1H")]
        [InlineData("PT1D")]
        [InlineData("P1D2D")]
        [InlineData("P-1D")]
        [InlineData("--P1D")]
        [InlineData("P1.5D")]
        [InlineData("P1Y")]
        [InlineData("PT5M ")]
        [InlineData("P99999999999D")]
        [InlineData("PT99999999999999999H")]
        public void IcalDuration_TryParse_MalformedValue_Fails(string value)
        {
            // act
            var testResult = IcalDuration.TryParse(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalDuration_Constructor_MixedSigns_Throws()
        {
            // act
            Action testAction = () => new IcalDuration(1, TimeSpan.FromHours(-1));

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void IcalDuration_Constructor_Negative_IsNegative()
        {
            // act
            var testResult = new IcalDuration(-2, TimeSpan.FromMinutes(-90));

            // assert
            testResult.IsNegative.Should().BeTrue();
            testResult.Days.Should().Be(-2);
            testResult.Time.Should().Be(TimeSpan.FromMinutes(-90));
        }

        [Fact]
        public void IcalDuration_ToString_Negative_IsWrittenWithSign()
        {
            // arrange
            var duration = new IcalDuration(-2, TimeSpan.FromMinutes(-90));

            // act
            var testResult = duration.ToString();

            // assert
            testResult.Should().Be("-P2DT1H30M");
        }

        [Fact]
        public void IcalDuration_Negate_Negative_IsPositive()
        {
            // arrange
            var duration = new IcalDuration(-2, TimeSpan.FromMinutes(-90));

            // act
            var testResult = duration.Negate();

            // assert
            testResult.Should().Be(new IcalDuration(2, TimeSpan.FromMinutes(90)));
        }

        [Fact]
        public void IcalDuration_Constructor_FractionalSeconds_Throws()
        {
            // act
            Action testAction = () => new IcalDuration(0, TimeSpan.FromMilliseconds(1500));

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        #endregion

        #region IcalPeriod

        [Theory]
        [MemberData(nameof(RfcExamplePeriods))]
        public void IcalPeriod_TryParse_RfcExample_Succeeds(string value)
        {
            // act
            var testResult = IcalPeriod.TryParse(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(RfcExamplePeriods))]
        public void IcalPeriod_ToString_ParsedRfcExample_IsWrittenExactly(string value)
        {
            // arrange
            var period = IcalPeriod.Parse(value.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = period.ToString();

            // assert
            testResult.Should().Be(value);
        }

        [Fact]
        public void IcalPeriod_Parse_Explicit_HasEndAndNoDuration()
        {
            // act
            var testResult = IcalPeriod.Parse(Rfc5545Examples.ExplicitPeriod.AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.Start.Should().Be(new IcalDateTime(new IcalDate(1997, 1, 1), new IcalTime(18, 0, 0, true)));
            testResult.End.Should().Be(new IcalDateTime(new IcalDate(1997, 1, 2), new IcalTime(7, 0, 0, true)));
            testResult.Duration.Should().BeNull();
        }

        [Fact]
        public void IcalPeriod_Parse_Start_HasDurationAndNoEnd()
        {
            // act
            var testResult = IcalPeriod.Parse(Rfc5545Examples.StartPeriod.AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.End.Should().BeNull();
            testResult.Duration.Should().Be(new IcalDuration(0, new TimeSpan(5, 30, 0)));
        }

        [Fact]
        public void IcalPeriod_Parse_WithTimeZoneId_ZonesStartAndEnd()
        {
            // act
            var testResult = IcalPeriod.Parse("19970101T180000/19970102T070000".AsSpan(), LondonTimeZoneId, IcalValueFormat.Text);

            // assert
            testResult.Start.TimeZoneId.Should().Be(LondonTimeZoneId);
            testResult.End.Value.TimeZoneId.Should().Be(LondonTimeZoneId);
        }

        [Theory]
        [InlineData(Rfc5545Examples.PeriodStart)]
        [InlineData("19970101T180000Z/")]
        [InlineData("/19970102T070000Z")]
        [InlineData("19970101T180000Z/19970101T180000Z")]
        [InlineData("19970102T070000Z/19970101T180000Z")]
        [InlineData("19970101T180000Z/-PT1H")]
        [InlineData("19970101T180000Z/PT0S")]
        [InlineData("19970101T180000Z/P0W")]
        [InlineData("19970101/19970102")]
        [InlineData("19970101T180000Z/PT1H/PT1H")]
        public void IcalPeriod_TryParse_InvalidValue_Fails(string value)
        {
            // act
            var testResult = IcalPeriod.TryParse(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalPeriod_TryParse_UtcWithTimeZoneId_Fails()
        {
            // act
            var testResult = IcalPeriod.TryParse("19970101T180000Z/PT1H".AsSpan(), LondonTimeZoneId, IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalPeriod_Constructor_EndNotAfterStart_Throws()
        {
            // arrange
            var start = new IcalDateTime(new IcalDate(1997, 1, 1), new IcalTime(18, 0, 0, true));

            // act
            Action testAction = () => new IcalPeriod(start, start);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("end");
        }

        [Fact]
        public void IcalPeriod_Constructor_NegativeDuration_Throws()
        {
            // arrange
            var start = new IcalDateTime(new IcalDate(1997, 1, 1), new IcalTime(18, 0, 0, true));
            var duration = new IcalDuration(0, TimeSpan.FromHours(-1));

            // act
            Action testAction = () => new IcalPeriod(start, duration);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("duration");
        }

        #endregion
    }
}
