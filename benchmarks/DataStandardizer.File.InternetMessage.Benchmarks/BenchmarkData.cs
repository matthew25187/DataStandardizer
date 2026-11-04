using System.Text;

namespace DataStandardizer.File.InternetMessage.Benchmarks;

/// <summary>
/// Generates the corpora from a fixed seed, so every run uses identical data.  The bytes are written directly, rather than with either library, so neither one's output is the input of
/// the other.  Everything follows the standards, so the strict defaults of <see cref="ImfReader"/> and <see cref="MboxReader"/> accept it.
/// </summary>
internal static class BenchmarkData
{
    /// <summary>The number of fields in the header of <see cref="CreateHeader"/>.</summary>
    public const int HeaderFieldCount = 41;

    /// <summary>The number of attachments in <see cref="CreateMessage"/> and <see cref="CreateLargeMessage"/>.</summary>
    public const int AttachmentCount = 3;

    /// <summary>The total size of the attachments of <see cref="CreateMessage"/>, once decoded: 100 KiB, 500 KiB and 1 MiB.</summary>
    public const long MessageAttachmentBytes = (100 + 500 + 1024) * 1024;

    /// <summary>The total size of the attachments of <see cref="CreateLargeMessage"/>, once decoded: 16 MiB each.</summary>
    public const long LargeMessageAttachmentBytes = 3L * 16 * 1024 * 1024;

    /// <summary>The number of parts of <see cref="CreateMessage"/> and <see cref="CreateLargeMessage"/> that hold content: the text, the HTML and the attachments.</summary>
    public const int BodyPartCount = 2 + AttachmentCount;

    /// <summary>The number of messages in <see cref="CreateMbox"/>.</summary>
    public const int MboxMessageCount = 10000;

    private const int Seed = 423;
    private const int Base64LineLength = 76;

    private const string Lipsum =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
        "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla " +
        "pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

