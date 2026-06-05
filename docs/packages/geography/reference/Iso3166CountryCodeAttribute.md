---
title: Iso3166CountryCodeAttribute Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166CountryCodeAttribute Class

## Definition

Namespace: `DataStandardizer.Geography`

Describes an ISO 3166-1 country code (`Iso3166Part1Alpha2Country` or
`Iso3166Part1Alpha3Country`) with its English names and independence flag.

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class Iso3166CountryCodeAttribute : CodeAttributeBase
```

## Remarks

This attribute is applied to each member of the ISO 3166-1 country code enums; its
constructors are `internal`. You normally read the metadata through the
[Iso3166Extensions](Iso3166Extensions.md) accessors rather than reading the
attribute directly. `CodeAttributeBase` is defined in `DataStandardizer.Core`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `EnglishFullName` | `string? EnglishFullName { get; }` | English full name, if defined. |
| `EnglishShortName` | `string EnglishShortName { get; }` | English short name. |
| `EnglishShortNameUpper` | `string EnglishShortNameUpper { get; }` | English short name in uppercase. |
| `IsIndependent` | `bool IsIndependent { get; set; }` | Whether the country is independent. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso3166Extensions](Iso3166Extensions.md)
- [Iso3166Part1Alpha2Country](Iso3166Part1Alpha2Country.md)
- [Iso3166Part1Alpha3Country](Iso3166Part1Alpha3Country.md)
- [Geography API reference](index.md)
</content>
