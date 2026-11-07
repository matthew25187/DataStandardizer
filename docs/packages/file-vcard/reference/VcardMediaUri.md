---
title: VcardMediaUri Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardMediaUri Class

## Definition

Namespace: `DataStandardizer.File.vCard`

The value of a `PHOTO`, `LOGO` or `SOUND` in vCard 4.0 (RFC 6350 §6.2.4, §6.6.3 and §6.7.5): a URI, often an RFC 2397 `data:` URI that holds the media inline. [VcardCard](VcardCard.md) gives one from `Photos`, `Logos` and `Sounds`.

**Syntax**

```csharp
public sealed class VcardMediaUri : IEquatable<VcardMediaUri>
```

## Remarks

The URI is held as text, so it can be any length on every runtime. On .NET Framework, `System.Uri` can't hold more than 65519 characters, and the `data:` URI of a photo is often longer. So `Uri` is `null` for text longer than that, on every runtime, and `TryGetData` gives inline data of any length.

```csharp
VcardMediaUri photo = card.Photos[0].Value;
MimeDataUri data;
if (photo.TryGetData(out data))
{
    byte[] bytes = data.GetData();
}
```

## Members

| Member | Notes |
| --- | --- |
| `FromUri(uri)` | Creates a value from an absolute URI. A URI of more than 65519 characters throws `ArgumentException`; use `FromData` for inline data of any length. |
| `FromData(data)` | Creates a value from a [MimeDataUri](../../communication/reference/MimeDataUri.md), of any length. One that isn't initialised throws `ArgumentException`. |
| `Text` | The URI, without the escaping of a content line. |
| `Uri` | The URI, or `null` if `Text` has more than 65519 characters. |
| `IsData` | Whether the URI is of the `data:` scheme. |
| `TryGetData(out data)` | Gives the inline data, if the URI is a valid `data:` URI. |
| `Equals`, `GetHashCode` | Compare `Text`, ordinally. |
| `ToString()` | `Text`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
