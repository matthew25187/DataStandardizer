---
title: Iso3166CountryNameAttribute Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166CountryNameAttribute Class

## Definition

Namespace: `DataStandardizer.Geography`

A native country name for an ISO 3166-1 country code in a particular language.

```csharp
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class Iso3166CountryNameAttribute : Iso3166LanguageAttributeBase
```

## Remarks

Applied to each ISO 3166-1 country code member, once per language
(`AllowMultiple = true`); its constructors are `internal`. The identifying
language is carried by the [Iso3166LanguageAttributeBase](Iso3166LanguageAttributeBase.md)
base. You normally read these names through the
[Iso3166Extensions](Iso3166Extensions.md) `GetNativeName` accessors.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `FullName` | `string? FullName { get; }` | The country's full name, if defined. |
| `ShortName` | `string ShortName { get; }` | The country's short name. |
| `ShortNameUpper` | `string ShortNameUpper { get; }` | The country's short name in uppercase. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso3166LanguageAttributeBase](Iso3166LanguageAttributeBase.md)
- [Iso3166Extensions](Iso3166Extensions.md)
- [Iso3166CountryName](Iso3166CountryName.md)
- [Geography API reference](index.md)
</content>
