---
title: Iso3166Extensions Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166Extensions Class

## Definition

Namespace: `DataStandardizer.Geography`

Extension methods that read the metadata attached to the ISO 3166-1 country code
enums and the ISO 3166-2 subdivision code struct.

```csharp
public static class Iso3166Extensions
```

## Remarks

Each accessor reads the relevant attribute applied to the enum member or
subdivision field and returns the requested metadata, or `null`/`false` when it
is unavailable or the code is undefined. The ISO 3166-1 accessors
(`GetEnglishName`, `GetNativeName`, `IsIndependent`) each exist as an overload
extending both `Iso3166Part1Alpha2Country` and `Iso3166Part1Alpha3Country`.

On .NET Standard targets the `string?` return and parameter types are `string`
annotated with JetBrains `[CanBeNull]` / `[NotNull]`.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `GetEnglishName(Iso3166CountryName nameType)` | `Iso3166Part1Alpha2Country` / `Iso3166Part1Alpha3Country` | `string?` | English name in the requested form. |
| `GetNativeName(string iso639LanguageCode, Iso3166CountryName nameType)` | `Iso3166Part1Alpha2Country` / `Iso3166Part1Alpha3Country` | `string?` | Native name in the requested language and form. |
| `GetSubdivisionCategoryIdentifier()` | `Iso3166Part2Subdivision` | `ushort?` | Identifier of the subdivision's category. |
| `GetSubdivisionCategoryName(string iso639LanguageCode)` | `Iso3166Part2Subdivision` | `string?` | Category name in the requested language. |
| `GetSubdivisionCategoryNamePlural(string iso639LanguageCode)` | `Iso3166Part2Subdivision` | `string?` | Plural form of the category name. |
| `GetSubdivisionCode()` | `Iso3166Part2Subdivision` | `string?` | The ISO 3166-2 code (e.g. `AU-NT`). |
| `GetSubdivisionNativeName(string iso639LanguageCode, string? romanizationSystem = null)` | `Iso3166Part2Subdivision` | `string?` | Native name of the subdivision. |
| `GetSubdivisionNativeNameLocalVariant(string iso639LanguageCode, string? romanizationSystem = null)` | `Iso3166Part2Subdivision` | `string?` | Local variant of the native name. |
| `GetSubdivisionParentCode()` | `Iso3166Part2Subdivision` | `string?` | The parent subdivision code, if any. |
| `IsIndependent()` | `Iso3166Part1Alpha2Country` / `Iso3166Part1Alpha3Country` | `bool` | Whether the country or territory is independent. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Access country and subdivision metadata](../how-to/access-country-and-subdivision-metadata.md)
- [Iso3166Part1Alpha2Country](Iso3166Part1Alpha2Country.md)
- [Iso3166Part1Alpha3Country](Iso3166Part1Alpha3Country.md)
- [Iso3166Part2Subdivision](Iso3166Part2Subdivision.md)
- [Iso3166CountryName](Iso3166CountryName.md)
- [Geography API reference](index.md)
</content>
