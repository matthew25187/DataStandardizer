---
title: MimeBase64Encoder Class
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeBase64Encoder Class

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Encodes data in base64 as it arrives, buffer by buffer.

**Syntax**

```csharp
public sealed class MimeBase64Encoder
```

## Remarks

The encoder works like `System.Buffers.Text.Base64.EncodeToUtf8`, but keeps its state
between calls. It holds the last 1 or 2 bytes of a block that isn't final until the next
call, and in `Mime` mode it keeps track of the line length, so the output is the same
however the data is split.

In `Mime` mode, it breaks lines after 76 characters with CRLF, and writes no line break
at the end of the data.

After a call with `isFinalBlock` set to `true` returns `Done`, the encoder is ready for
new data. An instance isn't thread-safe.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MimeBase64Encoder(MimeBase64Mode mode)` | Throws `ArgumentOutOfRangeException` for a mode that isn't defined. |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Mode` | [MimeBase64Mode](MimeBase64Mode.md) | The form of base64 the encoder writes. |

## Methods

| Method | Notes |
| --- | --- |
| `OperationStatus Encode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)` | Encodes into UTF-8, which for base64 is the same as ASCII. |
| `OperationStatus Encode(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten, bool isFinalBlock)` | Encodes into characters. |
| `void Reset()` | Discards any bytes held and the line length, to start on new data. |

`Encode` returns `Done` if all of `source` was consumed, including any bytes it holds for
the next call, or `DestinationTooSmall` if `destination` is full, in which case call again
with the rest of `source`.
