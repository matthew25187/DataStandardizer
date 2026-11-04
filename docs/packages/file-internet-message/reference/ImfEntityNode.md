---
title: ImfEntityNode Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfEntityNode Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

An entity that [`ImfMessage.EnumerateEntities`](ImfMessage.md) found, with where it is in the message.

**Syntax**

```csharp
public sealed class ImfEntityNode
```

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Depth` | `int Depth { get; }` | Gets the depth of the entity. The body of the message is at depth 0. |
| `Entity` | `ImfEntity Entity { get; }` | Gets the entity. |
| `Path` | `string Path { get; }` | Gets the path of the entity, as [`ImfEntityEvent.EntityPath`](ImfEntityEvent.md) describes. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read a message and its attachments](../how-to/read-a-message.md)
- [ImfEntityEvent](ImfEntityEvent.md)
- [ImfMessage](ImfMessage.md)
- [File.InternetMessage API reference](index.md)
