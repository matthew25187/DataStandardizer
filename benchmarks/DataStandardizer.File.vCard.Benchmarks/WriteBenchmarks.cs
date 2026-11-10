using BenchmarkDotNet.Attributes;
using FolkerKinzel.VCards;
using FolkerKinzel.VCards.Enums;
using FolkerKinzelCard = FolkerKinzel.VCards.VCard;

namespace DataStandardizer.File.vCard.Benchmarks;

/// <summary>
/// Writes the vCard 4.0 corpus from cards and lines read in <see cref="GlobalSetup"/>, so only the writing is measured.  Every benchmark
/// returns the number of bytes written.
/// </summary>
[MemoryDiagnoser]
public class WriteBenchmarks
{
    private List<VcardCard> _cards = [];
    private List<VcardContentLine> _lines = [];
    private List<FolkerKinzelCard> _folkerKinzelCards = [];
    private MemoryStream _output = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        var vcf = BenchmarkData.CreateVersion40();

        using (var reader = new VcardReader(new MemoryStream(vcf, writable: false)))
        {
            _cards = reader.ReadCards().ToList();
        }

        using (var reader = new VcardContentLineReader(new MemoryStream(vcf, writable: false)))
        {
            _lines = reader.ReadLines().ToList();
        }

        _folkerKinzelCards = Vcf.Deserialize(new MemoryStream(vcf, writable: false), null, leaveStreamOpen: false).ToList();

        // Pre-size the output so buffer growth is not part of the measurement.
        _output = new MemoryStream(vcf.Length * 2);
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
        using (var writer = new VcardContentLineWriter(_output))
        {
            writer.WriteLines(_lines);
        }

        return _output.Position;
    }

    [Benchmark(Baseline = true)]
    public long Write_Model()
    {
        ResetOutput();
        using (var writer = new VcardWriter(_output))
        {
            writer.WriteCards(_cards);
        }

        return _output.Position;
    }

    [Benchmark]
    public async Task<long> Write_ContentLines_Async()
    {
        ResetOutput();
        await using (var writer = new VcardContentLineWriter(_output))
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
        await using (var writer = new VcardWriter(_output))
        {
            await writer.WriteCardsAsync(_cards);
            await writer.FlushAsync();
        }

        return _output.Position;
    }

    [Benchmark]
    public long Write_FolkerKinzel()
    {
        ResetOutput();
        Vcf.Serialize(_folkerKinzelCards, _output, VCdVersion.V4_0, null, FolkerKinzelOptions.Write, leaveStreamOpen: true);
        return _output.Position;
    }

    private void ResetOutput()
    {
        _output.Position = 0;
        _output.SetLength(0);
    }
}
