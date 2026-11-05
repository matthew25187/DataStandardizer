---
title: VcardPropertyList Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardPropertyList Class

## Definition

Namespace: `DataStandardizer.File.vCard`

The instances of one property of a card whose values are valid, in order. It is a live view of `VcardCard.Properties`: adding or removing an instance changes the card, and changing the card changes what this gives.

**Syntax**

```csharp
public sealed class VcardPropertyList<TValue> : IReadOnlyList<VcardProperty<TValue>>
```

## Members

| Member | Notes |
| --- | --- |
| `Count`, indexer, `GetEnumerator()` | The instances there are when it is called. Each reads the properties of the card. |
| `Add(TValue value)` | Adds an instance after the last property of the card, and gives it. |
| `Add(TValue value, VcardPropertyParameters parameters)` | With a group and parameters. |
| `Remove(VcardProperty<TValue> property)` | Gives whether it was in the card. |
| `Clear()` | Removes every instance, including any whose value isn't valid. |
| `GetPreferred()` | The first instance with the lowest `PREF` (RFC 6350 §5.3), or `null` if there are none. |
| `GetAlternatives()` | The instances grouped by `ALTID` (RFC 6350 §5.4). |

`Add` throws `ArgumentNullException` for a `null` value, and `ArgumentException` for a value of a type the property doesn't allow or one that can't be written.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardCard](VcardCard.md)
- [VcardProperty](VcardProperty.md)
- [IVcard40PropertyList](IVcard40PropertyList.md)
- [VcardPropertyParameters](VcardPropertyParameters.md)
