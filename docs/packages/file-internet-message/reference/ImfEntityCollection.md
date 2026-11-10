---
title: ImfEntityCollection Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfEntityCollection Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The parts of an [`ImfMultipart`](ImfMultipart.md), in order.

**Syntax**

```csharp
public sealed class ImfEntityCollection : IReadOnlyList<ImfEntity>
```

## Remarks

An entity can be in one place at a time, so a tree can't hold itself.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Count` | `int Count { get; }` | The number of items. |
| `this[]` | `ImfEntity this[int index] { get; }` | The item at an index. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Add(ImfEntity entity)` | `void` | Adds a part after the others. **Throws** `ArgumentNullException`: `entity` is `null`. **Throws** `ArgumentException`: `entity` is, or holds, the multipart entity it is being added to. **Throws** `InvalidOperationException`: `entity` is already the body of a message or a part of a multipart entity. |
| `GetEnumerator()` | `IEnumerator<ImfEntity>` | Gets an enumerator over the items. |
| `Insert(int index, ImfEntity entity)` | `void` | Adds a part at an index. **Throws** `ArgumentNullException`: `entity` is `null`. **Throws** `ArgumentException`: `entity` is, or holds, the multipart entity it is being added to. **Throws** `ArgumentOutOfRangeException`: `index` is less than 0, or more than `Count`. **Throws** `InvalidOperationException`: `entity` is already the body of a message or a part of a multipart entity. |
| `RemoveAt(int index)` | `void` | Removes the part at an index. **Throws** `ArgumentOutOfRangeException`: `index` is less than 0, or not less than `Count`. |
| `Replace(int index, ImfEntity entity)` | `void` | Replaces the part at an index. **Throws** `ArgumentNullException`: `entity` is `null`. **Throws** `ArgumentException`: `entity` is, or holds, the multipart entity it is being added to. **Throws** `ArgumentOutOfRangeException`: `index` is less than 0, or not less than `Count`. **Throws** `InvalidOperationException`: `entity` is already the body of a message or a part of a multipart entity. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfMultipart](ImfMultipart.md)
- [File.InternetMessage API reference](index.md)
