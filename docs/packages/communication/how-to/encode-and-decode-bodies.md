---
title: Encode and decode with base64 and quoted-printable
parent: Communication
grand_parent: Packages
nav_order: 4
---

# Encode and decode with base64 and quoted-printable

A Content-Transfer-Encoding header field names how a body is encoded.
`MimeTransferEncoding` holds that value, and `MimeBase64` and `MimeQuotedPrintable`
do the encoding.

## Read a transfer encoding

```csharp
using DataStandardizer.Communication.Mime;

var encoding = MimeTransferEncoding.Parse("BASE64");

Console.WriteLine(encoding);                                // base64
Console.WriteLine(encoding == MimeTransferEncoding.Base64); // True
Console.WriteLine(encoding.IsKnown);                        // True
```

Mechanisms are compared case-insensitively. Any other token is an extension
mechanism (RFC 4289 §3), which is kept as written. `IsKnown` is `false` for one, and
its `Domain` is `Unknown`.

The mechanism tells you which code to use:

| Mechanism | `IsIdentity` | `Domain` | What to do |
| --- | --- | --- | --- |
| `7bit` | `true` | `SevenBit` | Nothing. The data isn't encoded. |
| `8bit` | `true` | `EightBit` | Nothing. The data isn't encoded. |
| `binary` | `true` | `Binary` | Nothing. The data isn't encoded. |
| `quoted-printable` | `false` | `SevenBit` | Use `MimeQuotedPrintable`, in `Body` mode. |
| `base64` | `false` | `SevenBit` | Use `MimeBase64`, in `Mime` mode. |

`Domain` is the kind of data the transport has to carry (RFC 2045 §2.7–§2.9).

## Base64

`MimeBase64` works on whole values. A mode chooses the form:

| Mode | Used by | Layout |
| --- | --- | --- |
| `Mime` | The `base64` transfer encoding (RFC 2045 §6.8). | Lines of at most 76 characters, separated by CRLF. A decoder ignores characters outside the alphabet. |
| `Strict` | iCalendar BINARY values and RFC 2047 "B" encoded words (RFC 4648 §4). | No line breaks. Padding is required, and nothing outside the alphabet is allowed. |

```csharp
using System.Text;

var data = Encoding.UTF8.GetBytes("Hello, world!");

var encoded = MimeBase64.Encode(data, MimeBase64Mode.Mime); // SGVsbG8sIHdvcmxkIQ==

var decoded = MimeBase64.Decode(
    encoded.AsSpan(), MimeBase64Mode.Mime, MimeBase64DecoderOptions.None);
```

`Decode` throws `FormatException` when the data isn't valid. `TryDecode` returns
`false` instead.

RFC 2045 §6.8 tells a decoder to ignore characters outside the alphabet, but also
says that they probably mean a transmission error. To find that error, set
`RejectNonAlphabetCharacters`, which accepts only CRLF line breaks:

```csharp
MimeBase64.TryDecode(
    "SG!Vs bG8=".AsSpan(), MimeBase64Mode.Mime,
    MimeBase64DecoderOptions.None, out _);                        // true

MimeBase64.TryDecode(
    "SG!Vs bG8=".AsSpan(), MimeBase64Mode.Mime,
    MimeBase64DecoderOptions.RejectNonAlphabetCharacters, out _); // false
```

## Quoted-printable

`MimeQuotedPrintable` works on whole values too. Use `Body` mode for the
`quoted-printable` transfer encoding (RFC 2045 §6.7). The other modes are the "Q"
encoding of encoded words: see [Use encoded words](use-encoded-words.md).

```csharp
var data = Encoding.UTF8.GetBytes("café = coffee");

var encoded = MimeQuotedPrintable.Encode(
    data, MimeQuotedPrintableMode.Body, MimeQuotedPrintableEncoderOptions.None);
// caf=C3=A9 =3D coffee

var decoded = MimeQuotedPrintable.Decode(
    encoded.AsSpan(), MimeQuotedPrintableMode.Body, MimeQuotedPrintableDecoderOptions.None);
```

