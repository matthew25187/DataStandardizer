---
title: Iso3166SubdivisionNameAttribute Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166SubdivisionNameAttribute Class

## Definition

Namespace: `DataStandardizer.Geography`

A native subdivision name for an ISO 3166-2 subdivision code in a particular
language.

```csharp
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class Iso3166SubdivisionNameAttribute : Iso3166LanguageAttributeBase
```

## Remarks

Applied to each subdivision field, once per language (`AllowMultiple = true`); its
constructor is `internal`. The identifying language is carried by the
[Iso3166LanguageAttributeBase](Iso3166LanguageAttributeBase.md) base. You normally
read these names through the [Iso3166Extensions](Iso3166Extensions.md)
`GetSubdivisionNativeName` accessors.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `RomanizationSystem` | `string? RomanizationSystem { get; set; }` | Romanization system used for the written form, if any. |
| `SubdivisionCategoryIdentifier` | `ushort SubdivisionCategoryIdentifier { get; }` | Category identifier for the subdivision. |
| `SubdivisionName` | `string SubdivisionName { get; }` | The subdivision's name. |
| `SubdivisionNameLocalVariant` | `string? SubdivisionNameLocalVariant { get; set; }` | Local variant of the name, if any. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso3166LanguageAttributeBase](Iso3166LanguageAttributeBase.md)
- [Iso3166Extensions](Iso3166Extensions.md)
- [Iso3166Part2Subdivision](Iso3166Part2Subdivision.md)
- [Geography API reference](index.md)
</content>
