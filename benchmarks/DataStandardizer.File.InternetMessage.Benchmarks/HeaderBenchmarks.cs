using BenchmarkDotNet.Attributes;
using MimeKit;

namespace DataStandardizer.File.InternetMessage.Benchmarks;

/// <summary>
/// Parses only the header of a message, so every benchmark returns <see cref="BenchmarkData.HeaderFieldCount"/>.
/// </summary>
[MemoryDiagnoser]
public class HeaderBenchmarks
{
    private byte[] _message = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _message = BenchmarkData.CreateHeader();
        Console.WriteLine($"// Corpus: {_message.Length:N0} bytes, {BenchmarkData.HeaderFieldCount:N0} fields.");
        BenchmarkData.Check(nameof(Header_View), Header_View(), BenchmarkData.HeaderFieldCount);
        BenchmarkData.Check(nameof(Header_Strings), Header_Strings(), BenchmarkData.HeaderFieldCount);
        BenchmarkData.Check(nameof(Header_MimeKit), Header_MimeKit(), BenchmarkData.HeaderFieldCount);
        BenchmarkData.Check(nameof(Header_MimeKit_Strings), Header_MimeKit_Strings(), BenchmarkData.HeaderFieldCount);
    }

    [Benchmark(Baseline = true)]
    public int Header_View()
    {
        using var stream = new MemoryStream(_message, writable: false);
        using var reader = new ImfHeaderReader(stream);

        var count = 0;
        while (reader.ReadField() is { } field)
        {
            // Touch the name and value spans of every field.
            if (!field.Name.IsEmpty && field.Value.Length >= 0)
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int Header_Strings()
    {
        using var stream = new MemoryStream(_message, writable: false);
        using var reader = new ImfHeaderReader(stream);

        var count = 0;
        while (reader.ReadField() is { } field)
        {
            if (field.GetName().Length > 0 && field.GetValue().Length >= 0)
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int Header_MimeKit()
    {
        using var stream = new MemoryStream(_message, writable: false);
        var parser = new MimeParser(stream, MimeFormat.Entity);
        return parser.ParseHeaders().Count;
    }

    [Benchmark]
    public int Header_MimeKit_Strings()
    {
        using var stream = new MemoryStream(_message, writable: false);
        var parser = new MimeParser(stream, MimeFormat.Entity);

        var count = 0;
        foreach (var header in parser.ParseHeaders())
        {
            if (header.Field.Length > 0 && header.Value.Length >= 0)
            {
                count++;
            }
        }

        return count;
    }
}
