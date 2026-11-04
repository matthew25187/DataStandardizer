---
title: ImfMessageBuilder Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfMessageBuilder Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Builds a message from its header, its text, and what goes with the text, in the structure that those call for.

**Syntax**

```csharp
public sealed class ImfMessageBuilder
```

## Remarks

`Build` makes the structure from what it has, so that a caller doesn't have to know it (RFC 2046 §5.1, RFC 2387):

- Text or HTML alone is the body of the message.

- Text and HTML go in a `multipart/alternative` entity, with the text first, as RFC 2046 §5.1.4 has the best last.

- HTML with inline resources goes in a `multipart/related` entity, with the HTML as its root, inside any `multipart/alternative` entity.

- Attachments go in a `multipart/mixed` entity, after all of that.

The builder doesn't make up a Date or Message-ID, or anything else that isn't given. A writer refuses a message that RFC 5322 §3.6 doesn't allow, and [`ImfMessage.Validate`](ImfMessage.md) reports it. A writer generates the boundaries, and chooses each transfer encoding.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfMessageBuilder()` | Creates a builder that uses the default options. |
| `ImfMessageBuilder(ImfOptions options)` | Creates a builder. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Header` | `ImfMessage Header { get; }` | Gets the message whose fields are copied to the header of the message that is built, so that they can be set with its accessors. Its body is not used, and a field whose name starts with `Content-` gives way to those of the body, if the message that is built has one. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `AddAttachment(ImfMessage message)` | `ImfMessageBuilder` | Adds a message as an attachment, in a `message/rfc822` entity (RFC 2046 §5.2.1), as when forwarding it. **Returns:** This object. **`message`:** The message, which is held by each message that is built, and not copied. **Throws** `ArgumentNullException`: `message` is `null`. |
| `AddAttachment(string fileName, MimeMediaType contentType, ReadOnlyMemory<byte> content)` | `ImfMessageBuilder` | Adds an attachment. **Returns:** This object. **`fileName`:** The suggested file name, which is written as RFC 2231 has it if it isn't US-ASCII. **`content`:** The content. The memory must not change until the last write of a message that is built. **Throws** `ArgumentNullException`: `fileName` is `null`. **Throws** `ArgumentException`: `contentType` is not initialised, or is of the top-level type `multipart`. |
| `AddInlineResource(ImfMessageId contentId, MimeMediaType contentType, ReadOnlyMemory<byte> content)` | `ImfMessageBuilder` | Adds a resource that the HTML refers to with a `cid:` URI (RFC 2392), such as an image. It goes in a `multipart/related` entity with the HTML (RFC 2387). **Returns:** This object. **`content`:** The content. The memory must not change until the last write of a message that is built. **Throws** `ArgumentNullException`: `contentId` is `null`. **Throws** `ArgumentException`: `contentType` is not initialised, or is of the top-level type `multipart`, or `contentId` has already been added, as RFC 2045 §7 requires it to be unique. |
| `Build()` | `ImfMessage` | Builds a message, which shares nothing that can change with any other built from this builder, except the options given to the builder, and a message given to `AddAttachment(ImfMessage)`, which they all hold, so that a later change to either changes them all. **Returns:** The message. With no text, HTML or attachments, it has no body. **Throws** `InvalidOperationException`: There are inline resources and no HTML to refer to them. **Throws** `ArgumentException`: A character set can't be resolved, or can't hold the characters of its text, or the text is too long for [`ImfOptions.MaxEntitySize`](ImfOptions.md). |
| `SetHtml(string html)` | `ImfMessageBuilder` | Sets the HTML text of the message, in UTF-8. **Returns:** This object. **Throws** `ArgumentNullException`: `html` is `null`. |
| `SetHtml(string html, MimeCharset charset)` | `ImfMessageBuilder` | Sets the HTML text of the message. **Returns:** This object. **`charset`:** The character set to write it in, which is checked by `Build`. **Throws** `ArgumentNullException`: `html` is `null`. |
| `SetText(string text)` | `ImfMessageBuilder` | Sets the plain text of the message, in UTF-8. **Returns:** This object. **Throws** `ArgumentNullException`: `text` is `null`. |
| `SetText(string text, MimeCharset charset)` | `ImfMessageBuilder` | Sets the plain text of the message. **Returns:** This object. **`charset`:** The character set to write it in, which is checked by `Build`. **Throws** `ArgumentNullException`: `text` is `null`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Build and write a message](../how-to/build-and-write-a-message.md)
- [ImfMessage](ImfMessage.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
