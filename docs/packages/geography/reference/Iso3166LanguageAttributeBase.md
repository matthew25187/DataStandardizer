---
title: Iso3166LanguageAttributeBase Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166LanguageAttributeBase Class

## Definition

Namespace: `DataStandardizer.Geography`

Base for the ISO 3166 name attributes; carries the language identifying a name.

```csharp
public abstract class Iso3166LanguageAttributeBase : Attribute
```

## Remarks

Derived attributes include `Iso3166CountryNameAttribute`,
`Iso3166CountryTerritoryAttribute`, `Iso3166LanguageAttribute`,
`Iso3166SubdivisionNameAttribute`, and `Iso3166SubdivisionCategoryNameAttribute`.
The constructor is `protected internal`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Iso639Part1Code` | `string? Iso639Part1Code { get; }` | ISO 639 Part 1 language code. |
| `Iso639Part2TCode` | `string Iso639Part2TCode { get; }` | ISO 639 Part 2T language code. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso3166CountryNameAttribute](Iso3166CountryNameAttribute.md)
- [Iso3166CountryTerritoryAttribute](Iso3166CountryTerritoryAttribute.md)
- [Iso3166LanguageAttribute](Iso3166LanguageAttribute.md)
- [Iso3166SubdivisionNameAttribute](Iso3166SubdivisionNameAttribute.md)
- [Iso3166SubdivisionCategoryNameAttribute](Iso3166SubdivisionCategoryNameAttribute.md)
- [Geography API reference](index.md)
</content>
