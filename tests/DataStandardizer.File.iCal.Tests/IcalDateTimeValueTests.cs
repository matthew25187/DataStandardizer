using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

public class IcalDateTimeValueTests
{
    #region IcalDate

    [Theory]
    [InlineData("19970714", 1997, 7, 14)]
    [InlineData("00010101", 1, 1, 1)]
    [InlineData("99991231", 9999, 12, 31)]
    [InlineData("20240229", 2024, 2, 29)]
    public void IcalDate_TryParse_ValidValue_ParsesAndRoundTrips(string value, int year, int month, int day)
    {
        // act
        var testResult = IcalDate.TryParse(value, IcalValueFormat.Text, out var date);

        // assert
        testResult.Should().BeTrue();
        date.Year.Should().Be(year);
        date.Month.Should().Be(month);
        date.Day.Should().Be(day);
        date.ToString().Should().Be(value);
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
        var testResult = IcalDate.TryParse(value, IcalValueFormat.Text, out _);

        // assert
        testResult.Should().BeFalse();
    }

    [Fact]
    public void IcalDate_Constructor_InvalidDay_Throws()
    {
        // act
        var act = () => new IcalDate(2026, 2, 30);

        // assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("day");
    }

    [Fact]
    public void IcalDate_Default_Is00010101()
    {
        // act
        var testResult = default(IcalDate).ToString();

        // assert
        testResult.Should().Be("00010101");
    }

    [Fact]
    public void IcalDate_DayOfWeek_MatchesDateTime()
    {
        // arrange
        var start = new DateTime(2026, 1, 1);

        // act / assert
        for (var offset = 0; offset < 14; offset++)
        {
            var dateTime = start.AddDays(offset);
            IcalDate.FromDateTime(dateTime).DayOfWeek.Should().Be(dateTime.DayOfWeek);
        }
    }

    [Fact]
    public void IcalDate_Comparison_OrdersByDate()
    {
        // arrange
        var earlier = new IcalDate(2026, 1, 31);
        var later = new IcalDate(2026, 2, 1);

        // assert
        (earlier < later).Should().BeTrue();
        (later >= earlier).Should().BeTrue();
        earlier.CompareTo(later).Should().BeNegative();
        earlier.Should().Be(new IcalDate(2026, 1, 31));
    }

    #endregion

    #region IcalTime

    [Theory]
    [InlineData("230000", 23, 0, 0, false)]
    [InlineData("070000Z", 7, 0, 0, true)]
    [InlineData("083000", 8, 30, 0, false)]
    [InlineData("235960Z", 23, 59, 60, true)]
    public void IcalTime_TryParse_ValidValue_ParsesAndRoundTrips(string value, int hour, int minute, int second, bool isUtc)
    {
        // act
        var testResult = IcalTime.TryParse(value, IcalValueFormat.Text, out var time);

        // assert
        testResult.Should().BeTrue();
        time.Should().Be(new IcalTime(hour, minute, second, isUtc));
        time.ToString().Should().Be(value);
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
        var testResult = IcalTime.TryParse(value, IcalValueFormat.Text, out _);

        // assert
        testResult.Should().BeFalse();
    }

    [Fact]
    public void IcalTime_TryParse_LowerCaseZ_IsUtc()
    {
        // act
        var testResult = IcalTime.Parse("070000z", IcalValueFormat.Text);

        // assert
        testResult.IsUtc.Should().BeTrue();
        testResult.ToString().Should().Be("070000Z");
    }

    [Fact]
    public void IcalTime_Equals_FloatingAndUtc_AreDifferent()
    {
        // assert
        new IcalTime(7, 0, 0).Should().NotBe(new IcalTime(7, 0, 0, true));
    }

    #endregion

    #region IcalDateTime

    [Theory]
    [InlineData("19980118T230000", IcalDateTimeKind.Floating)]
    [InlineData("19980119T070000Z", IcalDateTimeKind.Utc)]
    [InlineData("19970714T133000", IcalDateTimeKind.Floating)]
    [InlineData("19970714T173000Z", IcalDateTimeKind.Utc)]
    [InlineData("19970630T235960Z", IcalDateTimeKind.Utc)]
    public void IcalDateTime_TryParse_RfcExample_ParsesAndRoundTrips(string value, IcalDateTimeKind kind)
    {
        // act
        var testResult = IcalDateTime.TryParse(value, IcalValueFormat.Text, out var dateTime);

        // assert
        testResult.Should().BeTrue();
        dateTime.Kind.Should().Be(kind);
        dateTime.TimeZoneId.Should().BeNull();
        dateTime.ToString().Should().Be(value);
        IcalDateTime.Parse(dateTime.ToString(), IcalValueFormat.Text).Should().Be(dateTime);
    }

    [Fact]
    public void IcalDateTime_TryParse_WithTimeZoneId_IsZoned()
    {
        // act
        var testResult = IcalDateTime.TryParse("19980119T020000", "America/New_York", IcalValueFormat.Text, out var dateTime);

        // assert
        testResult.Should().BeTrue();
        dateTime.Kind.Should().Be(IcalDateTimeKind.Zoned);
        dateTime.TimeZoneId.Should().Be("America/New_York");
        dateTime.Date.Should().Be(new IcalDate(1998, 1, 19));
        dateTime.Time.Should().Be(new IcalTime(2, 0, 0));
        dateTime.ToString().Should().Be("19980119T020000");
    }

    [Fact]
    public void IcalDateTime_TryParse_UtcWithTimeZoneId_Fails()
    {
        // act
        var testResult = IcalDateTime.TryParse("19980119T070000Z", "America/New_York", IcalValueFormat.Text, out _);

        // assert
        testResult.Should().BeFalse();
    }

