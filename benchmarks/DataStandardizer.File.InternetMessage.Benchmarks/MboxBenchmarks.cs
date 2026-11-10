using BenchmarkDotNet.Attributes;
using MimeKit;

namespace DataStandardizer.File.InternetMessage.Benchmarks;

/// <summary>
/// Reads a large mbox file one message at a time, so every benchmark returns <see cref="BenchmarkData.MboxMessageCount"/>.
/// </summary>
[MemoryDiagnoser]
public class MboxBenchmarks
{
    private byte[] _mbox = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _mbox = BenchmarkData.CreateMbox();
        Console.WriteLine($"// Corpus: {_mbox.Length:N0} bytes.");
        BenchmarkData.Check(nameof(Mbox_Messages), Mbox_Messages(), BenchmarkData.MboxMessageCount);
        BenchmarkData.Check(nameof(Mbox_Events), Mbox_Events(), BenchmarkData.MboxMessageCount);
        BenchmarkData.Check(nameof(Mbox_MimeKit), Mbox_MimeKit(), BenchmarkData.MboxMessageCount);
        BenchmarkData.Check(nameof(Mbox_MimeKit_Persistent), Mbox_MimeKit_Persistent(), BenchmarkData.MboxMessageCount);
    }

    [Benchmark(Baseline = true)]
    public int Mbox_Messages()
    {
        using var stream = new MemoryStream(_mbox, writable: false);
        using var reader = new MboxReader(stream);

        var count = 0;
        while (reader.ReadMessage() is { } entry)
        {
            if (entry.Message.Subject is not null)
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int Mbox_Events()
    {
        using var stream = new MemoryStream(_mbox, writable: false);
        using var reader = new MboxReader(stream);

        var count = 0;
        while (reader.ReadEntityEvents() is { } events)
        {
            while (events.ReadEvent() is { })
            {
            }

            count++;
        }

        return count;
    }

    [Benchmark]
    public int Mbox_MimeKit()
    {
        using var stream = new MemoryStream(_mbox, writable: false);
        var parser = new MimeParser(stream, MimeFormat.Mbox);

        var count = 0;
        while (!parser.IsEndOfStream)
        {
            using var message = parser.ParseMessage();
            if (message.Subject is not null)
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int Mbox_MimeKit_Persistent()
    {
        using var stream = new MemoryStream(_mbox, writable: false);
        var parser = new MimeParser(stream, MimeFormat.Mbox, persistent: true);

        var count = 0;
        while (!parser.IsEndOfStream)
        {
            using var message = parser.ParseMessage();
            if (message.Subject is not null)
            {
                count++;
            }
        }

        return count;
    }
}
