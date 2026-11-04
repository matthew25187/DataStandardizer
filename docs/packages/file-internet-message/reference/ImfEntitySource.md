---
title: ImfEntitySource Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfEntitySource Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Describes one entity for an [`ImfEntityWriter`](ImfEntityWriter.md) to write: the fields and the content of a message or a part, or the raw bytes of one that has not changed.

**Syntax**

```csharp
public sealed class ImfEntitySource
```

## Remarks

A writer has to see all of a body to choose its transfer encoding, which comes before it in the output, and all of a multipart entity to choose a boundary, which comes before its parts. So it is given the whole entity at once, as a tree of these objects, and it reads a body from a stream without holding it in memory.

A stream is read from its current position. It is read at least twice if the writer has to look at its content, so it must support seeking. A stream that doesn't can be used only where nothing needs checking: as the content of an entity with a `TransferEncoding` of `base64`, or of `quoted-printable` if every enclosing boundary holds `=_`, or as the raw bytes of a whole message. The writer leaves a stream open, at the end of its data.

The memory and streams must not change until the write has finished.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Epilogue` | `ReadOnlyMemory<byte>? Epilogue { get; set; }` | Gets or sets the part of a multipart entity after its close delimiter (RFC 2046 §5.1.1), or `null` for none. It is written without the line break that separates it from the delimiter, which only an entity with an epilogue has, so an empty epilogue gives just the line break. **Throws** `InvalidOperationException`: On set: the entity is not a multipart entity made with `FromParts`. |
| `Preamble` | `ReadOnlyMemory<byte>? Preamble { get; set; }` | Gets or sets the part of a multipart entity before its first delimiter (RFC 2046 §5.1.1), or `null` for none. It is written without the line break that separates it from the delimiter. **Throws** `InvalidOperationException`: On set: the entity is not a multipart entity made with `FromParts`. |
| `TransferEncoding` | `MimeTransferEncoding TransferEncoding { get; set; }` | Gets or sets the transfer encoding to use, which overrides the writer's choice. The default value of [`MimeTransferEncoding`](../../communication/reference/MimeTransferEncoding.md) leaves the choice to the writer. The writer refuses an encoding that doesn't fit: `7bit` or `8bit` for data outside that domain (RFC 2045 §2.7 and §2.8), or anything but an identity encoding for a multipart or message entity (RFC 2045 §6.4). An extension encoding can't be written, as the writer can't apply it. For that, write the entity with `FromRaw`. **Throws** `InvalidOperationException`: On set: the entity is written from its raw bytes. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `AddField(ImfHeaderFieldView field)` | `ImfEntitySource` | Adds a field to the header of the entity, copying what a reader has just read, to be written byte for byte. The rules that `AddField` gives for the fields a writer adds itself apply here too. **Returns:** This object. **Throws** `ObjectDisposedException`: The reader of the field has been disposed. **Throws** `InvalidOperationException`: The entity is written from its raw bytes. -or- The reader has moved on from the field, or the view was not obtained from a reader. |
| `AddField(string name, string value, ImfHeaderFieldKind kind)` | `ImfEntitySource` | Adds a field to the header of the entity, to be folded as its kind allows, in the way [`ImfHeaderWriter`](ImfHeaderWriter.md) describes. The writer adds the Content-Type and Content-Transfer-Encoding fields itself, after these fields, and `MIME-Version: 1.0` to the header of a message, unless one is added here. One added here must agree with the entity: a Content-Type field with the media type it was made with, and a Content-Transfer-Encoding field with its `TransferEncoding`, for which the field then asks if that has not been set. This keeps the header of a multipart entity unchanged when only its parts have changed. The field is checked when the entity is written. **Returns:** This object. **Throws** `ArgumentNullException`: `name` or `value` is `null`. **Throws** `ArgumentOutOfRangeException`: `kind` is not an [`ImfHeaderFieldKind`](ImfHeaderFieldKind.md) member. **Throws** [`ImfFileException`](ImfFileException.md): `name` or `value` is not valid Unicode. **Throws** `InvalidOperationException`: The entity is written from its raw bytes. |
| `AddRawField(ReadOnlyMemory<byte> rawField)` | `ImfEntitySource` | Adds the raw bytes of a field to the header of the entity, to be written byte for byte, including its folding and its final line break. The rules that `AddField` gives for the fields a writer adds itself apply here too. When the entity is written, the bytes are checked in the way that [`ImfHeaderWriter.WriteRawField`](ImfHeaderWriter.md) describes. **Returns:** This object. **Throws** `InvalidOperationException`: The entity is written from its raw bytes. |
| `AddTextField(string name, string text)` | `ImfEntitySource` | Adds text to the header of the entity as an unstructured field, such as Subject, to be written as [`ImfHeaderWriter.WriteTextField`](ImfHeaderWriter.md) writes it, with the [`ImfOptions.HeaderEncoding`](ImfOptions.md) of the writer. The rules that `AddField` gives for the fields a writer adds itself apply here too. The field is checked when the entity is written. **Returns:** This object. **Throws** `ArgumentNullException`: `name` or `text` is `null`. **Throws** [`ImfFileException`](ImfFileException.md): `name` or `text` is not valid Unicode. **Throws** `InvalidOperationException`: The entity is written from its raw bytes. |
| `static FromContent(MimeMediaType contentType, Stream content)` | `ImfEntitySource` | Describes an entity with content in a stream, which is read when the entity is written. **Returns:** The entity. **Throws** `ArgumentException`: `contentType` is not initialised, or `content` does not support reading. **Throws** `ArgumentNullException`: `content` is `null`. |
| `static FromContent(MimeMediaType contentType, ReadOnlyMemory<byte> content)` | `ImfEntitySource` | Describes an entity with content in memory. **Returns:** The entity. **Throws** `ArgumentException`: `contentType` is not initialised. |
| `static FromMessage(ImfEntitySource message)` | `ImfEntitySource` | Describes an entity of type `message/rfc822` that holds a message (RFC 2046 §5.2.1). **Returns:** The entity. **Throws** `ArgumentNullException`: `message` is `null`. |
| `static FromMessage(MimeMediaType contentType, ImfEntitySource message)` | `ImfEntitySource` | Describes an entity that holds a message (RFC 2046 §5.2.1, RFC 6532 §3.7). **Returns:** The entity. **Throws** `ArgumentException`: `contentType` is not initialised, or is another media type. **Throws** `ArgumentNullException`: `message` is `null`. |
| `static FromParts(MimeMediaType contentType, IEnumerable<ImfEntitySource> parts)` | `ImfEntitySource` | Describes a multipart entity (RFC 2046 §5.1). **Returns:** The entity. **Throws** `ArgumentException`: `contentType` is not initialised or not `multipart`, or `parts` is empty or contains `null`. **Throws** `ArgumentNullException`: `parts` is `null`. |
| `static FromRaw(Stream rawBytes)` | `ImfEntitySource` | Describes an entity that has not changed, by a stream of its raw bytes, which are copied byte for byte when the entity is written. This keeps a signature over it valid. The stream is read to its end, so to copy a part of a larger stream, give a stream of just that part. The bytes are checked as `FromRaw` says. **Returns:** The entity. **Throws** `ArgumentException`: `rawBytes` does not support reading. **Throws** `ArgumentNullException`: `rawBytes` is `null`. |
| `static FromRaw(ReadOnlyMemory<byte> rawBytes)` | `ImfEntitySource` | Describes an entity that has not changed, by its raw bytes, which are copied byte for byte. This keeps a signature over it valid. With an [`ImfEntityReader`](ImfEntityReader.md), the raw bytes are the range of the data that an [`ImfEntityEventKind.EntityEnd`](ImfEntityEventKind.md) event covers. They don't include the line break before the delimiter that follows a part. A part is checked against the enclosing boundaries, and may not hold a line longer than RFC 5322 §2.1.1 allows, a NUL or a bare CR or LF, unless [`ImfOptions.AllowLongLines`](ImfOptions.md), [`ImfOptions.AllowInvalidBodyOctets`](ImfOptions.md) or [`ImfOptions.AllowBinaryTransferEncoding`](ImfOptions.md) is set. What the part says of itself isn't read, so its bytes can't be checked against its own transfer encoding, or its fields against their grammar. A whole message written this way, and bytes that were read and haven't changed, aren't checked at all. **Returns:** The entity. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Stream a large message](../how-to/stream-a-large-message.md)
- [Byte-exact round trips](../concepts/round-trips.md)
- [ImfEntityEventKind](ImfEntityEventKind.md)
- [ImfEntityReader](ImfEntityReader.md)
- [ImfEntityWriter](ImfEntityWriter.md)
- [ImfFileException](ImfFileException.md)
- [ImfHeaderFieldKind](ImfHeaderFieldKind.md)
- [ImfHeaderWriter](ImfHeaderWriter.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
