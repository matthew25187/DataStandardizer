using FluentAssertions;
using static DataStandardizer.Chronology.Tests.TzOffsets;

namespace DataStandardizer.Chronology.Tests;

public class TzDataTimezoneZoneLinesTests
{
    // zone1970.tab of TZ Database 2026e lists more than this number of timezones.
    private const int MinimumZone1970TimezoneCount = 300;

    // The FORMAT column of the last Zone line of Europe/London in TZ Database 2026e.
    private const string LondonFormat = "GMT/BST";

    // The EU rule for the start of summer time in TZ Database 2026e: Rule EU 1981 max - Mar lastSun 1:00u 1:00 S
    private const int EuropeanSummerTimeMonth = 3;

    public static TheoryData<string> TimezoneField_TestCases => TimezoneFieldCatalog.CreateIdentifierTestCases();

    [Fact]
    public void Timezones_PublicFieldsOfTzDataTimezone_IncludeEveryTimezoneOfZone1970Tab()
    {
        // act
        var testResult = TimezoneFieldCatalog.Timezones;

        // assert
        testResult.Should().HaveCountGreaterThan(MinimumZone1970TimezoneCount, "zone1970.tab lists more than 300 timezones");
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void ZoneLines_TimezoneField_HasAtLeastOneZoneLine(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFieldCatalog.Timezones[testIdentifier];

        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.Should().NotBeEmpty("every timezone has a Zone entry of at least one line");
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void ZoneLines_TimezoneField_HasOneZoneLineWithoutUntil(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFieldCatalog.Timezones[testIdentifier];

        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.Should().ContainSingle(zoneLine => zoneLine.Until == null, "only one zone line of {0} applies indefinitely", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void ZoneLines_TimezoneField_LastZoneLineHasNoUntil(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFieldCatalog.Timezones[testIdentifier];

        // act
        var testResult = testTimezone.ZoneLines.Last();

        // assert
        testResult.Until.Should().BeNull("the last zone line of {0} applies indefinitely", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void ZoneLines_TimezoneField_UntilYearAndMonthNeverDecrease(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFieldCatalog.Timezones[testIdentifier];

        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.SkipLast(1).Select(zoneLine => zoneLine.Until!.Value).Should().BeInAscendingOrder(until => until.Year, "the zone lines of {0} are in chronological order", testIdentifier)
            .And.ThenBeInAscendingOrder(until => until.Month);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void ZoneLines_TimezoneField_NoTwoZoneLinesHaveSameUntil(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFieldCatalog.Timezones[testIdentifier];

        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.Should().OnlyHaveUniqueItems(zoneLine => zoneLine.Until, "no zone line of {0} ceases to apply at the same moment as another", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void ZoneLines_TimezoneField_RuleSetLinesHaveRules(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFieldCatalog.Timezones[testIdentifier];

        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.Should().NotContain(zoneLine => zoneLine.RuleKind == TzDataZoneRuleKind.RuleSet && zoneLine.Rules.Count == 0, "every rule set referenced by {0} has at least one rule", testIdentifier);
    }

    [Fact]
    public void ZoneLines_EuropeLondon_HasExpectedLastZoneLine()
    {
        // act
        var testResult = TzDataTimezone.Europe.London.ZoneLines.Last();

        // assert
        testResult.StandardOffset.Should().Be(UtcZero);
        testResult.RuleKind.Should().Be(TzDataZoneRuleKind.RuleSet);
        testResult.Format.Should().Be(LondonFormat);
        testResult.Rules.Should().Contain(rule => rule.ToYear == int.MaxValue && rule.Month == EuropeanSummerTimeMonth && rule.DayKind == TzDataDayKind.LastWeekday && rule.DayOfWeek == DayOfWeek.Sunday && rule.AtTime == UtcPlus0100 && rule.AtTimeReference == TzDataTimeReference.Universal && rule.Save == UtcPlus0100 && rule.IsDaylight);
    }

    [Fact]
    public void ZoneLines_CastInstance_ReturnsSameZoneLinesAsField()
    {
        // arrange
        var testTimezone = (TzDataTimezone)"Europe/London";
        var expectedResult = TzDataTimezone.Europe.London.ZoneLines;

        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.Should().Equal(expectedResult, (castZoneLine, fieldZoneLine) => ReferenceEquals(castZoneLine, fieldZoneLine), "a cast instance has the zone lines of the field with the same identifier");
    }

    [Fact]
    public void ZoneLines_DefaultInstance_ThrowsInvalidOperationException()
    {
        // arrange
        var testTimezone = default(TzDataTimezone);

        // act
        Func<IReadOnlyList<TzDataZoneLine>> testAction = () => testTimezone.ZoneLines;

        // assert
        testAction.Should().Throw<InvalidOperationException>("the default instance has no identifier");
    }

    [Fact]
    public void ZoneLines_UnknownIdentifier_ThrowsInvalidOperationException()
    {
        // arrange
        var testTimezone = (TzDataTimezone)"Europe/Atlantis";

        // act
        Func<IReadOnlyList<TzDataZoneLine>> testAction = () => testTimezone.ZoneLines;

        // assert
        testAction.Should().Throw<InvalidOperationException>("no timezone field has the identifier");
    }

    [Fact]
    public void ZoneLines_EuropeLondon_IsNotWritableArray()
    {
        // act
        var testResult = TzDataTimezone.Europe.London.ZoneLines;

        // assert
        testResult.Should().NotBeAssignableTo<TzDataZoneLine[]>("the zone lines are shared, and must not be modified");
    }
}
