using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataTimezoneZoneLinesTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, TzDataTimezone>> TimezoneFields = new(() => GetTimezoneFields(typeof(TzDataTimezone)).ToDictionary(timezone => (string)timezone!, StringComparer.Ordinal));

    public static IEnumerable<object[]> TimezoneField_TestCases => TimezoneFields.Value.Keys.Select(identifier => new object[] { identifier });

    private static IEnumerable<TzDataTimezone> GetTimezoneFields(Type hostType)
    {
        var fields = hostType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.FieldType == typeof(TzDataTimezone))
            .Select(field => (TzDataTimezone)field.GetValue(null)!);
        var nestedFields = hostType.GetNestedTypes(BindingFlags.Public)
            .SelectMany(GetTimezoneFields);

        return fields.Concat(nestedFields);
    }

    private static DateTime GetApproximateUniversalUntil(TzDataZoneLine zoneLine)
    {
        var until = zoneLine.Until!.Value;
        var date = TzDataDayRule.Resolve(until.Year, until.Month, until.DayKind, until.Day, until.DayOfWeek);
        var offset = until.TimeReference switch
        {
            TzDataTimeReference.Universal => TimeSpan.Zero,
            TzDataTimeReference.Standard => zoneLine.StandardOffset,
            _ => zoneLine.StandardOffset + zoneLine.FixedSave.GetValueOrDefault()
        };

        return date + until.Time - offset;
    }

    [Fact]
    public void TimezoneFields_AreFound()
    {
        // act
        var testResult = TimezoneFields.Value;

        // assert
        testResult.Should().HaveCountGreaterThan(300, "zone1970.tab lists more than 300 timezones");
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases))]
    public void ZoneLines_TimezoneField_HasAtLeastOneZoneLine(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFields.Value[testIdentifier];

        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.Should().NotBeEmpty("every timezone has a Zone entry of at least one line");
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases))]
    public void ZoneLines_TimezoneField_OnlyLastLineHasNoUntil(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFields.Value[testIdentifier];

        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.Take(testResult.Count - 1).Should().OnlyContain(zoneLine => zoneLine.Until.HasValue, "every zone line of {0} but the last ceases to apply", testIdentifier);
        testResult[testResult.Count - 1].Until.Should().BeNull("the last zone line of {0} applies indefinitely", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases))]
    public void ZoneLines_TimezoneField_UntilStrictlyIncreases(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFields.Value[testIdentifier];

        // act
        var testResult = testTimezone.ZoneLines
            .Where(zoneLine => zoneLine.Until.HasValue)
            .Select(GetApproximateUniversalUntil)
            .ToList();

        // assert
        testResult.Should().BeInAscendingOrder("the zone lines of {0} are in chronological order", testIdentifier);
        testResult.Should().OnlyHaveUniqueItems("no zone line of {0} ceases to apply at the same moment as another", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases))]
    public void ZoneLines_TimezoneField_RuleSetLinesHaveRules(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFields.Value[testIdentifier];

        // act
        var testResult = testTimezone.ZoneLines.Where(zoneLine => zoneLine.RuleKind == TzDataZoneRuleKind.RuleSet && zoneLine.Rules.Count == 0);

        // assert
        testResult.Should().BeEmpty("every rule set referenced by {0} has at least one rule", testIdentifier);
    }

    [Fact]
    public void ZoneLines_EuropeLondon_HasExpectedLastZoneLine()
    {
        // act
        var testResult = TzDataTimezone.Europe.London.ZoneLines.Last();

        // assert
        testResult.StandardOffset.Should().Be(TimeSpan.Zero);
        testResult.RuleKind.Should().Be(TzDataZoneRuleKind.RuleSet);
        testResult.Format.Should().Be("GMT/BST");
        testResult.Rules.Should().Contain(rule => rule.ToYear == int.MaxValue && rule.Month == 3 && rule.DayKind == TzDataDayKind.LastWeekday && rule.DayOfWeek == DayOfWeek.Sunday && rule.AtTime == TimeSpan.FromHours(1) && rule.AtTimeReference == TzDataTimeReference.Universal && rule.Save == TimeSpan.FromHours(1) && rule.IsDaylight);
    }

    [Fact]
    public void ZoneLines_CastInstance_ReturnsSameZoneLinesAsField()
    {
        // arrange
        var testTimezone = (TzDataTimezone)"Europe/London";

        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.Should().Equal(TzDataTimezone.Europe.London.ZoneLines, (castZoneLine, fieldZoneLine) => ReferenceEquals(castZoneLine, fieldZoneLine), "a cast instance has the zone lines of the field with the same identifier");
    }

    [Fact]
    public void ZoneLines_DefaultInstance_ThrowsInvalidOperationException()
    {
        // arrange
        var testTimezone = default(TzDataTimezone);

        // act
        var testAction = () => testTimezone.ZoneLines;

        // assert
        testAction.Should().Throw<InvalidOperationException>("the default instance has no identifier");
    }

    [Fact]
    public void ZoneLines_UnknownIdentifier_ThrowsInvalidOperationException()
    {
        // arrange
        var testTimezone = (TzDataTimezone)"Europe/Atlantis";

        // act
        var testAction = () => testTimezone.ZoneLines;

        // assert
        testAction.Should().Throw<InvalidOperationException>("no timezone field has the identifier");
    }

    [Fact]
    public void ZoneLines_IsReadOnly()
    {
        // act
        var testResult = TzDataTimezone.Europe.London.ZoneLines;

        // assert
        testResult.Should().NotBeAssignableTo<TzDataZoneLine[]>("the zone lines are shared, and must not be modified");
    }

    [Fact]
    public void Registry_ConcurrentFirstUse_ReturnsSameEntriesToEveryThread()
    {
        // arrange
        const int threadCount = 8;
        var timeout = TimeSpan.FromSeconds(30);
        var testRegistry = new TzDataTimezone.Registry();
        var testResults = new Dictionary<string, TzDataTimezone.RegistryEntry>?[threadCount];
        var exceptions = new Exception?[threadCount];
        using var barrier = new Barrier(threadCount);
        var threads = Enumerable.Range(0, threadCount)
            .Select(index => new Thread(() =>
            {
                try
                {
                    if (!barrier.SignalAndWait(timeout))
                        throw new TimeoutException("The threads were not released together.");
                    testResults[index] = testRegistry.GetEntries();
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
        exceptions.Should().OnlyContain(exception => exception == null, "first use of the registry succeeds on every thread");
        testResults.Should().OnlyContain(entries => ReferenceEquals(entries, testResults[0]), "the registry is built once and shared by every thread");
        testResults[0].Should().HaveCount(TimezoneFields.Value.Count, "the registry holds every timezone field");
    }
}
