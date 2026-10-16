using BenchmarkDotNet.Attributes;
using Ical.Net;
using Ical.Net.DataTypes;
using IcalNetCalendar = Ical.Net.Calendar;
using IcalNetEvent = Ical.Net.CalendarComponents.CalendarEvent;

namespace DataStandardizer.File.iCal.Benchmarks;

/// <summary>
/// Expands every recurring event of the expansion corpus over the ten years from <see cref="BenchmarkData.WindowStartUtc"/> to
/// <see cref="BenchmarkData.WindowEndUtc"/>, and counts the occurrences.
/// </summary>
[MemoryDiagnoser]
public class ExpandBenchmarks
{
    private IcalCalendar _calendar = new();
    private List<IcalEvent> _events = [];
    private IcalNetCalendar _icalNetCalendar = new();
    private List<IcalNetEvent> _icalNetEvents = [];

    private static readonly IcalDateTime From = ToIcalDateTime(BenchmarkData.WindowStartUtc);
    private static readonly IcalDateTime To = ToIcalDateTime(BenchmarkData.WindowEndUtc);
    private static readonly CalDateTime IcalNetFrom = new(BenchmarkData.WindowStartUtc, CalDateTime.UtcTzId);
    private static readonly CalDateTime IcalNetTo = new(BenchmarkData.WindowEndUtc, CalDateTime.UtcTzId);

    [GlobalSetup]
    public void GlobalSetup()
    {
        var ics = BenchmarkData.CreateRecurringIcs();

        using (var reader = new IcalReader(new MemoryStream(ics, writable: false)))
        {
            _calendar = reader.ReadCalendar()!;
        }

        _events = _calendar.Events.ToList();
        _icalNetCalendar = IcalNetCalendar.Load(new MemoryStream(ics, writable: false))!;
        _icalNetEvents = _icalNetCalendar.Events.ToList();

        CheckOccurrenceCounts();
    }

    [Benchmark(Baseline = true)]
    public int Expand_CalendarTimeZones()
    {
        var count = 0;
        foreach (var calendarEvent in _events)
        {
            foreach (var _ in calendarEvent.GetOccurrences(_calendar, From, To))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int Expand_TzData()
    {
        var count = 0;
        foreach (var calendarEvent in _events)
        {
            foreach (var _ in calendarEvent.GetOccurrences(_calendar, From, To, IcalTimeZoneResolver.TzData))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int Expand_IcalNet()
    {
        var count = 0;
        foreach (var calendarEvent in _icalNetEvents)
        {
            foreach (var _ in calendarEvent.GetOccurrences(IcalNetFrom).TakeWhileBefore(IcalNetTo))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Compares the number of occurrences each library gives for each event, and reports any difference in the benchmark log.  A
    /// difference does not stop the run.
    /// </summary>
    private void CheckOccurrenceCounts()
    {
        var total = 0;
        var tzDataTotal = 0;
        var icalNetTotal = 0;
        var differences = 0;
        for (var i = 0; i < _events.Count; i++)
        {
            var count = _events[i].GetOccurrences(_calendar, From, To).Count();
            var tzDataCount = _events[i].GetOccurrences(_calendar, From, To, IcalTimeZoneResolver.TzData).Count();
            var icalNetCount = _icalNetEvents[i].GetOccurrences(IcalNetFrom).TakeWhileBefore(IcalNetTo).Count();
            total += count;
            tzDataTotal += tzDataCount;
            icalNetTotal += icalNetCount;
            if (count != icalNetCount || count != tzDataCount)
            {
                differences++;
                Console.WriteLine($"// Occurrence count differs for {_events[i].Uid} ({_events[i].RecurrenceRules.FirstOrDefault()}): " +
                    $"{count} (calendar time zones), {tzDataCount} (TZ Database), {icalNetCount} (Ical.Net).");
            }
        }

        Console.WriteLine($"// Occurrences: {total:N0} (calendar time zones), {tzDataTotal:N0} (TZ Database), {icalNetTotal:N0} (Ical.Net); " +
            $"{differences} event(s) differ.");
    }

    private static IcalDateTime ToIcalDateTime(DateTime utc)
    {
        return new IcalDateTime(new IcalDate(utc.Year, utc.Month, utc.Day), new IcalTime(utc.Hour, utc.Minute, utc.Second, true));
    }
}
