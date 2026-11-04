---
title: Build and write a message
parent: File.InternetMessage
grand_parent: Packages
nav_order: 3
---

# Build and write a message

`ImfMessageBuilder` makes a message from its header, its text, and what goes with the text,
in the structure those call for. `ImfWriter` writes it.

## Build a message

```csharp
using DataStandardizer.Communication.Mime;
using DataStandardizer.File.InternetMessage;
using System.Text;

var builder = new ImfMessageBuilder();

var header = builder.Header;
header.From = ImfAddressList.Parse("Ana Lima <ana@example.com>");
header.To = ImfAddressList.Parse("Ben Okafor <ben@example.org>, team: cara@example.org, dan@example.org;");
header.Subject = "Résumé attached";
header.Date = new ImfDateTime(DateTimeOffset.Now);
header.MessageId = new ImfMessageId("20261104.1", "example.com");

var logo = new ImfMessageId("logo.1", "example.com");
builder
    .SetText("Hi Ben,\nMy résumé is attached.\n")
    .SetHtml($"<p>Hi Ben,</p><p>My résumé is attached.</p><img src=\"{logo.ToCidUri()}\">")
    .AddInlineResource(logo, MimeMediaType.Parse("image/png"), File.ReadAllBytes("logo.png"))
    .AddAttachment("resume.pdf", MimeMediaType.Parse("application/pdf"), File.ReadAllBytes("resume.pdf"));

var message = builder.Build();

using var writer = new ImfWriter("outgoing.eml");
writer.WriteMessage(message);
```

The file starts like this:

```text
From: Ana Lima <ana@example.com>
To: Ben Okafor <ben@example.org>, team: cara@example.org, dan@example.org;
Subject: =?UTF-8?B?UsOpc3Vtw6k=?= attached
Date: 4 Nov 2026 09:30:00 +1300
Message-ID: <20261104.1@example.com>
MIME-Version: 1.0
Content-Type: multipart/mixed; boundary="=_994b5582fc0744899a69c93d489dd38f"

--=_994b5582fc0744899a69c93d489dd38f
Content-Type: multipart/alternative;
 boundary="=_8e43820029ec461dad9a799547574425"
```

`Build` chooses the structure, so you don't have to:

| You give | Structure |
| --- | --- |
| Text or HTML alone | It is the body of the message. |
| Text and HTML | `multipart/alternative`, with the text first, as RFC 2046 §5.1.4 has the best last. |
| HTML and inline resources | `multipart/related`, with the HTML as its root, inside any `multipart/alternative`. |
| Attachments | `multipart/mixed`, after all of that. |

Text and HTML are in UTF-8, unless you pass a `MimeCharset`, and each line break of text
becomes a CRLF. To attach a message, as when forwarding one, pass it to
`AddAttachment(ImfMessage)`.

> **Media types.** `MimeMediaType.Parse` takes a media type with no whitespace or comments:
> `text/plain;charset=utf-8`. To read the body of a Content-Type field, which may have
> them, use `ImfMimeFields.ParseContentType`. A multipart type needs a `boundary`, so for
> one you want the writer to choose, use a constant such as `MimeMediaType.MultipartMixed`.

## What you have to set

The builder doesn't make up anything you didn't give it. RFC 5322 §3.6 requires Date and
From, and §3.6.2 a Sender when From has more than one mailbox, so a writer refuses a
message without them:

```text
The header has no Date field, which RFC 5322 §3.6 requires.
```

It checks the whole message first, so a message it refuses leaves nothing in the output,
and you can go on with another. To see every problem at once, call `Validate`:

```csharp
foreach (var diagnostic in message.Validate())
{
    Console.WriteLine($"{diagnostic.Kind} {diagnostic.FieldName}: {diagnostic.Message}");
}
```

A Message-ID is optional, but RFC 5322 §3.6.4 says that a message SHOULD have one, so set
it. Only you know what is unique under your domain.

## Header text that isn't ASCII

By default, a writer writes such text as RFC 2047 encoded words, as in the Subject above.
With `HeaderEncoding = ImfHeaderEncoding.Utf8`, it writes UTF-8 (RFC 6532). Use that only if
what receives the message accepts it, such as a server that supports SMTPUTF8.

```csharp
var options = new ImfOptions { HeaderEncoding = ImfHeaderEncoding.Utf8 };
using var writer = new ImfWriter("outgoing.eml", options);
```

## Transfer encodings

A writer chooses one for each body. It takes `7bit` where that fits, and otherwise whichever
of `quoted-printable` and `base64` comes out smaller, so a message can pass through any
mail system. `8bit` and `binary` need a destination that can carry them, so they are used
only if you set `AllowEightBitTransferEncoding` or `AllowBinaryTransferEncoding`.

To ask for one, set `ImfEntity.ContentTransferEncoding` on the entity. The writer refuses
one that doesn't fit the data.

## Write several messages, or asynchronously

Any number of messages can be written to the one writer, one after another, with nothing
between them. To keep them apart in one file, use an
[mbox file](read-and-rewrite-mbox.md).

`ImfWriter` also takes a `Stream`, which it leaves open, and that's the only choice on .NET
Standard 1.1. On .NET Standard 2.0 and later, `WriteMessageAsync` does the same without
blocking:

```csharp
await using var writer = new ImfWriter("outgoing.eml");
await writer.WriteMessageAsync(message, cancellationToken: cancellationToken);
```

A file-path constructor overwrites a file that is already there.

## Build a tree yourself

For a structure the builder doesn't make, set `ImfMessage.Body` to a tree of `ImfMultipart`,
`ImfMessagePart` and `ImfEntity` objects:

```csharp
var report = new ImfMultipart(MimeMediaType.MultipartMixed);

var text = new ImfEntity { ContentType = MimeMediaType.Parse("text/plain;charset=utf-8") };
text.SetText("Attached.\n");
report.Parts.Add(text);

var message = new ImfMessage
{
    From = ImfAddressList.Parse("ana@example.com"),
    Date = new ImfDateTime(DateTimeOffset.Now),
    Body = report
};
```

To write a message without holding its bodies in memory, see
[Stream a large message](stream-a-large-message.md).

## Related

- [Read a message and its attachments](read-a-message.md)
- [Edit one header without breaking a signature](edit-a-header.md)
- [Compliant defaults and the opt-ins](../concepts/compliant-defaults.md)
- [ImfMessageBuilder](../reference/ImfMessageBuilder.md) and
  [ImfWriter](../reference/ImfWriter.md)
