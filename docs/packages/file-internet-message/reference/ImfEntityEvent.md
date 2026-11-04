---
title: ImfEntityEvent Struct
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfEntityEvent Struct

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A view of the event an [`ImfEntityReader`](ImfEntityReader.md) has just read, without allocation.

**Syntax**

```csharp
public readonly struct ImfEntityEvent
```

## Remarks

A view is valid only until the reader reads again or is disposed. After that, every member throws: `ObjectDisposedException` once the reader is disposed, otherwise `InvalidOperationException`.

A span or memory taken out of the view is valid for the same period, but that cannot be checked: using one later gives undefined content. Copy anything that must outlive the event.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `ByteLength` | `long ByteLength { get; }` | Gets the number of raw bytes that the event covers. <br><br> [`ImfEntityEventKind.EntityStart`](ImfEntityEventKind.md) covers nothing, as the length of the entity is not yet known. [`ImfEntityEventKind.HeaderEnd`](ImfEntityEventKind.md) covers the header section, with the empty line that ends it, so the offset plus the length is where the body starts. [`ImfEntityEventKind.EntityEnd`](ImfEntityEventKind.md) covers the whole entity, so an entity that has not changed can be copied byte for byte. <br><br> A part of a multipart entity does not include the delimiter line after it, or the line break before that line, which belongs to the delimiter (RFC 2046 §5.1.1). So if a part has no body, its range stops before the empty line that ended its header section, and is shorter than its [`ImfEntityEventKind.HeaderEnd`](ImfEntityEventKind.md) event covers. <br><br> A decoded body chunk covers the raw bytes consumed since the last chunk, which may include bytes the decoder holds for the next chunk, so the chunks of a body together cover all of its raw bytes. The last chunk of a body may cover nothing, if it only holds what the decoder was holding, and may hold no bytes, if it only covers raw bytes that gave no output, such as a line break after the last of the base64. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another event, or the view was not obtained from a reader. |
| `ByteOffset` | `long ByteOffset { get; }` | Gets the 0-based offset, in bytes from where the reader started, of the first raw byte that the event covers. [`ImfEntityEventKind.EntityStart`](ImfEntityEventKind.md), [`ImfEntityEventKind.HeaderEnd`](ImfEntityEventKind.md) and [`ImfEntityEventKind.EntityEnd`](ImfEntityEventKind.md) give the start of the entity. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another event, or the view was not obtained from a reader. |
| `ContentType` | `MimeMediaType ContentType { get; }` | Gets the media type of the entity, from its Content-Type field or the default (RFC 2045 §5.2, RFC 2046 §5.1.5). **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The event comes before the entity's [`ImfEntityEventKind.HeaderEnd`](ImfEntityEventKind.md) event, or the reader has moved on to another event, or the view was not obtained from a reader. |
| `Data` | `ReadOnlySpan<byte> Data { get; }` | Gets the bytes of a [`ImfEntityEventKind.Preamble`](ImfEntityEventKind.md), [`ImfEntityEventKind.BodyChunk`](ImfEntityEventKind.md) or [`ImfEntityEventKind.Epilogue`](ImfEntityEventKind.md) event. It is empty for an event of any other kind. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another event, or the view was not obtained from a reader. |
| `DataMemory` | `ReadOnlyMemory<byte> DataMemory { get; }` | Gets the bytes of a [`ImfEntityEventKind.Preamble`](ImfEntityEventKind.md), [`ImfEntityEventKind.BodyChunk`](ImfEntityEventKind.md) or [`ImfEntityEventKind.Epilogue`](ImfEntityEventKind.md) event. It is empty for an event of any other kind. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another event, or the view was not obtained from a reader. |
| `Depth` | `int Depth { get; }` | Gets the depth of the entity the event belongs to. The message itself is at depth 0. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another event, or the view was not obtained from a reader. |
| `EntityPath` | `string EntityPath { get; }` | Gets the path of the entity the event belongs to, as IMAP numbers the parts of a message (RFC 3501 §6.4.5). <br><br> The message itself has the empty path. The parts of a multipart entity are numbered from 1, under the path of that entity, so the second part of the first part of a message is `1.2`. <br><br> IMAP gives a `message/rfc822` part and the message inside it the one number. So they have the same path here, and differ in `Depth`. Likewise, IMAP numbers the body of a message that is not multipart as part 1 of the message, which matters only when that body is itself an embedded message: the embedded message of a top-level `message/rfc822` body has the path `1`. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another event, or the view was not obtained from a reader. |
| `Field` | `ImfHeaderFieldView Field { get; }` | Gets the header field, for a [`ImfEntityEventKind.HeaderField`](ImfEntityEventKind.md) event. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The event is of another kind, or the reader has moved on to another event, or the view was not obtained from a reader. |
| `IsDecoded` | `bool IsDecoded { get; }` | Gets a flag indicating that `Data` has been decoded from the entity's transfer encoding, rather than being the raw bytes. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another event, or the view was not obtained from a reader. |
| `Kind` | `ImfEntityEventKind Kind { get; }` | Gets the kind of event. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another event, or the view was not obtained from a reader. |
| `TransferEncoding` | `MimeTransferEncoding TransferEncoding { get; }` | Gets the transfer encoding of the entity, from its Content-Transfer-Encoding field or the default of `7bit` (RFC 2045 §6.1). **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The event comes before the entity's [`ImfEntityEventKind.HeaderEnd`](ImfEntityEventKind.md) event, or the reader has moved on to another event, or the view was not obtained from a reader. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Stream a large message](../how-to/stream-a-large-message.md)
- [ImfEntityEventKind](ImfEntityEventKind.md)
- [ImfEntityReader](ImfEntityReader.md)
- [File.InternetMessage API reference](index.md)