By default, the encoder treats the data as text, so each CRLF or bare LF becomes a
CRLF line break. For data that isn't text, set `Binary`, so that its line breaks
survive (RFC 2045 §6.7 rule 4):

```csharp
byte[] bytes = { 0x41, 0x0D, 0x0A, 0x42 };

var binary = MimeQuotedPrintable.Encode(
    bytes, MimeQuotedPrintableMode.Body, MimeQuotedPrintableEncoderOptions.Binary);
// A=0D=0AB
```

The decoder rejects what RFC 2045 §6.7 doesn't allow. Three flags of
[MimeQuotedPrintableDecoderOptions](../reference/MimeQuotedPrintableDecoderOptions.md)
accept what many producers write anyway:

| Flag | Accepts |
| --- | --- |
| `AllowLowercaseHexDigits` | `=c3=a9` as well as `=C3=A9`. |
| `AllowBareLineFeeds` | A bare LF as a line break, in `Body` mode. |
| `AllowLiteralEqualsSigns` | An `=` that isn't followed by two hexadecimal digits or a line break, decoded as itself. |

A format that carries quoted-printable values in its own lines, such as vCard 2.1,
can use `EndsWithSoftLineBreak` to join a value that continues on the next line:

```csharp
var isContinued = MimeQuotedPrintable.EndsWithSoftLineBreak(
    "Hello =".AsSpan(), out int dataLength);

// isContinued is true. The value is the first 6 characters of this line, then the next line.
```

## Data that arrives in parts

For data that arrives buffer by buffer, use `MimeBase64Encoder`, `MimeBase64Decoder`,
`MimeQuotedPrintableEncoder` or `MimeQuotedPrintableDecoder`. They work like
`System.Buffers.Text.Base64`, returning an `OperationStatus`, but they keep their
state between calls. The result is the same however the data is split.

Pass `isFinalBlock: true` with the last block. While the status is
`DestinationTooSmall`, call again with the rest of the block.

```csharp
using System.Buffers;

var decoder = new MimeBase64Decoder(MimeBase64Mode.Mime);
var output = new List<byte>();
var buffer = new byte[4096];

void DecodeBlock(ReadOnlySpan<char> block, bool isFinalBlock)
{
    OperationStatus status;
    do
    {
        status = decoder.Decode(block, buffer, out int consumed, out int written, isFinalBlock);
        output.AddRange(new ArraySegment<byte>(buffer, 0, written));
        block = block.Slice(consumed);
    }
    while (status == OperationStatus.DestinationTooSmall);

    if (status != OperationStatus.Done) throw new FormatException("The data isn't valid base64.");
}

DecodeBlock("SGVsbG8s".AsSpan(), isFinalBlock: false);
DecodeBlock("IHdvcmxk".AsSpan(), isFinalBlock: false);
DecodeBlock("IQ==".AsSpan(), isFinalBlock: true);

// output holds the bytes of "Hello, world!"
```

Each type also takes and writes UTF-8 bytes directly, with `Span<byte>` overloads.

Some points to watch:

- **Buffer sizes.** `MimeQuotedPrintableDecoder` needs a destination of at least 2
  bytes to make progress, and `MimeQuotedPrintableEncoder` needs at least 3. Use
  `GetEncodedLength`, `GetMaxEncodedLength` and `GetMaxDecodedLength` to size a buffer
  for a whole value. A decoder that holds data from an earlier call can write more
  than `GetMaxDecodedLength` gives in one call: up to 3 more bytes for base64, and up
  to 77 more for quoted-printable.
- **Reuse.** After a final block returns `Done`, the instance is ready for new data.
  After a decoder returns `InvalidData`, call `Reset` first.
- **Threads.** An instance isn't thread-safe.

## See also

- [MimeTransferEncoding](../reference/MimeTransferEncoding.md),
  [MimeBase64](../reference/MimeBase64.md) and
  [MimeQuotedPrintable](../reference/MimeQuotedPrintable.md) in the API reference.
