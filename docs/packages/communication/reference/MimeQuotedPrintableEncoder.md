---
title: MimeQuotedPrintableEncoder Class
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeQuotedPrintableEncoder Class

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Encodes data in quoted-printable as it arrives, buffer by buffer.

**Syntax**

```csharp
public sealed class MimeQuotedPrintableEncoder
```

## Remarks

The encoder works like `System.Buffers.Text.Base64.EncodeToUtf8`, but keeps its state
between calls. In `Body` mode, how a byte is written depends on whether a line break follows
it, so the encoder holds the last 1 or 2 bytes of a block that isn't final until the next
call. It also keeps track of the line length, so the output is the same however the data is
split.

In `Body` mode, it writes lines of at most 76 characters, with soft line breaks where a line
of the data is longer. A space or tab at the end of a line is encoded. Unless `Binary` is
set, each CRLF or bare LF in the data is written as a CRLF line break, and a bare CR is
encoded. It writes no line break at the end of the data.

In the "Q" modes, a space is written as `_`, and the characters that can't be written as
themselves in the mode's context are encoded. The encoder doesn't limit the length of the
output: splitting it into encoded words of at most 75 characters (RFC 2047 §2) is up to the
caller. [MimeEncodedWord.EncodeText](MimeEncodedWord.md) does that for text.

It writes up to 3 characters at a time, so it needs a destination of at least 3 bytes or
characters to make progress. After a call with `isFinalBlock` set to `true` returns `Done`,
the encoder is ready for new data. An instance isn't thread-safe.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MimeQuotedPrintableEncoder(MimeQuotedPrintableMode mode)` | Encodes data as text. |
| `MimeQuotedPrintableEncoder(MimeQuotedPrintableMode mode, MimeQuotedPrintableEncoderOptions options)` | Throws `ArgumentOutOfRangeException` for a value that isn't defined. |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Mode` | [MimeQuotedPrintableMode](MimeQuotedPrintableMode.md) | The form of quoted-printable the encoder writes. |
| `Options` | [MimeQuotedPrintableEncoderOptions](MimeQuotedPrintableEncoderOptions.md) | How the encoder treats the data. |

## Methods

| Method | Notes |
| --- | --- |
| `OperationStatus Encode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)` | Encodes into UTF-8, which for quoted-printable is the same as ASCII. |
| `OperationStatus Encode(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten, bool isFinalBlock)` | Encodes into characters. |
| `void Reset()` | Discards any bytes held and the line length, to start on new data. |

`Encode` returns `Done` if all of `source` was consumed, including any bytes it holds for the
next call, or `DestinationTooSmall` if `destination` is full, in which case call again with
the rest of `source`.
