using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

/// <remarks>
/// The expected offsets are those reported by <c>zdump -v</c> for TZ Database 2026e, compiled by zic in its main format.
/// </remarks>
public class TzDataExtensionsLocalTimeTests
{
    private static readonly TzDataTimezone UnknownTimezone = (TzDataTimezone)"Nowhere/Nothing";

    private static DateTime ParseLocal(string value) => DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseDateTimeOffset(string value) => DateTimeOffset.ParseExact(value, "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);

    private static TimeSpan ParseOffset(string value) => TimeSpan.Parse(value, CultureInfo.InvariantCulture);

    public static IEnumerable<object[]> SingleOffset_TestCases
    {
        get
        {
            yield return new object[] { "America/New_York", "2026-01-15T12:00:00", "-05:00", "EST" };
            yield return new object[] { "America/New_York", "2026-07-15T12:00:00", "-04:00", "EDT" };
            // America/New_York: either side of the gap, and either side of the overlap.
            yield return new object[] { "America/New_York", "2026-03-08T01:59:59", "-05:00", "EST" };
            yield return new object[] { "America/New_York", "2026-03-08T03:00:00", "-04:00", "EDT" };
            yield return new object[] { "America/New_York", "2026-11-01T00:59:59", "-04:00", "EDT" };
            yield return new object[] { "America/New_York", "2026-11-01T02:00:00", "-05:00", "EST" };
            // Europe/Dublin: negative daylight saving, so that summer time is standard time.
            yield return new object[] { "Europe/Dublin", "2026-01-15T12:00:00", "00:00", "GMT" };
            yield return new object[] { "Europe/Dublin", "2026-07-15T12:00:00", "01:00", "IST" };
            // Australia/Lord_Howe: 30 minutes of daylight saving.
            yield return new object[] { "Australia/Lord_Howe", "2026-01-15T12:00:00", "11:00", "+11" };
            yield return new object[] { "Australia/Lord_Howe", "2026-07-15T12:00:00", "10:30", "+1030" };
            // Asia/Kolkata: no daylight saving.
            yield return new object[] { "Asia/Kolkata", "2026-01-15T12:00:00", "05:30", "IST" };
            yield return new object[] { "Asia/Kolkata", "2026-07-15T12:00:00", "05:30", "IST" };
            // America/New_York: beyond the last year of explicit data, from the rules that apply indefinitely.
            yield return new object[] { "America/New_York", "2149-07-15T12:00:00", "-04:00", "EDT" };
        }
    }

    [Theory]
    [MemberData(nameof(SingleOffset_TestCases))]
    public void GetOffsetInfosForLocal_NormalTime_ReturnsOneOffset(string testIdentifier, string testLocal, string expectedOffset, string expectedAbbreviation)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var testLocalTime = ParseLocal(testLocal);

        // act
        var testResult = testTimezone.GetOffsetInfosForLocal(testLocalTime);

        // assert
        var testOffsetInfo = testResult.Should().ContainSingle().Which;
        testOffsetInfo.UtcOffset.Should().Be(ParseOffset(expectedOffset));
        testOffsetInfo.Abbreviation.Should().Be(expectedAbbreviation);
        testOffsetInfo.Should().Be(testTimezone.GetOffsetInfo(DateTime.SpecifyKind(testLocalTime - testOffsetInfo.UtcOffset, DateTimeKind.Utc)));
    }

    public static IEnumerable<object[]> SingleOffsetTime_TestCases => SingleOffset_TestCases.Select(testCase => new[] { testCase[0], testCase[1], testCase[2] });

