---
title: VcardMediaValue Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardMediaValue Class

## Definition

Namespace: `DataStandardizer.File.vCard`

The value of a `PHOTO`, `LOGO`, `SOUND` or `KEY` in the shape of vCard 3.0 or 2.1: inline data with a `TYPE` that says its format, a URI, or for a key, and a sound in vCard 2.1, text. The canonical form of inline data is an RFC 2397 `data:` URI, held in a [VcardMediaUri](VcardMediaUri.md). See [IVcard30Card](IVcard30Card.md) and [IVcard21Card](IVcard21Card.md).

**Syntax**

```csharp
public sealed class VcardMediaValue
```

## Members

| Member | Notes |
| --- | --- |
| `FromData(data, typeToken)` | Creates inline data, with a copy of the bytes. `typeToken` is optional, such as `JPEG`. |
| `FromUri(uri, typeToken)` | Creates a URI, which must be absolute. |
| `FromText(text, typeToken)` | Creates the text of a key, or of a sound in vCard 2.1. |
| `GetData()` | A copy of the inline data, or `null` if the value is something else. |
| `Uri` | The URI, or `null` if the value is something else. |
| `Text` | The text of a key, or `null` if the value is something else. |
| `TypeToken` | The `TYPE` that says the format, or `null` if there is none. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
