---
title: One message format, several file formats
parent: File.InternetMessage
grand_parent: Packages
nav_order: 14
---

# One message format, several file formats

EML, MHTML, mbox and EMLX all hold the same thing: an Internet message, as RFC 5322 and
MIME define it. That is why they're in one package, named for the message and not for a
file extension.

| Extension | Format | What the file holds | Standard |
| --- | --- | --- | --- |
| `.eml` | EML | One message, with nothing around it. | RFC 5322, MIME |
| `.mht`, `.mhtml` | MHTML | One message whose body is a web page and its resources. | RFC 2557 |
| `.mbox` | mbox | Many messages, each after a separator line. | RFC 4155 |
| `.emlx`, `.partial.emlx` | EMLX | One message, with a byte count before it and a property list after it. | None published |

Each reader gives an `ImfMessage`, and the code that works with messages is the same
whichever file one came from:

```csharp
using DataStandardizer.File.InternetMessage;

static void Describe(ImfMessage message) =>
    Console.WriteLine($"{message.Date}: {message.Subject} ({message.Attachments.Count} attachments)");

using (var reader = new ImfReader("message.eml"))
{
    Describe(reader.ReadMessage());
}

using (var reader = new MhtmlReader("page.mhtml"))
{
    Describe(reader.ReadDocument().Message);
}

using (var reader = new MboxReader("archive.mbox"))
{
    while (reader.ReadMessage() is { } entry)
    {
        Describe(entry.Message);
    }
}

using (var reader = new EmlxReader("123.emlx"))
{
    Describe(reader.ReadEntry().Message);
}
```

## EML

The base format. `ImfReader` and `ImfWriter` work with the file as it is, and the data
holds one message.

## MHTML

Renaming an `.eml` file to `.mht` changes no bytes. An MHTML file is a message, usually
with a `multipart/related` body, so `MhtmlReader` reads it as `ImfReader` does. What it
adds is `MhtmlDocument`, a view of the message that finds the root of the page, and
resolves the URLs that the page uses to the parts that hold them. See
[Open a saved web page](../how-to/open-a-saved-web-page.md).

## mbox

An mbox file is many messages, joined. Before each is a separator line, starting
`From `, and after each is an empty line. The file uses LF line breaks and seven-bit
characters, as RFC 4155 Appendix A requires, and a line of a message that starts `From `
has to be kept from being taken for a separator line. How that's done is what separates
the variants, and a file can't reliably say which it uses, so you have to. See
[Read, filter and rewrite an mbox file](../how-to/read-and-rewrite-mbox.md).

## EMLX

An EMLX file is what Apple Mail keeps for each message: the length of the message in
bytes on a line of its own, the message with LF line breaks, and an XML property list of
metadata, such as flags and dates. Apple doesn't document the format, so the package
follows the layout that open-source readers agree on, and keeps any key it doesn't know.
A partial file leaves large parts in `.emlxpart` files of their own. See
[Read Apple Mail EMLX files](../how-to/read-emlx.md).

## Moving a message between them

A message read from one kind of file can be written to any other. A file's line break
isn't part of the message: an `ImfWriter` writes CRLF and an `MboxWriter` or `EmlxWriter`
writes LF, so a message changes in that way when it moves. See
[Byte-exact round trips](round-trips.md#line-breaks-in-other-file-formats).

```csharp
// Save each message of a mailbox as an .eml file.
using var reader = new MboxReader("archive.mbox");
while (reader.ReadMessage() is { } entry)
{
    using var writer = new ImfWriter($"{reader.MessagesRead}.eml");
    writer.WriteMessage(entry.Message);
}
```

Outlook `.msg` files aren't here. They hold an OLE compound file of MAPI properties,
not an Internet message, so they would need a package of their own.

## Related

- [The layers](layers.md)
- [Byte-exact round trips](round-trips.md)
- [API reference](../reference/index.md)
