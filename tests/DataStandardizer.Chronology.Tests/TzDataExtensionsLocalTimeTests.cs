using FluentAssertions;
using static DataStandardizer.Chronology.Tests.TzAbbreviations;
using static DataStandardizer.Chronology.Tests.TzOffsets;

namespace DataStandardizer.Chronology.Tests;

/// <remarks>
/// The expected offsets are those reported by <c>zdump -v</c> for TZ Database 2026e, compiled by zic in its main format.
/// </remarks>
public class TzDataExtensionsLocalTimeTests
{
    private const string LocalParameterName = "local";

    // A value that is not a member of TzDataLocalTimeResolution.
    private const TzDataLocalTimeResolution UndefinedResolution = (TzDataLocalTimeResolution)99;

    private static readonly TzDataTimezone UnknownTimezone = (TzDataTimezone)"Nowhere/Nothing";

    // Times away from any transition, in the northern winter and summer of 2026.
    private static readonly DateTime MidJanuary2026 = new(2026, 1, 15, 12, 0, 0);
    private static readonly DateTime MidJuly2026 = new(2026, 7, 15, 12, 0, 0);
    private static readonly DateTime July2026 = new(2026, 7, 1, 12, 0, 0);

    // America/New_York: a time in the gap of March 8, 2026, and its instant under the RFC 5545 policy (RFC 5545, section 3.3.5).
    private static readonly DateTime NewYorkGapTime = new(2026, 3, 8, 2, 30, 0);
    private static readonly DateTimeOffset NewYorkGapTimeAfterGap = new(2026, 3, 8, 3, 30, 0, UtcMinus0400);
    private static readonly DateTime NewYorkGapTimeAfterGapUtc = new(2026, 3, 8, 7, 30, 0, DateTimeKind.Utc);

    // America/New_York: a time in the overlap of November 1, 2026, and its earlier instant.
    private static readonly DateTime NewYorkOverlapTime = new(2026, 11, 1, 1, 30, 0);
    private static readonly DateTimeOffset NewYorkOverlapTimeEarlier = new(2026, 11, 1, 1, 30, 0, UtcMinus0400);
    private static readonly DateTime NewYorkOverlapTimeEarlierUtc = new(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc);

    // Europe/London and Europe/Dublin: a time in the gap of March 29, 2026, the time the RFC 5545 policy moves it to, and a time in the overlap of October 25, 2026.
    private static readonly DateTime EuropeGapTime = new(2026, 3, 29, 1, 30, 0);
    private static readonly DateTime EuropeGapTimeAfterGap = new(2026, 3, 29, 2, 30, 0);
    private static readonly DateTime EuropeOverlapTime = new(2026, 10, 25, 1, 30, 0);

    // America/New_York: beyond the last year of explicit data, from the rules that apply indefinitely.
    private static readonly DateTime NewYorkFarFutureSummer = new(2149, 7, 15, 12, 0, 0);

    private static readonly (TzDataTimezone Timezone, DateTime Local, TimeSpan Offset, string Abbreviation)[] SingleOffsetCases =
    [
        (TzDataTimezone.America.New_York, MidJanuary2026, UtcMinus0500, Est),
        (TzDataTimezone.America.New_York, MidJuly2026, UtcMinus0400, Edt),
        // America/New_York: either side of the gap, and either side of the overlap.
        (TzDataTimezone.America.New_York, new DateTime(2026, 3, 8, 1, 59, 59), UtcMinus0500, Est),
        (TzDataTimezone.America.New_York, new DateTime(2026, 3, 8, 3, 0, 0), UtcMinus0400, Edt),
        (TzDataTimezone.America.New_York, new DateTime(2026, 11, 1, 0, 59, 59), UtcMinus0400, Edt),
        (TzDataTimezone.America.New_York, new DateTime(2026, 11, 1, 2, 0, 0), UtcMinus0500, Est),
        // Europe/Dublin: negative daylight saving, so that summer time is standard time.
        (TzDataTimezone.Europe.Dublin, MidJanuary2026, UtcZero, Gmt),
        (TzDataTimezone.Europe.Dublin, MidJuly2026, UtcPlus0100, Ist),
        // Australia/Lord_Howe: 30 minutes of daylight saving.
        (TzDataTimezone.Australia.Lord_Howe, MidJanuary2026, UtcPlus1100, Plus11),
        (TzDataTimezone.Australia.Lord_Howe, MidJuly2026, UtcPlus1030, Plus1030),
        // Asia/Kolkata: no daylight saving.
        (TzDataTimezone.Asia.Kolkata, MidJanuary2026, UtcPlus0530, Ist),
        (TzDataTimezone.Asia.Kolkata, MidJuly2026, UtcPlus0530, Ist),
        (TzDataTimezone.America.New_York, NewYorkFarFutureSummer, UtcMinus0400, Edt),
    ];

