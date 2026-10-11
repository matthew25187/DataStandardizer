using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using CsvHelper;
using CsvHelper.Configuration;

namespace DataStandardizer.File.CSV.Benchmarks;

[MemoryDiagnoser]
public class WriteBenchmarks
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    private List<Person> _people = [];
    private List<CsvFileRecordLine> _recordLines = [];
    private List<PersonLine> _personLines = [];
    private MemoryStream _output = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        _people = BenchmarkData.CreatePeople();
        _recordLines = _people.Select(BenchmarkData.ToRecordLine).ToList();
        _personLines = _people.Select(BenchmarkData.ToPersonLine).ToList();

        // Pre-size the output so buffer growth is not part of the measurement.
        _output = new MemoryStream(BenchmarkData.CreateCsv(_people).Length * 2);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _output.Dispose();
    }

    [Benchmark(Baseline = true)]
    public long Write_Raw()
    {
        var options = new CsvFileOptions { Culture = CultureInfo.InvariantCulture };
        using var streamWriter = CreateStreamWriter();
        using (var writer = new CsvFileWriter<CsvFileRecordLine>(streamWriter, options))
        {
            writer.WriteLine(BenchmarkData.CreateHeaderLine());
            foreach (var recordLine in _recordLines)
            {
                writer.WriteLine(recordLine);
            }
        }

        streamWriter.Flush();
        return _output.Position;
    }

    [Benchmark]
    public long Write_Mapped()
    {
        var options = new CsvFileOptions { Culture = CultureInfo.InvariantCulture };
        using var streamWriter = CreateStreamWriter();
        using (var writer = new CsvFileWriter<PersonLine>(streamWriter, options))
        {
            writer.RegisterMapper<PersonLineMapper>();
            writer.WriteLine(BenchmarkData.CreateHeaderLine());
            foreach (var personLine in _personLines)
            {
                writer.WriteLine(personLine);
            }
        }

        streamWriter.Flush();
        return _output.Position;
    }

    [Benchmark]
    public long Write_CsvHelper()
    {
        using var streamWriter = CreateStreamWriter();
        using (var writer = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture), leaveOpen: true))
        {
            writer.WriteRecords(_people);
        }

        streamWriter.Flush();
        return _output.Position;
    }

    private StreamWriter CreateStreamWriter()
    {
        _output.Position = 0;
        _output.SetLength(0);
        return new StreamWriter(_output, Utf8NoBom, bufferSize: 4096, leaveOpen: true);
    }
}
