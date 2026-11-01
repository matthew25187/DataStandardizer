---
title: MimeBase64Decoder Class
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeBase64Decoder Class

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Decodes base64 data as it arrives, buffer by buffer.

**Syntax**

```csharp
public sealed class MimeBase64Decoder
```

## Remarks

The decoder works like `System.Buffers.Text.Base64.DecodeFromUtf8`, but keeps its state
between calls. It holds an incomplete quantum until the next call, so it consumes all of a
block that isn't final, and the result is the same however the data is split.

Padding ends the data: after it, a character of the alphabet or another `=` is invalid. In
`Mime` mode, characters outside the alphabet are ignored, before and after the padding,
unless `RejectNonAlphabetCharacters` is set. The pad bits of the last quantum aren't
checked (RFC 4648 §3.5).

After a call with `isFinalBlock` set to `true` returns `Done`, the decoder is ready for new
data. After `InvalidData`, call `Reset` first. An instance isn't thread-safe.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MimeBase64Decoder(MimeBase64Mode mode)` | With no options. |
| `MimeBase64Decoder(MimeBase64Mode mode, MimeBase64DecoderOptions options)` | Throws `ArgumentOutOfRangeException` for a value that isn't defined. |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Mode` | [MimeBase64Mode](MimeBase64Mode.md) | The form of base64 the decoder reads. |
| `Options` | [MimeBase64DecoderOptions](MimeBase64DecoderOptions.md) | The checks the decoder makes beyond those of its mode. |

## Methods

| Method | Notes |
| --- | --- |
| `OperationStatus Decode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)` | Decodes from UTF-8, which for base64 is the same as ASCII. |
| `OperationStatus Decode(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten, bool isFinalBlock)` | Decodes from characters. |
| `void Reset()` | Discards any part of a quantum held, to start on new data. |

`Decode` returns one of these:

| Status | Meaning |
| --- | --- |
| `Done` | All of `source` was consumed, including anything the decoder holds for the next call. |
| `DestinationTooSmall` | `destination` is full. Call again with the rest of `source`. |
| `InvalidData` | The data isn't valid. The consumed count is the position of the invalid character, or the length of `source` if the data ends inside a quantum. |
