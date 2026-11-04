using BenchmarkDotNet.Attributes;
using DataStandardizer.Communication.Mime;
using MimeKit;

namespace DataStandardizer.File.InternetMessage.Benchmarks;

/// <summary>
/// Writes a message to a pre-sized stream, from a model made in <see cref="GlobalSetup"/>, so only the writing is measured.  The <c>Write_Read</c> benchmarks write a message that was read and
/// hasn't changed, which copies it.  The <c>Write_New</c> ones write a message built in code, which has to encode its attachments.  Each returns the number of bytes written.
/// </summary>
[MemoryDiagnoser]
public class WriteBenchmarks
{
    private readonly MemoryStream _output = new(8 * 1024 * 1024);

    private ImfMessage _readMessage = null!;
    private MimeMessage _readMimeKitMessage = null!;
    private ImfMessage _newMessage = null!;
    private ImfEntitySource _newSource = null!;
    private MimeMessage _newMimeKitMessage = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var bytes = BenchmarkData.CreateMessage();
        using (var stream = new MemoryStream(bytes, writable: false))
        using (var reader = new ImfReader(stream))
        {
            _readMessage = reader.ReadMessage();
        }

        _readMimeKitMessage = MimeMessage.Load(new MemoryStream(bytes, writable: false));

        var attachments = BenchmarkData.CreateAttachmentContent();
        _newMessage = BuildMessage(attachments);
        _newSource = BuildSource(attachments);
        _newMimeKitMessage = BuildMimeKitMessage(attachments);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _readMimeKitMessage.Dispose();
        _newMimeKitMessage.Dispose();
    }

    [Benchmark(Baseline = true)]
    public long Write_New()
    {
        _output.SetLength(0);
        using var writer = new ImfWriter(_output);
        writer.WriteMessage(_newMessage);
        return _output.Length;
    }

    [Benchmark]
    public long Write_New_Entities()
    {
        _output.SetLength(0);
        using var writer = new ImfEntityWriter(_output);
        writer.WriteMessage(_newSource);
        return _output.Length;
    }

    [Benchmark]
    public long Write_New_MimeKit()
    {
        _output.SetLength(0);
        _newMimeKitMessage.WriteTo(_output);
        return _output.Length;
    }

    [Benchmark]
    public long Write_Read()
    {
        _output.SetLength(0);
        using var writer = new ImfWriter(_output);
        writer.WriteMessage(_readMessage);
        return _output.Length;
    }

    [Benchmark]
    public long Write_Read_MimeKit()
    {
        _output.SetLength(0);
        _readMimeKitMessage.WriteTo(_output);
        return _output.Length;
    }

    private static ImfMessage BuildMessage(byte[][] attachments)
    {
        var builder = new ImfMessageBuilder();
        builder.Header.From = ImfAddressList.Parse("Ana Lima <ana@example.com>");
        builder.Header.To = ImfAddressList.Parse("Ben Okafor <ben@example.org>");
        builder.Header.Subject = "The files you asked for";
        builder.Header.Date = new ImfDateTime(new DateTimeOffset(2026, 11, 4, 9, 30, 0, TimeSpan.FromHours(13)));
        builder.Header.MessageId = new ImfMessageId("20261104093000.4711", "mail.example.com");
        builder.SetText(Text).SetHtml(Html);
        for (var index = 0; index < attachments.Length; index++)
        {
            builder.AddAttachment($"attachment{index}.bin", MimeMediaType.Parse("application/octet-stream"), attachments[index]);
        }

        return builder.Build();
    }

    private static ImfEntitySource BuildSource(byte[][] attachments)
    {
        var parts = new List<ImfEntitySource>
        {
            ImfEntitySource.FromParts(MimeMediaType.MultipartAlternative, new[]
            {
                ImfEntitySource.FromContent(MimeMediaType.Parse("text/plain;charset=utf-8"), System.Text.Encoding.UTF8.GetBytes(Text)),
                ImfEntitySource.FromContent(MimeMediaType.Parse("text/html;charset=utf-8"), System.Text.Encoding.UTF8.GetBytes(Html)),
            }),
        };
        for (var index = 0; index < attachments.Length; index++)
        {
            parts.Add(ImfEntitySource.FromContent(MimeMediaType.Parse("application/octet-stream"), attachments[index])
                .AddField("Content-Disposition", $"attachment; filename=attachment{index}.bin", ImfHeaderFieldKind.Structured));
        }

        return ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, parts)
            .AddField("From", "Ana Lima <ana@example.com>", ImfHeaderFieldKind.Structured)
            .AddField("To", "Ben Okafor <ben@example.org>", ImfHeaderFieldKind.Structured)
            .AddTextField("Subject", "The files you asked for")
            .AddField("Date", "Wed, 4 Nov 2026 09:30:00 +1300", ImfHeaderFieldKind.Structured)
            .AddField("Message-ID", "<20261104093000.4711@mail.example.com>", ImfHeaderFieldKind.Structured);
    }

    private static MimeMessage BuildMimeKitMessage(byte[][] attachments)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Ana Lima", "ana@example.com"));
        message.To.Add(new MailboxAddress("Ben Okafor", "ben@example.org"));
        message.Subject = "The files you asked for";
        message.Date = new DateTimeOffset(2026, 11, 4, 9, 30, 0, TimeSpan.FromHours(13));
        message.MessageId = "20261104093000.4711@mail.example.com";

        var builder = new BodyBuilder { TextBody = Text, HtmlBody = Html };
        for (var index = 0; index < attachments.Length; index++)
        {
            builder.Attachments.Add($"attachment{index}.bin", attachments[index], new MimeKit.ContentType("application", "octet-stream"));
        }

        message.Body = builder.ToMessageBody();
        return message;
    }

    private const string Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.\r\n";

    private const string Html = "<html><body><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.</p></body></html>\r\n";
}