    public static IEnumerable<object[]> Gap_TestCases
    {
        get
        {
            // The time, and the time to which the RFC 5545 policy moves it, with the offset in effect after the gap.
            yield return new object[] { "America/New_York", "2026-03-08T02:30:00", "2026-03-08T03:30:00-04:00" };
            yield return new object[] { "America/New_York", "2026-03-08T02:00:00", "2026-03-08T03:00:00-04:00" };
            yield return new object[] { "America/New_York", "2026-03-08T02:59:59", "2026-03-08T03:59:59-04:00" };
            yield return new object[] { "Europe/London", "2026-03-29T01:30:00", "2026-03-29T02:30:00+01:00" };
            yield return new object[] { "Europe/Dublin", "2026-03-29T01:30:00", "2026-03-29T02:30:00+01:00" };
            yield return new object[] { "Australia/Lord_Howe", "2026-10-04T02:15:00", "2026-10-04T02:45:00+11:00" };
            // Pacific/Apia: the move across the date line, which skipped December 30, 2011.
            yield return new object[] { "Pacific/Apia", "2011-12-30T12:00:00", "2011-12-31T12:00:00+14:00" };
            yield return new object[] { "America/New_York", "2149-03-09T02:30:00", "2149-03-09T03:30:00-04:00" };
        }
    }

    public static IEnumerable<object[]> GapTime_TestCases => Gap_TestCases.Select(testCase => new[] { testCase[0], testCase[1] });

    [Theory]
    [MemberData(nameof(GapTime_TestCases))]
    public void GetOffsetInfosForLocal_TimeInGap_ReturnsNoOffsets(string testIdentifier, string testLocal)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;

        // act
        var testResult = testTimezone.GetOffsetInfosForLocal(ParseLocal(testLocal));

