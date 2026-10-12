using System.Globalization;
using BenchmarkDotNet.Attributes;

namespace DataStandardizer.File.CSV.Benchmarks;

/// <summary>
/// Reads the benchmark data from a real temporary file, so the buffering of the <see cref="StreamReader"/>
/// and <see cref="FileStream"/> the reader creates is part of the measurement.
/// </summary>
[MemoryDiagnoser]
public class FileReadBenchmarks
{
    private string _path = string.Empty;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _path = Path.GetTempFileName();
        System.IO.File.WriteAllBytes(_path, BenchmarkData.CreateCsv(BenchmarkData.CreatePeople()));
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        System.IO.File.Delete(_path);
    }

    [Benchmark(Baseline = true)]
    public int Read_Raw()
    {
        var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, Culture = CultureInfo.InvariantCulture };
        using var reader = new CsvFileReader<CsvFileRecordLine>(_path, options);

        return ReadAll(reader);
    }

    [Benchmark]
    public int Read_Raw_Stream()
    {
        var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, Culture = CultureInfo.InvariantCulture };
        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new CsvFileReader<CsvFileRecordLine>(stream, options);

        return ReadAll(reader);
    }

    private static int ReadAll(CsvFileReader<CsvFileRecordLine> reader)
    {
        _ = reader.ReadLine(); // discard the header line

        var count = 0;
        while (reader.ReadLine() is not null)
        {
            count++;
        }

        return count;
    }
}
