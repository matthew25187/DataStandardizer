---
title: ImfMessagePart Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfMessagePart Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

An entity that holds a message (RFC 2046 §5.2.1, RFC 6532 §3.7), such as one that is being forwarded.

**Syntax**

```csharp
public sealed class ImfMessagePart : ImfEntity
```

## Remarks

[`ImfEntity.GetContent`](ImfEntity.md) gives the bytes of the whole message.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfMessagePart(ImfMessage message)` | Creates an entity of type `message/rfc822` that holds a message, whose accessors use the options of the message. **Throws** `ArgumentNullException`: `message` is `null`. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Message` | `ImfMessage Message { get; set; }` | Gets or sets the message. **Throws** `ArgumentNullException`: On set: the value is `null`. **Throws** `ArgumentException`: On set: the message holds this entity. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetContent()` | `ReadOnlyMemory<byte>` | As `ImfEntity.GetContent`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read a message and its attachments](../how-to/read-a-message.md)
- [ImfEntity](ImfEntity.md)
- [File.InternetMessage API reference](index.md)
