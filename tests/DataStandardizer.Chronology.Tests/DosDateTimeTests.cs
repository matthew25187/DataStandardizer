using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class DosDateTimeTests
{
    // Packed DOS date and time values: the date is the high 16 bits (day in bits 0-4, month in bits 5-8, year offset from 1980 in
    // bits 9-15) and the time is the low 16 bits (seconds divided by 2 in bits 0-4, minute in bits 5-10, hour in bits 11-15), as
    // Microsoft's "MS-DOS Date and Time" documentation for DosDateTimeToFileTime describes.
    private const uint PackedZero = 0x00000000;
    private const uint Packed19800100 = 0x00200000;
    private const uint Packed19801301 = 0x01A10000;
    private const uint Packed19840230 = 0x085E0000;
    private const uint Packed19850229 = 0x0A5D0000;
    private const uint Packed19840431 = 0x089F0000;
    private const uint Packed19800101Hour24 = 0x0021C000;
    private const uint Packed19800101Minute60 = 0x00210780;
    private const uint Packed19800101Second60 = 0x0021001E;
    private const uint Packed19800101 = 0x00210000;
    private const uint Packed19800101At235958 = 0x0021BF7D;
    private const uint Packed19840229 = 0x085D0000;
    private const uint Packed20000229 = 0x285D0000;
    private const uint Packed19840430 = 0x089E0000;
    private const uint Packed21071231At235958 = 0xFF9FBF7D;
    private const uint Packed19800101At120000 = 0x00216000;
    private const uint Packed19800101At123654 = 0x0021649B;
    private const uint Packed21071231At120000 = 0xFF9F6000;

    // Julian Day Numbers of the packed values above, worked out by hand from JDN 2451545 at noon on 1 January 2000.
    private const decimal Jdn19800101At000000 = 2444239.5m;
    private const decimal Jdn19800101At120000 = 2444240m;
    private const decimal Jdn19800101At123654 = 2444240.025625m;
    private const decimal Jdn19840229At000000 = 2445759.5m;
    private const decimal Jdn21071231At120000 = 2490990m;

    // The parameter names of the DosDateTime constructors.
    private const string ValueParameterName = "value";
    private const string YearParameterName = "year";
    private const string MonthParameterName = "month";
    private const string DayParameterName = "day";

    public static TheoryData<uint> InvalidPackedValue_TestCases => new()
    {
        PackedZero,
        Packed19800100,
        Packed19801301,
        Packed19840230,
        Packed19850229,
        Packed19840431,
        Packed19800101Hour24,
        Packed19800101Minute60,
        Packed19800101Second60,
    };

    public static TheoryData<uint> ValidPackedValue_TestCases => new()
    {
        Packed19800101,
        Packed19800101At235958,
        Packed19840229,
        Packed20000229,
        Packed19840430,
        Packed21071231At235958,
    };

    public static TheoryData<uint, decimal> JulianDayNumber_TestCases => new()
    {
        { Packed19800101, Jdn19800101At000000 },
        { Packed19800101At120000, Jdn19800101At120000 },
        { Packed19800101At123654, Jdn19800101At123654 },
        { Packed19840229, Jdn19840229At000000 },
        { Packed21071231At120000, Jdn21071231At120000 },
    };

    public static TheoryData<string, Func<object>> DefaultInstanceMember_TestCases => new()
    {
        { nameof(DosDateTime.JulianDayNumber), () => default(DosDateTime).JulianDayNumber },
        { nameof(SystemTimeExtensions.ToDateTime), () => default(DosDateTime).ToDateTime() },
        { nameof(SystemTimeExtensions.ToDateOnly), () => default(DosDateTime).ToDateOnly() },
        { nameof(SystemTimeExtensions.ToTimeOnly), () => default(DosDateTime).ToTimeOnly() },
        { nameof(SystemTimeWithGregorianCalendar.JulianDayNumber), () => new SystemTimeWithGregorianCalendar(default(DosDateTime)).JulianDayNumber },
        { nameof(SystemTimeWithGregorianCalendar.Year), () => new SystemTimeWithGregorianCalendar(default(DosDateTime)).Year },
        { nameof(SystemTimeWithGregorianCalendar.Month), () => new SystemTimeWithGregorianCalendar(default(DosDateTime)).Month },
        { nameof(SystemTimeWithGregorianCalendar.Day), () => new SystemTimeWithGregorianCalendar(default(DosDateTime)).Day },
        { nameof(SystemTimeWithGregorianCalendar.Hour), () => new SystemTimeWithGregorianCalendar(default(DosDateTime)).Hour },
        { nameof(SystemTimeWithGregorianCalendar.Minute), () => new SystemTimeWithGregorianCalendar(default(DosDateTime)).Minute },
        { nameof(SystemTimeWithGregorianCalendar.Second), () => new SystemTimeWithGregorianCalendar(default(DosDateTime)).Second },
    };

    public static TheoryData<ushort, ushort, ushort, uint> ValidDate_TestCases => new()
    {
        { 1984, 2, 29, Packed19840229 },
        { 2000, 2, 29, Packed20000229 },
        { 1984, 4, 30, Packed19840430 },
    };

    [Theory]
    [MemberData(nameof(InvalidPackedValue_TestCases))]
    public void Constructor_InvalidPackedValue_ThrowsArgumentOutOfRangeException(uint testValue)
    {
        // arrange

        // act
        Action testAction = () => new DosDateTime(testValue);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(ValueParameterName);
    }

    [Theory]
    [MemberData(nameof(ValidPackedValue_TestCases))]
    public void Constructor_ValidPackedValue_KeepsPackedValue(uint testValue)
    {
        // arrange

        // act
        var testResult = new DosDateTime(testValue);

        // assert
        ((uint)testResult).Should().Be(testValue);
    }

    [Theory]
    [InlineData(1984, 2, 30, DayParameterName),
     InlineData(1985, 2, 29, DayParameterName),
     InlineData(2100, 2, 29, DayParameterName),
     InlineData(1984, 4, 31, DayParameterName),
     InlineData(1984, 1, 0, DayParameterName),
     InlineData(1984, 1, 32, DayParameterName),
     InlineData(1984, 0, 1, MonthParameterName),
     InlineData(1984, 13, 1, MonthParameterName),
     InlineData(1979, 12, 31, YearParameterName),
     InlineData(2108, 1, 1, YearParameterName)]
    public void Constructor_InvalidDate_ThrowsArgumentOutOfRangeException(ushort testYear, ushort testMonth, ushort testDay, string expectedParameterName)
    {
        // arrange

        // act
        Action testAction = () => new DosDateTime(testYear, testMonth, testDay);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(expectedParameterName);
    }

    [Fact]
    public void Constructor_InvalidDateWithTime_ThrowsArgumentOutOfRangeException()
    {
        // arrange

        // act
        Action testAction = () => new DosDateTime(1984, 2, 31, 0, 0, 0);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(DayParameterName);
    }

    [Theory]
    [MemberData(nameof(ValidDate_TestCases))]
    public void Constructor_LastDayOfMonth_PacksDate(ushort testYear, ushort testMonth, ushort testDay, uint expectedResult)
    {
        // arrange

        // act
        var testResult = new DosDateTime(testYear, testMonth, testDay);

        // assert
        ((uint)testResult).Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(InvalidPackedValue_TestCases))]
    public void ExplicitConversion_InvalidPackedValue_ThrowsInvalidCastException(uint testValue)
    {
        // arrange

        // act
        Action testAction = () => _ = (DosDateTime)testValue;

        // assert
        testAction.Should().Throw<InvalidCastException>();
    }

    [Theory]
    [MemberData(nameof(ValidPackedValue_TestCases))]
    public void ExplicitConversion_ValidPackedValue_KeepsPackedValue(uint testValue)
    {
        // arrange

        // act
        var testResult = (DosDateTime)testValue;

        // assert
        ((uint)testResult).Should().Be(testValue);
    }

    [Theory]
    [MemberData(nameof(JulianDayNumber_TestCases))]
    public void JulianDayNumber_ValidPackedValue_ReturnsJulianDayNumber(uint testValue, decimal expectedResult)
    {
        // arrange
        var testDosDateTime = new DosDateTime(testValue);

        // act
        var testResult = testDosDateTime.JulianDayNumber;

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void ToDateTime_ValidPackedValue_ReturnsDateTime()
    {
        // arrange
        var testDosDateTime = new DosDateTime(Packed19800101At123654);
        var expectedResult = new DateTime(1980, 1, 1, 12, 36, 54);

        // act
        var testResult = testDosDateTime.ToDateTime();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(DefaultInstanceMember_TestCases))]
    public void Member_DefaultInstance_ThrowsInvalidOperationException(string testMemberName, Func<object> testMember)
    {
        // arrange

        // act
        Action testAction = () => testMember();

        // assert
        testAction.Should().Throw<InvalidOperationException>(testMemberName);
    }

    [Fact]
    public void ImplicitConversionToUInt32_DefaultInstance_ReturnsZero()
    {
        // arrange
        var testValue = default(DosDateTime);

        // act
        uint testResult = testValue;

        // assert
        testResult.Should().Be(PackedZero);
    }
}