    /// <summary>
    /// A message as a mail server delivers it: <see cref="HeaderFieldCount"/> fields, many of them folded, with a short body.
    /// </summary>
    public static byte[] CreateHeader()
    {
        var builder = new StringBuilder();
        builder.Append("Return-Path: <sender@example.com>\r\n");
        for (var hop = 0; hop < 8; hop++)
        {
            builder.Append("Received: from mx").Append(hop).Append(".example.net (mx").Append(hop).Append(".example.net [192.0.2.").Append(hop + 1).Append("])\r\n")
                .Append("\tby mx").Append(hop + 1).Append(".example.org with ESMTPS id 4a").Append(hop).Append("f2c9b1e\r\n")
                .Append("\tfor <recipient@example.org>; Wed, 4 Nov 2026 09:30:").Append((10 + hop).ToString("00")).Append(" +0000\r\n");
        }

        builder.Append("DKIM-Signature: v=1; a=rsa-sha256; c=relaxed/relaxed; d=example.com; s=mail; t=1793784610;\r\n")
            .Append("\th=from:to:subject:date:message-id:mime-version:content-type;\r\n")
            .Append("\tbh=47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU=;\r\n")
            .Append("\tb=dGhpcyBpcyBub3QgYSByZWFsIHNpZ25hdHVyZSBidXQgaXQgaXMgdGhlIHJpZ2h0IGtpbmQgb2YgbGVuZ3Ro\r\n")
            .Append("\t cmVhbGx5IGl0IGlzIHRoZSByaWdodCBraW5kIG9mIGxlbmd0aCBmb3IgYSBiZW5jaG1hcms=\r\n");
        builder.Append("From: =?UTF-8?Q?Ana_Lim=C3=A9?= <ana@example.com>\r\n")
            .Append("Sender: Mailing List <list-bounces@example.com>\r\n")
            .Append("Reply-To: Ana Lima <ana@example.com>\r\n")
            .Append("To: Ben Okafor <ben@example.org>, Cara Nguyen <cara@example.org>,\r\n")
            .Append(" Dan Evans <dan@example.org>, Eve Ito <eve@example.org>\r\n")
            .Append("Cc: \"Support, Team\" <support@example.org>\r\n")
            .Append("Subject: =?UTF-8?B?UsOpc3Vtw6k=?= for the quarterly planning meeting that\r\n")
            .Append(" takes place next week\r\n")
            .Append("Date: Wed, 4 Nov 2026 09:30:00 +1300\r\n")
            .Append("Message-ID: <20261104093000.4711@mail.example.com>\r\n")
            .Append("In-Reply-To: <20261103120000.4700@mail.example.org>\r\n")
            .Append("References: <20261101120000.4600@mail.example.org>\r\n")
            .Append(" <20261102120000.4650@mail.example.com>\r\n")
            .Append(" <20261103120000.4700@mail.example.org>\r\n")
            .Append("MIME-Version: 1.0\r\n")
            .Append("Content-Type: text/plain; charset=us-ascii\r\n")
            .Append("Content-Transfer-Encoding: 7bit\r\n");
        for (var extra = 0; extra < 18; extra++)
        {
            builder.Append("X-Example-").Append(extra).Append(": value number ").Append(extra).Append(" of a field that a filter on the way added\r\n");
        }

        builder.Append("\r\nHello.\r\n");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    /// <summary>
    /// A message with a text body, an HTML body and three base64 attachments.
    /// </summary>
    public static byte[] CreateMessage() => CreateMessageWithAttachments(100 * 1024, 500 * 1024, 1024 * 1024);

    /// <summary>
    /// A message of the same shape with three attachments of 16 MiB each, about 69 MB in all.
    /// </summary>
    public static byte[] CreateLargeMessage() => CreateMessageWithAttachments(16 * 1024 * 1024, 16 * 1024 * 1024, 16 * 1024 * 1024);

    /// <summary>
    /// The content of the attachments of <see cref="CreateMessage"/>, for building a message.
    /// </summary>
    public static byte[][] CreateAttachmentContent()
    {
        var random = new Random(Seed);
        return [RandomBytes(random, 100 * 1024), RandomBytes(random, 500 * 1024), RandomBytes(random, 1024 * 1024)];
    }

    /// <summary>
    /// An mbox file in the mboxrd variant with <see cref="MboxMessageCount"/> messages, with LF line breaks and only seven-bit characters, as RFC 4155 Appendix A requires, about 74 MB in
    /// all.  Most are plain text, some of it needing a quote, and 1 in 10 has a small base64 attachment.
    /// </summary>
    public static byte[] CreateMbox()
    {
        var random = new Random(Seed);
        using var output = new MemoryStream(64 * 1024 * 1024);
        using var writer = new StreamWriter(output, new ASCIIEncoding(), 64 * 1024) { NewLine = "\n" };
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < MboxMessageCount; index++)
        {
            var date = start.AddMinutes(index * 7);
            writer.WriteLine($"From sender{index % 50}@example.com {date.ToString("ddd MMM", System.Globalization.CultureInfo.InvariantCulture)} {date.Day,2} {date:HH:mm:ss} {date.Year}");
            writer.WriteLine($"Received: from mx.example.net (mx.example.net [192.0.2.1]) by mx.example.org with ESMTPS id {index:x8}; {Rfc5322(date)}");
            writer.WriteLine($"From: Sender {index % 50} <sender{index % 50}@example.com>");
            writer.WriteLine("To: Recipient <recipient@example.org>");
            writer.WriteLine($"Subject: Message number {index} of the archive");
            writer.WriteLine($"Date: {Rfc5322(date)}");
            writer.WriteLine($"Message-ID: <{index}@mail.example.com>");
            writer.WriteLine("MIME-Version: 1.0");
            if (index % 10 == 0)
            {
                writer.WriteLine("Content-Type: multipart/mixed; boundary=\"=_part\"");
                writer.WriteLine();
                writer.WriteLine("--=_part");
                writer.WriteLine("Content-Type: text/plain; charset=us-ascii");
                writer.WriteLine();
                WriteParagraphs(writer, random, 4);
                writer.WriteLine("--=_part");
                writer.WriteLine("Content-Type: application/octet-stream");
                writer.WriteLine("Content-Disposition: attachment; filename=data.bin");
                writer.WriteLine("Content-Transfer-Encoding: base64");
                writer.WriteLine();
                WriteBase64(writer, RandomBytes(random, 32 * 1024));
                writer.WriteLine("--=_part--");
            }
            else
            {
                writer.WriteLine("Content-Type: text/plain; charset=us-ascii");
                writer.WriteLine();
                WriteParagraphs(writer, random, random.Next(2, 12));
            }

            writer.WriteLine();
        }

        writer.Flush();
        return output.ToArray();
    }

