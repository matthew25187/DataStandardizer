using FluentAssertions;
using static DataStandardizer.Chronology.Tests.TzAbbreviations;

namespace DataStandardizer.Chronology.Tests;

/// <remarks>
/// The expected abbreviations are those reported by <c>zdump -v</c> for TZ Database 2026e, compiled by zic in its main format.
/// </remarks>
public class TzDataExtensionsAbbreviationTests
{
    // Instants away from any transition, in the northern winter and summer of 2025.
    private static readonly DateTime MidJanuary2025 = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MidJuly2025 = new(2025, 7, 15, 12, 0, 0, DateTimeKind.Utc);

    // An offset that none of the timezones tested has, so that only the instant of a DateTimeOffset can give the abbreviations.
    private static readonly TimeSpan UnrelatedOffset = new(-9, -30, 0);

    // The timezone and instant, with the abbreviation in force, the standard time abbreviation and the daylight saving time abbreviation.
    private static readonly (TzDataTimezone Timezone, DateTime Instant, string Abbreviation, string StandardAbbreviation, string? DaylightAbbreviation)[] AbbreviationCases =
    [
        // America/New_York: the US rules, in winter and in summer.
        (TzDataTimezone.America.New_York, MidJanuary2025, Est, Est, Edt),
        (TzDataTimezone.America.New_York, MidJuly2025, Edt, Est, Edt),
        // Europe/London: the EU rules, with the GMT/BST format.
        (TzDataTimezone.Europe.London, MidJanuary2025, Gmt, Gmt, Bst),
        (TzDataTimezone.Europe.London, MidJuly2025, Bst, Gmt, Bst),
        // Australia/Sydney: the southern summer is daylight saving time.
        (TzDataTimezone.Australia.Sydney, MidJanuary2025, Aedt, Aest, Aedt),
        (TzDataTimezone.Australia.Sydney, MidJuly2025, Aest, Aest, Aedt),
        // Europe/Dublin: negative daylight saving, so that winter time is daylight saving time.
        (TzDataTimezone.Europe.Dublin, MidJanuary2025, Gmt, Ist, Gmt),
        (TzDataTimezone.Europe.Dublin, MidJuly2025, Ist, Ist, Gmt),
        // Numeric abbreviations, with and without minutes, and a named abbreviation for a half-hour offset.
        (TzDataTimezone.Asia.Dubai, MidJuly2025, Plus04, Plus04, null),
        (TzDataTimezone.Asia.Kathmandu, MidJuly2025, Plus0545, Plus0545, null),
        (TzDataTimezone.Asia.Kolkata, MidJuly2025, Ist, Ist, null),
        // Australia/Lord_Howe: a numeric abbreviation with 30 minutes of daylight saving.
        (TzDataTimezone.Australia.Lord_Howe, MidJuly2025, Plus1030, Plus1030, Plus11),
        // Asia/Tokyo: no daylight saving time since 1951.
        (TzDataTimezone.Asia.Tokyo, MidJuly2025, Jst, Jst, null),
        // America/New_York: local mean time, before standard time.
        (TzDataTimezone.America.New_York, new DateTime(1880, 7, 15, 12, 0, 0, DateTimeKind.Utc), Lmt, Lmt, null),
        // America/New_York: war time from February 1942, in force all year in 1943 and 1944, then peace time in 1945.
        (TzDataTimezone.America.New_York, new DateTime(1942, 1, 15, 12, 0, 0, DateTimeKind.Utc), Est, Est, Ewt),
        (TzDataTimezone.America.New_York, new DateTime(1943, 1, 15, 12, 0, 0, DateTimeKind.Utc), Ewt, Est, Ewt),
        (TzDataTimezone.America.New_York, new DateTime(1945, 9, 15, 12, 0, 0, DateTimeKind.Utc), Ept, Est, Ept),
        (TzDataTimezone.America.New_York, new DateTime(1946, 1, 15, 12, 0, 0, DateTimeKind.Utc), Est, Est, Edt),
        // Europe/London: British Standard Time, +1 as standard time all year from 1968 to 1971.
        (TzDataTimezone.Europe.London, new DateTime(1970, 1, 15, 12, 0, 0, DateTimeKind.Utc), Bst, Bst, null),
        // Europe/London: before the first rule, standard time takes the letter of the first standard time rule.
        (TzDataTimezone.Europe.London, new DateTime(1900, 1, 15, 12, 0, 0, DateTimeKind.Utc), Gmt, Gmt, null),
        // Europe/Moscow: the standard time rule of October 1921 takes effect when the zone line ends, so it belongs to the next line.
        (TzDataTimezone.Europe.Moscow, new DateTime(1921, 1, 15, 12, 0, 0, DateTimeKind.Utc), Msk, Msk, Msd),
        // America/Argentina/Jujuy: a zone line that saves a fixed hour.
        (TzDataTimezone.America.Argentina.Jujuy, new DateTime(1991, 1, 15, 12, 0, 0, DateTimeKind.Utc), Minus03, Minus04, Minus03),
    ];

    // Instants across the whole range of DateTime, for every timezone.
    private static readonly DateTime[] WholeRangeInstants =
    [
        DateTime.MinValue,
        new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        DateTime.MaxValue,
    ];

