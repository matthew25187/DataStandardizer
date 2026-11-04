---
title: ImfMultipart Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfMultipart Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A multipart entity (RFC 2046 §5.1): an ordered list of entities, with a preamble and an epilogue.

**Syntax**

```csharp
public sealed class ImfMultipart : ImfEntity
```

## Remarks

While its parts stay the same, and its boundary, preamble and epilogue are not changed, a writer writes its delimiter lines as they were read, even if a part has changed. Otherwise it writes new ones, and it uses the `boundary` parameter if there is one, or else generates a boundary each time it writes.

A multipart entity has parts and no content of its own, so [`ImfEntity.GetContent`](ImfEntity.md) and the members that use it throw `InvalidOperationException`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfMultipart(MimeMediaType contentType)` | Creates a multipart entity with no parts, whose accessors use the default options. **Throws** `ArgumentException`: `contentType` is not initialised or not `multipart`. |
| `ImfMultipart(MimeMediaType contentType, ImfOptions options)` | Creates a multipart entity with no parts. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `contentType` is not initialised or not `multipart`, or `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Epilogue` | `ReadOnlyMemory<byte>? Epilogue { get; set; }` | Gets or sets the part of the entity after its close delimiter (RFC 2046 §5.1.1), or `null` for none. It doesn't include the line break after that delimiter, so an empty epilogue is just that line break. The memory must not change until a write has finished. Setting this means that the delimiter lines are written again. |
| `Parts` | `ImfEntityCollection Parts { get; }` | Gets the parts, in order. |
| `Preamble` | `ReadOnlyMemory<byte>? Preamble { get; set; }` | Gets or sets the part of the entity before its first delimiter (RFC 2046 §5.1.1), or `null` for none. It doesn't include the line break that belongs to the delimiter. The memory must not change until a write has finished. Setting this means that the delimiter lines are written again. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetContent()` | `ReadOnlyMemory<byte>` | Has no content to give: a multipart entity has parts. **Returns:** Nothing. **Throws** `InvalidOperationException`: Always. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read a message and its attachments](../how-to/read-a-message.md)
- [Build and write a message](../how-to/build-and-write-a-message.md)
- [ImfEntity](ImfEntity.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
