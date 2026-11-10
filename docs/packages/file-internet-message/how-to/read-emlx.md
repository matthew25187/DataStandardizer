---
title: Read Apple Mail EMLX files
parent: File.InternetMessage
grand_parent: Packages
nav_order: 7
---

# Read Apple Mail EMLX files

Apple Mail keeps each message in an EMLX file: its length in bytes on a line of its own, the
message, and an XML property list of metadata. `EmlxReader` reads one into an `EmlxEntry`.

Apple doesn't document the format, so the package follows the layout that open-source readers
agree on. Anything it doesn't know in the property list is kept, not rejected.

## Read a file

```csharp
using DataStandardizer.File.InternetMessage;

using var reader = new EmlxReader("123.emlx");
var entry = reader.ReadEntry();

Console.WriteLine(entry.Message.Subject);
Console.WriteLine(entry.Metadata.DateReceived);
Console.WriteLine(entry.Metadata.MessageFlags);
```

```text
Saved by Mail
1/Nov/2026 09:00:05 +00:00
Read, Answered
```

`entry.Message` is an [`ImfMessage`](read-a-message.md), with all that gives. A file holds one
entry, so it can be read only once.

## Read the metadata

`EmlxMetadata` has properties for the keys that are well known:

| Property | Key | Meaning |
| --- | --- | --- |
| `DateReceived`, `DateLastViewed` | `date-received`, `date-last-viewed` | Dates, in UTC. |
| `Subject` | `subject` | The subject, which Mail keeps apart from the message. |
| `RemoteId` | `remote-id` | The identifier of the message on its server. |
| `Flags` | `flags` | The whole integer. |
| `MessageFlags` | `flags` | The single-bit flags, as `EmlxMessageFlags`. |
| `AttachmentCount`, `PriorityLevel` | `flags` | Values that the `flags` key holds. |

These come from community documentation, and may not be right for every version of Mail. A key
whose value isn't of the kind expected gives `null` from its property, and the value itself is
still in `Entries`.

Every key and value is in `Entries`, in order, whether the package knows it or not:

```csharp
foreach (var pair in entry.Metadata.Entries)
{
    Console.WriteLine($"{pair.Key} {pair.Value.Kind} {pair.Value.Text}");
}

if (entry.Metadata.TryGetValue("x-my-key", out var value))
{
    Console.WriteLine(value.Text);
}
```

```text
date-received Integer 1793523605
flags Integer 5
remote-id String 4711
subject String Saved by Mail
x-my-key String kept
kept
```

## Read a partial file

Mail may keep a large part of a message in an `.emlxpart` file of its own, and leave an entity
with an `X-Apple-Content-Length` field and an empty body. So `123.partial.emlx` keeps part 2 in
`123.2.emlxpart`.

With nothing else given, the message is read without that content. No option is needed. The
reader records a `MissingEmlxPart` diagnostic, and lists the part in `MissingParts`:

```csharp
using var reader = new EmlxReader("456.partial.emlx");
var entry = reader.ReadEntry();

foreach (var part in entry.MissingParts)
{
    Console.WriteLine($"{part.Path} {part.GetPartFileName("456.partial.emlx")} {part.Entity.FileName}");
}
```

```text
2 456.2.emlxpart big.pdf
```

To have the content, give `ReadEntry` an `EmlxPartResolver`. It returns a stream of the bytes of
the part, still in its transfer encoding, which the reader reads to its end and disposes, or
`null` if the content isn't available:

```csharp
using var reader = new EmlxReader("456.partial.emlx");
var entry = reader.ReadEntry(part =>
{
    var path = part.GetPartFileName("456.partial.emlx");
    return File.Exists(path) ? File.OpenRead(path) : null;
});

var pdf = entry.Message.Attachments[0].GetContent();
```

`GetPartFileName` only works out the name. Opening the file is left to the resolver, so a
reader of a file from elsewhere can check the path, and a message that isn't on disk can be
supplied from wherever it is.

`ContentLength` gives the value of the `X-Apple-Content-Length` field. Community documentation
doesn't say whether it counts the bytes before or after the transfer encoding, so it is for
information only.

## Save a message as an .eml file

A message in an EMLX file has LF line breaks. Writing it with an `ImfWriter` gives it the CRLF
that RFC 5322 §2.1 requires:

```csharp
using var writer = new ImfWriter("123.eml");
writer.WriteMessage(entry.Message);
```

A message with CRLF line breaks in an EMLX file isn't one the reader accepts, as it reads a line
feed as the line break.

## Write a file

`EmlxWriter` works out the byte count, and writes the message with LF line breaks:

```csharp
var metadata = new EmlxMetadata
{
    DateReceived = DateTimeOffset.UtcNow,
    Flags = (long)(EmlxMessageFlags.Read | EmlxMessageFlags.Answered),
    RemoteId = "4711"
};
metadata.Set("x-my-key", EmlxPlistValue.FromString("kept"));

using var writer = new EmlxWriter("123.emlx");
writer.WriteEntry(new EmlxEntry(message, metadata));
```

An entry that was read and hasn't changed is written back byte for byte. A change to the metadata
writes the whole property list again, as Apple's tools lay it out, with the keys that weren't
changed still in order. A file holds one entry, so a second write throws
`InvalidOperationException`.

## Read damaged files

| Problem | Option | With it |
| --- | --- | --- |
| The first line isn't a byte count, or the count doesn't fit the file | `AllowInvalidEmlxByteCount` | The reader discards the line, and takes the last `<?xml`, or failing that the last `<plist`, as the start of the property list. This is a best guess: in a file with no property list, a message that holds such text is cut short there. |
| The property list is missing or not well formed | `AllowInvalidEmlxMetadata` | The metadata has no entries, `IsWellFormed` is `false`, and `RawBytes` keeps the bytes. |

Each records a diagnostic in `entry.Diagnostics`.

The reader holds the whole file while it reads it, so the limits in `ImfOptions` apply. See
[Limits for hostile input](../concepts/hostile-input-limits.md).

## Related

- [One message format, several file formats](../concepts/file-formats.md)
- [Read a message and its attachments](read-a-message.md)
- [EmlxReader](../reference/EmlxReader.md), [EmlxWriter](../reference/EmlxWriter.md),
  [EmlxEntry](../reference/EmlxEntry.md), [EmlxMetadata](../reference/EmlxMetadata.md) and
  [EmlxMissingPart](../reference/EmlxMissingPart.md)