    // The time, and the time to which the RFC 5545 policy moves it, with the offset in effect after the gap.
    private static readonly (TzDataTimezone Timezone, DateTime Local, DateTime ExpectedDateTime, TimeSpan ExpectedOffset)[] GapCases =
    [
        (TzDataTimezone.America.New_York, NewYorkGapTime, NewYorkGapTimeAfterGap.DateTime, UtcMinus0400),
        (TzDataTimezone.America.New_York, new DateTime(2026, 3, 8, 2, 0, 0), new DateTime(2026, 3, 8, 3, 0, 0), UtcMinus0400),
        (TzDataTimezone.America.New_York, new DateTime(2026, 3, 8, 2, 59, 59), new DateTime(2026, 3, 8, 3, 59, 59), UtcMinus0400),
        (TzDataTimezone.Europe.London, EuropeGapTime, EuropeGapTimeAfterGap, UtcPlus0100),
        (TzDataTimezone.Europe.Dublin, EuropeGapTime, EuropeGapTimeAfterGap, UtcPlus0100),
        (TzDataTimezone.Australia.Lord_Howe, new DateTime(2026, 10, 4, 2, 15, 0), new DateTime(2026, 10, 4, 2, 45, 0), UtcPlus1100),
        // Pacific/Apia: the move across the date line, which skipped December 30, 2011.
        (TzDataTimezone.Pacific.Apia, new DateTime(2011, 12, 30, 12, 0, 0), new DateTime(2011, 12, 31, 12, 0, 0), UtcPlus1400),
        (TzDataTimezone.America.New_York, new DateTime(2149, 3, 9, 2, 30, 0), new DateTime(2149, 3, 9, 3, 30, 0), UtcMinus0400),
    ];

    // The time, then the earlier and later offsets with their abbreviations and whether daylight saving time is in effect.
    private static readonly (TzDataTimezone Timezone, DateTime Local, TimeSpan FirstOffset, string FirstAbbreviation, bool FirstIsDaylightSavingTime, TimeSpan SecondOffset, string SecondAbbreviation, bool SecondIsDaylightSavingTime)[] OverlapCases =
    [
        (TzDataTimezone.America.New_York, NewYorkOverlapTime, UtcMinus0400, Edt, true, UtcMinus0500, Est, false),
        (TzDataTimezone.America.New_York, new DateTime(2026, 11, 1, 1, 0, 0), UtcMinus0400, Edt, true, UtcMinus0500, Est, false),
        (TzDataTimezone.America.New_York, new DateTime(2026, 11, 1, 1, 59, 59), UtcMinus0400, Edt, true, UtcMinus0500, Est, false),
        (TzDataTimezone.Europe.London, EuropeOverlapTime, UtcPlus0100, Bst, true, UtcZero, Gmt, false),
        (TzDataTimezone.Europe.Dublin, EuropeOverlapTime, UtcPlus0100, Ist, false, UtcZero, Gmt, true),
        (TzDataTimezone.Australia.Lord_Howe, new DateTime(2026, 4, 5, 1, 45, 0), UtcPlus1100, Plus11, true, UtcPlus1030, Plus1030, false),
        (TzDataTimezone.America.New_York, new DateTime(2149, 11, 2, 1, 30, 0), UtcMinus0400, Edt, true, UtcMinus0500, Est, false),
    ];

