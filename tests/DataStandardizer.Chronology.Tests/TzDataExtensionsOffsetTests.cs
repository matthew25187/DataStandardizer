using FluentAssertions;
using static DataStandardizer.Chronology.Tests.TzAbbreviations;
using static DataStandardizer.Chronology.Tests.TzOffsets;

namespace DataStandardizer.Chronology.Tests;

/// <remarks>
/// The expected transitions are those reported by <c>zdump -v</c> for TZ Database 2026e, compiled by zic in its main format.
/// </remarks>
public class TzDataExtensionsOffsetTests
{
    // The FORMAT column of a Zone line (zic(8)): a numeric abbreviation, a letter substitution, and a pair of abbreviations.
    private const string NumericFormat = "%z";
    private const string EasternFormat = "E%sT";
    private const string IrishFormat = "IST/GMT";
    private const string LetterFormat = "%s";
    private const string LondonFormat = "GMT/BST";

    private static readonly TzDataTimezone UnknownTimezone = (TzDataTimezone)"Nowhere/Nothing";

    // An instant away from any transition.
    private static readonly DateTime July2025 = new(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    // The start of a period, and the last tick before it.
    private static readonly DateTime StartOfJuly2025 = new(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime JustBeforeJuly2025 = StartOfJuly2025.AddTicks(-1);

    // America/New_York: the start of daylight saving time in 2007, under the rules then new; the same instant in Asia/Kolkata time,
    // and the last tick before it; and the same instant as a time of unspecified kind.
    private static readonly DateTime NewYorkDaylightStart2007 = new(2007, 3, 11, 7, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset NewYorkDaylightStart2007InKolkata = new(2007, 3, 11, 12, 30, 0, UtcPlus0530);
    private static readonly DateTimeOffset JustBeforeNewYorkDaylightStart2007InKolkata = NewYorkDaylightStart2007InKolkata.AddTicks(-1);
    private static readonly DateTimeOffset JustAfterNewYorkDaylightStart2007InKolkata = NewYorkDaylightStart2007InKolkata.AddTicks(1);
    private static readonly DateTime NewYorkDaylightStart2007Unspecified = new(2007, 3, 11, 7, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime JustAfterNewYorkDaylightStart2007Unspecified = NewYorkDaylightStart2007Unspecified.AddTicks(1);

    // America/New_York: the end of local mean time.
    private static readonly DateTime NewYorkStandardTimeStart = new(1883, 11, 18, 17, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime JustBeforeNewYorkStandardTimeStart = NewYorkStandardTimeStart.AddTicks(-1);

    // Pacific/Apia: the move across the date line, which skipped December 30, 2011.
    private static readonly DateTime ApiaDateLineChange = new(2011, 12, 30, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime JustBeforeApiaDateLineChange = ApiaDateLineChange.AddTicks(-1);

    // Europe/London: British Standard Time, +1 as standard time all year from 1968 to 1971.
    private static readonly DateTime BritishStandardTimeStart = new(1968, 10, 26, 23, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime BritishStandardTimeEnd = new(1971, 10, 31, 2, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime StartOf1970 = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // The timezone and instant of a transition, then the offset, whether daylight saving time is in effect and the abbreviation, before and after it.
    private static readonly (TzDataTimezone Timezone, DateTime Instant, TimeSpan BeforeOffset, bool BeforeIsDaylightSavingTime, string BeforeAbbreviation, TimeSpan AfterOffset, bool AfterIsDaylightSavingTime, string AfterAbbreviation)[] KnownTransitionCases =
    [
        // America/New_York: local mean time with seconds, then the US rules before and after the 2007 change.
        (TzDataTimezone.America.New_York, NewYorkStandardTimeStart, UtcMinus045602, false, Lmt, UtcMinus0500, false, Est),
        (TzDataTimezone.America.New_York, new DateTime(2006, 4, 2, 7, 0, 0, DateTimeKind.Utc), UtcMinus0500, false, Est, UtcMinus0400, true, Edt),
        (TzDataTimezone.America.New_York, new DateTime(2006, 10, 29, 6, 0, 0, DateTimeKind.Utc), UtcMinus0400, true, Edt, UtcMinus0500, false, Est),
        (TzDataTimezone.America.New_York, NewYorkDaylightStart2007, UtcMinus0500, false, Est, UtcMinus0400, true, Edt),
        (TzDataTimezone.America.New_York, new DateTime(2007, 11, 4, 6, 0, 0, DateTimeKind.Utc), UtcMinus0400, true, Edt, UtcMinus0500, false, Est),
        // Europe/London: British Standard Time, +1 as standard time all year from 1968 to 1971.
        (TzDataTimezone.Europe.London, new DateTime(1968, 2, 18, 2, 0, 0, DateTimeKind.Utc), UtcZero, false, Gmt, UtcPlus0100, true, Bst),
        (TzDataTimezone.Europe.London, BritishStandardTimeStart, UtcPlus0100, true, Bst, UtcPlus0100, false, Bst),
        (TzDataTimezone.Europe.London, BritishStandardTimeEnd, UtcPlus0100, false, Bst, UtcZero, false, Gmt),
        // Europe/Dublin: negative daylight saving, so that summer time is standard time.
        (TzDataTimezone.Europe.Dublin, new DateTime(2025, 3, 30, 1, 0, 0, DateTimeKind.Utc), UtcZero, true, Gmt, UtcPlus0100, false, Ist),
        (TzDataTimezone.Europe.Dublin, new DateTime(2025, 10, 26, 1, 0, 0, DateTimeKind.Utc), UtcPlus0100, false, Ist, UtcZero, true, Gmt),
        // Australia/Lord_Howe: 30 minutes of daylight saving.
        (TzDataTimezone.Australia.Lord_Howe, new DateTime(2025, 4, 5, 15, 0, 0, DateTimeKind.Utc), UtcPlus1100, true, Plus11, UtcPlus1030, false, Plus1030),
        (TzDataTimezone.Australia.Lord_Howe, new DateTime(2025, 10, 4, 15, 30, 0, DateTimeKind.Utc), UtcPlus1030, false, Plus1030, UtcPlus1100, true, Plus11),
        // Antarctica/Troll: two hours of daylight saving.
        (TzDataTimezone.Antarctica.Troll, new DateTime(2025, 3, 30, 1, 0, 0, DateTimeKind.Utc), UtcZero, false, Plus00, UtcPlus0200, true, Plus02),
        (TzDataTimezone.Antarctica.Troll, new DateTime(2025, 10, 26, 1, 0, 0, DateTimeKind.Utc), UtcPlus0200, true, Plus02, UtcZero, false, Plus00),
        // Africa/Casablanca: negative daylight saving for Ramadan, then permanent +00 from September 2026.
        (TzDataTimezone.Africa.Casablanca, new DateTime(2026, 2, 15, 2, 0, 0, DateTimeKind.Utc), UtcPlus0100, false, Plus01, UtcZero, true, Plus00),
        (TzDataTimezone.Africa.Casablanca, new DateTime(2026, 3, 22, 2, 0, 0, DateTimeKind.Utc), UtcZero, true, Plus00, UtcPlus0100, false, Plus01),
        (TzDataTimezone.Africa.Casablanca, new DateTime(2026, 9, 20, 1, 0, 0, DateTimeKind.Utc), UtcPlus0100, false, Plus01, UtcZero, false, Plus00),
        // Pacific/Apia: the move across the date line, which skipped December 30, 2011.
        (TzDataTimezone.Pacific.Apia, ApiaDateLineChange, UtcMinus1000, true, Minus10, UtcPlus1400, true, Plus14),
        // America/Sao_Paulo: the last transition before daylight saving was abolished in 2019.
        (TzDataTimezone.America.Sao_Paulo, new DateTime(2019, 2, 17, 2, 0, 0, DateTimeKind.Utc), UtcMinus0200, true, Minus02, UtcMinus0300, false, Minus03),
        // Asia/Kolkata: the last transition, after which +05:30 applies all year.
        (TzDataTimezone.Asia.Kolkata, new DateTime(1945, 10, 14, 17, 30, 0, DateTimeKind.Utc), UtcPlus0630, true, Plus0630, UtcPlus0530, false, Ist),
        // America/New_York: beyond the last year of explicit data, from the rules that apply indefinitely.
        (TzDataTimezone.America.New_York, new DateTime(2149, 3, 9, 7, 0, 0, DateTimeKind.Utc), UtcMinus0500, false, Est, UtcMinus0400, true, Edt),
        (TzDataTimezone.America.New_York, new DateTime(2149, 11, 2, 6, 0, 0, DateTimeKind.Utc), UtcMinus0400, true, Edt, UtcMinus0500, false, Est),
    ];

    // The timezone and instant, then the offset from universal time, the standard offset, the daylight saving, whether daylight saving time is in effect and the abbreviation.
    private static readonly (TzDataTimezone Timezone, DateTime Instant, TimeSpan UtcOffset, TimeSpan StandardOffset, TimeSpan DaylightSavings, bool IsDaylightSavingTime, string Abbreviation)[] OffsetComponentCases =
    [
        (TzDataTimezone.America.New_York, new DateTime(1880, 1, 1, 0, 0, 0, DateTimeKind.Utc), UtcMinus045602, UtcMinus045602, UtcZero, false, Lmt),
        (TzDataTimezone.America.New_York, new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc), UtcMinus0500, UtcMinus0500, UtcZero, false, Est),
        (TzDataTimezone.America.New_York, July2025, UtcMinus0400, UtcMinus0500, UtcPlus0100, true, Edt),
        (TzDataTimezone.Europe.London, StartOf1970, UtcPlus0100, UtcPlus0100, UtcZero, false, Bst),
        (TzDataTimezone.Europe.London, July2025, UtcPlus0100, UtcZero, UtcPlus0100, true, Bst),
        (TzDataTimezone.Europe.Dublin, new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc), UtcZero, UtcPlus0100, UtcMinus0100, true, Gmt),
        (TzDataTimezone.Europe.Dublin, July2025, UtcPlus0100, UtcPlus0100, UtcZero, false, Ist),
        (TzDataTimezone.Australia.Lord_Howe, new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc), UtcPlus1100, UtcPlus1030, UtcPlus0030, true, Plus11),
        (TzDataTimezone.Antarctica.Troll, July2025, UtcPlus0200, UtcZero, UtcPlus0200, true, Plus02),
        (TzDataTimezone.Africa.Casablanca, new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc), UtcZero, UtcPlus0100, UtcMinus0100, true, Plus00),
        (TzDataTimezone.Africa.Casablanca, new DateTime(2027, 3, 1, 12, 0, 0, DateTimeKind.Utc), UtcZero, UtcZero, UtcZero, false, Plus00),
        (TzDataTimezone.Pacific.Apia, new DateTime(2011, 12, 31, 12, 0, 0, DateTimeKind.Utc), UtcPlus1400, UtcPlus1300, UtcPlus0100, true, Plus14),
        (TzDataTimezone.America.Sao_Paulo, new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc), UtcMinus0300, UtcMinus0300, UtcZero, false, Minus03),
        (TzDataTimezone.Asia.Kolkata, July2025, UtcPlus0530, UtcPlus0530, UtcZero, false, Ist),
    ];

    // America/New_York beyond the last year of explicit data: the US rules from 2007 move clocks forward on the second Sunday in March,
    // and back on the first Sunday in November, both at 2:00 wall clock time, which is 07:00 and 06:00 universal time.
    private static readonly (int Year, DateTime DaylightStart, DateTime DaylightEnd)[] IndefiniteRuleYearCases =
    [
        (2150, new DateTime(2150, 3, 8, 7, 0, 0, DateTimeKind.Utc), new DateTime(2150, 11, 1, 6, 0, 0, DateTimeKind.Utc)),
        (2500, new DateTime(2500, 3, 14, 7, 0, 0, DateTimeKind.Utc), new DateTime(2500, 11, 7, 6, 0, 0, DateTimeKind.Utc)),
        (5000, new DateTime(5000, 3, 9, 7, 0, 0, DateTimeKind.Utc), new DateTime(5000, 11, 2, 6, 0, 0, DateTimeKind.Utc)),
        (9998, new DateTime(9998, 3, 8, 7, 0, 0, DateTimeKind.Utc), new DateTime(9998, 11, 1, 6, 0, 0, DateTimeKind.Utc)),
    ];

    #region Transitions

    public static TheoryData<TzDataTimezone, DateTime, TimeSpan, bool, string, DateTime> JustBeforeTransition_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, TimeSpan, bool, string, DateTime>();
            foreach (var testCase in KnownTransitionCases)
                testCases.Add(testCase.Timezone, testCase.Instant.AddTicks(-1), testCase.BeforeOffset, testCase.BeforeIsDaylightSavingTime, testCase.BeforeAbbreviation, testCase.Instant);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, TimeSpan, bool, string> AtTransition_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, TimeSpan, bool, string>();
            foreach (var testCase in KnownTransitionCases)
                testCases.Add(testCase.Timezone, testCase.Instant, testCase.AfterOffset, testCase.AfterIsDaylightSavingTime, testCase.AfterAbbreviation);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, DateTime, DateTime, TimeSpan, bool, string, TimeSpan, bool, string> DayEitherSideOfTransition_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, DateTime, DateTime, TimeSpan, bool, string, TimeSpan, bool, string>();
            foreach (var testCase in KnownTransitionCases)
                testCases.Add(testCase.Timezone, testCase.Instant.AddDays(-1), testCase.Instant.AddDays(1), testCase.Instant, testCase.BeforeOffset, testCase.BeforeIsDaylightSavingTime, testCase.BeforeAbbreviation, testCase.AfterOffset, testCase.AfterIsDaylightSavingTime, testCase.AfterAbbreviation);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, DateTime, DateTime> PeriodStartingAtTransition_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, DateTime, DateTime>();
            foreach (var testCase in KnownTransitionCases)
                testCases.Add(testCase.Timezone, testCase.Instant, testCase.Instant.AddTicks(1), testCase.Instant);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, DateTime> PeriodEndingAtTransition_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, DateTime>();
            foreach (var testCase in KnownTransitionCases)
                testCases.Add(testCase.Timezone, testCase.Instant.AddDays(-1), testCase.Instant);

            return testCases;
        }
    }

    [Theory]
    [MemberData(nameof(JustBeforeTransition_TestCases))]
    public void GetOffsetInfo_JustBeforeTransition_ReturnsOldOffsetUntilTransition(TzDataTimezone testTimezone, DateTime testInstant, TimeSpan expectedOffset, bool expectedIsDaylightSavingTime, string expectedAbbreviation, DateTime expectedValidUntilUtc)
    {
        // act
        var testResult = testTimezone.GetOffsetInfo(testInstant);

        // assert
        testResult.UtcOffset.Should().Be(expectedOffset, "{0} is at {1} until {2:O}", testTimezone, expectedAbbreviation, expectedValidUntilUtc);
        testResult.IsDaylightSavingTime.Should().Be(expectedIsDaylightSavingTime);
        testResult.Abbreviation.Should().Be(expectedAbbreviation);
        testResult.ValidUntilUtc.Should().Be(expectedValidUntilUtc, "the old offset ceases at the transition");
    }

    [Theory]
    [MemberData(nameof(AtTransition_TestCases))]
    public void GetOffsetInfo_AtTransition_ReturnsNewOffsetFromTransition(TzDataTimezone testTimezone, DateTime testInstant, TimeSpan expectedOffset, bool expectedIsDaylightSavingTime, string expectedAbbreviation)
    {
        // act
        var testResult = testTimezone.GetOffsetInfo(testInstant);

        // assert
        testResult.UtcOffset.Should().Be(expectedOffset, "{0} changes to {1} at {2:O}", testTimezone, expectedAbbreviation, testInstant);
        testResult.IsDaylightSavingTime.Should().Be(expectedIsDaylightSavingTime);
        testResult.Abbreviation.Should().Be(expectedAbbreviation);
        testResult.ValidFromUtc.Should().Be(testInstant, "the new offset takes effect at the transition");
    }

    [Theory]
    [MemberData(nameof(DayEitherSideOfTransition_TestCases))]
    public void GetTransitions_DayEitherSideOfTransition_ReturnsTransition(TzDataTimezone testTimezone, DateTime testFrom, DateTime testTo, DateTime expectedInstant, TimeSpan expectedBeforeOffset, bool expectedBeforeIsDaylightSavingTime, string expectedBeforeAbbreviation, TimeSpan expectedAfterOffset, bool expectedAfterIsDaylightSavingTime, string expectedAfterAbbreviation)
    {
        // act
        var testResult = testTimezone.GetTransitions(testFrom, testTo).ToList();

        // assert
        var testTransition = testResult.Should().ContainSingle("{0} has one transition within a day of {1:O}", testTimezone, expectedInstant).Which;
        testTransition.InstantUtc.Should().Be(expectedInstant);
        testTransition.InstantUtc.Kind.Should().Be(DateTimeKind.Utc);
        testTransition.Before.UtcOffset.Should().Be(expectedBeforeOffset);
        testTransition.Before.IsDaylightSavingTime.Should().Be(expectedBeforeIsDaylightSavingTime);
        testTransition.Before.Abbreviation.Should().Be(expectedBeforeAbbreviation);
        testTransition.Before.ValidUntilUtc.Should().Be(expectedInstant);
        testTransition.After.UtcOffset.Should().Be(expectedAfterOffset);
        testTransition.After.IsDaylightSavingTime.Should().Be(expectedAfterIsDaylightSavingTime);
        testTransition.After.Abbreviation.Should().Be(expectedAfterAbbreviation);
        testTransition.After.ValidFromUtc.Should().Be(expectedInstant);
    }

    [Theory]
    [MemberData(nameof(PeriodStartingAtTransition_TestCases))]
    public void GetTransitions_PeriodStartingAtTransition_IncludesTransition(TzDataTimezone testTimezone, DateTime testFrom, DateTime testTo, DateTime expectedInstant)
    {
        // act
        var testResult = testTimezone.GetTransitions(testFrom, testTo).ToList();

        // assert
        testResult.Should().ContainSingle("the start of the period is included").Which.InstantUtc.Should().Be(expectedInstant);
    }

    [Theory]
    [MemberData(nameof(PeriodEndingAtTransition_TestCases))]
    public void GetTransitions_PeriodEndingAtTransition_ExcludesTransition(TzDataTimezone testTimezone, DateTime testFrom, DateTime testTo)
    {
        // act
        var testResult = testTimezone.GetTransitions(testFrom, testTo).ToList();

        // assert
        testResult.Should().BeEmpty("the end of the period is excluded");
    }

    public static TheoryData<TzDataTimezone, DateTime> AfterLastTransition_TestCases => new()
    {
        { TzDataTimezone.America.Sao_Paulo, new DateTime(2019, 2, 17, 2, 0, 1, DateTimeKind.Utc) },
        { TzDataTimezone.Asia.Kolkata, new DateTime(1945, 10, 14, 17, 30, 1, DateTimeKind.Utc) },
        { TzDataTimezone.Africa.Casablanca, new DateTime(2026, 9, 20, 1, 0, 1, DateTimeKind.Utc) },
    };

    [Theory]
    [MemberData(nameof(AfterLastTransition_TestCases))]
    public void GetTransitions_AfterLastTransition_ReturnsNone(TzDataTimezone testTimezone, DateTime testFrom)
    {
        // act
        var testResult = testTimezone.GetTransitions(testFrom, DateTime.MaxValue);

        // assert
        testResult.Should().BeEmpty("{0} has no transitions after {1:O}", testTimezone, testFrom);
    }

    public static TheoryData<TzDataTimezone, DateTime, DateTime, DateTime, DateTime> IndefiniteRuleYear_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, DateTime, DateTime, DateTime>();
            foreach (var testCase in IndefiniteRuleYearCases)
                testCases.Add(TzDataTimezone.America.New_York, new DateTime(testCase.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(testCase.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc), testCase.DaylightStart, testCase.DaylightEnd);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime> IndefiniteRuleSummer_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime>();
            foreach (var testCase in IndefiniteRuleYearCases)
                testCases.Add(TzDataTimezone.America.New_York, new DateTime(testCase.Year, 7, 1, 0, 0, 0, DateTimeKind.Utc));

            return testCases;
        }
    }

