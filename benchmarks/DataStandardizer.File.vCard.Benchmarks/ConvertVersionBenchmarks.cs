using BenchmarkDotNet.Attributes;
using FolkerKinzel.VCards;
using FolkerKinzel.VCards.Enums;
using FolkerKinzelCard = FolkerKinzel.VCards.VCard;

namespace DataStandardizer.File.vCard.Benchmarks;

/// <summary>
/// Converts the cards of the vCard 4.0 corpus, read in <see cref="GlobalSetup"/>, to vCard 3.0 or 2.1 (<see cref="Version"/>).  The corpus
/// has data that neither version can hold, such as <c>KIND</c> and <c>LANG</c>, so <c>AllowLossyVersionConversion</c> is set.
/// </summary>
[MemoryDiagnoser]
public class ConvertVersionBenchmarks
{
    private List<VcardCard> _cards = [];
    private List<FolkerKinzelCard> _folkerKinzelCards = [];
    private VcardOptions _options = new() { MaxLineLength = null };
    private MemoryStream _output = new();

    [Params(VcardVersion.V3_0, VcardVersion.V2_1)]
    public VcardVersion Version { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var vcf = BenchmarkData.CreateVersion40();

        using (var reader = new VcardReader(new MemoryStream(vcf, writable: false)))
        {
            _cards = reader.ReadCards().ToList();
        }

        _folkerKinzelCards = Vcf.Deserialize(new MemoryStream(vcf, writable: false), null, leaveStreamOpen: false).ToList();
        _options = new VcardOptions { MaxLineLength = null, WriteVersion = Version, AllowLossyVersionConversion = true };

        // Pre-size the output so buffer growth is not part of the measurement.
        _output = new MemoryStream(vcf.Length * 2);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _output.Dispose();
    }

    /// <summary>
    /// Writes each card in <see cref="Version"/> with <c>VcardWriter</c>, returning the number of bytes written.
    /// </summary>
    [Benchmark(Baseline = true)]
    public long Write_Model()
    {
        ResetOutput();
        using (var writer = new VcardWriter(_output, _options))
        {
            writer.WriteCards(_cards);
        }

        return _output.Position;
    }

    /// <summary>
    /// Makes a copy of each card with only what <see cref="Version"/> can hold, returning the number of cards.
    /// </summary>
    [Benchmark]
    public int Convert_Model()
    {
        var count = 0;
        foreach (var card in _cards)
        {
            if (VcardVersionConverter.Convert(card, Version, _options).SourceVersion == Version)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// FolkerKinzel.VCards <c>Vcf.Serialize</c> in <see cref="Version"/>, returning the number of bytes written.
    /// </summary>
    [Benchmark]
    public long Write_FolkerKinzel()
    {
        ResetOutput();
        var version = Version == VcardVersion.V3_0 ? VCdVersion.V3_0 : VCdVersion.V2_1;
        Vcf.Serialize(_folkerKinzelCards, _output, version, null, FolkerKinzelOptions.Write, leaveStreamOpen: true);
        return _output.Position;
    }

    private void ResetOutput()
    {
        _output.Position = 0;
        _output.SetLength(0);
    }
}
