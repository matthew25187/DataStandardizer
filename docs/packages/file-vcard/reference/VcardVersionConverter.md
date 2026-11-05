---
title: VcardVersionConverter Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardVersionConverter Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Says what happens to a card in another version of vCard, and converts it. A card doesn't have to be converted to write it as another version: set `VcardOptions.WriteVersion`. See [Converting between versions](../version-conversion.md).

**Syntax**

```csharp
public static class VcardVersionConverter
```

## Methods

| Method | Notes |
| --- | --- |
| `Analyze(card, version)` | Lists what a card can't keep in a version, without changing or writing anything. For vCard 3.0 each item is `Lossy`. For vCard 4.0 each is `VersionConversion`: a property of an older version that stays as one 4.0 doesn't define. |
| `Convert(card, version)` | A copy of a card with `SourceVersion` set to that version. It throws `VcardFileException` if the card has a `Lossy` item. |
| `Convert(card, version, options)` | The same, and `AllowLossyVersionConversion` leaves out what the version can't hold. |

Each throws `ArgumentException` for vCard 2.1, which can't be written yet.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
