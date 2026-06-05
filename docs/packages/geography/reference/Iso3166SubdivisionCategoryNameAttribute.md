---
title: Iso3166SubdivisionCategoryNameAttribute Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166SubdivisionCategoryNameAttribute Class

## Definition

Namespace: `DataStandardizer.Geography`

A subdivision category name in a particular language, applied to the per-country
nested subdivision class.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class Iso3166SubdivisionCategoryNameAttribute : Iso3166LanguageAttributeBase
```

## Remarks

Applied to the per-country nested subdivision classes, once per category and
language (`AllowMultiple = true`); its constructors are `internal`. The
identifying language is carried by the
[Iso3166LanguageAttributeBase](Iso3166LanguageAttributeBase.md) base. You normally
read these names through the [Iso3166Extensions](Iso3166Extensions.md)
`GetSubdivisionCategoryName` accessors.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CategoryIdentifier` | `ushort CategoryIdentifier { get; }` | The category identifier. |
| `CategoryName` | `string CategoryName { get; }` | The category name. |
| `CategoryNamePlural` | `string? CategoryNamePlural { get; set; }` | Plural form of the category name, if defined. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso3166LanguageAttributeBase](Iso3166LanguageAttributeBase.md)
- [Iso3166Extensions](Iso3166Extensions.md)
- [Iso3166Part2Subdivision](Iso3166Part2Subdivision.md)
- [Geography API reference](index.md)
</content>
