using System.Globalization;
using BenchmarkDotNet.Attributes;

namespace DataStandardizer.File.CSV.Benchmarks;

/// <summary>
/// Writes the benchmark data to a real temporary file, so the buffering of the <see cref="StreamWriter"/>
/// and <see cref="FileStream"/> the writer creates is part of the measurement.
/// </summary>
[MemoryDiagnoser]
public class FileWriteBenchmarks
{
    private string _path = string.Empty;
    private List<CsvFileRecordLine> _recordLines = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _path = Path.GetTempFileName();
        _recordLines = BenchmarkData.CreatePeople().Select(BenchmarkData.ToRecordLine).ToList();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        System.IO.File.Delete(_path);
    }

    [Benchmark(Baseline = true)]
    public int Write_Raw()
    {
        var options = new CsvFileOptions { Culture = CultureInfo.InvariantCulture };
        using var writer = new CsvFileWriter<CsvFileRecordLine>(_path, options);

        return WriteAll(writer);
    }

    [Benchmark]
    public int Write_Raw_Stream()
    {
        var options = new CsvFileOptions { Culture = CultureInfo.InvariantCulture };
        using var stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new CsvFileWriter<CsvFileRecordLine>(stream, options);

        return WriteAll(writer);
    }

    private int WriteAll(CsvFileWriter<CsvFileRecordLine> writer)
    {
        writer.WriteLine(BenchmarkData.CreateHeaderLine());
        foreach (var recordLine in _recordLines)
        {
            writer.WriteLine(recordLine);
        }

        return _recordLines.Count;
    }
}
