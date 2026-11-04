---
title: Stream a large message
parent: File.InternetMessage
grand_parent: Packages
nav_order: 2
---

# Stream a large message

`ImfEntityReader` goes through a whole message as a sequence of events, and never holds a
whole body in memory. `ImfEntityWriter` writes one from a description, reading each body
from a stream a block at a time. Use them for a message too large to hold, or where you
only want part of one. For everything else, `ImfReader` and `ImfWriter` are simpler.

## Read the events

Each call to `ReadEvent` gives the next event, or `null` when the message has ended:

```csharp
using DataStandardizer.File.InternetMessage;

using var reader = new ImfEntityReader("large.eml");
while (reader.ReadEvent() is { } entityEvent)
{
    if (entityEvent.Kind == ImfEntityEventKind.HeaderEnd)
    {
        Console.WriteLine($"[{entityEvent.EntityPath}] {entityEvent.ContentType} {entityEvent.TransferEncoding}");
    }
}
```

Every entity gives these, in order. A multipart entity gives its parts in place of body
chunks, and an embedded message gives the message:

| Event | Gives |
| --- | --- |
| `EntityStart` | The depth and path of the entity. |
| `HeaderField` | `Field`, an `ImfHeaderFieldView`. |
| `HeaderEnd` | `ContentType` and `TransferEncoding`, which are known from here on. |
| `Preamble` | `Data`: the bytes before the first delimiter of a multipart entity. |
| `BodyChunk` | `Data`: the next bytes of the body. A body may take any number of these. |
| `Epilogue` | `Data`: the bytes after the close delimiter of a multipart entity. |
| `EntityEnd` | `ByteOffset` and `ByteLength`: the range of the whole entity in the data. |

The event is a view that doesn't allocate. It is valid until the next read, and after that
every member throws. A span taken from it can't be checked in that way, so copy anything
you need to keep.

Reading on with no copy means nothing is held. The reader still checks each body against
RFC 5322 and its transfer encoding as it goes, so an error can come from any read, and
ends reading.

## Save attachments to files

Set `DecodeTransferEncoding`, and each chunk is decoded from `quoted-printable` or `base64`
as it is read. This sample takes each attachment's file name from its Content-Disposition
field, and copies its body straight to a file:

```csharp
var options = new ImfOptions { DecodeTransferEncoding = true };
using var reader = new ImfEntityReader("large.eml", options);

string? fileName = null;
FileStream? output = null;
try
{
    while (reader.ReadEvent() is { } entityEvent)
    {
        switch (entityEvent.Kind)
        {
            case ImfEntityEventKind.HeaderField:
                var field = entityEvent.Field;
                if (field.GetName().Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase)
                    && ImfContentDisposition.TryParse(field.Value, out var disposition)
                    && disposition.IsAttachment)
                {
                    fileName = disposition.FileName;
                }
                break;

            case ImfEntityEventKind.HeaderEnd:
                // A multipart or message/rfc822 attachment holds entities, which come next with
                // their own headers, and has no body of its own to save.
                var type = entityEvent.ContentType.Type;
                if (fileName != null
                    && !type.Equals("multipart", StringComparison.OrdinalIgnoreCase)
                    && !type.Equals("message", StringComparison.OrdinalIgnoreCase))
                {
                    // A file name comes from the sender, so keep only its last segment, and don't
                    // overwrite a file.
                    var path = Path.Combine(saveFolder, Path.GetFileName(fileName));
                    output = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                }
                fileName = null;
                break;

            case ImfEntityEventKind.BodyChunk:
                output?.Write(entityEvent.Data);
                break;

            case ImfEntityEventKind.EntityEnd:
                output?.Dispose();
                output = null;
                break;
        }
    }
}
finally
{
    output?.Dispose();
}
```

`FileMode.CreateNew` throws `IOException` if the file is already there, which stops the loop,
so choose a name of your own if you need to go on. An error from `ReadEvent` leaves the file of
the attachment it was reading, with what had been read so far, so delete it if you don't want it.

