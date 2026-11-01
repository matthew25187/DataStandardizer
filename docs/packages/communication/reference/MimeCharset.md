---
title: MimeCharset Struct
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeCharset Struct

## Definition

Namespace: `DataStandardizer.Communication.Mime`

A character set name (RFC 2978 §2.3), as in the `charset` parameter of a media type
(RFC 2046 §4.1.2), such as `UTF-8`.

**Syntax**

```csharp
public readonly struct MimeCharset : IEquatable<MimeCharset>, ISpanFormattable, ISpanParsable<MimeCharset>
```

`ISpanFormattable` and `ISpanParsable<T>` are on the `net8.0` and `net10.0` builds only.

## Remarks

A name is kept as written, and names are compared case-insensitively. An alias is a different
name, so `latin1` doesn't equal `ISO-8859-1`, although both resolve to the same encoding. The
name isn't checked against the IANA registry, so an unregistered name such as `x-mac-roman` is
valid.

Use [MimeCharsetResolver](MimeCharsetResolver.md) to get the `Encoding` for a character set.
See [Resolve character sets](../how-to/resolve-character-sets.md).

## Fields

| Field | Name | Notes |
| --- | --- | --- |
| `UsAscii` | `US-ASCII` | RFC 2046 §4.1.2. |
| `Iso88591` | `ISO-8859-1` | Also known as Latin-1. |
| `Utf8` | `UTF-8` | RFC 3629. |
| `Utf16` | `UTF-16` | Big-endian unless the text starts with a byte order mark that says otherwise (RFC 2781 §3.1). |
| `Utf16BigEndian` | `UTF-16BE` | RFC 2781 §3.2. |
| `Utf16LittleEndian` | `UTF-16LE` | RFC 2781 §3.2. |
| `Utf32` | `UTF-32` | Big-endian unless the text starts with a byte order mark that says otherwise (The Unicode Standard §3.10, D101). |
| `Utf32BigEndian` | `UTF-32BE` | |
| `Utf32LittleEndian` | `UTF-32LE` | |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Name` | `string` | The name, as written. Throws `InvalidOperationException` on a value that isn't initialised. |

## Methods

| Method | Notes |
| --- | --- |
| `static bool TryGetDefault(MimeMediaType mediaType, out MimeCharset charset)` | The character set that text of a media type is in when it has no `charset` parameter (RFC 2046 §4.1.2, RFC 6657 §3). The parameters of the media type are ignored. Throws `ArgumentException` if the media type isn't initialised. |
| `static bool TryParse(value, out MimeCharset result)` | `value` is a `string` or `ReadOnlySpan<char>`, with no quotes or whitespace. |
| `static MimeCharset Parse(value)` | Throws `FormatException` if the value isn't a valid name. |
| `bool TryFormat(Span<char> destination, out int charsWritten)` | Writes nothing if the value isn't initialised. |
| `string ToString()` | The name; an empty string if the value isn't initialised. |

The type also has `Equals`, `GetHashCode`, and the `==` and `!=` operators.
