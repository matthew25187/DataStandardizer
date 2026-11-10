using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Chronology.IntegrationTests;

/// <remarks>
/// These tests run calls on several threads at once, so what they exercise depends on how the thread scheduler interleaves them.
/// Each still uses fixed inputs and compares its results exactly.
/// </remarks>
public class TzDataConcurrencyTests
{
    private const int ThreadCount = 8;

    // Each thread starts this many timezones further along than the one before it, so that calculators are built concurrently.
    private const int VisitingOrderStep = 37;

    private const string NewYorkIdentifier = "America/New_York";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    // An instant away from any transition.
    private static readonly DateTime July2025 = new(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private static IEnumerable<TzDataTimezone> GetTimezoneFields(Type hostType)
    {
        var fields = hostType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.FieldType == typeof(TzDataTimezone))
            .Select(field => (TzDataTimezone)field.GetValue(null)!);
        var nestedFields = hostType.GetNestedTypes(BindingFlags.Public)
            .SelectMany(GetTimezoneFields);

        return fields.Concat(nestedFields);
    }

    // A call of GetUtcOffset for every timezone field, and for each thread an order in which to make them.
    public static TheoryData<IReadOnlyList<Func<TimeSpan>>, IReadOnlyList<IReadOnlyList<int>>> EveryTimezoneInRotatedOrders_TestCases
    {
        get
        {
            var calls = GetTimezoneFields(typeof(TzDataTimezone))
                .Select(timezone => new Func<TimeSpan>(() => timezone.GetUtcOffset(July2025)))
                .ToList();
            var orders = new List<IReadOnlyList<int>>();
            for (var thread = 0; thread < ThreadCount; thread++)
                orders.Add(Enumerable.Range(0, calls.Count).Select(step => (step + thread * VisitingOrderStep) % calls.Count).ToList());

            return new TheoryData<IReadOnlyList<Func<TimeSpan>>, IReadOnlyList<IReadOnlyList<int>>> { { calls, orders } };
        }
    }

    public static TheoryData<int> TimezoneFieldCount_TestCases => new() { GetTimezoneFields(typeof(TzDataTimezone)).Count() };

    [Fact]
    public void CalculatorCache_ConcurrentFirstUse_ReturnsSameCalculatorToEveryThread()
    {
        // arrange
        var testCache = new TzDataZoneCalculator.Cache();
        var testZoneLines = TzDataTimezone.America.New_York.ZoneLines.ToArray();

        // act
        var testResult = ConcurrentRunner.RunOnEveryThread(ThreadCount, () => testCache.GetCalculator(NewYorkIdentifier, testZoneLines), Timeout);

        // assert
        testResult.Joined.Should().BeTrue("every thread completes within {0}", Timeout);
        testResult.Exceptions.Should().OnlyContain(exception => exception == null, "first use of the cache succeeds on every thread");
        testResult.Results[0].Should().NotBeNull();
        testResult.Results.Should().OnlyContain(calculator => ReferenceEquals(calculator, testResult.Results[0]), "the calculator is built once and shared by every thread");
    }

    [Theory]
    [MemberData(nameof(TimezoneFieldCount_TestCases))]
    public void Registry_ConcurrentFirstUse_ReturnsSameEntriesToEveryThread(int expectedEntryCount)
    {
        // arrange
        var testRegistry = new TzDataTimezone.Registry();

        // act
        var testResult = ConcurrentRunner.RunOnEveryThread(ThreadCount, testRegistry.GetEntries, Timeout);

        // assert
        testResult.Joined.Should().BeTrue("every thread completes within {0}", Timeout);
        testResult.Exceptions.Should().OnlyContain(exception => exception == null, "first use of the registry succeeds on every thread");
        testResult.Results.Should().OnlyContain(entries => ReferenceEquals(entries, testResult.Results[0]), "the registry is built once and shared by every thread");
        testResult.Results[0].Should().HaveCount(expectedEntryCount, "the registry holds every timezone field");
    }

    [Theory]
    [MemberData(nameof(EveryTimezoneInRotatedOrders_TestCases), DisableDiscoveryEnumeration = true)]
    public void OffsetMethods_ConcurrentFirstUse_ReturnSameOffsetsOnEveryThread(IReadOnlyList<Func<TimeSpan>> testCalls, IReadOnlyList<IReadOnlyList<int>> testOrders)
    {
        // act
        var testResult = ConcurrentRunner.RunInOrders(testCalls, testOrders, Timeout);

        // assert
        testResult.Joined.Should().BeTrue("every thread completes within {0}", Timeout);
        testResult.Exceptions.Should().OnlyContain(exception => exception == null, "concurrent use succeeds on every thread");
        testResult.Results.Should().AllSatisfy(results => results.Should().Equal(testResult.Results[0]), "every thread finds the same offsets");
    }
}
