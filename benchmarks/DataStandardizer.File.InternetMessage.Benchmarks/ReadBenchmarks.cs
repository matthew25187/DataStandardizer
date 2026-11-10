using BenchmarkDotNet.Attributes;
using MimeKit;
using MimeKit.IO;

namespace DataStandardizer.File.InternetMessage.Benchmarks;

/// <summary>
/// Reads a whole message with attachments into each library's object model.  The <c>_Parts</c> benchmarks return the number of attachments, and the <c>_Decoded</c> ones
/// <see cref="BenchmarkData.MessageAttachmentBytes"/>.
/// </summary>
[MemoryDiagnoser]
public class ReadBenchmarks
{
    private byte[] _message = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _message = BenchmarkData.CreateMessage();
        Console.WriteLine($"// Corpus: {_message.Length:N0} bytes.");
        BenchmarkData.Check(nameof(Read_Parts), Read_Parts(), BenchmarkData.AttachmentCount);
        BenchmarkData.Check(nameof(Read_MimeKit_Parts), Read_MimeKit_Parts(), BenchmarkData.AttachmentCount);
        BenchmarkData.Check(nameof(Read_Decoded), Read_Decoded(), BenchmarkData.MessageAttachmentBytes);
        BenchmarkData.Check(nameof(Read_MimeKit_Decoded), Read_MimeKit_Decoded(), BenchmarkData.MessageAttachmentBytes);
    }

    [Benchmark(Baseline = true)]
    public int Read_Parts()
    {
        using var stream = new MemoryStream(_message, writable: false);
        using var reader = new ImfReader(stream);
        return reader.ReadMessage().Attachments.Count;
    }

    [Benchmark]
    public int Read_MimeKit_Parts()
    {
        using var stream = new MemoryStream(_message, writable: false);
        using var message = MimeMessage.Load(stream);
        return message.Attachments.Count();
    }

    [Benchmark]
    public long Read_Decoded()
    {
        using var stream = new MemoryStream(_message, writable: false);
        using var reader = new ImfReader(stream);

        var total = 0L;
        foreach (var attachment in reader.ReadMessage().Attachments)
        {
            total += attachment.GetContent().Length;
        }

        return total;
    }

    [Benchmark]
    public long Read_MimeKit_Decoded()
    {
        using var stream = new MemoryStream(_message, writable: false);
        using var message = MimeMessage.Load(stream);
        using var measure = new MeasuringStream();
        foreach (var attachment in message.Attachments.OfType<MimePart>())
        {
            attachment.Content!.DecodeTo(measure);
        }

        return measure.Length;
    }
}
