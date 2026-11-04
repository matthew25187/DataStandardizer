---
title: Read, filter and rewrite an mbox file
parent: File.InternetMessage
grand_parent: Packages
nav_order: 6
---

# Read, filter and rewrite an mbox file

An mbox file holds many messages, each after a separator line (RFC 4155). `MboxReader` reads
them one at a time, so a mailbox of any size takes the same memory.

## Read the messages

`ReadMessage` gives each message with its separator line, as an `MboxEntry`, and `null` at the
end of the file:

```csharp
using DataStandardizer.File.InternetMessage;

using var reader = new MboxReader("archive.mbox");
while (reader.ReadMessage() is { } entry)
{
    Console.WriteLine($"{entry.Envelope.Sender?.Address} {entry.Envelope.Date:u} {entry.Message.Subject}");
}
```

```text
ana@example.com 2026-11-01 09:00:01Z Message 1
ana@example.com 2026-11-02 09:00:01Z Message 2
ana@example.com 2026-11-03 09:00:01Z Message 3
```

`entry.Message` is an [`ImfMessage`](read-a-message.md), with all that gives. `entry.Envelope` is
the separator line, and its sender and date say who delivered the message and when, which may
differ from its From and Date fields.

## Filter into a new file

`WriteMessages` takes one entry at a time from an enumerator, so a filter over a reader takes
the same memory for any number of messages:

```csharp
using var reader = new MboxReader("archive.mbox");
using var writer = new MboxWriter("filtered.mbox");

writer.WriteMessages(Filter(reader));

Console.WriteLine($"Read {reader.MessagesRead}, wrote {writer.MessagesWritten}");

static IEnumerable<MboxEntry> Filter(MboxReader source)
{
    while (source.ReadMessage() is { } entry)
    {
        if (entry.Message.From?.OfType<ImfMailbox>().Any(mailbox => mailbox.Domain == "example.net") != true)
        {
            yield return entry;
        }
    }
}
```

```text
Read 3, wrote 2
```

Mailbox locking and changing a mailbox in place aren't supported, so write to a new file. A
message you keep is copied byte for byte, with its separator line, when the variant and line
breaks stay the same. A file-path constructor overwrites a file that's there. To append, give
`MboxWriter` a stream positioned at the end of the file.

To add a message, give it a separator line of your own:

```csharp
var envelope = new MboxEnvelope(new ImfMailbox("ana", "example.com"), DateTimeOffset.UtcNow);
writer.WriteMessage(envelope, message);
```

## Go through a mailbox without holding messages

`ReadMessage` holds one whole message at a time. If some are too large for that, or you need
only their headers, `ReadEntityEvents` gives an `ImfEntityReader` for each message:

```csharp
using var reader = new MboxReader("archive.mbox");
while (reader.ReadEntityEvents() is { } events)
{
    while (events.ReadEvent() is { } entityEvent)
    {
        if (entityEvent.Kind == ImfEntityEventKind.HeaderField
            && entityEvent.Depth == 0
            && entityEvent.Field.GetName().Equals("Subject", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"{reader.Envelope}: {entityEvent.Field.GetDecodedValue()}");
        }
    }
}
```

```text
From ana@example.com Sun Nov  1 09:00:01 2026: Message 1
From ana@example.com Mon Nov  2 09:00:01 2026: Message 2
From ana@example.com Tue Nov  3 09:00:01 2026: Message 3
```

The reader it gives is disposed by the next read, and you can move on before it has ended.
What's left of the message is skipped. See
[Stream a large message](stream-a-large-message.md).

## Choose a variant

The mbox variants differ in how they keep a line starting `From ` from being taken for a
separator line. A file can't reliably say which it uses, so you have to. The default is
`Mboxrd`:

