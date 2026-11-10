using BenchmarkDotNet.Attributes;

namespace DataStandardizer.File.vCard.Benchmarks;

/// <summary>
/// Reads the vCard 4.0 corpus in one format (<see cref="Source"/>) and writes it in another (<see cref="Target"/>), card by card, returning
/// the number of bytes written.  FolkerKinzel.VCards reads and writes neither jCard nor xCard, so there is no comparison.
/// </summary>
[MemoryDiagnoser]
public class ConvertFormatBenchmarks
{
    private byte[] _input = [];
    private MemoryStream _output = new();

    [Params(CardFormat.Vcf, CardFormat.Jcard, CardFormat.Xcard)]
    public CardFormat Source { get; set; }

    [Params(CardFormat.Vcf, CardFormat.Jcard, CardFormat.Xcard)]
    public CardFormat Target { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var vcf = BenchmarkData.CreateVersion40();
        List<VcardCard> cards;
        using (var stream = new MemoryStream(vcf, writable: false))
        {
            cards = CardFormats.Read(stream, CardFormat.Vcf).ToList();
        }

        using (var stream = new MemoryStream())
        {
            CardFormats.Write(stream, Source, cards);
            _input = stream.ToArray();
        }

        Console.WriteLine($"// Input: {_input.Length:N0} bytes of {Source}.");

        // Pre-size the output so buffer growth is not part of the measurement.
        _output = new MemoryStream(_input.Length * 4);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _output.Dispose();
    }

    [Benchmark]
    public long Convert()
    {
        _output.Position = 0;
        _output.SetLength(0);
        using var input = new MemoryStream(_input, writable: false);
        CardFormats.Write(_output, Target, CardFormats.Read(input, Source));
        return _output.Position;
    }
}
