using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TimeOnlyExtensionsTests
{
    [Theory]
    [InlineData(0,0,0,0)]
    public void ToDosDateTime_OnTimeOnlyValue_ReturnsDosDateTimeEquivalent(ushort testHour, ushort testMinute, ushort testSecond, uint expectedResult)
    {// arrange
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