    /// <summary>
    /// Throws if a benchmark doesn't give the result expected, so that a library that reads the corpus differently from the other is noticed, and not reported as being faster.
    /// </summary>
    public static void Check<T>(string benchmark, T actual, T expected)
        where T : IEquatable<T>
    {
        if (!actual.Equals(expected))
        {
            throw new InvalidOperationException($"{benchmark} gave {actual}, where {expected} was expected.");
        }
    }

    private static byte[] CreateMessageWithAttachments(params int[] attachmentSizes)
    {
        var random = new Random(Seed);
        var capacity = 16 * 1024 + attachmentSizes.Sum(size => size / 3 * 4 + size / 57 * 2);
        using var output = new MemoryStream(capacity);
        using var writer = new StreamWriter(output, new ASCIIEncoding(), 64 * 1024) { NewLine = "\r\n" };
        writer.WriteLine("From: Ana Lima <ana@example.com>");
        writer.WriteLine("To: Ben Okafor <ben@example.org>");
        writer.WriteLine("Subject: The files you asked for");
        writer.WriteLine("Date: Wed, 4 Nov 2026 09:30:00 +1300");
        writer.WriteLine("Message-ID: <20261104093000.4711@mail.example.com>");
        writer.WriteLine("MIME-Version: 1.0");
        writer.WriteLine("Content-Type: multipart/mixed; boundary=\"=_mixed\"");
        writer.WriteLine();
        writer.WriteLine("--=_mixed");
        writer.WriteLine("Content-Type: multipart/alternative; boundary=\"=_alternative\"");
        writer.WriteLine();
        writer.WriteLine("--=_alternative");
        writer.WriteLine("Content-Type: text/plain; charset=us-ascii");
        writer.WriteLine();
        WriteParagraphs(writer, random, 8);
        writer.WriteLine("--=_alternative");
        writer.WriteLine("Content-Type: text/html; charset=us-ascii");
        writer.WriteLine();
        writer.WriteLine("<html><body>");
        WriteParagraphs(writer, random, 8);
        writer.WriteLine("</body></html>");
        writer.WriteLine("--=_alternative--");
        for (var index = 0; index < attachmentSizes.Length; index++)
        {
            writer.WriteLine("--=_mixed");
            writer.WriteLine("Content-Type: application/octet-stream");
            writer.WriteLine($"Content-Disposition: attachment; filename=attachment{index}.bin");
            writer.WriteLine("Content-Transfer-Encoding: base64");
            writer.WriteLine();
            WriteBase64(writer, RandomBytes(random, attachmentSizes[index]));
        }

        writer.WriteLine("--=_mixed--");
        writer.Flush();
        return output.ToArray();
    }

    private static string Rfc5322(DateTime date) => date.ToString("ddd, d MMM yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " +0000";

    private static byte[] RandomBytes(Random random, int length)
    {
        var bytes = new byte[length];
        random.NextBytes(bytes);
        return bytes;
    }

    private static void WriteBase64(TextWriter writer, byte[] content)
    {
        var encoded = Convert.ToBase64String(content);
        for (var index = 0; index < encoded.Length; index += Base64LineLength)
        {
            writer.WriteLine(encoded.AsSpan(index, Math.Min(Base64LineLength, encoded.Length - index)));
        }
    }

    private static void WriteParagraphs(TextWriter writer, Random random, int count)
    {
        for (var paragraph = 0; paragraph < count; paragraph++)
        {
            var start = random.Next(0, 100);
            var text = Lipsum.AsSpan(start);
            while (!text.IsEmpty)
            {
                var length = Math.Min(72, text.Length);
                writer.WriteLine(text[..length]);
                text = text[length..];
            }

            if (random.Next(8) == 0)
            {
                // A line that mboxrd quotes, and that a reader of that variant gets back without the quote.
                writer.WriteLine(">From here on, the line is quoted.");
            }

            writer.WriteLine();
        }
    }
}
