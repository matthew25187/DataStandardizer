---
title: Use data URIs
parent: Communication
grand_parent: Packages
nav_order: 6
---

# Use data URIs

`MimeDataUri` holds a `data:` URI (RFC 2397), which carries a media type and its
data inline.

## Parse a data URI

```csharp
using System.Text;
using DataStandardizer.Communication.Mime;

var uri = MimeDataUri.Parse("data:,A%20brief%20note");

Console.WriteLine(uri.MediaType);  // text/plain;charset=US-ASCII
Console.WriteLine(uri.IsBase64);   // False
Console.WriteLine(uri.DataLength); // 12

var text = Encoding.ASCII.GetString(uri.GetData()); // A brief note
```

A URI with no media type is `text/plain;charset=US-ASCII` (RFC 2397 §2). One with
parameters but no type, such as `data:;charset=UTF-8,abc`, is `text/plain` with those
parameters.

Parsing checks the data but keeps it encoded. It's decoded only when you call
`GetData` or `TryCopyData`, so a large value costs no more than its text until you
need its data. To avoid allocating, decode into your own buffer:

```csharp
var buffer = new byte[uri.DataLength];

uri.TryCopyData(buffer, out int bytesWritten);
```

The value must be the URI alone, with no fragment or whitespace. `Parse` throws
`FormatException` if it isn't valid, and `TryParse` returns `false`.

## Create a data URI

By default, the data is written in base64. To percent-encode it, which suits text,
pass `false` for `base64`.

```csharp
var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

var image = MimeDataUri.Create(MimeMediaType.Parse("image/png"), png);

Console.WriteLine(image); // data:image/png;base64,iVBORw0KGgo=

var note = MimeDataUri.Create(
    MimeMediaType.TextPlain.WithParameter("charset", "UTF-8"),
    Encoding.UTF8.GetBytes("A brief note"),
    base64: false);

Console.WriteLine(note); // data:text/plain;charset=UTF-8,A%20brief%20note
```

`ToUri` gives the value as a `System.Uri`. It throws `UriFormatException` if the URI
is longer than `Uri` allows on the platform.

## Compare data URIs

Two URIs are equal if they have equal media types and the same decoded data, however
each is written.

```csharp
var base64 = MimeDataUri.Parse("data:text/plain;base64,SGk=");
var percent = MimeDataUri.Parse("data:text/plain,Hi");

Console.WriteLine(base64 == percent); // True
```

A parsed URI is still formatted exactly as it was written.

## See also

- [MimeDataUri](../reference/MimeDataUri.md) in the API reference.
