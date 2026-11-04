---
title: ImfEntity Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfEntity Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A MIME entity (RFC 2045 §2.4): an ordered list of header fields, with typed accessors over them, and a body. An [`ImfMultipart`](ImfMultipart.md) holds other entities, an [`ImfMessagePart`](ImfMessagePart.md) holds a message, and any other entity holds content.

**Syntax**

```csharp
public class ImfEntity
```

## Remarks

**Header fields.** Each accessor reads and writes through `Fields`, as those of [`ImfMessage`](ImfMessage.md) do. An accessor gives `null` if the field is absent or its body is not valid. Changing a field rewrites that field only.

**Content.** What was read is held in memory as it is, and is decoded each time it is asked for. An entity that has not changed is written back byte for byte. `SetContent` holds new content, for which a writer chooses a transfer encoding.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfEntity()` | Creates an entity with no fields and no content, whose accessors use the default options. |
| `ImfEntity(ImfOptions options)` | Creates an entity with no fields and no content. **`options`:** The options that the accessors parse fields under, and that decide how content is read. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `ContentDescription` | `string? ContentDescription { get; set; }` | Gets or sets the text of the Content-Description field (RFC 2045 §8). **Throws** [`ImfFileException`](ImfFileException.md): On set: the text is not valid Unicode, or holds a control character other than tab. |
| `ContentDisposition` | `ImfContentDisposition? ContentDisposition { get; set; }` | Gets or sets the disposition of the Content-Disposition field (RFC 2183). |
| `ContentId` | `ImfMessageId? ContentId { get; set; }` | Gets or sets the identifier of the Content-ID field (RFC 2045 §7). |
| `ContentLanguage` | `IReadOnlyList<Bcp47LanguageTag>? ContentLanguage { get; set; }` | Gets or sets the language tags of the Content-Language field (RFC 3282). Setting `null`, or an empty list, removes the field. **Throws** `ArgumentException`: On set: the list holds a tag that is not initialised. |
| `ContentLocation` | `Uri? ContentLocation { get; set; }` | Gets or sets the URI of the Content-Location field (RFC 2557 §4), which is absolute or relative, and is kept as it was written, so `Uri.OriginalString` gives it octet by octet. It labels the entity, and doesn't say where it came from (RFC 2557 §11.1). Reading unfolds the URI and decodes any RFC 2047 encoded words (RFC 2557 §4.4.3). Writing folds a long URI, and writes one with characters that a header can't hold as encoded words (RFC 2557 §4.4.1 and §4.4.2). To resolve one that is relative, use an [`MhtmlDocument`](MhtmlDocument.md). **Throws** `ArgumentException`: On set: the URI holds an unpaired surrogate. |
| `ContentTransferEncoding` | `MimeTransferEncoding ContentTransferEncoding { get; set; }` | Gets or sets the transfer encoding of the Content-Transfer-Encoding field (RFC 2045 §6). If the field is absent or not valid, it gives the default of `7bit` (RFC 2045 §6.1). Setting an encoding has a writer use it for this entity, and setting the default value of [`MimeTransferEncoding`](../../communication/reference/MimeTransferEncoding.md) removes the field and leaves the choice to the writer. Content that was read is decoded first, and encoded again when it is written. **Throws** `ArgumentException`: On set: the entity holds other entities, which can have only an identity encoding (RFC 2045 §6.4). **Throws** [`ImfFileException`](ImfFileException.md): On set: the content that was read can't be decoded. |
| `ContentType` | `MimeMediaType ContentType { get; set; }` | Gets or sets the media type of the Content-Type field (RFC 2045 §5). If the field is absent, it gives the default of `text/plain; charset=us-ascii` (RFC 2045 §5.2), or `message/rfc822` for a part of a `multipart/digest` entity (RFC 2046 §5.1.5). A reader goes by the parameters it needs to find the parts of a message, and accepts a field that [`ImfMimeFields.TryParseContentType(ReadOnlySpan<byte>, ImfOptions, out MimeMediaType)`](ImfMimeFields.md) doesn't, such as one with an encoded word in a parameter, which RFC 2047 §5 doesn't allow. This gives what the reader found for such a field, with the parameter as it was written. With [`ImfOptions.AllowMalformedParameters`](ImfOptions.md), the parameter is decoded as that option describes. **Throws** `ArgumentException`: On set: the value is not initialised, or is of a kind that this type of entity doesn't hold. |
| `Fields` | `ImfHeaderFieldCollection Fields { get; }` | Gets the header fields, in order. For the [`ImfMessage.Body`](ImfMessage.md) of a message, these are the [`ImfMessage.Fields`](ImfMessage.md). |
| `FileName` | `string? FileName { get; }` | Gets the suggested file name of the entity: the `filename` parameter of the Content-Disposition field, in the form of RFC 2231 if it has one (`filename*`), or the `name` parameter of the Content-Type field, which RFC 2046 §4.5.1 leaves in use for this; or `null` if it has none. A file name comes from whoever sent the message. See the remarks on [`ImfContentDisposition`](ImfContentDisposition.md) before using it. |
| `Options` | `ImfOptions Options { get; }` | Gets the options that the accessors parse fields under, and that decide how content is read. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetContent()` | `ReadOnlyMemory<byte>` | Gets the content of the entity, decoded from its transfer encoding (RFC 2045 §6). The bytes are not to be changed. An identity encoding gives the bytes that the message holds, and the others give new bytes each time. An embedded message gives the bytes of the whole message. **Returns:** The bytes. **Throws** [`ImfFileException`](ImfFileException.md): The body is not valid in its transfer encoding, or the encoding is an extension mechanism, which can't be decoded (RFC 2045 §6.3). **Throws** `InvalidOperationException`: The entity is an [`ImfMultipart`](ImfMultipart.md), which has parts and no content of its own. |
| `GetText()` | `string` | Gets the content as text, in the character set of the `charset` parameter of the Content-Type field. With no such parameter, it is the default for the type, or otherwise US-ASCII (RFC 2046 §4.1.2, RFC 6657 §3). The character set is resolved by [`ImfOptions.CharsetResolver`](ImfOptions.md). The line breaks are those of the content, which are CRLF in a `text` entity that follows RFC 2046 §4.1.1. **Returns:** The text. **Throws** [`ImfFileException`](ImfFileException.md): The body is not valid in its transfer encoding, the character set can't be resolved, or the content is not valid in it and [`ImfOptions.AllowInvalidTextOctets`](ImfOptions.md) is not set. **Throws** `InvalidOperationException`: The entity is not of the top-level type `text`. |
| `OpenContentStream()` | `Stream` | Opens a read-only stream over the content, as `GetContent` gives it. **Returns:** The stream, which doesn't need to be disposed. **Throws** [`ImfFileException`](ImfFileException.md): The body is not valid in its transfer encoding, or the encoding is an extension mechanism, which can't be decoded (RFC 2045 §6.3). **Throws** `InvalidOperationException`: The entity is an [`ImfMultipart`](ImfMultipart.md), which has parts and no content of its own. |
| `SetContent(ReadOnlyMemory<byte> content)` | `void` | Sets the content of the entity. A writer chooses a transfer encoding for it, as [`ImfEntityWriter`](ImfEntityWriter.md) describes, so this removes any Content-Transfer-Encoding field. To ask for an encoding, set `ContentTransferEncoding` afterwards. The other fields are not touched, so set `ContentType` to say what the content is. **`content`:** The content, before any transfer encoding. A writer gives it back unchanged after decoding, so give text with CRLF line breaks (RFC 2046 §4.1.1). The memory must not change until a write has finished. **Throws** `ArgumentException`: `content` is longer than [`ImfOptions.MaxEntitySize`](ImfOptions.md). **Throws** `InvalidOperationException`: The entity is an [`ImfMultipart`](ImfMultipart.md) or [`ImfMessagePart`](ImfMessagePart.md), which has no content of its own to set. |
| `SetText(string text)` | `void` | Sets the content of the entity to text in UTF-8, as `SetText(string, MimeCharset)` does. **Throws** `ArgumentNullException`: `text` is `null`. **Throws** `ArgumentException`: `text` is not valid Unicode, or is too long for [`ImfOptions.MaxEntitySize`](ImfOptions.md). **Throws** `InvalidOperationException`: The entity is not of the top-level type `text`, or is an [`ImfMultipart`](ImfMultipart.md) or [`ImfMessagePart`](ImfMessagePart.md). |
| `SetText(string text, MimeCharset charset)` | `void` | Sets the content of the entity to text, and the `charset` parameter of its Content-Type field to say what character set it is in (RFC 2046 §4.1.2). Each line break of the text, whether CR, LF or CRLF, becomes a CRLF, the only line break that RFC 2046 §4.1.1 allows in a `text` entity. The rest is as `SetContent` describes. **`charset`:** The character set, which [`ImfOptions.CharsetResolver`](ImfOptions.md) must resolve. **Throws** `ArgumentNullException`: `text` is `null`. **Throws** `ArgumentException`: `charset` is not initialised or can't be resolved, or `text` has characters that it can't hold, or is too long for [`ImfOptions.MaxEntitySize`](ImfOptions.md). **Throws** `InvalidOperationException`: The entity is not of the top-level type `text`, or is an [`ImfMultipart`](ImfMultipart.md) or [`ImfMessagePart`](ImfMessagePart.md). |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read a message and its attachments](../how-to/read-a-message.md)
- [Build and write a message](../how-to/build-and-write-a-message.md)
- [ImfContentDisposition](ImfContentDisposition.md)
- [ImfEntityWriter](ImfEntityWriter.md)
- [ImfFileException](ImfFileException.md)
- [ImfMessage](ImfMessage.md)
- [ImfMessagePart](ImfMessagePart.md)
- [ImfMimeFields](ImfMimeFields.md)
- [ImfMultipart](ImfMultipart.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
