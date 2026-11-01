---
title: MimeTransferEncoding Struct
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeTransferEncoding Struct

## Definition

Namespace: `DataStandardizer.Communication.Mime`

A transfer encoding mechanism, as in a Content-Transfer-Encoding header field
(RFC 2045 §6.1), such as `base64`.

**Syntax**

```csharp
public readonly struct MimeTransferEncoding : IEquatable<MimeTransferEncoding>, ISpanFormattable, ISpanParsable<MimeTransferEncoding>
```

`ISpanFormattable` and `ISpanParsable<T>` are on the `net8.0` and `net10.0` builds only.

## Remarks

The known mechanisms are written in lower case. Any other token is an extension
mechanism: an `x-` token, or one registered with IANA (RFC 4289 §3). An extension
mechanism is kept as written. Mechanisms are compared case-insensitively.

See [Encode and decode with base64 and quoted-printable](../how-to/encode-and-decode-bodies.md).

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `SevenBit` | `7bit` | Labels 7bit data that isn't encoded (RFC 2045 §6.2). |
| `EightBit` | `8bit` | Labels 8bit data that isn't encoded (RFC 2045 §6.2). |
| `Binary` | `binary` | Labels binary data that isn't encoded (RFC 2045 §6.2). |
| `QuotedPrintable` | `quoted-printable` | Encodes mostly-text data as 7bit data (RFC 2045 §6.7). |
| `Base64` | `base64` | Encodes any data as 7bit data (RFC 2045 §6.8). |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Name` | `string` | The name of the mechanism. |
| `IsKnown` | `bool` | Whether RFC 2045 defines the mechanism, rather than it being an extension. |
| `IsIdentity` | `bool` | Whether the mechanism leaves the data as it is, and only labels its domain: `7bit`, `8bit` or `binary`. |
| `Domain` | [MimeDataDomain](MimeDataDomain.md) | The domain of the encoded data, which the transport must be able to carry. `Unknown` for an extension. |

The properties throw `InvalidOperationException` on a value that isn't initialised.

## Methods

| Method | Notes |
| --- | --- |
| `static bool TryParse(value, out MimeTransferEncoding result)` | `value` is a `string` or `ReadOnlySpan<char>`. |
| `static MimeTransferEncoding Parse(value)` | Throws `FormatException` if the value isn't a token. |
| `bool TryFormat(Span<char> destination, out int charsWritten)` | Writes nothing if the value isn't initialised. |
| `string ToString()` | An empty string if the value isn't initialised. |

The type also has `Equals`, `GetHashCode`, and the `==` and `!=` operators.
