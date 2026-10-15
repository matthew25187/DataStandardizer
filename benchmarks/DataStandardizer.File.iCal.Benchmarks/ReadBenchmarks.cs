using BenchmarkDotNet.Attributes;
using IcalNetCalendar = Ical.Net.Calendar;

namespace DataStandardizer.File.iCal.Benchmarks;

/// <summary>
/// Reads the main corpus and counts its VEVENT components, so every benchmark returns <see cref="BenchmarkData.EventCount"/>.
/// </summary>
[MemoryDiagnoser]
public class ReadBenchmarks
{
    private byte[] _ics = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _ics = BenchmarkData.CreateIcs();
        Console.WriteLine($"// Corpus: {_ics.Length:N0} bytes, {BenchmarkData.CountContentLines(_ics):N0} content lines.");
    }

    [Benchmark]
    public int Read_View()
    {
        using var stream = new MemoryStream(_ics, writable: false);
        using var reader = new IcalContentLineReader(stream);

        var count = 0;
        while (reader.ReadContentLine())
        {
            // Touch the name and value spans of every line.
            var line = reader.CurrentLine;
            if (line.NameEquals("BEGIN") && line.RawValue.SequenceEqual("VEVENT"))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int Read_ContentLines()
    {
        using var stream = new MemoryStream(_ics, writable: false);
        using var reader = new IcalContentLineReader(stream);

        var count = 0;
        while (reader.ReadLine() is { } line)
        {
            if (line.Name == "BEGIN" && line.Value == "VEVENT")
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark(Baseline = true)]
    public int Read_Model()
    {
        using var stream = new MemoryStream(_ics, writable: false);
        using var reader = new IcalReader(stream);

        var count = 0;
        foreach (var calendar in reader.ReadCalendars())
        {
            count += calendar.Events.Count();
        }

        return count;
    }

    [Benchmark]
    public async Task<int> Read_View_Async()
    {
        using var stream = new MemoryStream(_ics, writable: false);
        await using var reader = new IcalContentLineReader(stream);

        var count = 0;
        while (await reader.ReadContentLineAsync())
        {
            var line = reader.CurrentLine;
            if (line.NameEquals("BEGIN") && line.RawValue.SequenceEqual("VEVENT"))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public async Task<int> Read_ContentLines_Async()
    {
        using var stream = new MemoryStream(_ics, writable: false);
        await using var reader = new IcalContentLineReader(stream);

        var count = 0;
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.Name == "BEGIN" && line.Value == "VEVENT")
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public async Task<int> Read_Model_Async()
    {
        using var stream = new MemoryStream(_ics, writable: false);
        await using var reader = new IcalReader(stream);

        var count = 0;
        await foreach (var calendar in reader.ReadCalendarsAsync())
        {
            count += calendar.Events.Count();
        }

        return count;
    }

    [Benchmark]
    public int Read_IcalNet()
    {
        using var stream = new MemoryStream(_ics, writable: false);
        var calendar = IcalNetCalendar.Load(stream)!;
        return calendar.Events.Count;
    }
}
