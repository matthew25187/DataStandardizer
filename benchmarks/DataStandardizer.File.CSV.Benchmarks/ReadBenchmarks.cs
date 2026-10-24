using System.Globalization;
using BenchmarkDotNet.Attributes;
using CsvHelper;
using CsvHelper.Configuration;

namespace DataStandardizer.File.CSV.Benchmarks;

[MemoryDiagnoser]
public class ReadBenchmarks
{
    private byte[] _csv = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _csv = BenchmarkData.CreateCsv(BenchmarkData.CreatePeople());
    }

    [Benchmark(Baseline = true)]
    public int Read_Raw()
    {
        var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, Culture = CultureInfo.InvariantCulture };
        using var stream = new MemoryStream(_csv, writable: false);
        using var reader = new CsvFileReader<CsvFileRecordLine>(stream, options);

        _ = reader.ReadLine(); // discard the header line

        var count = 0;
        while (reader.ReadLine() is not null)
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int Read_Mapped()
    {
        var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, Culture = CultureInfo.InvariantCulture };
        using var stream = new MemoryStream(_csv, writable: false);
        using var reader = new CsvFileReader<PersonLine>(stream, options);
        reader.RegisterMapper<PersonLineMapper>();

        _ = reader.ReadLine(); // discard the header line

        var count = 0;
        while (reader.ReadLine() is not null)
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int Read_CsvHelper_Records()
    {
        using var stream = new MemoryStream(_csv, writable: false);
        using var streamReader = new StreamReader(stream);
        using var reader = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture));

        var count = 0;
        foreach (var _ in reader.GetRecords<Person>())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int Read_CsvHelper_Raw()
    {
        using var stream = new MemoryStream(_csv, writable: false);
        using var streamReader = new StreamReader(stream);
        using var reader = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture));

        reader.Read();
        reader.ReadHeader();

        var count = 0;
        while (reader.Read())
        {
            for (var i = 0; i < BenchmarkData.FieldNames.Length; i++)
            {
                _ = reader.GetField(i);
            }

            count++;
        }

        return count;
    }
}
