---
title: MimeDataUri Struct
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeDataUri Struct

## Definition

Namespace: `DataStandardizer.Communication.Mime`

A `data:` URI (RFC 2397), which holds a media type and its data inline, such as
`data:image/png;base64,iVBORw0KGgo=`.

**Syntax**

```csharp
public readonly struct MimeDataUri : IEquatable<MimeDataUri>, ISpanFormattable, ISpanParsable<MimeDataUri>
```

`ISpanFormattable` and `ISpanParsable<T>` are on the `net8.0` and `net10.0` builds only.

## Remarks

The data is base64 when the media type is followed by `;base64`, and otherwise
percent-encoded. Parsing checks the data but keeps it encoded, and it's decoded only by
`GetData` and `TryCopyData`.

A parsed URI is formatted exactly as it was written. Two URIs are equal if they have equal
media types and the same decoded data, however each is written. A URI with no media type
equals one with `text/plain;charset=US-ASCII`.

See [Use data URIs](../how-to/use-data-uris.md).

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `MediaType` | [MimeMediaType](MimeMediaType.md) | The media type; `text/plain;charset=US-ASCII` if the URI has none (RFC 2397 §2), or `text/plain` with the parameters given if it has parameters but no type. |
| `IsBase64` | `bool` | Whether the data is base64, rather than percent-encoded. |
| `DataLength` | `int` | The number of bytes of decoded data. |

The properties throw `InvalidOperationException` on a value that isn't initialised.

## Methods

| Method | Notes |
| --- | --- |
| `static MimeDataUri Create(MimeMediaType mediaType, ReadOnlySpan<byte> data)` | With base64 data. |
| `static MimeDataUri Create(MimeMediaType mediaType, ReadOnlySpan<byte> data, bool base64)` | `false` percent-encodes the data, which suits text. Any character of the media type that can't appear in a URI is percent-encoded. |
| `byte[] GetData()` | The decoded data. |
| `bool TryCopyData(Span<byte> destination, out int bytesWritten)` | `false` if `destination` is too short. `DataLength` gives the length it needs. |
| `Uri ToUri()` | The value as a `Uri`. Throws `UriFormatException` if it's longer than `Uri` allows on the platform. |
| `static bool TryParse(value, out MimeDataUri result)` | `value` is a `string` or `ReadOnlySpan<char>`, with no fragment or whitespace. |
| `static MimeDataUri Parse(value)` | Throws `FormatException` if the value isn't valid. |
| `bool TryFormat(Span<char> destination, out int charsWritten)` | Writes nothing if the value isn't initialised. |
| `string ToString()` | An empty string if the value isn't initialised. |

`Create` throws `ArgumentException` if the media type isn't initialised, and
`ArgumentOutOfRangeException` if the URI would be longer than `int.MaxValue`. The type also
has `Equals`, `GetHashCode`, and the `==` and `!=` operators.
