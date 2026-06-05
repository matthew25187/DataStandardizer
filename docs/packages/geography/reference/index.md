---
title: API reference
parent: Geography
grand_parent: Packages
nav_order: 20
---

# DataStandardizer.Geography API reference

The public types of **DataStandardizer.Geography**. All types are in the
`DataStandardizer.Geography` namespace.

> **Applies to:** members shown returning a nullable reference type (e.g.
> `string?`) are non-nullable reference types on the .NET Standard targets, which
> use JetBrains `[CanBeNull]` / `[NotNull]` annotations instead of nullable
> reference type syntax. Behaviour is the same — a missing value is returned as
> `null`.

## Structures

| Type | Description |
| --- | --- |
| [Iso3166Part2Subdivision](Iso3166Part2Subdivision.md) | An ISO 3166-2 country subdivision code, exposed through predefined per-country nested static members. |

## Enumerations

| Type | Description |
| --- | --- |
| [Iso3166CountryName](Iso3166CountryName.md) | Selects the form of a country name to retrieve. |
| [Iso3166Part1Alpha2Country](Iso3166Part1Alpha2Country.md) | ISO 3166-1 alpha-2 country codes (e.g. `GB`). |
| [Iso3166Part1Alpha3Country](Iso3166Part1Alpha3Country.md) | ISO 3166-1 alpha-3 country codes (e.g. `GRC`). |
| [UnM49AreaByAlpha2CountryCode](UnM49AreaByAlpha2CountryCode.md) | UN M49 area codes keyed by ISO 3166-1 alpha-2 country code. |
| [UnM49AreaByAlpha3CountryCode](UnM49AreaByAlpha3CountryCode.md) | UN M49 area codes keyed by ISO 3166-1 alpha-3 country code. |

## Classes

| Type | Description |
| --- | --- |
| [Iso3166CountryCodeAttribute](Iso3166CountryCodeAttribute.md) | Metadata for an ISO 3166-1 country code. |
| [Iso3166CountryNameAttribute](Iso3166CountryNameAttribute.md) | A native country name in a particular language. |
| [Iso3166CountryTerritoryAttribute](Iso3166CountryTerritoryAttribute.md) | A named territory of an ISO 3166-1 country. |
| [Iso3166Extensions](Iso3166Extensions.md) | Extension methods that read ISO 3166 country and subdivision metadata. |
| [Iso3166LanguageAttribute](Iso3166LanguageAttribute.md) | Describes a language used to express names for a country or its subdivisions. |
| [Iso3166LanguageAttributeBase](Iso3166LanguageAttributeBase.md) | Base for the ISO 3166 name attributes; carries the identifying language. |
| [Iso3166Part2Enum](Iso3166Part2Enum.md) | Helpers for enumerating the subdivisions defined for a country. |
| [Iso3166SubdivisionCategoryNameAttribute](Iso3166SubdivisionCategoryNameAttribute.md) | A subdivision category name in a particular language. |
| [Iso3166SubdivisionCodeAttribute](Iso3166SubdivisionCodeAttribute.md) | Metadata for an ISO 3166-2 subdivision code. |
| [Iso3166SubdivisionNameAttribute](Iso3166SubdivisionNameAttribute.md) | A native subdivision name in a particular language. |
| [UnM49AreaCodeAttribute](UnM49AreaCodeAttribute.md) | Metadata for a UN M49 area code. |
| [UnM49Extensions](UnM49Extensions.md) | Extension methods that read UN M49 area metadata. |