    public static TheoryData<TzDataTimezone, DateTime, TimeSpan, string> SingleOffset_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, TimeSpan, string>();
            foreach (var testCase in SingleOffsetCases)
                testCases.Add(testCase.Timezone, testCase.Local, testCase.Offset, testCase.Abbreviation);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, TzDataLocalTimeResolution, TimeSpan> SingleOffsetWithResolution_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, TzDataLocalTimeResolution, TimeSpan>();
            foreach (var testCase in SingleOffsetCases)
            {
                testCases.Add(testCase.Timezone, testCase.Local, TzDataLocalTimeResolution.Strict, testCase.Offset);
                testCases.Add(testCase.Timezone, testCase.Local, TzDataLocalTimeResolution.Rfc5545, testCase.Offset);
            }

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime> GapTime_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime>();
            foreach (var testCase in GapCases)
                testCases.Add(testCase.Timezone, testCase.Local);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, DateTime, TimeSpan> Gap_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, DateTime, TimeSpan>();
            foreach (var testCase in GapCases)
                testCases.Add(testCase.Timezone, testCase.Local, testCase.ExpectedDateTime, testCase.ExpectedOffset);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, TimeSpan, string, bool, TimeSpan, string, bool> Overlap_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, TimeSpan, string, bool, TimeSpan, string, bool>();
            foreach (var testCase in OverlapCases)
                testCases.Add(testCase.Timezone, testCase.Local, testCase.FirstOffset, testCase.FirstAbbreviation, testCase.FirstIsDaylightSavingTime, testCase.SecondOffset, testCase.SecondAbbreviation, testCase.SecondIsDaylightSavingTime);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime> OverlapTime_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime>();
            foreach (var testCase in OverlapCases)
                testCases.Add(testCase.Timezone, testCase.Local);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, TimeSpan> OverlapEarlierOffset_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, TimeSpan>();
            foreach (var testCase in OverlapCases)
                testCases.Add(testCase.Timezone, testCase.Local, testCase.FirstOffset);

