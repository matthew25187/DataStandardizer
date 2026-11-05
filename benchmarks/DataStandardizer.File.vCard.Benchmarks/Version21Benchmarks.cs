using BenchmarkDotNet.Attributes;
using FolkerKinzel.VCards;
using FolkerKinzel.VCards.Enums;

namespace DataStandardizer.File.vCard.Benchmarks;

/// <summary>
/// Reads the vCard 2.1 corpus, which is heavy in quoted-printable, and converts it to vCard 4.0.  The read benchmarks return
/// <see cref="BenchmarkData.CardCount"/>, and the conversions the number of bytes written.
/// </summary>
[MemoryDiagnoser]
public class Version21Benchmarks
{
    private byte[] _vcf = [];
    private MemoryStream _output = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        _vcf = BenchmarkData.CreateVersion21();
        Console.WriteLine($"// Corpus: {_vcf.Length:N0} bytes, {BenchmarkData.CountContentLines(_vcf):N0} content lines, " +
            $"{BenchmarkData.CountQuotedPrintableLines(_vcf):N0} of them in quoted-printable.");

        // Pre-size the output so buffer growth is not part of the measurement.
        _output = new MemoryStream(_vcf.Length * 2);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _output.Dispose();
    }

    [Benchmark]
    public int Read_View()
    {
        using var stream = new MemoryStream(_vcf, writable: false);
        using var reader = new VcardContentLineReader(stream);

        var count = 0;
        while (reader.ReadContentLine())
        {
            var line = reader.CurrentLine;
            if (line.NameEquals("BEGIN") && line.RawValue.Equals("VCARD", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// As <see cref="Read_View"/>, also decoding the quoted-printable and <c>CHARSET</c> of every <c>NOTE</c> with <c>GetDecodedText</c>.
    /// </summary>
    [Benchmark]
    public int Read_View_Decoded()
    {
        using var stream = new MemoryStream(_vcf, writable: false);
        using var reader = new VcardContentLineReader(stream);

        var count = 0;
        var characters = 0L;
        while (reader.ReadContentLine())
        {
            var line = reader.CurrentLine;
            if (line.NameEquals("BEGIN"))
            {
                count++;
            }
            else if (line.NameEquals("NOTE"))
            {
                characters += line.GetDecodedText().Length;
            }
        }

        return characters > 0 ? count : -1;
    }

    [Benchmark(Baseline = true)]
    public int Read_Model()
    {
        using var stream = new MemoryStream(_vcf, writable: false);
        using var reader = new VcardReader(stream);
        return reader.ReadCards().Count();
    }

    [Benchmark]
    public int Read_FolkerKinzel()
    {
        using var stream = new MemoryStream(_vcf, writable: false);
        using var text = new StreamReader(stream);
        using var reader = new VcfReader(text);
        return reader.ReadToEnd().Count();
    }

    /// <summary>
    /// Reads each card with <c>VcardReader</c> and writes it as vCard 4.0 with <c>VcardWriter</c>.
    /// </summary>
    [Benchmark]
    public long Upgrade_Model()
    {
        ResetOutput();
        using (var reader = new VcardReader(new MemoryStream(_vcf, writable: false)))
        using (var writer = new VcardWriter(_output))
        {
            writer.WriteCards(reader.ReadCards());
        }

        return _output.Position;
    }

    /// <summary>
    /// Reads each card with FolkerKinzel.VCards <c>VcfReader</c> and writes it as vCard 4.0 with <c>Vcf.Serialize</c>.
    /// </summary>
    [Benchmark]
    public long Upgrade_FolkerKinzel()
    {
        ResetOutput();
        using (var text = new StreamReader(new MemoryStream(_vcf, writable: false)))
        using (var reader = new VcfReader(text))
        {
            Vcf.Serialize(reader.ReadToEnd(), _output, VCdVersion.V4_0, null, FolkerKinzelOptions.Write, leaveStreamOpen: true);
        }

        return _output.Position;
    }

    private void ResetOutput()
    {
        _output.Position = 0;
        _output.SetLength(0);
    }
}
