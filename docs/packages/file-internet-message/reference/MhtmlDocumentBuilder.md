---
title: MhtmlDocumentBuilder Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# MhtmlDocumentBuilder Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Builds an aggregate document (RFC 2557) from a root document and its resources: a `multipart/related` entity whose `type` and `start` parameters name the root, with each part labelled with its URL.

**Syntax**

```csharp
public sealed class MhtmlDocumentBuilder
```

## Remarks

The root gets a generated Content-ID for `start` to name, unless its URL is a `cid:` URI. A `cid:` URL becomes the Content-ID of a part, as RFC 2557 §8.3 doesn't match one in a Content-Location field, and any other becomes its Content-Location. The builder doesn't make up a Date or From field: set them on `Header` to send the document as a message. A writer generates the boundary, and chooses each transfer encoding.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MhtmlDocumentBuilder()` | Creates a builder that uses the default options. |
| `MhtmlDocumentBuilder(ImfOptions options)` | Creates a builder. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Header` | `ImfMessage Header { get; }` | Gets the message whose fields are copied to the header of the message that is built, so that they can be set with its accessors. Its body is not used, and a field whose name starts with `Content-` gives way to those of the body. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `AddResource(Uri location, MimeMediaType contentType, ReadOnlyMemory<byte> content)` | `MhtmlDocumentBuilder` | Adds a resource that the document refers to by a URL. **Returns:** This object. **Throws** `ArgumentNullException`: `location` is `null`. **Throws** `ArgumentException`: `contentType` is not initialised, or is of the top-level type `multipart`, or `location` has the `cid` scheme and is not valid. |
| `Build()` | `MhtmlDocument` | Builds a document, whose message shares nothing that can change with any other built from this builder, except the options given to the builder, which they all use, so do not change those afterwards. **Returns:** The document, with the base URI given to `SetLocation`, if any. **Throws** `InvalidOperationException`: There is no root document. **Throws** `ArgumentException`: A character set can't be resolved, or can't hold the characters of its text, or the text is too long for [`ImfOptions.MaxEntitySize`](ImfOptions.md), or the URL of the root has the `cid` scheme and is not valid. **Throws** [`ImfFileException`](ImfFileException.md): Two parts have the same Content-ID, or a URL that resolves to the same URL (RFC 2557 §7), whatever the options say. |
| `SetHtml(Uri location, string html)` | `MhtmlDocumentBuilder` | Sets the root document to HTML in UTF-8. **Returns:** This object. **Throws** `ArgumentNullException`: `location` or `html` is `null`. |
| `SetHtml(Uri location, string html, MimeCharset charset)` | `MhtmlDocumentBuilder` | Sets the root document to HTML. **Returns:** This object. **Throws** `ArgumentNullException`: `location` or `html` is `null`. |
| `SetLocation(Uri location)` | `MhtmlDocumentBuilder` | Sets the URI of the whole aggregate, which is written as the Content-Location of the `multipart/related` entity, and is the base of the relative URIs in the document (RFC 2557 §4.3 and §5). **Returns:** This object. **Throws** `ArgumentNullException`: `location` is `null`. **Throws** `ArgumentException`: `location` is not absolute. |
| `SetRoot(Uri location, MimeMediaType contentType, ReadOnlyMemory<byte> content)` | `MhtmlDocumentBuilder` | Sets the root document. **Returns:** This object. **Throws** `ArgumentNullException`: `location` is `null`. **Throws** `ArgumentException`: `contentType` is not initialised, or is of the top-level type `multipart`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Open a saved web page](../how-to/open-a-saved-web-page.md)
- [ImfFileException](ImfFileException.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