| `MboxVariant` | How it works | Reads back exactly |
| --- | --- | --- |
| `Mboxrd` | Any line matching `>*From ` gets one more `>`, and reading takes one off. | Yes |
| `Mboxo` | A line starting `From ` gets a `>`. Reading can't tell that from a line that had one. | No: the quotes stay |
| `Mboxcl` | The quoting of `Mboxo`, and a Content-Length field. | No: the quotes stay |
| `Mboxcl2` | No quoting, and a Content-Length field that finds the end of the message. | Yes |

```csharp
var options = new ImfOptions();
using var reader = new MboxReader("archive.mbox", MboxVariant.Mboxcl2, options);
```

## Read files from Thunderbird and others

Many mailboxes on disk don't follow RFC 4155 Appendix A, so a reader refuses them by default:

```text
The first line starts with "From " and doesn't have the form of a separator line (RFC 4155 Appendix A).  ImfOptions.AllowInvalidMboxSeparator accepts other forms.
```

Each difference has an option of its own:

| Difference | Option |
| --- | --- |
| A separator line in another form, such as `From - Thu Nov 05 08:12:45 2026` | `AllowInvalidMboxSeparator` |
| Octets above 127, such as raw UTF-8 in a message | `AllowEightBitMboxContent` |
| CRLF line breaks | `AllowMboxCarriageReturnLineFeed` |
| Data before the first separator line | `AllowMissingMboxSeparator` |
| A message with no empty line after it | `AllowMissingMboxBlankLine` |
| A Content-Length field that is missing or wrong, in the `Mboxcl` and `Mboxcl2` variants | `AllowInvalidMboxContentLength` |

```csharp
var options = new ImfOptions
{
    AllowInvalidMboxSeparator = true,
    AllowEightBitMboxContent = true
};

using var reader = new MboxReader("Inbox", MboxVariant.Mboxrd, options);
while (reader.ReadMessage() is { } entry)
{
    foreach (var diagnostic in entry.Diagnostics)
    {
        Console.WriteLine($"{diagnostic.Kind}: {diagnostic.Message}");
    }
}
```

With a separator line in another form, `Envelope.Sender` or `Envelope.Date` is `null` if it
can't be understood, and `Envelope.RawLine` keeps the line.

## Handle problems

There are two kinds of error, and `ImfFileException.DataItemName.MessageIndex` says which
message each one came from:

- **A problem with a message** that the file holds correctly ends only that message, and the
  next read goes on to the next one, so you can skip a bad message:

  ```csharp
  while (true)
  {
      try
      {
          if (reader.ReadMessage() is not { } entry)
          {
              break;
          }

          Process(entry);
      }
      catch (ImfFileException exception)
      {
          Console.WriteLine($"Skipped: {exception.Message}");
      }
  }
  ```

- **A problem with the format of the file** ends reading, because where the next message starts
  is then unknown. The next read throws `InvalidOperationException`, which the loop above
  doesn't catch, so it stops there.

`MaxMessageCount` limits the messages a reader takes from a file, and the other limits apply to
each message. See [Limits for hostile input](../concepts/hostile-input-limits.md).

## Move messages to and from other files

A message read from an mbox file has LF line breaks. Writing it with an `ImfWriter` gives it
CRLF, as RFC 5322 §2.1 requires, so it can be saved as an `.eml` file. An `MboxWriter` does the
reverse. See [Byte-exact round trips](../concepts/round-trips.md#line-breaks-in-other-file-formats).

RFC 4155 Appendix A requires seven-bit characters, so an `MboxWriter` refuses a message with an
octet above 127, unless `AllowEightBitMboxContent` is set. A body written by this package gets a
transfer encoding that keeps to seven bits by default.

## Related

- [One message format, several file formats](../concepts/file-formats.md)
- [Stream a large message](stream-a-large-message.md)
- [MboxReader](../reference/MboxReader.md), [MboxWriter](../reference/MboxWriter.md),
  [MboxEntry](../reference/MboxEntry.md) and [MboxEnvelope](../reference/MboxEnvelope.md)
