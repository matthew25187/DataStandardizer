---
title: ImfMessage Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfMessage Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

An Internet message (RFC 5322): an ordered list of header fields, with typed accessors over them, and a body.

**Syntax**

```csharp
public sealed class ImfMessage
```

## Remarks

**Header fields.** `Fields` keeps every field in order, so a field that hasn't changed is written back byte for byte. Each accessor reads and writes through it. Setting one replaces the field where it is, or adds it after the other fields if there is none, and removes any others of that name. Setting one to `null`, or to an empty list, removes the field. Other fields are not touched.

**Values.** An accessor gives `null`, or nothing, if the field is absent or its body is not valid, which `Validate` reports. If there is more than one of a field that may appear only once, it gives the first. Text and display names are decoded from RFC 2047 encoded words. A value that is set is formatted when it is written, with the [`ImfOptions.HeaderEncoding`](ImfOptions.md) of the writer.

**Body.** `RawBody` holds the bytes after the empty line that ends the header, and `Body` gives the MIME entities they hold. The fields of `Body` are those of the message.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfMessage()` | Creates a message with no fields and no body, whose accessors use the default options. |
| `ImfMessage(ImfOptions options)` | Creates a message with no fields and no body. **`options`:** The options that the accessors and `Validate` parse fields under. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Attachments` | `IReadOnlyList<ImfEntity> Attachments { get; }` | Gets the attachments of the message: each entity with a Content-Disposition that [`ImfContentDisposition.IsAttachment`](ImfContentDisposition.md) says is one, and each entity without parts that is not text and has a [`ImfEntity.FileName`](ImfEntity.md), in the order of the message. The list is read-only, and doesn't follow later changes. What is inside an embedded message is not looked at, as that belongs to that message, and neither are the parts of a multipart entity that is an attachment. Such an entity is listed, and has parts rather than content. **Throws** [`ImfFileException`](ImfFileException.md): `RawBody` was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept. |
| `Bcc` | `ImfAddressList? Bcc { get; set; }` | Gets or sets the addresses of the Bcc field (RFC 5322 §3.6.3). It also gives `null` for a field with no addresses, which that section allows, and which is among the `Fields`. |
| `Body` | `ImfEntity Body { get; set; }` | Gets or sets the body as a MIME entity: an [`ImfMultipart`](ImfMultipart.md) for a multipart body, an [`ImfMessagePart`](ImfMessagePart.md) for a `message/rfc822` body, and otherwise an [`ImfEntity`](ImfEntity.md). <br><br> Its fields are those of the message, so its [`ImfEntity.ContentType`](ImfEntity.md), for one, reads and writes the Content-Type field of the message. <br><br> Setting this takes the fields whose names start with `Content-` out of the message, and adds those of the entity after the other fields. <br><br> The type of the entity, and the parts it holds, are settled when it is read. Replacing the Content-Type field through `Fields` afterwards doesn't read the body again, so to have it read under a new media type, set `RawBody`. To change the media type of what the entity already holds, use [`ImfEntity.ContentType`](ImfEntity.md). **Throws** [`ImfFileException`](ImfFileException.md): On get: `RawBody` was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept. **Throws** `ArgumentNullException`: On set: the value is `null`. **Throws** `ArgumentException`: On set: the entity holds this message. **Throws** `InvalidOperationException`: On set: the entity is already the body of a message or a part of a multipart entity. |
| `Cc` | `ImfAddressList? Cc { get; set; }` | Gets or sets the addresses of the Cc field (RFC 5322 §3.6.3). |
| `Comments` | `IReadOnlyList<string> Comments { get; set; }` | Gets or sets the text of each Comments field, in order (RFC 5322 §3.6.5). **Throws** `ArgumentNullException`: On set: the value is `null`. **Throws** `ArgumentException`: On set: the value holds a `null` element. **Throws** [`ImfFileException`](ImfFileException.md): On set: a text is not valid Unicode, or holds a control character other than tab. |
| `Date` | `ImfDateTime? Date { get; set; }` | Gets or sets the date and time of the Date field (RFC 5322 §3.6.1). **Throws** `ArgumentException`: On set: the value was made with `default`. |
| `Fields` | `ImfHeaderFieldCollection Fields { get; }` | Gets the header fields, in order. |
| `From` | `ImfAddressList? From { get; set; }` | Gets or sets the addresses of the From field (RFC 5322 §3.6.2, RFC 6854). |
| `HtmlBody` | `ImfEntity? HtmlBody { get; }` | Gets the entity of the HTML text of the message, or `null` if it has none. It is found as `TextBody` describes. **Throws** [`ImfFileException`](ImfFileException.md): `RawBody` was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept. |
| `InReplyTo` | `IReadOnlyList<ImfMessageId>? InReplyTo { get; set; }` | Gets or sets the identifiers of the In-Reply-To field (RFC 5322 §3.6.4). **Throws** `ArgumentException`: On set: the identifiers hold a `null` element. |
| `Keywords` | `IReadOnlyList<ImfPhraseList> Keywords { get; set; }` | Gets or sets the phrases of each Keywords field, in order (RFC 5322 §3.6.5). A field that is not valid is left out. **Throws** `ArgumentNullException`: On set: the value is `null`. **Throws** `ArgumentException`: On set: the value holds a `null` element. |
| `MessageId` | `ImfMessageId? MessageId { get; set; }` | Gets or sets the identifier of the Message-ID field (RFC 5322 §3.6.4). |
| `Options` | `ImfOptions Options { get; }` | Gets the options that the accessors and `Validate` parse fields under. |
| `RawBody` | `ReadOnlyMemory<byte> RawBody { get; set; }` | Gets or sets the body: the bytes after the empty line that ends the header. They are written as they are. <br><br> The memory must not change until a write has finished. <br><br> Setting this replaces `Body`, which is read again from these bytes when it is next asked for. A change made through `Body` doesn't change these bytes: a writer writes the change, and this still gives what was read or set. |
| `References` | `IReadOnlyList<ImfMessageId>? References { get; set; }` | Gets or sets the identifiers of the References field (RFC 5322 §3.6.4). **Throws** `ArgumentException`: On set: the identifiers hold a `null` element. |
| `ReplyTo` | `ImfAddressList? ReplyTo { get; set; }` | Gets or sets the addresses of the Reply-To field (RFC 5322 §3.6.2). |
| `ResentBlocks` | `IReadOnlyList<ImfResentBlock> ResentBlocks { get; }` | Gets the blocks of resent fields, in the order of the header (RFC 5322 §3.6.6). The list is read-only, and doesn't follow later changes to the fields. To add a block, add its fields to `Fields`. RFC 5322 gives no mark between blocks, and no order within one. So a block ends at any other field, or at a field that it already has. |
| `Sender` | `ImfAddress? Sender { get; set; }` | Gets or sets the address of the Sender field (RFC 5322 §3.6.2, RFC 6854). |
| `Subject` | `string? Subject { get; set; }` | Gets or sets the text of the Subject field (RFC 5322 §3.6.5). **Throws** [`ImfFileException`](ImfFileException.md): On set: the text is not valid Unicode, or holds a control character other than tab. |
| `TextBody` | `ImfEntity? TextBody { get; }` | Gets the entity of the plain text of the message, or `null` if it has none. The search follows the structure of the message. Of the parts of a `multipart/alternative` entity, it takes the last that has one, as RFC 2046 §5.1.4 puts the best last. In a `multipart/related` entity, it looks only in the root part (RFC 2387 §3.2). In any other multipart entity, it takes the first part that has one. An entity with a Content-Disposition that [`ImfContentDisposition.IsAttachment`](ImfContentDisposition.md) says is an attachment isn't the text of the message, and neither is what it holds, or what is inside an embedded message. **Throws** [`ImfFileException`](ImfFileException.md): `RawBody` was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept. |
| `To` | `ImfAddressList? To { get; set; }` | Gets or sets the addresses of the To field (RFC 5322 §3.6.3). |
| `TraceFields` | `IReadOnlyList<ImfTraceField> TraceFields { get; }` | Gets the Return-Path and Received fields, in the order of the header (RFC 5322 §3.6.7). The list is read-only, and doesn't follow later changes to the fields. To add one, add it to `Fields`. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `EnumerateEntities()` | `IEnumerable<ImfEntityNode>` | Enumerates the entities of the message, depth first, each with its path. The paths are those of [`ImfEntityEvent.EntityPath`](ImfEntityEvent.md), so an [`ImfMessagePart`](ImfMessagePart.md) and the `Body` of the message inside it have the same path, and differ in depth. The first entity is `Body`, with the empty path. Changing the tree while this is enumerating gives undefined results. **Returns:** The entities, including those of embedded messages. **Throws** [`ImfFileException`](ImfFileException.md): `RawBody` was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept. |
| `FindByContentId(string cidUri)` | `ImfEntity?` | Finds the entity that a `cid:` URI refers to, as `FindByContentId(ImfMessageId)` does. **Returns:** The entity, or `null` if there is none. **Throws** `ArgumentNullException`: `cidUri` is `null`. **Throws** `FormatException`: `cidUri` is not a valid `cid:` URI. **Throws** [`ImfFileException`](ImfFileException.md): `RawBody` was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept. |
| `FindByContentId(ImfMessageId contentId)` | `ImfEntity?` | Finds the entity with a Content-ID within this message (RFC 2392 §2), the first in the order of `EnumerateEntities`. What is inside an embedded message is not looked at, as a `cid:` URI refers to the message that holds it. **Returns:** The entity, or `null` if there is none. **Throws** `ArgumentNullException`: `contentId` is `null`. **Throws** [`ImfFileException`](ImfFileException.md): `RawBody` was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept. |
| `Validate()` | `IReadOnlyList<ImfDiagnostic>` | Checks the message, without throwing: the required fields, the fields that may appear only once, and the grammar of the body of each field that RFC 5322 §3.6 defines; the MIME-Version field (RFC 2045 §4); embedded messages (RFC 2046 §5.2.1); the bytes of each body that hasn't changed (RFC 5322 §2.1.1 and §2.3, RFC 2045 §2.7, §2.8, §6.4, §6.7 and §6.8); and the octets of the header of each nested entity that was read and hasn't changed, under an entity that was read (RFC 2045 §2.7 and §6.4). A body whose structure can't be read is reported as a problem, and what it holds isn't checked. The options don't change what is reported, except in how a field body is read. **Returns:** Every problem found, in a read-only list. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read a message and its attachments](../how-to/read-a-message.md)
- [Build and write a message](../how-to/build-and-write-a-message.md)
- [The layers](../concepts/layers.md)
- [ImfContentDisposition](ImfContentDisposition.md)
- [ImfEntity](ImfEntity.md)
- [ImfEntityEvent](ImfEntityEvent.md)
- [ImfFileException](ImfFileException.md)
- [ImfMessagePart](ImfMessagePart.md)
- [ImfMultipart](ImfMultipart.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
