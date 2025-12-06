using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class DateTimeExtensionsTests
{
    [Theory]
    [InlineData(1980, 1, 1, 0, 0, 0, 0x210000),
     InlineData(1980, 1, 1, 23, 59, 58, 0x21BF7D),
     InlineData(1984, 2, 29, 12, 30, 0, 0x85D63C0),
     InlineData(1999, 12, 31, 6, 15, 30, 0x279F31EF),
     InlineData(2000, 1, 1, 15, 45, 10, 0x28217DA5),
     InlineData(2025, 11, 23, 8, 5, 2, 0x5B7740A1),
     InlineData(2107, 12, 31, 23, 59, 58, 0xFF9FBF7D)]
    public void ToDosDateTime_OnDateTimeValue_ReturnsDosDateTimeEquivalent(ushort testYear, ushort testMonth, ushort testDay, ushort testHour, ushort testMinute, ushort testSecond, uint expectedResult)
    {
        // arrange
        var testDateTime = new DateTime(testYear, testMonth, testDay, testHour, testMinute, testSecond);

        // act
        var testResult = testDateTime.ToDosDateTime();

        // assert
        ((uint)testResult).Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(2000, 1, 1, 15, 0, 0, 946738800L)]
    public void ToUnixTime_OnDateTimeValue_ReturnsUnixTimeEquivalent(ushort testYear, ushort testMonth, ushort testDay, ushort testHour, ushort testMinute, ushort testSecond, long expectedResult)
    {
        // arrange
        var testDateTime = new DateTime(testYear, testMonth, testDay, testHour, testMinute, testSecond);

        // act
        var testResult = testDateTime.ToUnixTime();

        // assert
        ((long)testResult).Should().Be(expectedResult);
    }
}