using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class SystemTimeExtensionsTests
{
    // Unix time 0 is the Unix epoch, 1970-01-01T00:00:00Z.
    private const long UnixEpoch = 0L;
    private const long OneSecondBeforeEpoch = -1L;
    private const long UnixTime20231114T221320Z = 1700000000L;

    // The first and last seconds that DateTime and DateOnly can hold, 0001-01-01T00:00:00Z and 9999-12-31T23:59:59Z, and the
    // seconds just outside them.
    private const long FirstSecondOfYear1 = -62135596800L;
    private const long LastSecondBeforeYear1 = -62135596801L;
    private const long LastSecondOfYear9999 = 253402300799L;
    private const long FirstSecondOfYear10000 = 253402300800L;

    // Less than half a nanosecond before the midnight that starts 1970-01-02 (JDN 2440588.5), within the rounding tolerance.
    private const decimal JustBeforeMidnight19700102 = 2440588.4999999999999999m;

    // DOS date 0x0021 is 1980-01-01; DOS time 0x0001 is two seconds past midnight, as a DOS time counts seconds in units of two.
    private const uint DosDateTime19800101T000002 = 0x00210001U;

    [Theory]
    [InlineData(2451545.125, 2000, 1, 1, 15, 0, 0),
     InlineData(2451544.625, 2000,1,1,3, 0, 0)]
    public void ToDateTime_OnSystemTimeValue_ReturnsDateTimeEquivalent(decimal testJdn, ushort expectedYear, ushort expectedMonth, ushort expectedDay, ushort expectedHour, ushort expectedMinute, ushort expectedSecond)
    {
        // arrange
        var expectedResult = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond);
        var testValue = new TestSystemTime(testJdn);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(2451545.125, 2000, 1, 1)]
    public void ToDateOnly_OnSystemTimeValue_ReturnsDateEquivalent(decimal testJdn, ushort expectedYear, ushort expectedMonth, ushort expectedDay)
    {
        // arrange
        var expectedResult = new DateOnly(expectedYear, expectedMonth, expectedDay);
        var testValue = new TestSystemTime(testJdn);

        // act
        var testResult = testValue.ToDateOnly();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(2451545.125, 15, 0, 0),
     InlineData(2451544.625,3,0,0)]
    public void ToTimeOnly_OnSystemTimeValue_ReturnsTimeEquivalent(decimal testJdn, ushort expectedHour, ushort expectedMinute, ushort expectedSecond)
    {
        // arrange
        var expectedResult = new TimeOnly(expectedHour, expectedMinute, expectedSecond);
        var testValue = new TestSystemTime(testJdn);

        // act
        var testResult = testValue.ToTimeOnly();

        // assert
        testResult.Should().Be(expectedResult);
    }

    // Julian Day Numbers a fraction of a second after noon on 2000-01-01 (JDN 2451545), and what each gives.
    public static TheoryData<decimal, DateTime> FractionOfSecondCases => new()
    {
        // Half a second after noon.
        { 2451545.0000057870370370370370m, new DateTime(2000, 1, 1, 12, 0, 0) },
        // Two nanoseconds before 12:00:01, outside the tolerance of half a nanosecond.
        { 2451545.0000115740740509259259m, new DateTime(2000, 1, 1, 12, 0, 0) },
        // 0.4 nanoseconds before 12:00:01, within the tolerance of half a nanosecond.
        { 2451545.0000115740740694444444m, new DateTime(2000, 1, 1, 12, 0, 1) },
        // Within the tolerance of the next midnight, so on the next day.
        { JustBeforeMidnight19700102, new DateTime(1970, 1, 2, 0, 0, 0) },
    };

    // Julian Day Numbers and the time of day each gives. A whole number is noon.
    public static TheoryData<decimal, TimeOnly> TimeOfDayCases => new()
    {
        { JustBeforeMidnight19700102, new TimeOnly(0, 0, 0) },
        { decimal.MaxValue, new TimeOnly(12, 0, 0) },
        { decimal.MinValue, new TimeOnly(12, 0, 0) },
    };

    [Theory]
    [MemberData(nameof(TimeOfDayCases))]
    public void ToTimeOnly_OnSystemTimeValueAnywhere_ReturnsTimeOfDay(decimal testJdn, TimeOnly expectedResult)
    {
        // arrange
        var testValue = new TestSystemTime(testJdn);

        // act
        var testResult = testValue.ToTimeOnly();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(FractionOfSecondCases))]
    public void ToDateTime_OnSystemTimeValueWithFractionOfSecond_DropsFractionBeyondTolerance(decimal testJdn, DateTime expectedResult)
    {
        // arrange
        var testValue = new TestSystemTime(testJdn);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Should().Be(expectedResult);
    }

    // Each Julian Day Number of these values is inexact, and falls a tiny fraction below the whole second it stands for.
    [Theory]
    [InlineData(-199998L, 1969, 12, 29, 16, 26, 42),
     InlineData(OneSecondBeforeEpoch, 1969, 12, 31, 23, 59, 59),
     InlineData(UnixEpoch, 1970, 1, 1, 0, 0, 0),
     InlineData(2L, 1970, 1, 1, 0, 0, 2),
     InlineData(86399L, 1970, 1, 1, 23, 59, 59),
     InlineData(123456789L, 1973, 11, 29, 21, 33, 9),
     InlineData(946684802L, 2000, 1, 1, 0, 0, 2),
     InlineData(UnixTime20231114T221320Z, 2023, 11, 14, 22, 13, 20),
     InlineData(4102444799L, 2099, 12, 31, 23, 59, 59),
     InlineData(FirstSecondOfYear1, 1, 1, 1, 0, 0, 0),
     InlineData(LastSecondOfYear9999, 9999, 12, 31, 23, 59, 59)]
    public void ToDateTime_OnUnixTimeValue_ReturnsDateTimeEquivalent(long testUnixTime, ushort expectedYear, ushort expectedMonth, ushort expectedDay, ushort expectedHour, ushort expectedMinute, ushort expectedSecond)
    {
        // arrange
        var expectedResult = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond);
        var testValue = new UnixTime(testUnixTime);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(-199998L),
     InlineData(OneSecondBeforeEpoch),
     InlineData(UnixEpoch),
     InlineData(2L),
     InlineData(86399L),
     InlineData(123456789L),
     InlineData(946684802L),
     InlineData(UnixTime20231114T221320Z),
     InlineData(4102444799L)]
    public void ToDateTime_OnUnixTimeValue_RoundTripsThroughToUnixTime(long testUnixTime)
    {
        // arrange
        var testValue = new UnixTime(testUnixTime);

        // act
        var testResult = testValue.ToDateTime().ToUnixTime();

        // assert
        ((long)testResult).Should().Be(testUnixTime);
    }

    [Theory]
    [InlineData(OneSecondBeforeEpoch),
     InlineData(UnixEpoch),
     InlineData(UnixTime20231114T221320Z)]
    public void ToDateTime_OnUnixTimeValue_ReturnsUtcKind(long testUnixTime)
    {
        // arrange
        var testValue = new UnixTime(testUnixTime);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(DosDateTime19800101T000002)]
    public void ToDateTime_OnDosDateTimeValue_ReturnsUnspecifiedKind(uint testDosDateTime)
    {
        // arrange
        var testValue = new DosDateTime(testDosDateTime);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Theory]
    [InlineData(UnixEpoch)]
    public void ToDateTime_OnSystemTimeWithGregorianCalendarWrappingUnixTime_ReturnsUtcKind(long testUnixTime)
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new UnixTime(testUnixTime));

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(DosDateTime19800101T000002)]
    public void ToDateTime_OnSystemTimeWithGregorianCalendarWrappingDosDateTime_ReturnsUnspecifiedKind(uint testDosDateTime)
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new DosDateTime(testDosDateTime));

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    // Each date conversion of a system time outside the range of DateTime and DateOnly, 0001-01-01 to 9999-12-31.
    public static TheoryData<string, Func<object>> BeyondDateRangeConversionCases => new()
    {
        { "ToDateTime long.MaxValue", () => new UnixTime(long.MaxValue).ToDateTime() },
        { "ToDateTime long.MinValue", () => new UnixTime(long.MinValue).ToDateTime() },
        { "ToDateTime year 10000", () => new UnixTime(FirstSecondOfYear10000).ToDateTime() },
        { "ToDateTime year 0", () => new UnixTime(LastSecondBeforeYear1).ToDateTime() },
        { "ToDateTime decimal.MaxValue", () => new TestSystemTime(decimal.MaxValue).ToDateTime() },
        { "ToDateTime decimal.MinValue", () => new TestSystemTime(decimal.MinValue).ToDateTime() },
        { "ToDateOnly long.MaxValue", () => new UnixTime(long.MaxValue).ToDateOnly() },
        { "ToDateOnly long.MinValue", () => new UnixTime(long.MinValue).ToDateOnly() },
        { "ToDateOnly year 10000", () => new UnixTime(FirstSecondOfYear10000).ToDateOnly() },
        { "ToDateOnly year 0", () => new UnixTime(LastSecondBeforeYear1).ToDateOnly() },
        { "ToDateOnly decimal.MaxValue", () => new TestSystemTime(decimal.MaxValue).ToDateOnly() },
        { "ToDateOnly decimal.MinValue", () => new TestSystemTime(decimal.MinValue).ToDateOnly() },
    };

    [Theory]
    [MemberData(nameof(BeyondDateRangeConversionCases))]
    public void DateConversion_SystemTimeBeyondDateRange_ThrowsArgumentOutOfRangeException(string testCaseName, Func<object> testConversion)
    {
        // arrange

        // act
        Action testAction = () => testConversion();

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>(testCaseName).WithParameterName("systemTime");
    }

    [Theory]
    [InlineData(FirstSecondOfYear1, 1, 1, 1),
     InlineData(LastSecondOfYear9999, 9999, 12, 31)]
    public void ToDateOnly_OnUnixTimeValue_ReturnsDateEquivalent(long testUnixTime, ushort expectedYear, ushort expectedMonth, ushort expectedDay)
    {
        // arrange
        var expectedResult = new DateOnly(expectedYear, expectedMonth, expectedDay);
        var testValue = new UnixTime(testUnixTime);

        // act
        var testResult = testValue.ToDateOnly();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(DosDateTime19800101T000002, 1980, 1, 1, 0, 0, 2)]
    public void ToDateTime_OnDosDateTimeValue_ReturnsDateTimeEquivalent(uint testDosDateTime, ushort expectedYear, ushort expectedMonth, ushort expectedDay, ushort expectedHour, ushort expectedMinute, ushort expectedSecond)
    {
        // arrange
        var expectedResult = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond);
        var testValue = new DosDateTime(testDosDateTime);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(-199998L, 16, 26, 42),
     InlineData(2L, 0, 0, 2),
     InlineData(UnixTime20231114T221320Z, 22, 13, 20),
     // long.MaxValue is 55807 seconds past a midnight, and long.MinValue 30592 seconds past one.
     InlineData(long.MaxValue, 15, 30, 7),
     InlineData(long.MinValue, 8, 29, 52)]
    public void ToTimeOnly_OnUnixTimeValue_ReturnsTimeEquivalent(long testUnixTime, ushort expectedHour, ushort expectedMinute, ushort expectedSecond)
    {
        // arrange
        var expectedResult = new TimeOnly(expectedHour, expectedMinute, expectedSecond);
        var testValue = new UnixTime(testUnixTime);

        // act
        var testResult = testValue.ToTimeOnly();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private struct TestSystemTime : ISystemTime
    {
        public TestSystemTime(decimal jdn)
        {
            JulianDayNumber = jdn;
        }

        public decimal JulianDayNumber { get; }
    }
}