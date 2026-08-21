---
title: Iso3166CountryTerritoryAttribute Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166CountryTerritoryAttribute Class

## Definition

Namespace: `DataStandardizer.Geography`

A named territory of an ISO 3166-1 country, in a particular language.

```csharp
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class Iso3166CountryTerritoryAttribute : Iso3166LanguageAttributeBase
```

## Remarks

Applied to ISO 3166-1 country code members, once per territory and language
(`AllowMultiple = true`); its constructor is `internal`. The identifying language
is carried by the [Iso3166LanguageAttributeBase](Iso3166LanguageAttributeBase.md)
base.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `TerritoryIdentifier` | `ushort TerritoryIdentifier { get; }` | Identifier of the territory. |
| `TerritoryName` | `string TerritoryName { get; }` | Name of the territory. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso3166LanguageAttributeBase](Iso3166LanguageAttributeBase.md)
- [Iso3166Part1Alpha2Country](Iso3166Part1Alpha2Country.md)
- [Iso3166Part1Alpha3Country](Iso3166Part1Alpha3Country.md)
- [Geography API reference](index.md)
</content>
