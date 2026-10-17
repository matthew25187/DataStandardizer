using BenchmarkDotNet.Attributes;

namespace DataStandardizer.File.iCal.Benchmarks;

/// <summary>
/// Reads the main corpus from a real temporary file, in each format, through a <see cref="FileStream"/> opened as the file-path
/// constructors open it, with <c>FileOptions.Asynchronous</c> or without it.  Every benchmark returns <see cref="BenchmarkData.EventCount"/>.
/// </summary>
[MemoryDiagnoser]
public class FileReadBenchmarks
{
    private string _path = string.Empty;

    [Params(CalendarFormat.Ical, CalendarFormat.Xcal, CalendarFormat.Jcal)]
    public CalendarFormat Format { get; set; }

    [Params(false, true)]
    public bool Asynchronous { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        IcalCalendar calendar;
        using (var reader = new IcalReader(new MemoryStream(BenchmarkData.CreateIcs(), writable: false)))
        {
            calendar = reader.ReadCalendar()!;
        }

        _path = Path.GetTempFileName();
        using (var stream = new FileStream(_path, FileMode.Create, FileAccess.Write))
        {
            CalendarFiles.Write(stream, Format, calendar);
        }

        Console.WriteLine($"// {Format} file: {new FileInfo(_path).Length:N0} bytes.");
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        System.IO.File.Delete(_path);
    }

    [Benchmark(Baseline = true)]
    public int Read()
    {
        using var stream = CalendarFiles.OpenRead(_path, Format, Asynchronous);
        return CalendarFiles.CountEvents(stream, Format);
    }

    [Benchmark]
    public async Task<int> Read_Async()
    {
        await using var stream = CalendarFiles.OpenRead(_path, Format, Asynchronous);
        return await CalendarFiles.CountEventsAsync(stream, Format);
    }
}
