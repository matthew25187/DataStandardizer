---
title: Read a message and its attachments
parent: File.InternetMessage
grand_parent: Packages
nav_order: 1
---

# Read a message and its attachments

`ImfReader` reads a whole message into an `ImfMessage`, which holds it in memory and has
typed accessors for its fields and its parts. For a message too large to hold, see
[Stream a large message](stream-a-large-message.md).

## Read the fields

```csharp
using DataStandardizer.File.InternetMessage;

using var reader = new ImfReader("message.eml");
var message = reader.ReadMessage();

Console.WriteLine(message.Subject);
Console.WriteLine(message.From);
Console.WriteLine(message.Date);
```

```text
Résumé attached
Ana Lima <ana@example.com>
4 Nov 2026 09:30:00 +1300
```

`ImfReader` also takes a `Stream`, and that is the only choice on .NET Standard 1.1. It
leaves a stream you give it open.

An accessor gives `null` if the field is absent. `Subject` and display names are decoded
from RFC 2047 encoded words. `Date` is an `ImfDateTime`, which keeps what a date in a
message can say and a `DateTimeOffset` can't, such as an unknown UTC offset (`-0000`) or
a leap second:

```csharp
if (message.Date is { IsOffsetUnknown: false } date)
{
    DateTimeOffset value = date.ToDateTimeOffset();
}
```

Fields without an accessor are in `Fields`. Lookups by name ignore case:

```csharp
var mailer = message.Fields.Find("X-Mailer")?.GetDecodedValue();
```

## Read addresses

`From`, `To`, `Cc`, `Bcc` and `ReplyTo` are each an `ImfAddressList`. An address in it is
an `ImfMailbox`, or an `ImfGroup` that holds mailboxes:

```csharp
foreach (var address in message.To!)
{
    switch (address)
    {
        case ImfMailbox mailbox:
            Console.WriteLine($"{mailbox.DisplayName} at {mailbox.Address}");
            break;
        case ImfGroup group:
            Console.WriteLine($"{group.DisplayName}: {string.Join(", ", group.Mailboxes.Select(m => m.Address))}");
            break;
    }
}
```

```text
Ben Okafor at ben@example.org
team: cara@example.org, dan@example.org
```

## Read the text

`TextBody` and `HtmlBody` find the entity that holds the text of the message, following
its structure: the last suitable part of a `multipart/alternative` entity, as RFC 2046
§5.1.4 puts the best last, and the root of a `multipart/related` entity. They skip
attachments, and what is inside an embedded message. `GetText` decodes the entity from its
transfer encoding and its character set:

```csharp
Console.WriteLine(message.TextBody?.GetText());
Console.WriteLine(message.HtmlBody?.GetText());
```

Either can be `null`. Line breaks are those of the content, which are CRLF in a `text`
entity that follows RFC 2046 §4.1.1.

A character set such as `windows-1252` needs a step of its own on .NET Core and later.
See [Read text in other character sets](register-code-pages.md).

## Save the attachments

`Attachments` lists each entity with a Content-Disposition of `attachment`, and each
entity without parts that isn't text and has a file name. `OpenContentStream` gives the
bytes, decoded from the transfer encoding, and `GetContent` gives the same bytes as memory:

```csharp
foreach (var attachment in message.Attachments)
{
    // A multipart entity has parts and no content of its own.
    if (attachment is ImfMultipart)
    {
        continue;
    }

    using var content = attachment.OpenContentStream();

    // A file name comes from the sender, so keep only its last segment, and don't overwrite a file.
    var fileName = Path.GetFileName(attachment.FileName ?? "attachment.bin");
    using var file = new FileStream(Path.Combine(saveFolder, fileName), FileMode.CreateNew, FileAccess.Write);
    content.CopyTo(file);
}
```

A file name comes from whoever sent the message, so it may hold a path, or name a file
you don't want overwritten. RFC 2183 §2.3 says not to respect any directory path in it, and
`FileName` doesn't check it. Here `FileMode.CreateNew` throws `IOException` if the file is
already there, which stops the loop, so choose a name of your own if you need to go on. See
[Limits for hostile input](../concepts/hostile-input-limits.md#what-the-limits-dont-cover).

For a multipart attachment, `GetContent` and `OpenContentStream` throw
`InvalidOperationException`. To save what it holds, go through its `Parts`.

## Walk the parts

`EnumerateEntities` gives every entity, depth first, including those of embedded messages.
Each `ImfEntityNode` has the entity, its depth and its path, numbered as IMAP numbers the
parts of a message:

```csharp
foreach (var node in message.EnumerateEntities())
{
    Console.WriteLine($"[{node.Path}] {node.Entity.GetType().Name} {node.Entity.ContentType}");
}
```

```text
[] ImfMultipart multipart/mixed;boundary="=_994b5582"
[1] ImfMultipart multipart/alternative;boundary="=_8e438200"
[1.1] ImfEntity text/plain;charset=UTF-8
[1.2] ImfMultipart multipart/related;type="text/html";boundary="=_f8dc65fa"
[1.2.1] ImfEntity text/html;charset=UTF-8
[1.2.2] ImfEntity image/png
[2] ImfEntity application/pdf
```

An `ImfMultipart` has `Parts`, an `ImfMessagePart` has the `Message` it holds, and any
other entity has content. `Body` is the first of these, and its fields are those of the
message.

## Find an inline image

HTML refers to an image in the same message with a `cid:` URI (RFC 2392).
`FindByContentId` finds the part:

```csharp
var image = message.FindByContentId("cid:logo.1@example.com");
```

It gives `null` if the message has no such part, and doesn't look inside an embedded
message, as a `cid:` URI refers to the message that holds it.

## Handle problems

A message that breaks a rule throws `ImfFileException`, and reading then ends:

```csharp
try
{
    var message = reader.ReadMessage();
}
catch (ImfFileException exception)
{
    Console.WriteLine(exception.Message);
    if (exception.Data.Contains(ImfFileException.DataItemName.LineNumber))
    {
        Console.WriteLine($"On line {exception.Data[ImfFileException.DataItemName.LineNumber]}");
    }
}
```

Some problems don't stop a reader, because RFC 5322 §4 requires it to accept them. A
missing Date, for one, is recorded in `reader.Diagnostics`. To accept others, turn on the
option for each. See [Compliant defaults and the opt-ins](../concepts/compliant-defaults.md).

The bytes of a body are decoded only when you ask for them, so a body that isn't valid in
its transfer encoding throws `ImfFileException` from `GetContent` or `GetText`, not from
`ReadMessage`.

## Read asynchronously

On .NET Standard 2.0 and later, `ReadMessageAsync` does the same without blocking, and
takes a `CancellationToken`:

```csharp
await using var reader = new ImfReader("message.eml");
var message = await reader.ReadMessageAsync(cancellationToken);
```

## Related

- [Stream a large message](stream-a-large-message.md)
- [Build and write a message](build-and-write-a-message.md)
- [The layers](../concepts/layers.md)
- [ImfReader](../reference/ImfReader.md), [ImfMessage](../reference/ImfMessage.md) and
  [ImfEntity](../reference/ImfEntity.md)
