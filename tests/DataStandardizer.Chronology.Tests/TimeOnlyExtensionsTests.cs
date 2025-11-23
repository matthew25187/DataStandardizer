using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TimeOnlyExtensionsTests
{
    [Theory]
    [InlineData(0, 0, 0, 0x210000),
     InlineData(0, 0, 2, 0x210001),
     InlineData(6, 15, 30, 0x2131EF),
     InlineData(12, 30, 0, 0x2163C0),
     InlineData(15, 45, 10, 0x217DA5),
     InlineData(23, 59, 58, 0x21bf7d)]
    public void ToDosDateTime_OnTimeOnlyValue_ReturnsDosDateTimeEquivalent(ushort testHour, ushort testMinute, ushort testSecond, uint expectedResult)
    {
        // arrange
        var testTime = new TimeOnly(testHour, testMinute, testSecond);

        // act
        var testResult = testTime.ToDosDateTime();

        // assert
        ((uint)testResult).Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(15, 45, 47, 56747)]
    public void ToUnixTime_OnTimeOnlyValue_ReturnsUnixTimeEquivalent(ushort testHour, ushort testMinute, ushort testSecond, long expectedResult)
    {
        // arrange
        var testTime = new TimeOnly(testHour, testMinute, testSecond);

        // act
        var testResult = testTime.ToUnixTime();

        // assert
        ((long)testResult).Should().Be(expectedResult);
    }
}