    [Theory]
    [MemberData(nameof(IndefiniteRuleYear_TestCases))]
    public void GetTransitions_YearBeyondExplicitData_ReturnsTransitionsOfIndefiniteRules(TzDataTimezone testTimezone, DateTime testFrom, DateTime testTo, DateTime expectedDaylightStart, DateTime expectedDaylightEnd)
    {
        // act
        var testResult = testTimezone.GetTransitions(testFrom, testTo).ToList();

        // assert
        testResult.Should().HaveCount(2);
        testResult[0].InstantUtc.Should().Be(expectedDaylightStart);
        testResult[0].After.Abbreviation.Should().Be(Edt);
        testResult[1].InstantUtc.Should().Be(expectedDaylightEnd);
        testResult[1].After.Abbreviation.Should().Be(Est);
    }

    [Theory]
    [MemberData(nameof(IndefiniteRuleSummer_TestCases))]
    public void GetUtcOffset_SummerBeyondExplicitData_ReturnsDaylightSavingTimeOffset(TzDataTimezone testTimezone, DateTime testInstant)
    {
        // act
        var testResult = testTimezone.GetUtcOffset(testInstant);

        // assert
        testResult.Should().Be(UtcMinus0400);
    }

    #endregion

    #region Every timezone

    public static TheoryData<TzDataTimezone> EveryTimezone_TestCases => TimezoneFieldCatalog.CreateTimezoneTestCases();

