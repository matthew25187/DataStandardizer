using System.Globalization;
using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

/// <remarks>
/// The expected transitions are those reported by <c>zdump -v</c> for TZ Database 2026e, compiled by zic in its main format.
/// </remarks>
public class TzDataExtensionsOffsetTests
{
    private static readonly TzDataTimezone UnknownTimezone = (TzDataTimezone)"Nowhere/Nothing";

    private static IEnumerable<TzDataTimezone> GetTimezoneFields(Type hostType)
    {
        var fields = hostType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.FieldType == typeof(TzDataTimezone))
            .Select(field => (TzDataTimezone)field.GetValue(null)!);
        var nestedFields = hostType.GetNestedTypes(BindingFlags.Public)
            .SelectMany(GetTimezoneFields);

        return fields.Concat(nestedFields);
    }

    private static DateTime ParseUtc(string value) => DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    private static TimeSpan ParseOffset(string value) => TimeSpan.Parse(value, CultureInfo.InvariantCulture);

    public static IEnumerable<object[]> KnownTransition_TestCases
    {
        get
        {
            // America/New_York: local mean time with seconds, then the US rules before and after the 2007 change.
            yield return new object[] { "America/New_York", "1883-11-18T17:00:00", "-04:56:02", false, "LMT", "-05:00", false, "EST" };
            yield return new object[] { "America/New_York", "2006-04-02T07:00:00", "-05:00", false, "EST", "-04:00", true, "EDT" };
            yield return new object[] { "America/New_York", "2006-10-29T06:00:00", "-04:00", true, "EDT", "-05:00", false, "EST" };
            yield return new object[] { "America/New_York", "2007-03-11T07:00:00", "-05:00", false, "EST", "-04:00", true, "EDT" };
            yield return new object[] { "America/New_York", "2007-11-04T06:00:00", "-04:00", true, "EDT", "-05:00", false, "EST" };
            // Europe/London: British Standard Time, +1 as standard time all year from 1968 to 1971.
            yield return new object[] { "Europe/London", "1968-02-18T02:00:00", "00:00", false, "GMT", "01:00", true, "BST" };
            yield return new object[] { "Europe/London", "1968-10-26T23:00:00", "01:00", true, "BST", "01:00", false, "BST" };
            yield return new object[] { "Europe/London", "1971-10-31T02:00:00", "01:00", false, "BST", "00:00", false, "GMT" };
            // Europe/Dublin: negative daylight saving, so that summer time is standard time.
            yield return new object[] { "Europe/Dublin", "2025-03-30T01:00:00", "00:00", true, "GMT", "01:00", false, "IST" };
            yield return new object[] { "Europe/Dublin", "2025-10-26T01:00:00", "01:00", false, "IST", "00:00", true, "GMT" };
            // Australia/Lord_Howe: 30 minutes of daylight saving.
            yield return new object[] { "Australia/Lord_Howe", "2025-04-05T15:00:00", "11:00", true, "+11", "10:30", false, "+1030" };
            yield return new object[] { "Australia/Lord_Howe", "2025-10-04T15:30:00", "10:30", false, "+1030", "11:00", true, "+11" };
            // Antarctica/Troll: two hours of daylight saving.
            yield return new object[] { "Antarctica/Troll", "2025-03-30T01:00:00", "00:00", false, "+00", "02:00", true, "+02" };
            yield return new object[] { "Antarctica/Troll", "2025-10-26T01:00:00", "02:00", true, "+02", "00:00", false, "+00" };
            // Africa/Casablanca: negative daylight saving for Ramadan, then permanent +00 from September 2026.
            yield return new object[] { "Africa/Casablanca", "2026-02-15T02:00:00", "01:00", false, "+01", "00:00", true, "+00" };
            yield return new object[] { "Africa/Casablanca", "2026-03-22T02:00:00", "00:00", true, "+00", "01:00", false, "+01" };
            yield return new object[] { "Africa/Casablanca", "2026-09-20T01:00:00", "01:00", false, "+01", "00:00", false, "+00" };
            // Pacific/Apia: the move across the date line, which skipped December 30, 2011.
            yield return new object[] { "Pacific/Apia", "2011-12-30T10:00:00", "-10:00", true, "-10", "14:00", true, "+14" };
            // America/Sao_Paulo: the last transition before daylight saving was abolished in 2019.
            yield return new object[] { "America/Sao_Paulo", "2019-02-17T02:00:00", "-02:00", true, "-02", "-03:00", false, "-03" };
            // Asia/Kolkata: the last transition, after which +05:30 applies all year.
            yield return new object[] { "Asia/Kolkata", "1945-10-14T17:30:00", "06:30", true, "+0630", "05:30", false, "IST" };
            // America/New_York: beyond the last year of explicit data, from the rules that apply indefinitely.
            yield return new object[] { "America/New_York", "2149-03-09T07:00:00", "-05:00", false, "EST", "-04:00", true, "EDT" };
            yield return new object[] { "America/New_York", "2149-11-02T06:00:00", "-04:00", true, "EDT", "-05:00", false, "EST" };
        }
    }

    [Theory]
    [MemberData(nameof(KnownTransition_TestCases))]
    public void GetOffsetInfo_AroundTransition_ReturnsOldOffsetBeforeAndNewOffsetAt(string testIdentifier, string testInstant, string beforeOffset, bool beforeIsDaylightSavingTime, string beforeAbbreviation, string afterOffset, bool afterIsDaylightSavingTime, string afterAbbreviation)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var testInstantUtc = ParseUtc(testInstant);

        // act
        var testBefore = testTimezone.GetOffsetInfo(testInstantUtc.AddTicks(-1));
        var testAt = testTimezone.GetOffsetInfo(testInstantUtc);

        // assert
        testBefore.UtcOffset.Should().Be(ParseOffset(beforeOffset), "{0} is at {1} until {2}", testIdentifier, beforeAbbreviation, testInstant);
        testBefore.IsDaylightSavingTime.Should().Be(beforeIsDaylightSavingTime);
        testBefore.Abbreviation.Should().Be(beforeAbbreviation);
        testBefore.ValidUntilUtc.Should().Be(testInstantUtc, "the old offset ceases at the transition");
        testAt.UtcOffset.Should().Be(ParseOffset(afterOffset), "{0} changes to {1} at {2}", testIdentifier, afterAbbreviation, testInstant);
        testAt.IsDaylightSavingTime.Should().Be(afterIsDaylightSavingTime);
        testAt.Abbreviation.Should().Be(afterAbbreviation);
        testAt.ValidFromUtc.Should().Be(testInstantUtc, "the new offset takes effect at the transition");
    }

    [Theory]
    [MemberData(nameof(KnownTransition_TestCases))]
    public void GetTransitions_PeriodAroundTransition_ReturnsTransition(string testIdentifier, string testInstant, string beforeOffset, bool beforeIsDaylightSavingTime, string beforeAbbreviation, string afterOffset, bool afterIsDaylightSavingTime, string afterAbbreviation)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var testInstantUtc = ParseUtc(testInstant);

        // act
        var testResult = testTimezone.GetTransitions(testInstantUtc.AddDays(-1), testInstantUtc.AddDays(1)).ToList();
        var testStartingAtTransition = testTimezone.GetTransitions(testInstantUtc, testInstantUtc.AddTicks(1)).ToList();
        var testEndingAtTransition = testTimezone.GetTransitions(testInstantUtc.AddDays(-1), testInstantUtc).ToList();

        // assert
        testResult.Should().ContainSingle("{0} has one transition within a day of {1}", testIdentifier, testInstant);
        var transition = testResult[0];
        transition.InstantUtc.Should().Be(testInstantUtc);
        transition.InstantUtc.Kind.Should().Be(DateTimeKind.Utc);
        transition.Before.UtcOffset.Should().Be(ParseOffset(beforeOffset));
        transition.Before.IsDaylightSavingTime.Should().Be(beforeIsDaylightSavingTime);
        transition.Before.Abbreviation.Should().Be(beforeAbbreviation);
        transition.Before.ValidUntilUtc.Should().Be(testInstantUtc);
        transition.After.UtcOffset.Should().Be(ParseOffset(afterOffset));
        transition.After.IsDaylightSavingTime.Should().Be(afterIsDaylightSavingTime);
        transition.After.Abbreviation.Should().Be(afterAbbreviation);
        transition.After.ValidFromUtc.Should().Be(testInstantUtc);
        testStartingAtTransition.Should().Equal(new[] { transition }, "the start of the period is included");
        testEndingAtTransition.Should().BeEmpty("the end of the period is excluded");
    }

    public static IEnumerable<object[]> OffsetComponents_TestCases
    {
        get
        {
            yield return new object[] { "America/New_York", "1880-01-01T00:00:00", "-04:56:02", "00:00", false, "LMT" };
            yield return new object[] { "America/New_York", "2025-01-15T12:00:00", "-05:00", "00:00", false, "EST" };
            yield return new object[] { "America/New_York", "2025-07-01T12:00:00", "-05:00", "01:00", true, "EDT" };
            yield return new object[] { "Europe/London", "1970-01-01T00:00:00", "01:00", "00:00", false, "BST" };
            yield return new object[] { "Europe/London", "2025-07-01T12:00:00", "00:00", "01:00", true, "BST" };
            yield return new object[] { "Europe/Dublin", "2025-01-15T12:00:00", "01:00", "-01:00", true, "GMT" };
            yield return new object[] { "Europe/Dublin", "2025-07-01T12:00:00", "01:00", "00:00", false, "IST" };
            yield return new object[] { "Australia/Lord_Howe", "2025-01-15T12:00:00", "10:30", "00:30", true, "+11" };
            yield return new object[] { "Antarctica/Troll", "2025-07-01T12:00:00", "00:00", "02:00", true, "+02" };
            yield return new object[] { "Africa/Casablanca", "2026-03-01T12:00:00", "01:00", "-01:00", true, "+00" };
            yield return new object[] { "Africa/Casablanca", "2027-03-01T12:00:00", "00:00", "00:00", false, "+00" };
            yield return new object[] { "Pacific/Apia", "2011-12-31T12:00:00", "13:00", "01:00", true, "+14" };
            yield return new object[] { "America/Sao_Paulo", "2025-01-15T12:00:00", "-03:00", "00:00", false, "-03" };
            yield return new object[] { "Asia/Kolkata", "2025-07-01T12:00:00", "05:30", "00:00", false, "IST" };
        }
    }

    [Theory]
    [MemberData(nameof(OffsetComponents_TestCases))]
    public void OffsetMethods_Instant_ReturnOffsetComponents(string testIdentifier, string testInstant, string expectedStandardOffset, string expectedDaylightSavings, bool expectedIsDaylightSavingTime, string expectedAbbreviation)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var testInstantUtc = ParseUtc(testInstant);
        var standardOffset = ParseOffset(expectedStandardOffset);
        var daylightSavings = ParseOffset(expectedDaylightSavings);

        // act
        var testUtcOffset = testTimezone.GetUtcOffset(testInstantUtc);
        var testStandardOffset = testTimezone.GetStandardOffset(testInstantUtc);
        var testDaylightSavings = testTimezone.GetDaylightSavings(testInstantUtc);
        var testIsDaylightSavingTime = testTimezone.IsDaylightSavingTime(testInstantUtc);
        var testOffsetInfo = testTimezone.GetOffsetInfo(testInstantUtc);

        // assert
        testUtcOffset.Should().Be(standardOffset + daylightSavings, "the offset of {0} at {1} is the sum of its standard offset and daylight saving", testIdentifier, testInstant);
        testStandardOffset.Should().Be(standardOffset);
        testDaylightSavings.Should().Be(daylightSavings);
        testIsDaylightSavingTime.Should().Be(expectedIsDaylightSavingTime);
        testOffsetInfo.Abbreviation.Should().Be(expectedAbbreviation);
    }

    [Theory]
    [InlineData("America/Sao_Paulo", "2019-02-17T02:00:01")]
    [InlineData("Asia/Kolkata", "1945-10-14T17:30:01")]
    [InlineData("Africa/Casablanca", "2026-09-20T01:00:01")]
    public void GetTransitions_AfterLastTransition_ReturnsNone(string testIdentifier, string testFrom)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;

        // act
        var testResult = testTimezone.GetTransitions(ParseUtc(testFrom), DateTime.MaxValue);

        // assert
        testResult.Should().BeEmpty("{0} has no transitions after {1}", testIdentifier, testFrom);
        testTimezone.GetOffsetInfo(DateTime.MaxValue).ValidUntilUtc.Should().BeNull("the last offset of {0} applies indefinitely", testIdentifier);
    }

    [Theory]
    [InlineData(2150)]
    [InlineData(2500)]
    [InlineData(5000)]
    [InlineData(9998)]
    public void GetTransitions_YearBeyondExplicitData_ReturnsTransitionsOfIndefiniteRules(int testYear)
    {
        // arrange
        var testTimezone = TzDataTimezone.America.New_York;
        // The US rules from 2007: the second Sunday in March and the first Sunday in November, both at 2:00 wall clock time.
        var expectedStart = TzDataDayRule.Resolve(testYear, 3, TzDataDayKind.WeekdayOnOrAfter, 8, DayOfWeek.Sunday).AddHours(7);
        var expectedEnd = TzDataDayRule.Resolve(testYear, 11, TzDataDayKind.WeekdayOnOrAfter, 1, DayOfWeek.Sunday).AddHours(6);

        // act
        var testResult = testTimezone.GetTransitions(new DateTime(testYear, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(testYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)).ToList();

        // assert
        testResult.Select(transition => transition.InstantUtc).Should().Equal(DateTime.SpecifyKind(expectedStart, DateTimeKind.Utc), DateTime.SpecifyKind(expectedEnd, DateTimeKind.Utc));
        testResult[0].After.Abbreviation.Should().Be("EDT");
        testResult[1].After.Abbreviation.Should().Be("EST");
        testTimezone.GetUtcOffset(new DateTime(testYear, 7, 1, 0, 0, 0, DateTimeKind.Utc)).Should().Be(TimeSpan.FromHours(-4));
    }

    [Fact]
    public void GetTransitions_WholeRange_ChainsOffsetsForEveryTimezone()
    {
        // arrange
        var testTimezones = GetTimezoneFields(typeof(TzDataTimezone)).ToList();

        foreach (var testTimezone in testTimezones)
        {
            // act
            var testResult = testTimezone.GetTransitions(DateTime.MinValue, DateTime.MaxValue).ToList();

            // assert
            var expectedBefore = testTimezone.GetOffsetInfo(DateTime.MinValue);
            foreach (var transition in testResult)
            {
                transition.Before.Should().Be(expectedBefore, "each transition of {0} follows the offset before it", (string)testTimezone!);
                transition.After.ValidFromUtc.Should().Be(transition.InstantUtc);
                expectedBefore = transition.After;
            }

            expectedBefore.ValidUntilUtc.Should().BeNull("the offset after the last transition of {0} applies indefinitely", (string)testTimezone!);
        }
    }

    [Fact]
    public void GetZoneLine_InstantsAroundZoneLineChange_ReturnZoneLineInForce()
    {
        // arrange
        var testTimezone = TzDataTimezone.America.New_York;
        var testChange = ParseUtc("1883-11-18T17:00:00");

        // act
        var testBefore = testTimezone.GetZoneLine(testChange.AddTicks(-1));
        var testAt = testTimezone.GetZoneLine(testChange);

        // assert
        testBefore.Should().BeSameAs(testTimezone.ZoneLines[0], "local mean time applies until 17:00 universal time");
        testAt.Should().BeSameAs(testTimezone.ZoneLines[1], "Eastern Standard Time applies from 17:00 universal time");
    }

    [Fact]
    public void GetZoneLine_DateLineChange_ReturnsZoneLineInForce()
    {
        // arrange
        var testTimezone = TzDataTimezone.Pacific.Apia;
        var testChange = ParseUtc("2011-12-30T10:00:00");

        // act
        var testBefore = testTimezone.GetZoneLine(testChange.AddTicks(-1));
        var testAt = testTimezone.GetZoneLine(testChange);

        // assert
        testBefore.StandardOffset.Should().Be(TimeSpan.FromHours(-11));
        testAt.StandardOffset.Should().Be(TimeSpan.FromHours(13));
        testAt.Should().BeSameAs(testTimezone.ZoneLines[testTimezone.ZoneLines.Count - 1]);
    }

    [Fact]
    public void GetZoneLine_RangeLimits_ReturnFirstAndLastZoneLines()
    {
        // arrange
        var testTimezone = TzDataTimezone.Europe.London;

        // act
        var testFirst = testTimezone.GetZoneLine(DateTime.MinValue);
        var testLast = testTimezone.GetZoneLine(DateTime.MaxValue);

        // assert
        testFirst.Should().BeSameAs(testTimezone.ZoneLines[0]);
        testLast.Should().BeSameAs(testTimezone.ZoneLines[testTimezone.ZoneLines.Count - 1]);
    }

    [Fact]
    public void OffsetMethods_UnspecifiedKind_TreatedAsUniversalTime()
    {
        // arrange
        var testTimezone = TzDataTimezone.America.New_York;
        var testUtc = ParseUtc("2007-03-11T07:00:00");
        var testUnspecified = DateTime.SpecifyKind(testUtc, DateTimeKind.Unspecified);

        // act
        var testResult = testTimezone.GetOffsetInfo(testUnspecified);

        // assert
        testResult.Should().Be(testTimezone.GetOffsetInfo(testUtc));
        testTimezone.GetTransitions(testUnspecified, testUnspecified.AddTicks(1)).Should().ContainSingle();
    }

    [Fact]
    public void OffsetMethods_DateTimeOffset_UseInstant()
    {
        // arrange
        var testTimezone = TzDataTimezone.America.New_York;
        var testUtc = ParseUtc("2007-03-11T07:00:00");
        var testInstant = new DateTimeOffset(testUtc).ToOffset(TimeSpan.FromHours(5.5));

        // act & assert
        testTimezone.GetZoneLine(testInstant).Should().BeSameAs(testTimezone.GetZoneLine(testUtc));
        testTimezone.GetUtcOffset(testInstant).Should().Be(TimeSpan.FromHours(-4));
        testTimezone.GetStandardOffset(testInstant).Should().Be(TimeSpan.FromHours(-5));
        testTimezone.GetDaylightSavings(testInstant).Should().Be(TimeSpan.FromHours(1));
        testTimezone.IsDaylightSavingTime(testInstant).Should().BeTrue();
        testTimezone.GetOffsetInfo(testInstant).Should().Be(testTimezone.GetOffsetInfo(testUtc));
        testTimezone.GetOffsetInfo(testInstant.AddTicks(-1)).Should().Be(testTimezone.GetOffsetInfo(testUtc.AddTicks(-1)));
        testTimezone.GetTransitions(testInstant, testInstant.AddTicks(1)).Should().ContainSingle().Which.InstantUtc.Should().Be(testUtc);
    }

    [Fact]
    public void OffsetMethods_CastInstance_MatchField()
    {
        // arrange
        var testField = TzDataTimezone.Europe.London;
        var testCast = (TzDataTimezone)"Europe/London";
        var testUtc = ParseUtc("1970-01-01T00:00:00");

        // act
        var testResult = testCast.GetOffsetInfo(testUtc);

        // assert
        testResult.Should().Be(testField.GetOffsetInfo(testUtc));
    }

    public static IEnumerable<object[]> OffsetMethod_TestCases
    {
        get
        {
            yield return new object[] { "GetZoneLine", new Action<TzDataTimezone, DateTime>((timezone, utc) => timezone.GetZoneLine(utc)) };
            yield return new object[] { "GetUtcOffset", new Action<TzDataTimezone, DateTime>((timezone, utc) => timezone.GetUtcOffset(utc)) };
            yield return new object[] { "GetStandardOffset", new Action<TzDataTimezone, DateTime>((timezone, utc) => timezone.GetStandardOffset(utc)) };
            yield return new object[] { "GetDaylightSavings", new Action<TzDataTimezone, DateTime>((timezone, utc) => timezone.GetDaylightSavings(utc)) };
            yield return new object[] { "IsDaylightSavingTime", new Action<TzDataTimezone, DateTime>((timezone, utc) => timezone.IsDaylightSavingTime(utc)) };
            yield return new object[] { "GetOffsetInfo", new Action<TzDataTimezone, DateTime>((timezone, utc) => timezone.GetOffsetInfo(utc)) };
            yield return new object[] { "GetAbbreviation", new Action<TzDataTimezone, DateTime>((timezone, utc) => timezone.GetAbbreviation(utc)) };
            yield return new object[] { "GetStandardAbbreviation", new Action<TzDataTimezone, DateTime>((timezone, utc) => timezone.GetStandardAbbreviation(utc)) };
            yield return new object[] { "GetDaylightAbbreviation", new Action<TzDataTimezone, DateTime>((timezone, utc) => timezone.GetDaylightAbbreviation(utc)) };
            yield return new object[] { "GetTransitions(from)", new Action<TzDataTimezone, DateTime>((timezone, utc) => timezone.GetTransitions(utc, DateTime.MaxValue)) };
            yield return new object[] { "GetTransitions(to)", new Action<TzDataTimezone, DateTime>((timezone, utc) => timezone.GetTransitions(DateTime.MinValue, utc)) };
        }
    }

    public static IEnumerable<object[]> OffsetMethodWithDateTimeOffset_TestCases
    {
        get
        {
            yield return new object[] { "GetZoneLine", new Action<TzDataTimezone, DateTimeOffset>((timezone, instant) => timezone.GetZoneLine(instant)) };
            yield return new object[] { "GetUtcOffset", new Action<TzDataTimezone, DateTimeOffset>((timezone, instant) => timezone.GetUtcOffset(instant)) };
            yield return new object[] { "GetStandardOffset", new Action<TzDataTimezone, DateTimeOffset>((timezone, instant) => timezone.GetStandardOffset(instant)) };
            yield return new object[] { "GetDaylightSavings", new Action<TzDataTimezone, DateTimeOffset>((timezone, instant) => timezone.GetDaylightSavings(instant)) };
            yield return new object[] { "IsDaylightSavingTime", new Action<TzDataTimezone, DateTimeOffset>((timezone, instant) => timezone.IsDaylightSavingTime(instant)) };
            yield return new object[] { "GetOffsetInfo", new Action<TzDataTimezone, DateTimeOffset>((timezone, instant) => timezone.GetOffsetInfo(instant)) };
            yield return new object[] { "GetAbbreviation", new Action<TzDataTimezone, DateTimeOffset>((timezone, instant) => timezone.GetAbbreviation(instant)) };
            yield return new object[] { "GetStandardAbbreviation", new Action<TzDataTimezone, DateTimeOffset>((timezone, instant) => timezone.GetStandardAbbreviation(instant)) };
            yield return new object[] { "GetDaylightAbbreviation", new Action<TzDataTimezone, DateTimeOffset>((timezone, instant) => timezone.GetDaylightAbbreviation(instant)) };
            yield return new object[] { "GetTransitions", new Action<TzDataTimezone, DateTimeOffset>((timezone, instant) => timezone.GetTransitions(instant, DateTimeOffset.MaxValue)) };
        }
    }

    [Theory]
    [MemberData(nameof(OffsetMethod_TestCases))]
    public void OffsetMethod_LocalKind_ThrowsArgumentException(string testMethod, Action<TzDataTimezone, DateTime> testAction)
    {
        // arrange
        var testLocal = new DateTime(2025, 7, 1, 12, 0, 0, DateTimeKind.Local);

        // act
        var testResult = () => testAction(TzDataTimezone.Europe.London, testLocal);

        // assert
        testResult.Should().Throw<ArgumentException>("{0} accepts only universal times", testMethod);
    }

    [Theory]
    [MemberData(nameof(OffsetMethod_TestCases))]
    public void OffsetMethod_DefaultTimezone_ThrowsInvalidOperationException(string testMethod, Action<TzDataTimezone, DateTime> testAction)
    {
        // act
        var testResult = () => testAction(default, new DateTime(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc));

        // assert
        testResult.Should().Throw<InvalidOperationException>("{0} needs a timezone with an identifier", testMethod);
    }

    [Theory]
    [MemberData(nameof(OffsetMethod_TestCases))]
    public void OffsetMethod_UnknownTimezone_ThrowsInvalidOperationException(string testMethod, Action<TzDataTimezone, DateTime> testAction)
    {
        // act
        var testResult = () => testAction(UnknownTimezone, new DateTime(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc));

        // assert
        testResult.Should().Throw<InvalidOperationException>("{0} needs a known timezone", testMethod);
    }

    [Theory]
    [MemberData(nameof(OffsetMethodWithDateTimeOffset_TestCases))]
    public void OffsetMethodWithDateTimeOffset_InvalidTimezone_ThrowsInvalidOperationException(string testMethod, Action<TzDataTimezone, DateTimeOffset> testAction)
    {
        // arrange
        var testInstant = new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var testDefault = () => testAction(default, testInstant);
        var testUnknown = () => testAction(UnknownTimezone, testInstant);

        // assert
        testDefault.Should().Throw<InvalidOperationException>("{0} needs a timezone with an identifier", testMethod);
        testUnknown.Should().Throw<InvalidOperationException>("{0} needs a known timezone", testMethod);
    }

    [Fact]
    public void GetTransitions_EndPrecedesStart_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var testFrom = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        // act
        var testResult = () => TzDataTimezone.Europe.London.GetTransitions(testFrom, testFrom.AddTicks(-1));

        // assert
        testResult.Should().Throw<ArgumentOutOfRangeException>("the end of the period must not precede its start");
    }

    [Theory]
    [InlineData("%z", "04:00", false, null, "+04")]
    [InlineData("%z", "05:30", false, null, "+0530")]
    [InlineData("%z", "-03:30", false, null, "-0330")]
    [InlineData("%z", "-04:56:02", false, null, "-045602")]
    [InlineData("%z", "00:00", false, null, "+00")]
    [InlineData("E%sT", "-04:00", true, "D", "EDT")]
    [InlineData("E%sT", "-05:00", false, "S", "EST")]
    [InlineData("%s", "00:00", false, "GMT", "GMT")]
    [InlineData("IST/GMT", "00:00", true, "", "GMT")]
    [InlineData("IST/GMT", "01:00", false, "", "IST")]
    [InlineData("LMT", "-04:56:02", false, null, "LMT")]
    public void FormatAbbreviation_Format_ReturnsAbbreviation(string testFormat, string testOffset, bool testIsDaylightSavingTime, string? testLetter, string expectedResult)
    {
        // act
        var testResult = TzDataZoneCalculator.FormatAbbreviation(testFormat, testLetter, testIsDaylightSavingTime, ParseOffset(testOffset));

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void CalculatorCache_ConcurrentFirstUse_ReturnsSameCalculatorToEveryThread()
    {
        // arrange
        const int threadCount = 8;
        var timeout = TimeSpan.FromSeconds(30);
        var testCache = new TzDataZoneCalculator.Cache();
        var testZoneLines = TzDataTimezone.America.New_York.ZoneLines.ToArray();
        var testResults = new TzDataZoneCalculator?[threadCount];
        var exceptions = new Exception?[threadCount];
        using var barrier = new Barrier(threadCount);
        var threads = Enumerable.Range(0, threadCount)
            .Select(index => new Thread(() =>
            {
                try
                {
                    if (!barrier.SignalAndWait(timeout))
                        throw new TimeoutException("The threads were not released together.");
                    testResults[index] = testCache.GetCalculator("America/New_York", testZoneLines);
                }
                catch (Exception exception)
                {
                    exceptions[index] = exception;
                }
            }))
            .ToList();

        // act
        threads.ForEach(thread => thread.Start());
        var joined = threads.All(thread => thread.Join(timeout));

        // assert
        joined.Should().BeTrue("every thread completes within {0}", timeout);
        exceptions.Should().OnlyContain(exception => exception == null, "first use of the cache succeeds on every thread");
        testResults.Should().OnlyContain(calculator => ReferenceEquals(calculator, testResults[0]), "the calculator is built once and shared by every thread");
        testResults[0].Should().NotBeNull();
    }

    [Fact]
    public void OffsetMethods_ConcurrentFirstUse_ReturnSameOffsetsOnEveryThread()
    {
        // arrange
        const int threadCount = 8;
        var timeout = TimeSpan.FromSeconds(30);
        var testTimezones = GetTimezoneFields(typeof(TzDataTimezone)).ToList();
        var testInstant = new DateTime(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var testResults = new List<TimeSpan>?[threadCount];
        var exceptions = new Exception?[threadCount];
        using var barrier = new Barrier(threadCount);
        var threads = Enumerable.Range(0, threadCount)
            .Select(index => new Thread(() =>
            {
                try
                {
                    if (!barrier.SignalAndWait(timeout))
                        throw new TimeoutException("The threads were not released together.");
                    // Each thread visits the timezones in a different order, so that calculators are built concurrently.
                    var results = new TimeSpan[testTimezones.Count];
                    for (var step = 0; step < testTimezones.Count; step++)
                    {
                        var position = (step + index * 37) % testTimezones.Count;
                        results[position] = testTimezones[position].GetUtcOffset(testInstant);
                    }

                    testResults[index] = results.ToList();
                }
                catch (Exception exception)
                {
                    exceptions[index] = exception;
                }
            }))
            .ToList();

        // act
        threads.ForEach(thread => thread.Start());
        var joined = threads.All(thread => thread.Join(timeout));

        // assert
        joined.Should().BeTrue("every thread completes within {0}", timeout);
        exceptions.Should().OnlyContain(exception => exception == null, "concurrent use succeeds on every thread");
        testResults.Should().OnlyContain(results => results!.SequenceEqual(testResults[0]!), "every thread finds the same offsets");
    }
}
