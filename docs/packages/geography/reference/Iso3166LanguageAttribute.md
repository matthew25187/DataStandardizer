---
title: Iso3166LanguageAttribute Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166LanguageAttribute Class

## Definition

Namespace: `DataStandardizer.Geography`

Describes a language used to express names for a country or its subdivisions.

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
public sealed class Iso3166LanguageAttribute : Iso3166LanguageAttributeBase
```

## Remarks

Applied to country code fields or per-country nested subdivision classes, once per
language (`AllowMultiple = true`); its constructor is `internal`. The identifying
language is carried by the [Iso3166LanguageAttributeBase](Iso3166LanguageAttributeBase.md)
base.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `IsAdministrative` | `bool IsAdministrative { get; set; }` | Whether the language is used by the country for administrative purposes. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso3166LanguageAttributeBase](Iso3166LanguageAttributeBase.md)
- [Geography API reference](index.md)
</content>