An attached message is passed over here, and so is a `message/partial` or `message/external-body`
body. To save one whole, copy the range of the data that its `EntityEnd` event gives.

`Write(ReadOnlySpan<byte>)` is on .NET Standard 2.1 and later. On the other targets, use
`entityEvent.DataMemory` with a `MemoryMarshal.TryGetArray`, or copy to an array of your own.

`IsDecoded` says whether a chunk was decoded. A body with an extension transfer encoding
stays as it is, with a diagnostic in `reader.Diagnostics`.

## Read asynchronously

On .NET Standard 2.0 and later, `ReadEventAsync` does the same. A span can't cross an
`await`, so use `DataMemory`:

```csharp
await using var reader = new ImfEntityReader("large.eml", options);
while (await reader.ReadEventAsync(cancellationToken) is { } entityEvent)
{
    if (entityEvent.Kind == ImfEntityEventKind.BodyChunk)
    {
        await output.WriteAsync(entityEvent.DataMemory, cancellationToken);
    }
}
```

## Write a message from streams

A writer has to see all of a body to choose its transfer encoding, which comes before it
in the output, so it is given the whole message at once, as a tree of `ImfEntitySource`
objects. A body can be in memory or in a stream:

```csharp
using DataStandardizer.Communication.Mime;
using DataStandardizer.File.InternetMessage;
using System.Text;

using var content = File.OpenRead("video.bin");

var message = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[]
{
    ImfEntitySource.FromContent(
        MimeMediaType.Parse("text/plain;charset=us-ascii"),
        Encoding.ASCII.GetBytes("The file is attached.\r\n")),
    ImfEntitySource.FromContent(MimeMediaType.Parse("application/octet-stream"), content)
        .AddField("Content-Disposition", "attachment; filename=video.bin", ImfHeaderFieldKind.Structured),
});
message.AddField("From", "ana@example.com", ImfHeaderFieldKind.Structured)
    .AddField("Date", "Wed, 4 Nov 2026 09:30:00 +1300", ImfHeaderFieldKind.Structured)
    .AddTextField("Subject", "A large file");

using var writer = new ImfEntityWriter("large.eml");
writer.WriteMessage(message);
```

```text
From: ana@example.com
Date: Wed, 4 Nov 2026 09:30:00 +1300
Subject: A large file
MIME-Version: 1.0
Content-Type: multipart/mixed; boundary="=_de04ffaca38347028dce044db9b80641"

--=_de04ffaca38347028dce044db9b80641
Content-Type: text/plain; charset=us-ascii

The file is attached.

--=_de04ffaca38347028dce044db9b80641
Content-Disposition: attachment; filename=video.bin
Content-Type: application/octet-stream
Content-Transfer-Encoding: base64
```

The writer adds `MIME-Version`, each `Content-Type` and any `Content-Transfer-Encoding`.
It picks a random boundary and checks that the content doesn't hold it, and gives each
body the lightest transfer encoding that fits.

Looking at a body means reading a stream more than once, so it has to support seeking. A
stream that doesn't can be used only where nothing needs checking: with `TransferEncoding`
set to `base64`, for one. The writer leaves a stream open.

The writer checks the whole message before it writes any of it, so one it refuses leaves
nothing in the output.

## Copy an entity unchanged

`ImfEntitySource.FromRaw` describes an entity by its raw bytes, and the writer copies them.
That keeps a `multipart/signed` part valid. With an `ImfEntityReader`, the bytes of an entity
are the range that its `EntityEnd` event gives. See
[Byte-exact round trips](../concepts/round-trips.md).

## Related

- [Read a message and its attachments](read-a-message.md)
- [Read, filter and rewrite an mbox file](read-and-rewrite-mbox.md)
- [The layers](../concepts/layers.md)
- [ImfEntityReader](../reference/ImfEntityReader.md),
  [ImfEntityEvent](../reference/ImfEntityEvent.md),
  [ImfEntityWriter](../reference/ImfEntityWriter.md) and
  [ImfEntitySource](../reference/ImfEntitySource.md)
