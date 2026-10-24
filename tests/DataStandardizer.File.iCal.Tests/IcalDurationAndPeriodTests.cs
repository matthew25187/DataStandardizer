using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalDurationAndPeriodTests
    {
        #region IcalDuration

        [Theory]
        [InlineData("P15DT5H0M20S")]
        [InlineData("P7W")]
        [InlineData("PT15M")]
        [InlineData("-PT15M")]
        [InlineData("P1D")]
        [InlineData("PT24H")]
        [InlineData("PT1H")]
        [InlineData("PT1H30M")]
        [InlineData("PT5S")]
        [InlineData("PT1M5S")]
        [InlineData("P2DT1H")]
        [InlineData("-P1W")]
        [InlineData("P0W")]
        [InlineData("PT0S")]
        public void IcalDuration_TryParse_CanonicalValue_RoundTripsExactly(string value)
        {
            // act
            var testResult = IcalDuration.TryParse(value.AsSpan(), IcalValueFormat.Text, out var duration);

            // assert
            testResult.Should().BeTrue();
            duration.ToString().Should().Be(value);
        }

        [Theory]
        [InlineData("+P1D", "P1D")]
        [InlineData("PT90M", "PT1H30M")]
        [InlineData("PT0H0M5S", "PT5S")]
        [InlineData("PT1H0M0S", "PT1H")]
        [InlineData("P0D", "PT0S")]
        [InlineData("-PT0S", "PT0S")]
        [InlineData("p1dt2h", "P1DT2H")]
        public void IcalDuration_TryParse_NonCanonicalValue_IsWrittenCanonically(string value, string expected)
        {
            // act
            var testResult = IcalDuration.Parse(value.AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.ToString().Should().Be(expected);
            IcalDuration.Parse(testResult.ToString().AsSpan(), IcalValueFormat.Text).Should().Be(testResult);
        }

        [Fact]
        public void IcalDuration_RfcExample_HasNominalAndExactParts()
        {
            // act
            var testResult = IcalDuration.Parse("P15DT5H0M20S".AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.IsNegative.Should().BeFalse();
            testResult.IsWeeks.Should().BeFalse();
            testResult.Days.Should().Be(15);
            testResult.Time.Should().Be(new TimeSpan(5, 0, 20));
        }

        [Fact]
        public void IcalDuration_OneDayAnd24Hours_AreDistinct()
        {
            // act
            var day = IcalDuration.Parse("P1D".AsSpan(), IcalValueFormat.Text);
            var hours = IcalDuration.Parse("PT24H".AsSpan(), IcalValueFormat.Text);

            // assert
            day.Should().NotBe(hours);
            day.Days.Should().Be(1);
            day.Time.Should().Be(TimeSpan.Zero);
            hours.Days.Should().Be(0);
            hours.Time.Should().Be(TimeSpan.FromHours(24));
        }

        [Fact]
        public void IcalDuration_Weeks_AreNominalAndDistinctFromDays()
        {
            // act
            var weeks = IcalDuration.Parse("-P7W".AsSpan(), IcalValueFormat.Text);

            // assert
            weeks.IsWeeks.Should().BeTrue();
            weeks.Weeks.Should().Be(-7);
            weeks.Days.Should().Be(0);
            IcalDuration.FromWeeks(1).Should().NotBe(new IcalDuration(7, TimeSpan.Zero));
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
            Action act = () => new IcalDuration(1, TimeSpan.FromHours(-1));

            // assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void IcalDuration_Constructor_Negative_IsWrittenWithSign()
        {
            // act
            var testResult = new IcalDuration(-2, TimeSpan.FromMinutes(-90));

            // assert
            testResult.IsNegative.Should().BeTrue();
            testResult.Days.Should().Be(-2);
            testResult.Time.Should().Be(TimeSpan.FromMinutes(-90));
            testResult.ToString().Should().Be("-P2DT1H30M");
            testResult.Negate().ToString().Should().Be("P2DT1H30M");
        }

        [Fact]
        public void IcalDuration_Constructor_FractionalSeconds_Throws()
        {
            // act
            Action act = () => new IcalDuration(0, TimeSpan.FromMilliseconds(1500));

            // assert
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region IcalPeriod

        [Theory]
        [InlineData("19970101T180000Z/19970102T070000Z")]
        [InlineData("19970101T180000Z/PT5H30M")]
        public void IcalPeriod_TryParse_RfcExample_ParsesAndRoundTrips(string value)
        {
            // act
            var testResult = IcalPeriod.TryParse(value.AsSpan(), IcalValueFormat.Text, out var period);

            // assert
            testResult.Should().BeTrue();
            period.ToString().Should().Be(value);
            IcalPeriod.Parse(period.ToString().AsSpan(), IcalValueFormat.Text).Should().Be(period);
        }

        [Fact]
        public void IcalPeriod_Explicit_HasEndAndNoDuration()
        {
            // act
            var testResult = IcalPeriod.Parse("19970101T180000Z/19970102T070000Z".AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.Start.Should().Be(IcalDateTime.Parse("19970101T180000Z".AsSpan(), IcalValueFormat.Text));
            testResult.End.Should().Be(IcalDateTime.Parse("19970102T070000Z".AsSpan(), IcalValueFormat.Text));
            testResult.Duration.Should().BeNull();
        }

        [Fact]
        public void IcalPeriod_Start_HasDurationAndNoEnd()
        {
            // act
            var testResult = IcalPeriod.Parse("19970101T180000Z/PT5H30M".AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.End.Should().BeNull();
            testResult.Duration.Should().Be(new IcalDuration(0, new TimeSpan(5, 30, 0)));
        }

        [Fact]
        public void IcalPeriod_TryParse_WithTimeZoneId_ZonesStartAndEnd()
        {
            // act
            var testResult = IcalPeriod.Parse("19970101T180000/19970102T070000".AsSpan(), "Europe/London", IcalValueFormat.Text);

            // assert
            testResult.Start.TimeZoneId.Should().Be("Europe/London");
            testResult.End.Value.TimeZoneId.Should().Be("Europe/London");
        }

        [Theory]
        [InlineData("19970101T180000Z")]
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
            var testResult = IcalPeriod.TryParse("19970101T180000Z/PT1H".AsSpan(), "Europe/London", IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IcalPeriod_Constructor_EndNotAfterStart_Throws()
        {
            // arrange
            var start = IcalDateTime.Parse("19970101T180000Z".AsSpan(), IcalValueFormat.Text);

            // act
            Action act = () => new IcalPeriod(start, start);

            // assert
            act.Should().Throw<ArgumentException>().WithParameterName("end");
        }

        [Fact]
        public void IcalPeriod_Constructor_NegativeDuration_Throws()
        {
            // arrange
            var start = IcalDateTime.Parse("19970101T180000Z".AsSpan(), IcalValueFormat.Text);

            // act
            Action act = () => new IcalPeriod(start, IcalDuration.Parse("-PT1H".AsSpan(), IcalValueFormat.Text));

            // assert
            act.Should().Throw<ArgumentException>().WithParameterName("duration");
        }

        #endregion
    }
}