    [Theory]
    [MemberData(nameof(EveryTimezone_TestCases), DisableDiscoveryEnumeration = true)]
    public void GetTransitions_WholeRange_EachTransitionStartsFromOffsetThePreviousOneEndedWith(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.GetTransitions(DateTime.MinValue, DateTime.MaxValue).ToList();

        // assert
        testResult.Skip(1).Select(transition => transition.Before).Should().Equal(testResult.SkipLast(1).Select(transition => transition.After), "each transition of {0} follows the offset before it", testTimezone);
    }

    [Theory]
    [MemberData(nameof(EveryTimezone_TestCases), DisableDiscoveryEnumeration = true)]
    public void GetTransitions_WholeRange_EachTransitionEndsOneOffsetAndStartsTheNext(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.GetTransitions(DateTime.MinValue, DateTime.MaxValue).ToList();

        // assert
        testResult.Should().OnlyContain(transition => transition.Before.ValidUntilUtc == transition.InstantUtc && transition.After.ValidFromUtc == transition.InstantUtc, "each offset of {0} changes at the instant of the transition", testTimezone);
    }

    [Theory]
    [MemberData(nameof(EveryTimezone_TestCases), DisableDiscoveryEnumeration = true)]
    public void GetOffsetInfo_MinimumInstant_AppliesFromBeginning(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.GetOffsetInfo(DateTime.MinValue);

        // assert
        testResult.ValidFromUtc.Should().BeNull("the first offset of {0} has no start", testTimezone);
    }

