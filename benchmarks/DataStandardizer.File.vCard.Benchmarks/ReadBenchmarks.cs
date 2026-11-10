using BenchmarkDotNet.Attributes;
using FolkerKinzel.VCards;

namespace DataStandardizer.File.vCard.Benchmarks;

/// <summary>
/// Reads the vCard 4.0 corpus and counts its cards, so every benchmark returns <see cref="BenchmarkData.CardCount"/>.
/// </summary>
[MemoryDiagnoser]
public class ReadBenchmarks
{
    private byte[] _vcf = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _vcf = BenchmarkData.CreateVersion40();
        Console.WriteLine($"// Corpus: {_vcf.Length:N0} bytes, {BenchmarkData.CountContentLines(_vcf):N0} content lines.");
    }

    [Benchmark]
    public int Read_View()
    {
        using var stream = new MemoryStream(_vcf, writable: false);
        using var reader = new VcardContentLineReader(stream);

        var count = 0;
        while (reader.ReadContentLine())
        {
            // Touch the name and value spans of every line.
            var line = reader.CurrentLine;
            if (line.NameEquals("BEGIN") && line.RawValue.Equals("VCARD", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int Read_ContentLines()
    {
        using var stream = new MemoryStream(_vcf, writable: false);
        using var reader = new VcardContentLineReader(stream);

        var count = 0;
        while (reader.ReadLine() is { } line)
        {
            if (line.Name == "BEGIN" && line.Value == "VCARD")
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark(Baseline = true)]
    public int Read_Model()
    {
        using var stream = new MemoryStream(_vcf, writable: false);
        using var reader = new VcardReader(stream);
        return reader.ReadCards().Count();
    }

    [Benchmark]
    public async Task<int> Read_View_Async()
    {
        using var stream = new MemoryStream(_vcf, writable: false);
        await using var reader = new VcardContentLineReader(stream);

        var count = 0;
        while (await reader.ReadContentLineAsync())
        {
            var line = reader.CurrentLine;
            if (line.NameEquals("BEGIN") && line.RawValue.Equals("VCARD", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public async Task<int> Read_ContentLines_Async()
    {
        using var stream = new MemoryStream(_vcf, writable: false);
        await using var reader = new VcardContentLineReader(stream);

        var count = 0;
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.Name == "BEGIN" && line.Value == "VCARD")
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public async Task<int> Read_Model_Async()
    {
        using var stream = new MemoryStream(_vcf, writable: false);
        await using var reader = new VcardReader(stream);

        var count = 0;
        await foreach (var card in reader.ReadCardsAsync())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int Read_FolkerKinzel()
    {
        using var stream = new MemoryStream(_vcf, writable: false);
        using var text = new StreamReader(stream);
        using var reader = new VcfReader(text);
        return reader.ReadToEnd().Count();
    }

    [Benchmark]
    public async Task<int> Read_FolkerKinzel_Async()
    {
        var cards = await Vcf.DeserializeAsync(_ => Task.FromResult<Stream>(new MemoryStream(_vcf, writable: false)), (System.Text.Encoding?)null, default);
        return cards.Count;
    }
}
