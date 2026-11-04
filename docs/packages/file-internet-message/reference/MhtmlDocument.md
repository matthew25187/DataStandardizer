---
title: MhtmlDocument Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# MhtmlDocument Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A view of an [`ImfMessage`](ImfMessage.md) as an aggregate document (RFC 2557): a web page saved with its resources, as in an MHTML file.

**Syntax**

```csharp
public sealed class MhtmlDocument
```

## Remarks

**Root.** The root of a `multipart/related` body is the part that its `start` parameter names, or otherwise the first (RFC 2387 §3.2). If that part is `multipart/alternative`, the root is its `text/html` part, or otherwise its last (RFC 2557 §7). Any other body is its own root, as a page with no resources needs no `multipart/related` entity (RFC 2557 §6).

**URIs.** A relative Content-Location resolves against the nearest absolute Content-Location of the entities around it, then the base URI given here, and then `thismessage:/` (RFC 2557 §5). URIs resolve as RFC 1808 §4 says, and are compared octet by octet, with no change to their `%` escapes (RFC 2557 §8.2). A fragment is ignored, as it is not part of what a URL identifies (RFC 1808 §2.1).

**Conformance.** Each `multipart/related` entity is checked against RFC 2387 §3 and RFC 2557 §7, and each Content-Location field against RFC 2557 §4. A problem causes an [`ImfFileException`](ImfFileException.md), unless [`ImfOptions.AllowInvalidRelatedStructure`](ImfOptions.md), [`ImfOptions.AllowInvalidValues`](ImfOptions.md) or [`ImfOptions.AllowCardinalityViolations`](ImfOptions.md) is set in the options of the message. With them set, it is recorded in `Diagnostics` instead.

The document describes the message as it was when the document was made. After a change to the message, make another.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MhtmlDocument(ImfMessage message)` | Creates a view of a message, with no base URI. **Throws** `ArgumentNullException`: `message` is `null`. **Throws** [`ImfFileException`](ImfFileException.md): The message breaks a rule that its options don't accept. |
| `MhtmlDocument(ImfMessage message, Uri? baseUri)` | Creates a view of a message. **`baseUri`:** The absolute URI for a relative one to resolve against when the message has none of its own, such as the one the file was retrieved with (RFC 2557 §5 (d)); or `null` for none. **Throws** `ArgumentNullException`: `message` is `null`. **Throws** `ArgumentException`: `baseUri` is not absolute. **Throws** [`ImfFileException`](ImfFileException.md): The message breaks a rule that its options don't accept. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems that the options of the message accept. The problems that [`ImfMessage.Validate`](ImfMessage.md) reports are not among them. The list is read-only. |
| `Message` | `ImfMessage Message { get; }` | Gets the message. |
| `Resources` | `IReadOnlyList<MhtmlResource> Resources { get; }` | Gets every part, including the root, in the order of the message, each with its resolved URI. A body that isn't multipart is the one part. A multipart body isn't listed, as its Content-Location is that of the whole aggregate (RFC 2557 §4.3), and what is inside an embedded message isn't either. The list is read-only. |
| `Root` | `ImfEntity Root { get; }` | Gets the root entity. |
| `RootMediaType` | `MimeMediaType RootMediaType { get; }` | Gets the media type of the root entity. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `TryResolve(Uri uri, out MhtmlResource? resource)` | `bool` | Finds the part that the root refers to with a URI (RFC 2557 §8.2). **Returns:** `true` if a part was found. **`uri`:** The URI, as the root gives it. A `cid:` URI is matched against Content-ID fields only (RFC 2557 §8.3), and any other against Content-Location fields. A relative one resolves against the base URI of the root. **Throws** `ArgumentNullException`: `uri` is `null`. |
| `TryResolve(Uri uri, MhtmlResource referrer, out MhtmlResource? resource)` | `bool` | Finds the part that another part refers to with a URI (RFC 2557 §8.2), such as an image in a style sheet. A reference reaches the parts of the `multipart/related` entity that holds the part it is in, and of those around that one. RFC 2557 §7 and its example in §9.6 don't have it reach those of an entity that is nested or parallel. **Returns:** `true` if a part was found. **`uri`:** The URI, as the part gives it. A `cid:` URI is matched against Content-ID fields only (RFC 2557 §8.3), and any other against Content-Location fields. A relative one resolves against the base URI of that part. **Throws** `ArgumentNullException`: `uri` or `referrer` is `null`. **Throws** `ArgumentException`: `referrer` is not from this document. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Open a saved web page](../how-to/open-a-saved-web-page.md)
- [One message format, several file formats](../concepts/file-formats.md)
- [ImfFileException](ImfFileException.md)
- [ImfMessage](ImfMessage.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