    [Theory]
    [MemberData(nameof(EveryTimezone_TestCases), DisableDiscoveryEnumeration = true)]
    public void GetOffsetInfo_MaximumInstant_AppliesIndefinitely(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.GetOffsetInfo(DateTime.MaxValue);

        // assert
        testResult.ValidUntilUtc.Should().BeNull("the offset after the last transition of {0} applies indefinitely", testTimezone);
    }

    #endregion

    #region Zone lines

    [Fact]
    public void GetZoneLine_JustBeforeZoneLineChange_ReturnsLocalMeanTimeZoneLine()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.GetZoneLine(JustBeforeNewYorkStandardTimeStart);

        // assert
        testResult.StandardOffset.Should().Be(UtcMinus045602, "local mean time applies until 17:00 universal time");
        testResult.Format.Should().Be(Lmt);
    }

    [Fact]
    public void GetZoneLine_AtZoneLineChange_ReturnsEasternStandardTimeZoneLine()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.GetZoneLine(NewYorkStandardTimeStart);

        // assert
        testResult.StandardOffset.Should().Be(UtcMinus0500, "Eastern Standard Time applies from 17:00 universal time");
        testResult.Format.Should().Be(EasternFormat);
    }

    [Fact]
    public void GetZoneLine_JustBeforeDateLineChange_ReturnsZoneLineWestOfDateLine()
    {
        // act
        var testResult = TzDataTimezone.Pacific.Apia.GetZoneLine(JustBeforeApiaDateLineChange);

        // assert
        testResult.StandardOffset.Should().Be(UtcMinus1100);
    }

    [Fact]
    public void GetZoneLine_AtDateLineChange_ReturnsLastZoneLine()
    {
        // act
        var testResult = TzDataTimezone.Pacific.Apia.GetZoneLine(ApiaDateLineChange);

        // assert
        testResult.StandardOffset.Should().Be(UtcPlus1300);
        testResult.Until.Should().BeNull("the zone line east of the date line is the last");
    }

    [Fact]
    public void GetZoneLine_MinimumInstant_ReturnsFirstZoneLine()
    {
        // act
        var testResult = TzDataTimezone.Europe.London.GetZoneLine(DateTime.MinValue);

        // assert
        // Europe/London kept local mean time, -0:01:15, until 1847.
        testResult.StandardOffset.Should().Be(new TimeSpan(0, -1, -15));
        testResult.Format.Should().Be(Lmt);
    }

    [Fact]
    public void GetZoneLine_MaximumInstant_ReturnsLastZoneLine()
    {
        // act
        var testResult = TzDataTimezone.Europe.London.GetZoneLine(DateTime.MaxValue);

        // assert
        testResult.Format.Should().Be(LondonFormat);
        testResult.Until.Should().BeNull();
    }

    #endregion

    #region Kinds and offsets of the instant

    [Fact]
    public void GetOffsetInfo_UnspecifiedKind_TreatedAsUniversalTime()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.GetOffsetInfo(NewYorkDaylightStart2007Unspecified);

        // assert
        testResult.UtcOffset.Should().Be(UtcMinus0400);
        testResult.ValidFromUtc.Should().Be(NewYorkDaylightStart2007);
    }

    [Fact]
    public void GetTransitions_UnspecifiedKind_TreatedAsUniversalTime()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.GetTransitions(NewYorkDaylightStart2007Unspecified, JustAfterNewYorkDaylightStart2007Unspecified);

        // assert
        testResult.Should().ContainSingle().Which.InstantUtc.Should().Be(NewYorkDaylightStart2007);
    }

    [Fact]
    public void GetZoneLine_DateTimeOffset_UsesInstant()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.GetZoneLine(NewYorkDaylightStart2007InKolkata);

        // assert
        testResult.StandardOffset.Should().Be(UtcMinus0500);
        testResult.Format.Should().Be(EasternFormat);
        testResult.Until.Should().BeNull("the zone line from 1967 is the last");
    }

    [Fact]
    public void GetUtcOffset_DateTimeOffset_UsesInstant()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.GetUtcOffset(NewYorkDaylightStart2007InKolkata);

        // assert
        testResult.Should().Be(UtcMinus0400);
    }

    [Fact]
    public void GetStandardOffset_DateTimeOffset_UsesInstant()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.GetStandardOffset(NewYorkDaylightStart2007InKolkata);

        // assert
        testResult.Should().Be(UtcMinus0500);
    }

    [Fact]
    public void GetDaylightSavings_DateTimeOffset_UsesInstant()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.GetDaylightSavings(NewYorkDaylightStart2007InKolkata);

        // assert
        testResult.Should().Be(UtcPlus0100);
    }

    [Fact]
    public void IsDaylightSavingTime_DateTimeOffset_UsesInstant()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.IsDaylightSavingTime(NewYorkDaylightStart2007InKolkata);

        // assert
        testResult.Should().BeTrue();
    }

    [Fact]
    public void GetOffsetInfo_DateTimeOffset_UsesInstant()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.GetOffsetInfo(NewYorkDaylightStart2007InKolkata);

        // assert
        testResult.UtcOffset.Should().Be(UtcMinus0400);
        testResult.ValidFromUtc.Should().Be(NewYorkDaylightStart2007);
    }

    [Fact]
    public void GetOffsetInfo_DateTimeOffsetJustBeforeTransition_UsesInstant()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.GetOffsetInfo(JustBeforeNewYorkDaylightStart2007InKolkata);

        // assert
        testResult.UtcOffset.Should().Be(UtcMinus0500);
        testResult.ValidUntilUtc.Should().Be(NewYorkDaylightStart2007);
    }

    [Fact]
    public void GetTransitions_DateTimeOffset_UsesInstant()
    {
        // act
        var testResult = TzDataTimezone.America.New_York.GetTransitions(NewYorkDaylightStart2007InKolkata, JustAfterNewYorkDaylightStart2007InKolkata);

        // assert
        testResult.Should().ContainSingle().Which.InstantUtc.Should().Be(NewYorkDaylightStart2007);
    }

    #endregion

    #region Offset components

    public static TheoryData<TzDataTimezone, DateTime, TimeSpan> UtcOffset_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, TimeSpan>();
            foreach (var testCase in OffsetComponentCases)
                testCases.Add(testCase.Timezone, testCase.Instant, testCase.UtcOffset);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, TimeSpan> StandardOffset_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, TimeSpan>();
            foreach (var testCase in OffsetComponentCases)
                testCases.Add(testCase.Timezone, testCase.Instant, testCase.StandardOffset);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, TimeSpan> DaylightSavings_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, TimeSpan>();
            foreach (var testCase in OffsetComponentCases)
                testCases.Add(testCase.Timezone, testCase.Instant, testCase.DaylightSavings);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, bool> IsDaylightSavingTime_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, bool>();
            foreach (var testCase in OffsetComponentCases)
                testCases.Add(testCase.Timezone, testCase.Instant, testCase.IsDaylightSavingTime);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, DateTime, string> OffsetInfoAbbreviation_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, DateTime, string>();
            foreach (var testCase in OffsetComponentCases)
                testCases.Add(testCase.Timezone, testCase.Instant, testCase.Abbreviation);

            return testCases;
        }
    }

    [Theory]
    [MemberData(nameof(UtcOffset_TestCases))]
    public void GetUtcOffset_Instant_ReturnsOffsetFromUniversalTime(TzDataTimezone testTimezone, DateTime testInstant, TimeSpan expectedResult)
    {
        // act
        var testResult = testTimezone.GetUtcOffset(testInstant);

        // assert
        testResult.Should().Be(expectedResult, "the offset of {0} at {1:O} is the sum of its standard offset and daylight saving", testTimezone, testInstant);
    }

    [Theory]
    [MemberData(nameof(StandardOffset_TestCases))]
    public void GetStandardOffset_Instant_ReturnsStandardOffset(TzDataTimezone testTimezone, DateTime testInstant, TimeSpan expectedResult)
    {
        // act
        var testResult = testTimezone.GetStandardOffset(testInstant);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(DaylightSavings_TestCases))]
    public void GetDaylightSavings_Instant_ReturnsDaylightSaving(TzDataTimezone testTimezone, DateTime testInstant, TimeSpan expectedResult)
    {
        // act
        var testResult = testTimezone.GetDaylightSavings(testInstant);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(IsDaylightSavingTime_TestCases))]
    public void IsDaylightSavingTime_Instant_ReturnsWhetherDaylightSavingTimeIsInEffect(TzDataTimezone testTimezone, DateTime testInstant, bool expectedResult)
    {
        // act
        var testResult = testTimezone.IsDaylightSavingTime(testInstant);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(OffsetInfoAbbreviation_TestCases))]
    public void GetOffsetInfo_Instant_ReturnsAbbreviation(TzDataTimezone testTimezone, DateTime testInstant, string expectedResult)
    {
        // act
        var testResult = testTimezone.GetOffsetInfo(testInstant);

        // assert
        testResult.Abbreviation.Should().Be(expectedResult);
    }

    [Fact]
    public void GetOffsetInfo_CastInstance_ReturnsOffsetInForce()
    {
        // arrange
        var testTimezone = (TzDataTimezone)"Europe/London";

        // act
        var testResult = testTimezone.GetOffsetInfo(StartOf1970);

        // assert
        testResult.UtcOffset.Should().Be(UtcPlus0100);
        testResult.StandardOffset.Should().Be(UtcPlus0100);
        testResult.IsDaylightSavingTime.Should().BeFalse();
        testResult.Abbreviation.Should().Be(Bst);
        testResult.ValidFromUtc.Should().Be(BritishStandardTimeStart);
        testResult.ValidUntilUtc.Should().Be(BritishStandardTimeEnd);
    }

    #endregion

    #region Invalid arguments

    public static TheoryData<string, Action<TzDataTimezone, DateTime>> OffsetMethod_TestCases => new()
    {
        { nameof(TzDataExtensions.GetZoneLine), (timezone, utc) => timezone.GetZoneLine(utc) },
        { nameof(TzDataExtensions.GetUtcOffset), (timezone, utc) => timezone.GetUtcOffset(utc) },
        { nameof(TzDataExtensions.GetStandardOffset), (timezone, utc) => timezone.GetStandardOffset(utc) },
        { nameof(TzDataExtensions.GetDaylightSavings), (timezone, utc) => timezone.GetDaylightSavings(utc) },
        { nameof(TzDataExtensions.IsDaylightSavingTime), (timezone, utc) => timezone.IsDaylightSavingTime(utc) },
        { nameof(TzDataExtensions.GetOffsetInfo), (timezone, utc) => timezone.GetOffsetInfo(utc) },
        { nameof(TzDataExtensions.GetAbbreviation), (timezone, utc) => timezone.GetAbbreviation(utc) },
        { nameof(TzDataExtensions.GetStandardAbbreviation), (timezone, utc) => timezone.GetStandardAbbreviation(utc) },
        { nameof(TzDataExtensions.GetDaylightAbbreviation), (timezone, utc) => timezone.GetDaylightAbbreviation(utc) },
        { $"{nameof(TzDataExtensions.GetTransitions)}(from)", (timezone, utc) => timezone.GetTransitions(utc, DateTime.MaxValue) },
        { $"{nameof(TzDataExtensions.GetTransitions)}(to)", (timezone, utc) => timezone.GetTransitions(DateTime.MinValue, utc) },
    };

    public static TheoryData<string, Action<TzDataTimezone, DateTimeOffset>> OffsetMethodWithDateTimeOffset_TestCases => new()
    {
        { nameof(TzDataExtensions.GetZoneLine), (timezone, instant) => timezone.GetZoneLine(instant) },
        { nameof(TzDataExtensions.GetUtcOffset), (timezone, instant) => timezone.GetUtcOffset(instant) },
        { nameof(TzDataExtensions.GetStandardOffset), (timezone, instant) => timezone.GetStandardOffset(instant) },
        { nameof(TzDataExtensions.GetDaylightSavings), (timezone, instant) => timezone.GetDaylightSavings(instant) },
        { nameof(TzDataExtensions.IsDaylightSavingTime), (timezone, instant) => timezone.IsDaylightSavingTime(instant) },
        { nameof(TzDataExtensions.GetOffsetInfo), (timezone, instant) => timezone.GetOffsetInfo(instant) },
        { nameof(TzDataExtensions.GetAbbreviation), (timezone, instant) => timezone.GetAbbreviation(instant) },
        { nameof(TzDataExtensions.GetStandardAbbreviation), (timezone, instant) => timezone.GetStandardAbbreviation(instant) },
        { nameof(TzDataExtensions.GetDaylightAbbreviation), (timezone, instant) => timezone.GetDaylightAbbreviation(instant) },
        { nameof(TzDataExtensions.GetTransitions), (timezone, instant) => timezone.GetTransitions(instant, DateTimeOffset.MaxValue) },
    };

    [Theory]
    [MemberData(nameof(OffsetMethod_TestCases))]
    public void OffsetMethod_LocalKind_ThrowsArgumentException(string testMethod, Action<TzDataTimezone, DateTime> testMethodAction)
    {
        // arrange
        var testLocal = DateTime.SpecifyKind(July2025, DateTimeKind.Local);

        // act
        Action testAction = () => testMethodAction(TzDataTimezone.Europe.London, testLocal);

        // assert
        testAction.Should().Throw<ArgumentException>("{0} accepts only universal times", testMethod);
    }

    [Theory]
    [MemberData(nameof(OffsetMethod_TestCases))]
    public void OffsetMethod_DefaultTimezone_ThrowsInvalidOperationException(string testMethod, Action<TzDataTimezone, DateTime> testMethodAction)
    {
        // act
        Action testAction = () => testMethodAction(default, July2025);

        // assert
        testAction.Should().Throw<InvalidOperationException>("{0} needs a timezone with an identifier", testMethod);
    }

    [Theory]
    [MemberData(nameof(OffsetMethod_TestCases))]
    public void OffsetMethod_UnknownTimezone_ThrowsInvalidOperationException(string testMethod, Action<TzDataTimezone, DateTime> testMethodAction)
    {
        // act
        Action testAction = () => testMethodAction(UnknownTimezone, July2025);

        // assert
        testAction.Should().Throw<InvalidOperationException>("{0} needs a known timezone", testMethod);
    }

    [Theory]
    [MemberData(nameof(OffsetMethodWithDateTimeOffset_TestCases))]
    public void OffsetMethodWithDateTimeOffset_DefaultTimezone_ThrowsInvalidOperationException(string testMethod, Action<TzDataTimezone, DateTimeOffset> testMethodAction)
    {
        // arrange
        var testInstant = new DateTimeOffset(July2025);

        // act
        Action testAction = () => testMethodAction(default, testInstant);

        // assert
        testAction.Should().Throw<InvalidOperationException>("{0} needs a timezone with an identifier", testMethod);
    }

    [Theory]
    [MemberData(nameof(OffsetMethodWithDateTimeOffset_TestCases))]
    public void OffsetMethodWithDateTimeOffset_UnknownTimezone_ThrowsInvalidOperationException(string testMethod, Action<TzDataTimezone, DateTimeOffset> testMethodAction)
    {
        // arrange
        var testInstant = new DateTimeOffset(July2025);

        // act
        Action testAction = () => testMethodAction(UnknownTimezone, testInstant);

        // assert
        testAction.Should().Throw<InvalidOperationException>("{0} needs a known timezone", testMethod);
    }

    [Fact]
    public void GetTransitions_EndPrecedesStart_ThrowsArgumentOutOfRangeException()
    {
        // act
        Action testAction = () => TzDataTimezone.Europe.London.GetTransitions(StartOfJuly2025, JustBeforeJuly2025);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>("the end of the period must not precede its start");
    }

    #endregion

    public static TheoryData<string, TimeSpan, bool, string?, string> FormatAbbreviation_TestCases => new()
    {
        { NumericFormat, TimeSpan.FromHours(4), false, null, Plus04 },
        { NumericFormat, UtcPlus0530, false, null, "+0530" },
        { NumericFormat, new TimeSpan(-3, -30, 0), false, null, "-0330" },
        { NumericFormat, UtcMinus045602, false, null, "-045602" },
        { NumericFormat, UtcZero, false, null, Plus00 },
        { EasternFormat, UtcMinus0400, true, "D", Edt },
        { EasternFormat, UtcMinus0500, false, "S", Est },
        { LetterFormat, UtcZero, false, Gmt, Gmt },
        { IrishFormat, UtcZero, true, string.Empty, Gmt },
        { IrishFormat, UtcPlus0100, false, string.Empty, Ist },
        { Lmt, UtcMinus045602, false, null, Lmt },
    };

    [Theory]
    [MemberData(nameof(FormatAbbreviation_TestCases))]
    public void FormatAbbreviation_Format_ReturnsAbbreviation(string testFormat, TimeSpan testOffset, bool testIsDaylightSavingTime, string? testLetter, string expectedResult)
    {
        // act
        var testResult = TzDataZoneCalculator.FormatAbbreviation(testFormat, testLetter, testIsDaylightSavingTime, testOffset);

        // assert
        testResult.Should().Be(expectedResult);
    }
}
