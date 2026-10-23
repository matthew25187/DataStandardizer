using System.Text;
using BenchmarkDotNet.Attributes;
using Ical.Net.Serialization;
using IcalNetCalendar = Ical.Net.Calendar;

namespace DataStandardizer.File.iCal.Benchmarks;

/// <summary>
/// Writes the main corpus from a model built in <see cref="GlobalSetup"/>, so only the writing is measured.
/// </summary>
[MemoryDiagnoser]
public class WriteBenchmarks
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    private IcalCalendar _calendar = new();
    private List<IcalContentLine> _lines = [];
    private IcalNetCalendar _icalNetCalendar = new();
    private MemoryStream _output = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        var ics = BenchmarkData.CreateIcs();

        using (var reader = new IcalReader(new MemoryStream(ics, writable: false)))
        {
            _calendar = reader.ReadCalendar()!;
        }

        using (var reader = new IcalContentLineReader(new MemoryStream(ics, writable: false)))
        {
            _lines = reader.ReadLines().ToList();
        }

        _icalNetCalendar = IcalNetCalendar.Load(new MemoryStream(ics, writable: false))!;

        // Pre-size the output so buffer growth is not part of the measurement.
        _output = new MemoryStream(ics.Length * 2);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _output.Dispose();
    }

    [Benchmark]
    public long Write_ContentLines()
    {
        ResetOutput();
        using (var writer = new IcalContentLineWriter(_output))
        {
            writer.WriteLines(_lines);
        }

        return _output.Position;
    }

    [Benchmark(Baseline = true)]
    public long Write_Model()
    {
        ResetOutput();
        using (var writer = new IcalWriter(_output))
        {
            writer.WriteCalendar(_calendar);
        }

        return _output.Position;
    }

    [Benchmark]
    public async Task<long> Write_ContentLines_Async()
    {
        ResetOutput();
        await using (var writer = new IcalContentLineWriter(_output))
        {
            await writer.WriteLinesAsync(_lines);
            await writer.FlushAsync();
        }

        return _output.Position;
    }

    [Benchmark]
    public async Task<long> Write_Model_Async()
    {
        ResetOutput();
        await using (var writer = new IcalWriter(_output))
        {
            await writer.WriteCalendarAsync(_calendar);
            await writer.FlushAsync();
        }

        return _output.Position;
    }

    [Benchmark]
    public long Write_IcalNet()
    {
        ResetOutput();
        new CalendarSerializer().Serialize(_icalNetCalendar, _output, Utf8NoBom);
        return _output.Position;
    }

    private void ResetOutput()
    {
        _output.Position = 0;
        _output.SetLength(0);
    }
}