    [Fact]
    public void IcalDateTime_TryParse_NullTimeZoneId_IsFloating()
    {
        // act
        var testResult = IcalDateTime.Parse("19980119T020000", null, IcalValueFormat.Text);

        // assert
        testResult.Kind.Should().Be(IcalDateTimeKind.Floating);
    }

    [Fact]
    public void IcalDateTime_TryParse_EmptyTimeZoneId_Throws()
    {
        // act
        var act = () => IcalDateTime.TryParse("19980119T020000", string.Empty, IcalValueFormat.Text, out _);

        // assert
        act.Should().Throw<ArgumentException>().WithParameterName("timeZoneId");
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
        var testResult = IcalDateTime.TryParse(value, IcalValueFormat.Text, out _);

        // assert
        testResult.Should().BeFalse();
    }

    [Fact]
    public void IcalDateTime_Equals_DifferentTimeZoneIds_AreDifferent()
    {
        // arrange
        var date = new IcalDate(2026, 3, 1);
        var time = new IcalTime(9, 0, 0);

        // assert
        new IcalDateTime(date, time, "Europe/London").Should().NotBe(new IcalDateTime(date, time, "Europe/Paris"));
        new IcalDateTime(date, time, "Europe/London").Should().NotBe(new IcalDateTime(date, time));
        new IcalDateTime(date, time, "Europe/London").Should().Be(new IcalDateTime(date, time, "Europe/London"));
    }

    [Fact]
    public void IcalDateTime_Constructor_UtcTimeWithTimeZoneId_Throws()
    {
        // act
        var act = () => new IcalDateTime(new IcalDate(2026, 3, 1), new IcalTime(9, 0, 0, true), "Europe/London");

        // assert
        act.Should().Throw<ArgumentException>().WithParameterName("time");
    }

    [Fact]
    public void IcalDateTime_FromDateTime_KeepsKindAndDropsFraction()
    {
        // arrange
        var utc = new DateTime(2026, 3, 1, 9, 30, 15, 500, DateTimeKind.Utc);
        var local = new DateTime(2026, 3, 1, 9, 30, 15, DateTimeKind.Local);

        // act
        var utcResult = IcalDateTime.FromDateTime(utc);
        var localResult = IcalDateTime.FromDateTime(local);
        var zonedResult = IcalDateTime.FromDateTime(local, "Pacific/Auckland");

        // assert
        utcResult.ToString().Should().Be("20260301T093015Z");
        utcResult.ToDateTime().Should().Be(new DateTime(2026, 3, 1, 9, 30, 15, DateTimeKind.Utc));
        utcResult.ToDateTime().Kind.Should().Be(DateTimeKind.Utc);
        localResult.Kind.Should().Be(IcalDateTimeKind.Floating);
        localResult.ToDateTime().Kind.Should().Be(DateTimeKind.Unspecified);
        zonedResult.Kind.Should().Be(IcalDateTimeKind.Zoned);
        zonedResult.TimeZoneId.Should().Be("Pacific/Auckland");
    }

    [Fact]
    public void IcalDateTime_ToDateTime_LeapSecond_Throws()
    {
        // arrange
        var leapSecond = IcalDateTime.Parse("19970630T235960Z", IcalValueFormat.Text);

        // act
        var act = () => leapSecond.ToDateTime();

        // assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IcalDateTime_TryFormat_DestinationTooShort_ReturnsFalse()
    {
        // arrange
        var dateTime = IcalDateTime.Parse("19980119T070000Z", IcalValueFormat.Text);
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
        var dateTime = IcalDateTime.Parse("19980119T070000Z", IcalValueFormat.Text);
        var destination = new char[16];

        // act
        var testResult = dateTime.TryFormat(destination, out var charsWritten, IcalValueFormat.Text);

        // assert
        testResult.Should().BeTrue();
        new string(destination, 0, charsWritten).Should().Be("19980119T070000Z");
    }

    #endregion

    #region IcalUtcOffset

    [Theory]
    [InlineData("-0500", -5 * 3600)]
    [InlineData("+0100", 3600)]
    [InlineData("+0000", 0)]
    [InlineData("+1345", 13 * 3600 + 45 * 60)]
    [InlineData("-000001", -1)]
    [InlineData("+023045", 2 * 3600 + 30 * 60 + 45)]
    public void IcalUtcOffset_TryParse_ValidValue_ParsesAndRoundTrips(string value, int totalSeconds)
    {
        // act
        var testResult = IcalUtcOffset.TryParse(value, IcalValueFormat.Text, out var offset);

        // assert
        testResult.Should().BeTrue();
        offset.Offset.Should().Be(TimeSpan.FromSeconds(totalSeconds));
        offset.ToString().Should().Be(value);
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
        var testResult = IcalUtcOffset.TryParse(value, IcalValueFormat.Text, out _);

        // assert
        testResult.Should().BeFalse();
    }

    [Fact]
    public void IcalUtcOffset_TryParse_ZeroSeconds_IsWrittenWithoutSeconds()
    {
        // act
        var testResult = IcalUtcOffset.Parse("+010000", IcalValueFormat.Text).ToString();

        // assert
        testResult.Should().Be("+0100");
    }

    [Fact]
    public void IcalUtcOffset_Constructor_OutOfRange_Throws()
    {
        // act
        var tooLarge = () => new IcalUtcOffset(TimeSpan.FromHours(24));
        var fractional = () => new IcalUtcOffset(TimeSpan.FromMilliseconds(500));

        // assert
        tooLarge.Should().Throw<ArgumentOutOfRangeException>();
        fractional.Should().Throw<ArgumentException>();
    }

    #endregion
}