        // assert
        testResult.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Gap_TestCases))]
    public void ToDateTimeOffset_TimeInGapWithRfc5545_TakesOffsetBeforeGap(string testIdentifier, string testLocal, string expectedResult)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;

        // act
        var testResult = ParseLocal(testLocal).ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Rfc5545);

        // assert
        var expected = ParseDateTimeOffset(expectedResult);
        testResult.DateTime.Should().Be(expected.DateTime);
        testResult.Offset.Should().Be(expected.Offset);
    }

    [Theory]
    [MemberData(nameof(GapTime_TestCases))]
    public void ToDateTimeOffset_TimeInGapWithStrict_ThrowsArgumentException(string testIdentifier, string testLocal)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;

        // act
        var testResult = () => ParseLocal(testLocal).ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Strict);

        // assert
        testResult.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("local");
    }

    public static IEnumerable<object[]> Overlap_TestCases
    {
        get
        {
            // The time, then the earlier and later offsets with their abbreviations and whether daylight saving time is in effect.
            yield return new object[] { "America/New_York", "2026-11-01T01:30:00", "-04:00", "EDT", true, "-05:00", "EST", false };
            yield return new object[] { "America/New_York", "2026-11-01T01:00:00", "-04:00", "EDT", true, "-05:00", "EST", false };
            yield return new object[] { "America/New_York", "2026-11-01T01:59:59", "-04:00", "EDT", true, "-05:00", "EST", false };
            yield return new object[] { "Europe/London", "2026-10-25T01:30:00", "01:00", "BST", true, "00:00", "GMT", false };
            yield return new object[] { "Europe/Dublin", "2026-10-25T01:30:00", "01:00", "IST", false, "00:00", "GMT", true };
            yield return new object[] { "Australia/Lord_Howe", "2026-04-05T01:45:00", "11:00", "+11", true, "10:30", "+1030", false };
            yield return new object[] { "America/New_York", "2149-11-02T01:30:00", "-04:00", "EDT", true, "-05:00", "EST", false };
        }
    }

    [Theory]
    [MemberData(nameof(Overlap_TestCases))]
    public void GetOffsetInfosForLocal_TimeInOverlap_ReturnsBothOffsetsInOrder(string testIdentifier, string testLocal, string expectedFirstOffset, string expectedFirstAbbreviation, bool expectedFirstIsDaylightSavingTime, string expectedSecondOffset, string expectedSecondAbbreviation, bool expectedSecondIsDaylightSavingTime)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;

        // act
        var testResult = testTimezone.GetOffsetInfosForLocal(ParseLocal(testLocal));

        // assert
        testResult.Should().HaveCount(2);
        testResult[0].UtcOffset.Should().Be(ParseOffset(expectedFirstOffset));
        testResult[0].Abbreviation.Should().Be(expectedFirstAbbreviation);
        testResult[0].IsDaylightSavingTime.Should().Be(expectedFirstIsDaylightSavingTime);
        testResult[1].UtcOffset.Should().Be(ParseOffset(expectedSecondOffset));
        testResult[1].Abbreviation.Should().Be(expectedSecondAbbreviation);
        testResult[1].IsDaylightSavingTime.Should().Be(expectedSecondIsDaylightSavingTime);
        testResult[0].ValidUntilUtc.Should().Be(testResult[1].ValidFromUtc);
    }

    public static IEnumerable<object[]> OverlapTime_TestCases => Overlap_TestCases.Select(testCase => new[] { testCase[0], testCase[1] });

    public static IEnumerable<object[]> OverlapEarlierOffset_TestCases => Overlap_TestCases.Select(testCase => new[] { testCase[0], testCase[1], testCase[2] });

    [Theory]
    [MemberData(nameof(OverlapEarlierOffset_TestCases))]
    public void ToDateTimeOffset_TimeInOverlapWithRfc5545_TakesEarlierInstant(string testIdentifier, string testLocal, string expectedFirstOffset)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var testLocalTime = ParseLocal(testLocal);

        // act
        var testResult = testLocalTime.ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Rfc5545);

        // assert
        testResult.DateTime.Should().Be(testLocalTime);
        testResult.Offset.Should().Be(ParseOffset(expectedFirstOffset));
    }

    [Theory]
    [MemberData(nameof(OverlapTime_TestCases))]
    public void ToDateTimeOffset_TimeInOverlapWithStrict_ThrowsArgumentException(string testIdentifier, string testLocal)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;

        // act
        var testResult = () => ParseLocal(testLocal).ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Strict);

        // assert
        testResult.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("local");
    }

    [Fact]
    public void ToDateTimeOffset_NewYorkExamples_GiveExpectedInstants()
    {
        // arrange
        var testTimezone = TzDataTimezone.America.New_York;

        // act
        var testGap = ParseLocal("2026-03-08T02:30:00").ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Rfc5545);
        var testOverlap = ParseLocal("2026-11-01T01:30:00").ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Rfc5545);

        // assert
        testGap.Should().Be(ParseDateTimeOffset("2026-03-08T03:30:00-04:00"));
        testGap.UtcDateTime.Should().Be(new DateTime(2026, 3, 8, 7, 30, 0, DateTimeKind.Utc));
        testOverlap.Should().Be(ParseDateTimeOffset("2026-11-01T01:30:00-04:00"));
        testOverlap.UtcDateTime.Should().Be(new DateTime(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc));
    }

    [Theory]
    [MemberData(nameof(SingleOffsetTime_TestCases))]
    public void ToDateTimeOffset_NormalTime_KeepsTimeWithOffsetInEffect(string testIdentifier, string testLocal, string expectedOffset)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var testLocalTime = ParseLocal(testLocal);

        // act
        var testStrict = testLocalTime.ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Strict);
        var testRfc5545 = testLocalTime.ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Rfc5545);

        // assert
        testStrict.DateTime.Should().Be(testLocalTime);
        testStrict.Offset.Should().Be(ParseOffset(expectedOffset));
        testRfc5545.Should().Be(testStrict);
        testRfc5545.Offset.Should().Be(testStrict.Offset);
    }

    [Theory]
    [InlineData("America/New_York")]
    [InlineData("Europe/London")]
    [InlineData("Europe/Dublin")]
    [InlineData("Australia/Lord_Howe")]
    [InlineData("Asia/Kolkata")]
    public void GetOffsetInfosForLocal_EveryQuarterHourOfYear_ReturnsOffsetsThatGiveTimeBack(string testIdentifier)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var testStart = new DateTime(2026, 1, 1, 0, 0, 0);

        for (var testLocal = testStart; testLocal < testStart.AddYears(1); testLocal = testLocal.AddMinutes(15))
        {
            // act
            var testResult = testTimezone.GetOffsetInfosForLocal(testLocal);

            // assert
            testResult.Count.Should().BeLessThanOrEqualTo(2, "at most one transition falls near {0}", testLocal);
            foreach (var testOffsetInfo in testResult)
            {
                var testUtc = DateTime.SpecifyKind(testLocal - testOffsetInfo.UtcOffset, DateTimeKind.Utc);
                testTimezone.GetOffsetInfo(testUtc).Should().Be(testOffsetInfo, "the offset gives {0} back", testLocal);
            }

            if (testIdentifier == "Asia/Kolkata")
                testResult.Should().ContainSingle("Asia/Kolkata has no daylight saving");
        }
    }

    [Theory]
    [InlineData("America/New_York")]
    [InlineData("Europe/London")]
    [InlineData("Europe/Dublin")]
    [InlineData("Australia/Lord_Howe")]
    [InlineData("Asia/Kolkata")]
    public void GetOffsetInfosForLocal_EveryQuarterHourOfYear_FindsEveryInstant(string testIdentifier)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var testStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var testUtc = testStart; testUtc < testStart.AddYears(1); testUtc = testUtc.AddMinutes(15))
        {
            // act
            var testOffsetInfo = testTimezone.GetOffsetInfo(testUtc);
            var testLocal = DateTime.SpecifyKind(testUtc + testOffsetInfo.UtcOffset, DateTimeKind.Unspecified);
            var testResult = testTimezone.GetOffsetInfosForLocal(testLocal);

            // assert
            testResult.Should().Contain(testOffsetInfo, "the local clock shows {0} at {1}", testLocal, testUtc);
        }
    }

    [Fact]
    public void GetOffsetInfosForLocal_RangeLimits_ReturnOnlyRepresentableInstants()
    {
        // arrange
        var testMinimum = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified);
        var testMaximum = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified);

        // act & assert
        // America/New_York is behind universal time, so the earliest local time has an instant and the latest has none.
        TzDataTimezone.America.New_York.GetOffsetInfosForLocal(testMinimum).Should().ContainSingle();
        TzDataTimezone.America.New_York.GetOffsetInfosForLocal(testMaximum).Should().BeEmpty();
        // Asia/Kolkata is ahead of universal time, so the reverse.
        TzDataTimezone.Asia.Kolkata.GetOffsetInfosForLocal(testMinimum).Should().BeEmpty();
        TzDataTimezone.Asia.Kolkata.GetOffsetInfosForLocal(testMaximum).Should().ContainSingle();
    }

    [Fact]
    public void ToDateTimeOffset_OffsetWithSeconds_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        // America/New_York kept local mean time, -04:56:02, until 1883.
        var testLocal = new DateTime(1880, 1, 1, 12, 0, 0);

        // act
        var testResult = () => testLocal.ToDateTimeOffset(TzDataTimezone.America.New_York, TzDataLocalTimeResolution.Strict);

        // assert
        testResult.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("local");
    }

    [Fact]
    public void ToDateTimeOffset_UndefinedResolution_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var testLocal = new DateTime(2026, 7, 1, 12, 0, 0);

        // act
        var testResult = () => testLocal.ToDateTimeOffset(TzDataTimezone.Europe.London, (TzDataLocalTimeResolution)99);

        // assert
        testResult.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("resolution");
    }

    public static IEnumerable<object[]> LocalTimeMethod_TestCases
    {
        get
        {
            yield return new object[] { nameof(TzDataExtensions.GetOffsetInfosForLocal), new Action<TzDataTimezone, DateTime>((timezone, local) => timezone.GetOffsetInfosForLocal(local)) };
            yield return new object[] { $"{nameof(DateTimeExtensions.ToDateTimeOffset)}({nameof(TzDataLocalTimeResolution.Strict)})", new Action<TzDataTimezone, DateTime>((timezone, local) => local.ToDateTimeOffset(timezone, TzDataLocalTimeResolution.Strict)) };
            yield return new object[] { $"{nameof(DateTimeExtensions.ToDateTimeOffset)}({nameof(TzDataLocalTimeResolution.Rfc5545)})", new Action<TzDataTimezone, DateTime>((timezone, local) => local.ToDateTimeOffset(timezone, TzDataLocalTimeResolution.Rfc5545)) };
        }
    }

    [Theory]
    [MemberData(nameof(LocalTimeMethod_TestCases))]
    public void LocalTimeMethod_UtcOrLocalKind_ThrowsArgumentException(string testMethod, Action<TzDataTimezone, DateTime> testAction)
    {
        // arrange
        var testUtc = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var testLocal = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Local);

        // act
        var testUtcResult = () => testAction(TzDataTimezone.Europe.London, testUtc);
        var testLocalResult = () => testAction(TzDataTimezone.Europe.London, testLocal);

        // assert
        testUtcResult.Should().Throw<ArgumentException>("{0} accepts only times of kind Unspecified", testMethod).Which.ParamName.Should().Be("local");
        testLocalResult.Should().Throw<ArgumentException>("{0} accepts only times of kind Unspecified", testMethod).Which.ParamName.Should().Be("local");
    }

    [Theory]
    [MemberData(nameof(LocalTimeMethod_TestCases))]
    public void LocalTimeMethod_InvalidTimezone_ThrowsInvalidOperationException(string testMethod, Action<TzDataTimezone, DateTime> testAction)
    {
        // arrange
        var testLocal = new DateTime(2026, 7, 1, 12, 0, 0);

        // act
        var testDefault = () => testAction(default, testLocal);
        var testUnknown = () => testAction(UnknownTimezone, testLocal);

        // assert
        testDefault.Should().Throw<InvalidOperationException>("{0} needs a timezone with an identifier", testMethod);
        testUnknown.Should().Throw<InvalidOperationException>("{0} needs a known timezone", testMethod);
    }

    public static IEnumerable<object[]> ToOffset_TestCases
    {
        get
        {
            yield return new object[] { "America/New_York", "2026-03-08T06:59:59+00:00" };
            yield return new object[] { "America/New_York", "2026-03-08T07:00:00+00:00" };
            yield return new object[] { "America/New_York", "2026-11-01T05:30:00+00:00" };
            yield return new object[] { "America/New_York", "2026-11-01T06:30:00+00:00" };
            yield return new object[] { "Europe/London", "2026-07-01T12:00:00+05:30" };
            yield return new object[] { "Europe/Dublin", "2026-01-15T12:00:00-08:00" };
            yield return new object[] { "Australia/Lord_Howe", "2026-10-03T15:45:00+00:00" };
            yield return new object[] { "Asia/Kolkata", "2026-07-01T12:00:00+00:00" };
            yield return new object[] { "Pacific/Apia", "2011-12-30T10:00:00+00:00" };
            yield return new object[] { "America/New_York", "2149-07-15T12:00:00+00:00" };
        }
    }

    [Theory]
    [MemberData(nameof(ToOffset_TestCases))]
    public void ToOffset_Instant_KeepsInstantWithOffsetOfTimezone(string testIdentifier, string testInstant)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var testValue = ParseDateTimeOffset(testInstant);

        // act
        var testResult = testValue.ToOffset(testTimezone);

        // assert
        testResult.UtcDateTime.Should().Be(testValue.UtcDateTime);
        testResult.Offset.Should().Be(testTimezone.GetUtcOffset(testValue));
    }

    [Fact]
    public void ToOffset_InvalidTimezone_ThrowsInvalidOperationException()
    {
        // arrange
        var testInstant = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var testDefault = () => testInstant.ToOffset(default(TzDataTimezone));
        var testUnknown = () => testInstant.ToOffset(UnknownTimezone);

        // assert
        testDefault.Should().Throw<InvalidOperationException>();
        testUnknown.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToOffset_OffsetWithSeconds_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var testInstant = new DateTimeOffset(1880, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var testResult = () => testInstant.ToOffset(TzDataTimezone.America.New_York);

        // assert
        testResult.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("instant");
    }
}