            return testCases;
        }
    }

    [Theory]
    [MemberData(nameof(SingleOffset_TestCases))]
    public void GetOffsetInfosForLocal_NormalTime_ReturnsOneOffset(TzDataTimezone testTimezone, DateTime testLocal, TimeSpan expectedOffset, string expectedAbbreviation)
    {
        // act
        var testResult = testTimezone.GetOffsetInfosForLocal(testLocal);

        // assert
        var testOffsetInfo = testResult.Should().ContainSingle().Which;
        testOffsetInfo.UtcOffset.Should().Be(expectedOffset);
        testOffsetInfo.Abbreviation.Should().Be(expectedAbbreviation);
    }

    [Theory]
    [MemberData(nameof(GapTime_TestCases))]
    public void GetOffsetInfosForLocal_TimeInGap_ReturnsNoOffsets(TzDataTimezone testTimezone, DateTime testLocal)
    {
        // act
        var testResult = testTimezone.GetOffsetInfosForLocal(testLocal);

        // assert
        testResult.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Gap_TestCases))]
    public void ToDateTimeOffset_TimeInGapWithRfc5545_TakesOffsetBeforeGap(TzDataTimezone testTimezone, DateTime testLocal, DateTime expectedDateTime, TimeSpan expectedOffset)
    {
        // act
        var testResult = testLocal.ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Rfc5545);

        // assert
        testResult.DateTime.Should().Be(expectedDateTime);
        testResult.Offset.Should().Be(expectedOffset);
    }

    [Theory]
    [MemberData(nameof(GapTime_TestCases))]
    public void ToDateTimeOffset_TimeInGapWithStrict_ThrowsArgumentException(TzDataTimezone testTimezone, DateTime testLocal)
    {
        // act
        Action testAction = () => testLocal.ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Strict);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(LocalParameterName);
    }

    [Theory]
    [MemberData(nameof(Overlap_TestCases))]
    public void GetOffsetInfosForLocal_TimeInOverlap_ReturnsBothOffsetsInOrder(TzDataTimezone testTimezone, DateTime testLocal, TimeSpan expectedFirstOffset, string expectedFirstAbbreviation, bool expectedFirstIsDaylightSavingTime, TimeSpan expectedSecondOffset, string expectedSecondAbbreviation, bool expectedSecondIsDaylightSavingTime)
    {
        // act
        var testResult = testTimezone.GetOffsetInfosForLocal(testLocal);

        // assert
        testResult.Should().HaveCount(2);
        testResult[0].UtcOffset.Should().Be(expectedFirstOffset);
        testResult[0].Abbreviation.Should().Be(expectedFirstAbbreviation);
        testResult[0].IsDaylightSavingTime.Should().Be(expectedFirstIsDaylightSavingTime);
        testResult[1].UtcOffset.Should().Be(expectedSecondOffset);
        testResult[1].Abbreviation.Should().Be(expectedSecondAbbreviation);
        testResult[1].IsDaylightSavingTime.Should().Be(expectedSecondIsDaylightSavingTime);
        testResult[0].ValidUntilUtc.Should().Be(testResult[1].ValidFromUtc);
    }

    [Theory]
    [MemberData(nameof(OverlapEarlierOffset_TestCases))]
    public void ToDateTimeOffset_TimeInOverlapWithRfc5545_TakesEarlierInstant(TzDataTimezone testTimezone, DateTime testLocal, TimeSpan expectedFirstOffset)
    {
        // act
        var testResult = testLocal.ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Rfc5545);

        // assert
        testResult.DateTime.Should().Be(testLocal);
        testResult.Offset.Should().Be(expectedFirstOffset);
    }

    [Theory]
    [MemberData(nameof(OverlapTime_TestCases))]
    public void ToDateTimeOffset_TimeInOverlapWithStrict_ThrowsArgumentException(TzDataTimezone testTimezone, DateTime testLocal)
    {
        // act
        Action testAction = () => testLocal.ToDateTimeOffset(testTimezone, TzDataLocalTimeResolution.Strict);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(LocalParameterName);
    }

    [Fact]
    public void ToDateTimeOffset_NewYorkTimeInGap_GivesInstantAfterGap()
    {
        // act
        var testResult = NewYorkGapTime.ToDateTimeOffset(TzDataTimezone.America.New_York, TzDataLocalTimeResolution.Rfc5545);

        // assert
        testResult.Should().Be(NewYorkGapTimeAfterGap);
        testResult.Offset.Should().Be(UtcMinus0400);
        testResult.UtcDateTime.Should().Be(NewYorkGapTimeAfterGapUtc);
    }

    [Fact]
    public void ToDateTimeOffset_NewYorkTimeInOverlap_GivesEarlierInstant()
    {
        // act
        var testResult = NewYorkOverlapTime.ToDateTimeOffset(TzDataTimezone.America.New_York, TzDataLocalTimeResolution.Rfc5545);

        // assert
        testResult.Should().Be(NewYorkOverlapTimeEarlier);
        testResult.Offset.Should().Be(UtcMinus0400);
        testResult.UtcDateTime.Should().Be(NewYorkOverlapTimeEarlierUtc);
    }

    [Theory]
    [MemberData(nameof(SingleOffsetWithResolution_TestCases))]
    public void ToDateTimeOffset_NormalTime_KeepsTimeWithOffsetInEffect(TzDataTimezone testTimezone, DateTime testLocal, TzDataLocalTimeResolution testResolution, TimeSpan expectedOffset)
    {
        // act
        var testResult = testLocal.ToDateTimeOffset(testTimezone, testResolution);

        // assert
        testResult.DateTime.Should().Be(testLocal);
        testResult.Offset.Should().Be(expectedOffset);
    }

    #region Every quarter hour of 2026

    // The offsets in force during 2026 in each timezone swept, with the instants at which they begin and cease to apply.
    private static readonly TzDataOffsetInfo NewYorkStandardTimeOfWinter2025 = new(UtcMinus0500, UtcZero, false, Est, Utc(2025, 11, 2, 6, 0), Utc(2026, 3, 8, 7, 0));
    private static readonly TzDataOffsetInfo NewYorkDaylightTime2026 = new(UtcMinus0500, UtcPlus0100, true, Edt, Utc(2026, 3, 8, 7, 0), Utc(2026, 11, 1, 6, 0));
    private static readonly TzDataOffsetInfo NewYorkStandardTimeOfWinter2026 = new(UtcMinus0500, UtcZero, false, Est, Utc(2026, 11, 1, 6, 0), Utc(2027, 3, 14, 7, 0));
    private static readonly TzDataOffsetInfo LondonStandardTimeOfWinter2025 = new(UtcZero, UtcZero, false, Gmt, Utc(2025, 10, 26, 1, 0), Utc(2026, 3, 29, 1, 0));
    private static readonly TzDataOffsetInfo LondonSummerTime2026 = new(UtcZero, UtcPlus0100, true, Bst, Utc(2026, 3, 29, 1, 0), Utc(2026, 10, 25, 1, 0));
    private static readonly TzDataOffsetInfo LondonStandardTimeOfWinter2026 = new(UtcZero, UtcZero, false, Gmt, Utc(2026, 10, 25, 1, 0), Utc(2027, 3, 28, 1, 0));
    private static readonly TzDataOffsetInfo DublinWinterTime2025 = new(UtcPlus0100, UtcMinus0100, true, Gmt, Utc(2025, 10, 26, 1, 0), Utc(2026, 3, 29, 1, 0));
    private static readonly TzDataOffsetInfo DublinStandardTime2026 = new(UtcPlus0100, UtcZero, false, Ist, Utc(2026, 3, 29, 1, 0), Utc(2026, 10, 25, 1, 0));
    private static readonly TzDataOffsetInfo DublinWinterTime2026 = new(UtcPlus0100, UtcMinus0100, true, Gmt, Utc(2026, 10, 25, 1, 0), Utc(2027, 3, 28, 1, 0));
    private static readonly TzDataOffsetInfo LordHoweDaylightTime2025 = new(UtcPlus1030, UtcPlus0030, true, Plus11, Utc(2025, 10, 4, 15, 30), Utc(2026, 4, 4, 15, 0));
    private static readonly TzDataOffsetInfo LordHoweStandardTime2026 = new(UtcPlus1030, UtcZero, false, Plus1030, Utc(2026, 4, 4, 15, 0), Utc(2026, 10, 3, 15, 30));
    private static readonly TzDataOffsetInfo LordHoweDaylightTime2026 = new(UtcPlus1030, UtcPlus0030, true, Plus11, Utc(2026, 10, 3, 15, 30), Utc(2027, 4, 3, 15, 0));
    private static readonly TzDataOffsetInfo KolkataStandardTime = new(UtcPlus0530, UtcZero, false, Ist, Utc(1945, 10, 14, 17, 30), null);

    // From each local time given until the next, the offsets that give that local time, earliest first.
    private static readonly (TzDataTimezone Timezone, (DateTime LocalStart, TzDataOffsetInfo[] Offsets)[] Segments)[] LocalTimeSegments =
    [
        (TzDataTimezone.America.New_York,
        [
            (new DateTime(2026, 1, 1, 0, 0, 0), [NewYorkStandardTimeOfWinter2025]),
            (new DateTime(2026, 3, 8, 2, 0, 0), []),
            (new DateTime(2026, 3, 8, 3, 0, 0), [NewYorkDaylightTime2026]),
            (new DateTime(2026, 11, 1, 1, 0, 0), [NewYorkDaylightTime2026, NewYorkStandardTimeOfWinter2026]),
            (new DateTime(2026, 11, 1, 2, 0, 0), [NewYorkStandardTimeOfWinter2026]),
        ]),
        (TzDataTimezone.Europe.London,
        [
            (new DateTime(2026, 1, 1, 0, 0, 0), [LondonStandardTimeOfWinter2025]),
            (new DateTime(2026, 3, 29, 1, 0, 0), []),
            (new DateTime(2026, 3, 29, 2, 0, 0), [LondonSummerTime2026]),
            (new DateTime(2026, 10, 25, 1, 0, 0), [LondonSummerTime2026, LondonStandardTimeOfWinter2026]),
            (new DateTime(2026, 10, 25, 2, 0, 0), [LondonStandardTimeOfWinter2026]),
        ]),
        (TzDataTimezone.Europe.Dublin,
        [
            (new DateTime(2026, 1, 1, 0, 0, 0), [DublinWinterTime2025]),
            (new DateTime(2026, 3, 29, 1, 0, 0), []),
            (new DateTime(2026, 3, 29, 2, 0, 0), [DublinStandardTime2026]),
            (new DateTime(2026, 10, 25, 1, 0, 0), [DublinStandardTime2026, DublinWinterTime2026]),
            (new DateTime(2026, 10, 25, 2, 0, 0), [DublinWinterTime2026]),
        ]),
        (TzDataTimezone.Australia.Lord_Howe,
        [
            (new DateTime(2026, 1, 1, 0, 0, 0), [LordHoweDaylightTime2025]),
            (new DateTime(2026, 4, 5, 1, 30, 0), [LordHoweDaylightTime2025, LordHoweStandardTime2026]),
            (new DateTime(2026, 4, 5, 2, 0, 0), [LordHoweStandardTime2026]),
            (new DateTime(2026, 10, 4, 2, 0, 0), []),
            (new DateTime(2026, 10, 4, 2, 30, 0), [LordHoweDaylightTime2026]),
        ]),
        (TzDataTimezone.Asia.Kolkata,
        [
            (new DateTime(2026, 1, 1, 0, 0, 0), [KolkataStandardTime]),
        ]),
    ];

    // The offsets in force during 2026 in each timezone swept, in chronological order.
    private static readonly (TzDataTimezone Timezone, TzDataOffsetInfo[] Offsets)[] UniversalTimeSegments =
    [
        (TzDataTimezone.America.New_York, [NewYorkStandardTimeOfWinter2025, NewYorkDaylightTime2026, NewYorkStandardTimeOfWinter2026]),
        (TzDataTimezone.Europe.London, [LondonStandardTimeOfWinter2025, LondonSummerTime2026, LondonStandardTimeOfWinter2026]),
        (TzDataTimezone.Europe.Dublin, [DublinWinterTime2025, DublinStandardTime2026, DublinWinterTime2026]),
        (TzDataTimezone.Australia.Lord_Howe, [LordHoweDaylightTime2025, LordHoweStandardTime2026, LordHoweDaylightTime2026]),
        (TzDataTimezone.Asia.Kolkata, [KolkataStandardTime]),
    ];

    private const int SweepYear = 2026;

    private static readonly TimeSpan SweepStep = TimeSpan.FromMinutes(15);

    private static DateTime Utc(int year, int month, int day, int hour, int minute) => new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    public static TheoryData<TzDataTimezone, DateTime, TzDataOffsetInfo[]> EveryLocalQuarterHour_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, TzDataOffsetInfo[]>();
            foreach (var (timezone, segments) in LocalTimeSegments)
            {
                var segment = 0;
                for (var local = new DateTime(SweepYear, 1, 1, 0, 0, 0); local.Year == SweepYear; local += SweepStep)
                {
                    while (segment + 1 < segments.Length && segments[segment + 1].LocalStart <= local)
                        segment++;

                    testCases.Add(timezone, local, segments[segment].Offsets);
                }
            }

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, TzDataOffsetInfo> EveryUniversalQuarterHour_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, TzDataOffsetInfo>();
            foreach (var (timezone, offsets) in UniversalTimeSegments)
            {
                var segment = 0;
                for (var utc = Utc(SweepYear, 1, 1, 0, 0); utc.Year == SweepYear; utc += SweepStep)
                {
                    while (offsets[segment].ValidUntilUtc <= utc)
                        segment++;

                    // The local time that the clock shows at the instant.
                    testCases.Add(timezone, DateTime.SpecifyKind(utc + offsets[segment].UtcOffset, DateTimeKind.Unspecified), offsets[segment]);
                }
            }

            return testCases;
        }
    }

    [Theory]
    [MemberData(nameof(EveryLocalQuarterHour_TestCases), DisableDiscoveryEnumeration = true)]
    public void GetOffsetInfosForLocal_EveryQuarterHourOfYear_ReturnsOffsetsThatGiveTime(TzDataTimezone testTimezone, DateTime testLocal, TzDataOffsetInfo[] expectedResult)
    {
        // act
        var testResult = testTimezone.GetOffsetInfosForLocal(testLocal);

        // assert
        testResult.Should().Equal(expectedResult);
    }

    [Theory]
    [MemberData(nameof(EveryUniversalQuarterHour_TestCases), DisableDiscoveryEnumeration = true)]
    public void GetOffsetInfosForLocal_EveryQuarterHourOfYear_FindsEveryInstant(TzDataTimezone testTimezone, DateTime testLocal, TzDataOffsetInfo expectedOffsetInfo)
    {
        // act
        var testResult = testTimezone.GetOffsetInfosForLocal(testLocal);

        // assert
        testResult.Should().Contain(expectedOffsetInfo);
    }

    #endregion

    public static TheoryData<TzDataTimezone, DateTime> RepresentableRangeLimit_TestCases => new()
    {
        // America/New_York is behind universal time, so the earliest local time has an instant.
        { TzDataTimezone.America.New_York, DateTime.MinValue },
        // Asia/Kolkata is ahead of universal time, so the latest local time has an instant.
        { TzDataTimezone.Asia.Kolkata, DateTime.MaxValue },
    };

    public static TheoryData<TzDataTimezone, DateTime> UnrepresentableRangeLimit_TestCases => new()
    {
        // America/New_York is behind universal time, so the latest local time has no instant.
        { TzDataTimezone.America.New_York, DateTime.MaxValue },
        // Asia/Kolkata is ahead of universal time, so the earliest local time has no instant.
        { TzDataTimezone.Asia.Kolkata, DateTime.MinValue },
    };

    [Theory]
    [MemberData(nameof(RepresentableRangeLimit_TestCases))]
    public void GetOffsetInfosForLocal_RepresentableRangeLimit_ReturnsOneOffset(TzDataTimezone testTimezone, DateTime testLocal)
    {
        // act
        var testResult = testTimezone.GetOffsetInfosForLocal(testLocal);

        // assert
        testResult.Should().ContainSingle();
    }

    [Theory]
    [MemberData(nameof(UnrepresentableRangeLimit_TestCases))]
    public void GetOffsetInfosForLocal_UnrepresentableRangeLimit_ReturnsNoOffsets(TzDataTimezone testTimezone, DateTime testLocal)
    {
        // act
        var testResult = testTimezone.GetOffsetInfosForLocal(testLocal);

        // assert
        testResult.Should().BeEmpty();
    }

    [Fact]
    public void ToDateTimeOffset_OffsetWithSeconds_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        // America/New_York kept local mean time, -04:56:02, until 1883.
        var testLocal = new DateTime(1880, 1, 1, 12, 0, 0);

        // act
        Action testAction = () => testLocal.ToDateTimeOffset(TzDataTimezone.America.New_York, TzDataLocalTimeResolution.Strict);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be(LocalParameterName);
    }

    [Fact]
    public void ToDateTimeOffset_UndefinedResolution_ThrowsArgumentOutOfRangeException()
    {
        // act
        Action testAction = () => July2026.ToDateTimeOffset(TzDataTimezone.Europe.London, UndefinedResolution);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("resolution");
    }

    private static readonly (string Method, Action<TzDataTimezone, DateTime> Action)[] LocalTimeMethods =
    [
        (nameof(TzDataExtensions.GetOffsetInfosForLocal), (timezone, local) => timezone.GetOffsetInfosForLocal(local)),
        ($"{nameof(DateTimeExtensions.ToDateTimeOffset)}({nameof(TzDataLocalTimeResolution.Strict)})", (timezone, local) => local.ToDateTimeOffset(timezone, TzDataLocalTimeResolution.Strict)),
        ($"{nameof(DateTimeExtensions.ToDateTimeOffset)}({nameof(TzDataLocalTimeResolution.Rfc5545)})", (timezone, local) => local.ToDateTimeOffset(timezone, TzDataLocalTimeResolution.Rfc5545)),
    ];

    public static TheoryData<string, Action<TzDataTimezone, DateTime>> LocalTimeMethod_TestCases
    {
        get
        {
            var testCases = new TheoryData<string, Action<TzDataTimezone, DateTime>>();
            foreach (var (method, action) in LocalTimeMethods)
                testCases.Add(method, action);

            return testCases;
        }
    }

    public static TheoryData<string, Action<TzDataTimezone, DateTime>, DateTime> LocalTimeMethodWithUtcOrLocalKind_TestCases
    {
        get
        {
            var testCases = new TheoryData<string, Action<TzDataTimezone, DateTime>, DateTime>();
            foreach (var (method, action) in LocalTimeMethods)
            {
                testCases.Add(method, action, DateTime.SpecifyKind(July2026, DateTimeKind.Utc));
                testCases.Add(method, action, DateTime.SpecifyKind(July2026, DateTimeKind.Local));
            }

            return testCases;
        }
    }

    [Theory]
    [MemberData(nameof(LocalTimeMethodWithUtcOrLocalKind_TestCases))]
    public void LocalTimeMethod_UtcOrLocalKind_ThrowsArgumentException(string testMethod, Action<TzDataTimezone, DateTime> testMethodAction, DateTime testLocal)
    {
        // act
        Action testAction = () => testMethodAction(TzDataTimezone.Europe.London, testLocal);

        // assert
        testAction.Should().Throw<ArgumentException>("{0} accepts only times of kind Unspecified", testMethod).Which.ParamName.Should().Be(LocalParameterName);
    }

    [Theory]
    [MemberData(nameof(LocalTimeMethod_TestCases))]
    public void LocalTimeMethod_DefaultTimezone_ThrowsInvalidOperationException(string testMethod, Action<TzDataTimezone, DateTime> testMethodAction)
    {
        // act
        Action testAction = () => testMethodAction(default, July2026);

        // assert
        testAction.Should().Throw<InvalidOperationException>("{0} needs a timezone with an identifier", testMethod);
    }

    [Theory]
    [MemberData(nameof(LocalTimeMethod_TestCases))]
    public void LocalTimeMethod_UnknownTimezone_ThrowsInvalidOperationException(string testMethod, Action<TzDataTimezone, DateTime> testMethodAction)
    {
        // act
        Action testAction = () => testMethodAction(UnknownTimezone, July2026);

        // assert
        testAction.Should().Throw<InvalidOperationException>("{0} needs a known timezone", testMethod);
    }

    public static TheoryData<TzDataTimezone, DateTimeOffset, TimeSpan> ToOffset_TestCases => new()
    {
        // America/New_York: either side of the start of daylight saving time, and either side of its end.
        { TzDataTimezone.America.New_York, new DateTimeOffset(2026, 3, 8, 6, 59, 59, UtcZero), UtcMinus0500 },
        { TzDataTimezone.America.New_York, new DateTimeOffset(2026, 3, 8, 7, 0, 0, UtcZero), UtcMinus0400 },
        { TzDataTimezone.America.New_York, new DateTimeOffset(2026, 11, 1, 5, 30, 0, UtcZero), UtcMinus0400 },
        { TzDataTimezone.America.New_York, new DateTimeOffset(2026, 11, 1, 6, 30, 0, UtcZero), UtcMinus0500 },
        { TzDataTimezone.Europe.London, new DateTimeOffset(2026, 7, 1, 12, 0, 0, UtcPlus0530), UtcPlus0100 },
        { TzDataTimezone.Europe.Dublin, new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.FromHours(-8)), UtcZero },
        { TzDataTimezone.Australia.Lord_Howe, new DateTimeOffset(2026, 10, 3, 15, 45, 0, UtcZero), UtcPlus1100 },
        { TzDataTimezone.Asia.Kolkata, new DateTimeOffset(2026, 7, 1, 12, 0, 0, UtcZero), UtcPlus0530 },
        // Pacific/Apia: the first instant after the move across the date line.
        { TzDataTimezone.Pacific.Apia, new DateTimeOffset(2011, 12, 30, 10, 0, 0, UtcZero), UtcPlus1400 },
        { TzDataTimezone.America.New_York, new DateTimeOffset(2149, 7, 15, 12, 0, 0, UtcZero), UtcMinus0400 },
    };

    [Theory]
    [MemberData(nameof(ToOffset_TestCases))]
    public void ToOffset_Instant_KeepsInstantWithOffsetOfTimezone(TzDataTimezone testTimezone, DateTimeOffset testInstant, TimeSpan expectedOffset)
    {
        // act
        var testResult = testInstant.ToOffset(testTimezone);

        // assert
        testResult.UtcDateTime.Should().Be(testInstant.UtcDateTime);
        testResult.Offset.Should().Be(expectedOffset);
    }

    [Fact]
    public void ToOffset_DefaultTimezone_ThrowsInvalidOperationException()
    {
        // arrange
        var testInstant = new DateTimeOffset(July2026, UtcZero);

        // act
        Action testAction = () => testInstant.ToOffset(default(TzDataTimezone));

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToOffset_UnknownTimezone_ThrowsInvalidOperationException()
    {
        // arrange
        var testInstant = new DateTimeOffset(July2026, UtcZero);

        // act
        Action testAction = () => testInstant.ToOffset(UnknownTimezone);

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToOffset_OffsetWithSeconds_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        // America/New_York kept local mean time, -04:56:02, until 1883.
        var testInstant = new DateTimeOffset(1880, 1, 1, 12, 0, 0, UtcZero);

        // act
        Action testAction = () => testInstant.ToOffset(TzDataTimezone.America.New_York);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("instant");
    }
}
