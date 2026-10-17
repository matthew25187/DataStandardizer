using BenchmarkDotNet.Attributes;

namespace DataStandardizer.File.iCal.Benchmarks;

/// <summary>
/// Writes the main corpus to a real temporary file, in each format, through a <see cref="FileStream"/> opened as the file-path
/// constructors open it, with <c>FileOptions.Asynchronous</c> or without it.  The model is built in <see cref="GlobalSetup"/>, so only the
/// writing is measured.  Every benchmark returns the number of bytes written.
/// </summary>
[MemoryDiagnoser]
public class FileWriteBenchmarks
{
    private string _path = string.Empty;
    private IcalCalendar _calendar = new();

    [Params(CalendarFormat.Ical, CalendarFormat.Xcal, CalendarFormat.Jcal)]
    public CalendarFormat Format { get; set; }

    [Params(false, true)]
    public bool Asynchronous { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        using (var reader = new IcalReader(new MemoryStream(BenchmarkData.CreateIcs(), writable: false)))
        {
            _calendar = reader.ReadCalendar()!;
        }

        _path = Path.GetTempFileName();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        System.IO.File.Delete(_path);
    }

    [Benchmark(Baseline = true)]
    public long Write()
    {
        using var stream = CalendarFiles.OpenWrite(_path, Format, Asynchronous);
        CalendarFiles.Write(stream, Format, _calendar);
        return stream.Length;
    }

    [Benchmark]
    public async Task<long> Write_Async()
    {
        await using var stream = CalendarFiles.OpenWrite(_path, Format, Asynchronous);
        await CalendarFiles.WriteAsync(stream, Format, _calendar);
        return stream.Length;
    }
}