    public static TheoryData<TzDataTimezone, DateTime, string> Abbreviation_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, string>();
            foreach (var testCase in AbbreviationCases)
                testCases.Add(testCase.Timezone, testCase.Instant, testCase.Abbreviation);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, string> StandardAbbreviation_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, string>();
            foreach (var testCase in AbbreviationCases)
                testCases.Add(testCase.Timezone, testCase.Instant, testCase.StandardAbbreviation);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, string?> DaylightAbbreviation_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, string?>();
            foreach (var testCase in AbbreviationCases)
                testCases.Add(testCase.Timezone, testCase.Instant, testCase.DaylightAbbreviation);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTimeOffset, string> AbbreviationWithDateTimeOffset_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTimeOffset, string>();
            foreach (var testCase in AbbreviationCases)
                testCases.Add(testCase.Timezone, new DateTimeOffset(testCase.Instant).ToOffset(UnrelatedOffset), testCase.Abbreviation);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTimeOffset, string> StandardAbbreviationWithDateTimeOffset_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTimeOffset, string>();
            foreach (var testCase in AbbreviationCases)
                testCases.Add(testCase.Timezone, new DateTimeOffset(testCase.Instant).ToOffset(UnrelatedOffset), testCase.StandardAbbreviation);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTimeOffset, string?> DaylightAbbreviationWithDateTimeOffset_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTimeOffset, string?>();
            foreach (var testCase in AbbreviationCases)
                testCases.Add(testCase.Timezone, new DateTimeOffset(testCase.Instant).ToOffset(UnrelatedOffset), testCase.DaylightAbbreviation);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime> WholeRange_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime>();
            foreach (var timezone in TimezoneFieldCatalog.Timezones.Values)
            {
                foreach (var instant in WholeRangeInstants)
                    testCases.Add(timezone, instant);
            }

            return testCases;
        }
    }

    [Theory]
    [MemberData(nameof(Abbreviation_TestCases))]
    public void GetAbbreviation_Instant_ReturnsAbbreviationInForce(TzDataTimezone testTimezone, DateTime testInstant, string expectedResult)
    {
        // act
        var testResult = testTimezone.GetAbbreviation(testInstant);

        // assert
        testResult.Should().Be(expectedResult, "{0} is known as {1} at {2:O}", testTimezone, expectedResult, testInstant);
    }

    [Theory]
    [MemberData(nameof(Abbreviation_TestCases))]
    public void GetOffsetInfo_Instant_HasAbbreviationInForce(TzDataTimezone testTimezone, DateTime testInstant, string expectedResult)
    {
        // act
        var testResult = testTimezone.GetOffsetInfo(testInstant);

        // assert
        testResult.Abbreviation.Should().Be(expectedResult, "{0} is known as {1} at {2:O}", testTimezone, expectedResult, testInstant);
    }

    [Theory]
    [MemberData(nameof(StandardAbbreviation_TestCases))]
    public void GetStandardAbbreviation_Instant_ReturnsStandardTimeAbbreviation(TzDataTimezone testTimezone, DateTime testInstant, string expectedResult)
    {
        // act
        var testResult = testTimezone.GetStandardAbbreviation(testInstant);

        // assert
        testResult.Should().Be(expectedResult, "standard time in {0} is {1} at {2:O}", testTimezone, expectedResult, testInstant);
    }

    [Theory]
    [MemberData(nameof(DaylightAbbreviation_TestCases))]
    public void GetDaylightAbbreviation_Instant_ReturnsDaylightSavingTimeAbbreviation(TzDataTimezone testTimezone, DateTime testInstant, string? expectedResult)
    {
        // act
        var testResult = testTimezone.GetDaylightAbbreviation(testInstant);

        // assert
        testResult.Should().Be(expectedResult, "daylight saving time in {0} has the abbreviation {1} at {2:O}", testTimezone, expectedResult, testInstant);
    }

    [Theory]
    [MemberData(nameof(AbbreviationWithDateTimeOffset_TestCases))]
    public void GetAbbreviation_DateTimeOffset_UsesInstant(TzDataTimezone testTimezone, DateTimeOffset testInstant, string expectedResult)
    {
        // act
        var testResult = testTimezone.GetAbbreviation(testInstant);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(StandardAbbreviationWithDateTimeOffset_TestCases))]
    public void GetStandardAbbreviation_DateTimeOffset_UsesInstant(TzDataTimezone testTimezone, DateTimeOffset testInstant, string expectedResult)
    {
        // act
        var testResult = testTimezone.GetStandardAbbreviation(testInstant);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(DaylightAbbreviationWithDateTimeOffset_TestCases))]
    public void GetDaylightAbbreviation_DateTimeOffset_UsesInstant(TzDataTimezone testTimezone, DateTimeOffset testInstant, string? expectedResult)
    {
        // act
        var testResult = testTimezone.GetDaylightAbbreviation(testInstant);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(WholeRange_TestCases), DisableDiscoveryEnumeration = true)]
    public void GetStandardAbbreviation_EveryTimezoneAcrossWholeRange_ReturnsAbbreviation(TzDataTimezone testTimezone, DateTime testInstant)
    {
        // act
        var testResult = testTimezone.GetStandardAbbreviation(testInstant);

        // assert
        testResult.Should().NotBeNullOrEmpty("{0} has a standard abbreviation at {1:O}", testTimezone, testInstant);
    }

    [Theory]
    [MemberData(nameof(WholeRange_TestCases), DisableDiscoveryEnumeration = true)]
    public void GetDaylightAbbreviation_EveryTimezoneAcrossWholeRange_ReturnsNoEmptyAbbreviation(TzDataTimezone testTimezone, DateTime testInstant)
    {
        // act
        var testResult = testTimezone.GetDaylightAbbreviation(testInstant);

        // assert
        testResult.Should().NotBe(string.Empty, "{0} has no empty daylight abbreviation at {1:O}", testTimezone, testInstant);
    }
}
