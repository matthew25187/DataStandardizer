---
title: MimeQuotedPrintable Class
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeQuotedPrintable Class

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Encodes and decodes whole values in quoted-printable: the `quoted-printable`
Content-Transfer-Encoding of RFC 2045 §6.7, or the RFC 2047 §4.2 "Q" encoding of encoded
words.

**Syntax**

```csharp
public static class MimeQuotedPrintable
```

## Remarks

To encode or decode data that arrives in parts, use
[MimeQuotedPrintableEncoder](MimeQuotedPrintableEncoder.md) or
[MimeQuotedPrintableDecoder](MimeQuotedPrintableDecoder.md). See
[Encode and decode with base64 and quoted-printable](../how-to/encode-and-decode-bodies.md).

## Methods

| Method | Notes |
| --- | --- |
| `static int GetMaxEncodedLength(int length, MimeQuotedPrintableMode mode)` | The most characters that `length` bytes can encode to. |
| `static int GetMaxDecodedLength(int length)` | The most bytes that `length` characters can decode to. |
| `static bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten, MimeQuotedPrintableMode mode, MimeQuotedPrintableEncoderOptions options)` | `false` if `destination` is too short. |
| `static string Encode(ReadOnlySpan<byte> source, MimeQuotedPrintableMode mode, MimeQuotedPrintableEncoderOptions options)` | The encoded data. |
| `static bool TryDecode(ReadOnlySpan<char> source, MimeQuotedPrintableMode mode, MimeQuotedPrintableDecoderOptions options, out byte[] result)` | `false` if the encoded data isn't valid. |
| `static byte[] Decode(ReadOnlySpan<char> source, MimeQuotedPrintableMode mode, MimeQuotedPrintableDecoderOptions options)` | Throws `FormatException` if the encoded data isn't valid. |
| `static bool EndsWithSoftLineBreak(ReadOnlySpan<char> line, out int dataLength)` | Whether a line of body data ends with `=`, which may be followed by spaces and tabs that a transport added (RFC 2045 §6.7 rules 3 and 5). `dataLength` is the length of the line before the break, or the length of the line if it has none. |
| `static bool EndsWithSoftLineBreak(ReadOnlySpan<byte> line, out int dataLength)` | As above, for a line in UTF-8 or ASCII. |

These methods throw `ArgumentOutOfRangeException` in the following cases:

| Method | Throws when |
| --- | --- |
| `GetMaxEncodedLength` | `length` is negative, its encoding could be longer than `int.MaxValue`, or `mode` isn't defined. |
| `GetMaxDecodedLength` | `length` is negative. |
| `TryEncode`, `TryDecode`, `Decode` | `mode` or `options` isn't defined. |
| `Encode` | `mode` or `options` isn't defined, or the encoding could be longer than `int.MaxValue`. |
