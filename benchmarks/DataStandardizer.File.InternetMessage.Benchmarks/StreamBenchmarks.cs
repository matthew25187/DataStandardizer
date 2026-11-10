using BenchmarkDotNet.Attributes;
using MimeKit;
using MimeKit.IO;

namespace DataStandardizer.File.InternetMessage.Benchmarks;

/// <summary>
/// Goes through a large message without holding its bodies in memory.  <see cref="ImfEntityReader"/> gives a body as it reads it.  The closest that MimeKit has is a persistent parse, which
/// gives each body as a range of the stream to read later, so the stream has to be seekable.  The <c>_InMemory</c> benchmark is the MimeKit default, which copies each body, for comparison.
/// </summary>
[MemoryDiagnoser]
public class StreamBenchmarks
{
    private static readonly ImfOptions DecodingOptions = new() { DecodeTransferEncoding = true };

    private byte[] _message = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _message = BenchmarkData.CreateLargeMessage();
        Console.WriteLine($"// Corpus: {_message.Length:N0} bytes.");
        BenchmarkData.Check(nameof(Stream_Events), Stream_Events(), BenchmarkData.BodyPartCount);
        BenchmarkData.Check(nameof(Stream_MimeKit_Persistent), Stream_MimeKit_Persistent(), BenchmarkData.BodyPartCount);
        BenchmarkData.Check(nameof(Stream_Events_Decoded), Stream_Events_Decoded(), BenchmarkData.LargeMessageAttachmentBytes);
        BenchmarkData.Check(nameof(Stream_MimeKit_Persistent_Decoded), Stream_MimeKit_Persistent_Decoded(), BenchmarkData.LargeMessageAttachmentBytes);
        BenchmarkData.Check(nameof(Stream_MimeKit_InMemory_Decoded), Stream_MimeKit_InMemory_Decoded(), BenchmarkData.LargeMessageAttachmentBytes);
    }

    [Benchmark(Baseline = true)]
    public long Stream_Events()
    {
        using var stream = new MemoryStream(_message, writable: false);
        using var reader = new ImfEntityReader(stream);

        var parts = 0L;
        while (reader.ReadEvent() is { } entityEvent)
        {
            // Every event is read, so every byte of the bodies goes past.  An entity with a body of its own is one that isn't multipart.
            if (entityEvent.Kind == ImfEntityEventKind.HeaderEnd && !entityEvent.ContentType.Type.Equals("multipart", StringComparison.OrdinalIgnoreCase))
            {
                parts++;
            }
        }

        return parts;
    }

    [Benchmark]
    public long Stream_MimeKit_Persistent()
    {
        using var stream = new MemoryStream(_message, writable: false);
        using var message = MimeMessage.Load(stream, persistent: true);
        return message.BodyParts.Count();
    }

    [Benchmark]
    public long Stream_Events_Decoded()
    {
        using var stream = new MemoryStream(_message, writable: false);
        using var reader = new ImfEntityReader(stream, DecodingOptions);

        var total = 0L;
        var inAttachment = false;
        while (reader.ReadEvent() is { } entityEvent)
        {
            switch (entityEvent.Kind)
            {
                case ImfEntityEventKind.HeaderEnd:
                    inAttachment = entityEvent.TransferEncoding == DataStandardizer.Communication.Mime.MimeTransferEncoding.Base64;
                    break;
                case ImfEntityEventKind.BodyChunk when inAttachment:
                    total += entityEvent.Data.Length;
                    break;
            }
        }

        return total;
    }

    [Benchmark]
    public long Stream_MimeKit_Persistent_Decoded()
    {
        using var stream = new MemoryStream(_message, writable: false);
        using var message = MimeMessage.Load(stream, persistent: true);
        using var measure = new MeasuringStream();
        foreach (var attachment in message.Attachments.OfType<MimePart>())
        {
            attachment.Content!.DecodeTo(measure);
        }

        return measure.Length;
    }

    [Benchmark]
    public long Stream_MimeKit_InMemory_Decoded()
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
