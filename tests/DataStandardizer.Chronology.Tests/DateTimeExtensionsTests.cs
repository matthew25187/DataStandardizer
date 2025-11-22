using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class DateTimeExtensionsTests
{
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