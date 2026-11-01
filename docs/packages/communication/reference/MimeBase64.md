---
title: MimeBase64 Class
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeBase64 Class

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Encodes and decodes whole values in base64: the `base64` Content-Transfer-Encoding of
RFC 2045 §6.8, or the strict base64 of RFC 4648 §4 that iCalendar BINARY values and
RFC 2047 "B" encoded words use.

**Syntax**

```csharp
public static class MimeBase64
```

## Remarks

To encode or decode data that arrives in parts, use
[MimeBase64Encoder](MimeBase64Encoder.md) or [MimeBase64Decoder](MimeBase64Decoder.md).
See [Encode and decode with base64 and quoted-printable](../how-to/encode-and-decode-bodies.md).

## Methods

| Method | Notes |
| --- | --- |
| `static int GetEncodedLength(int length, MimeBase64Mode mode)` | The characters in the encoding of `length` bytes, including any line breaks. |
| `static int GetMaxDecodedLength(int length)` | The most bytes that `length` characters can decode to, whatever the mode. |
| `static bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten, MimeBase64Mode mode)` | `false` if `destination` is too short. |
| `static string Encode(ReadOnlySpan<byte> source, MimeBase64Mode mode)` | The encoded data. |
| `static bool TryDecode(ReadOnlySpan<char> source, MimeBase64Mode mode, MimeBase64DecoderOptions options, out byte[] result)` | `false` if the encoded data isn't valid. |
| `static byte[] Decode(ReadOnlySpan<char> source, MimeBase64Mode mode, MimeBase64DecoderOptions options)` | Throws `FormatException` if the encoded data isn't valid. |

Each throws `ArgumentOutOfRangeException` for a `mode` or `options` value that isn't
defined, a negative length, or an encoding longer than `int.MaxValue`.
