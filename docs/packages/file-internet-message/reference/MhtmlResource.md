---
title: MhtmlResource Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# MhtmlResource Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A part of an [`MhtmlDocument`](MhtmlDocument.md), with the URI that its Content-Location field resolves to.

**Syntax**

```csharp
public sealed class MhtmlResource
```

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Entity` | `ImfEntity Entity { get; }` | Gets the entity. |
| `Uri` | `Uri? Uri { get; }` | Gets the absolute URI that the Content-Location field of the entity resolves to (RFC 2557 §5), which starts with `thismessage:/` if a relative one has nothing else to resolve against; or `null` if the entity has no such field. A field with a `cid:` URI is disregarded (RFC 2557 §8.3). It labels the entity, and doesn't say where the entity came from, which a sender can misrepresent (RFC 2557 §11.1). Its `Uri.OriginalString` is the text that is compared. A URL that `Uri` can't hold after it is resolved also gives `null`, and can still be found. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Open a saved web page](../how-to/open-a-saved-web-page.md)
- [MhtmlDocument](MhtmlDocument.md)
- [File.InternetMessage API reference](index.md)
