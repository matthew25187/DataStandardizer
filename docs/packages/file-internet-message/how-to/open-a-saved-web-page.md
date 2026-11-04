---
title: Open a saved web page
parent: File.InternetMessage
grand_parent: Packages
nav_order: 5
---

# Open a saved web page

An MHTML file (`.mht`, `.mhtml`) is a web page saved with its resources, in one Internet
message (RFC 2557). `MhtmlReader` reads it into an `MhtmlDocument`, which finds the page and
resolves the URLs it uses to the parts that hold them.

## Read the page

```csharp
using DataStandardizer.File.InternetMessage;

using var reader = new MhtmlReader("page.mhtml");
var document = reader.ReadDocument();

Console.WriteLine(document.RootMediaType);
Console.WriteLine(document.Root.GetText());
```

```text
text/html;charset=UTF-8
<html><body><img src="images/logo.png"><link rel=stylesheet href="site.css"></body></html>
```

`Root` is the entity that holds the page: the part that the `start` parameter of the
`multipart/related` body names, or otherwise its first part. A page with no resources needs
no `multipart/related` entity, and then the body is the root.

The whole message is in `document.Message`, with all that an
[`ImfMessage`](read-a-message.md) gives.

## List the resources

`Resources` gives every part, including the root, each with the URI that its Content-Location
field resolves to:

```csharp
foreach (var resource in document.Resources)
{
    Console.WriteLine($"{resource.Uri} {resource.Entity.ContentType}");
}
```

```text
https://example.com/page.html text/html;charset=UTF-8
https://example.com/images/logo.png image/png
https://example.com/site.css text/css;charset=utf-8
```

`Uri` is `null` for a part that has no such field, such as one that the page refers to by
Content-ID.

## Resolve a URL

`TryResolve` finds the part that an absolute URI refers to. The URLs in the page are relative
to the page, so resolve them against its URI first:

```csharp
var pageUri = document.Resources[0].Uri!;
var imageUri = new Uri(pageUri, "images/logo.png");

if (document.TryResolve(imageUri, out var image))
{
    ReadOnlyMemory<byte> bytes = image.Entity.GetContent();
}
```

A URI is compared octet by octet, with no change to its `%` escapes, as RFC 2557 §8.2 says, and
a fragment is ignored. A URL that isn't in the file gives `false`: the file doesn't have it.
Whether to fetch it is your decision, and a saved page has no way to say that it's safe.

For a reference from a part other than the root, such as an image in a style sheet, pass that
part too. A reference reaches the parts of the `multipart/related` entity that holds it, and
of those around that one:

```csharp
document.TryResolve(imageUri, styleSheetResource, out var image);
```

A relative Content-Location resolves against the nearest absolute one of the entities around
it, then the base URI you give, and then `thismessage:/` (RFC 2557 §5). To give a base URI, pass
it to `ReadDocument`, or to the `MhtmlDocument` constructor:

```csharp
var document = reader.ReadDocument(new Uri("https://example.com/"));
```

A Content-Location labels a part, and doesn't say where it came from, which a sender can
misrepresent (RFC 2557 §11.1). Don't treat content as if it came from that URL.

## Read files from browsers and Word

Files in the wild don't all follow RFC 2557, so some need an option. Each is off by default:

| Written by | What it does | Option |
| --- | --- | --- |
| Chromium | `From: <Saved by Blink>`, which isn't a valid address | `AllowInvalidValues` |
| Word | A `multipart/related` entity with no `type` parameter | `AllowInvalidRelatedStructure` |
| Word | HTML in `windows-1252` | See [Read text in other character sets](register-code-pages.md) |

A page from Chromium is refused by default:

```text
The body of the From field does not follow its grammar (RFC 5322 §3.6, RFC 6854 §2.1).
```

With the option, it is read, and the problem is recorded:

```csharp
var options = new ImfOptions { AllowInvalidValues = true };

using var reader = new MhtmlReader("saved.mhtml", options);
var document = reader.ReadDocument();

foreach (var diagnostic in reader.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Kind} {diagnostic.FieldName}");
}
```

```text
InvalidFieldValue From
```

A missing Date or From is recorded in `Diagnostics` with no option, as it is for any message.

## Write a page

`MhtmlDocumentBuilder` makes a `multipart/related` body, whose `type` and `start` parameters name
the root, with each part labelled with its URL:

```csharp
using DataStandardizer.Communication.Mime;
using System.Text;

var builder = new MhtmlDocumentBuilder();
builder.Header.From = ImfAddressList.Parse("ana@example.com");
builder.Header.Date = new ImfDateTime(DateTimeOffset.Now);
builder.Header.Subject = "A saved page";

var document = builder
    .SetHtml(new Uri("https://example.com/page.html"), "<html>...</html>")
    .AddResource(new Uri("https://example.com/images/logo.png"), MimeMediaType.Parse("image/png"), logoBytes)
    .AddResource(new Uri("https://example.com/site.css"), MimeMediaType.Parse("text/css;charset=utf-8"), Encoding.UTF8.GetBytes(css))
    .Build();

using var writer = new MhtmlWriter("page.mhtml");
writer.WriteDocument(document);
```

The builder doesn't make up a Date or From, and a writer refuses a document without them,
under the same rules as any message. A document that was read and hasn't changed is written
back byte for byte.

An `MhtmlDocument` describes the message as it was when the document was made. After a change
to the message, make another.

## Related

- [One message format, several file formats](../concepts/file-formats.md)
- [Compliant defaults and the opt-ins](../concepts/compliant-defaults.md)
- [MhtmlReader](../reference/MhtmlReader.md), [MhtmlDocument](../reference/MhtmlDocument.md),
  [MhtmlDocumentBuilder](../reference/MhtmlDocumentBuilder.md) and
  [MhtmlWriter](../reference/MhtmlWriter.md)
