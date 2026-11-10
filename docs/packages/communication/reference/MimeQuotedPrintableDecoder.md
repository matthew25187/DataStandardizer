---
title: MimeQuotedPrintableDecoder Class
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeQuotedPrintableDecoder Class

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Decodes quoted-printable data as it arrives, buffer by buffer.

**Syntax**

```csharp
public sealed class MimeQuotedPrintableDecoder
```

## Remarks

The decoder works like `System.Buffers.Text.Base64.DecodeFromUtf8`, but keeps its state
between calls. It holds an incomplete escape, soft line break or line break, and a run of
spaces and tabs that may turn out to be trailing whitespace, until the next call. So it
consumes all of a block that isn't final, and the result is the same however the data is
split.

In `Body` mode, it removes soft line breaks, including whitespace a transport added after the
`=`, and removes spaces and tabs at the end of a line or of the data (RFC 2045 §6.7 rule 3). A
CRLF line break is decoded as CRLF. As a line is at most 76 characters, a run of more than 76
spaces and tabs isn't valid. The decoder doesn't otherwise check the line length.

In the "Q" modes, `_` is decoded as a space, and only the characters that the mode's context
allows can appear as themselves. Spaces, tabs and line breaks aren't valid.

In every mode, an `=` must be followed by two upper-case hexadecimal digits (or in a body by a
line break), and control characters and characters outside ASCII aren't valid.
[MimeQuotedPrintableDecoderOptions](MimeQuotedPrintableDecoderOptions.md) names the forms that
can be accepted as well.

It writes up to 2 bytes at a time, so it needs a destination of at least 2 bytes to make
progress. After a call with `isFinalBlock` set to `true` returns `Done`, the decoder is ready
for new data. After `InvalidData`, call `Reset` first. An instance isn't thread-safe.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MimeQuotedPrintableDecoder(MimeQuotedPrintableMode mode)` | With no options. |
| `MimeQuotedPrintableDecoder(MimeQuotedPrintableMode mode, MimeQuotedPrintableDecoderOptions options)` | Throws `ArgumentOutOfRangeException` for a value that isn't defined. |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Mode` | [MimeQuotedPrintableMode](MimeQuotedPrintableMode.md) | The form of quoted-printable the decoder reads. |
| `Options` | [MimeQuotedPrintableDecoderOptions](MimeQuotedPrintableDecoderOptions.md) | The forms the decoder accepts beyond those of its mode. |

## Methods

| Method | Notes |
| --- | --- |
| `OperationStatus Decode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)` | Decodes from UTF-8, which for quoted-printable is the same as ASCII. |
| `OperationStatus Decode(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten, bool isFinalBlock)` | Decodes from characters. |
| `void Reset()` | Discards anything held from earlier calls, to start on new data. |

`Decode` returns one of these:

| Status | Meaning |
| --- | --- |
| `Done` | All of `source` was consumed, including anything the decoder holds for the next call. |
| `DestinationTooSmall` | `destination` is full. Call again with the rest of `source`. |
| `InvalidData` | The data isn't valid. The consumed count is the position of the invalid character, or the length of `source` if the data ends inside an escape. |
