---
title: Iso3166Part2Enum Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166Part2Enum Class

## Definition

Namespace: `DataStandardizer.Geography`

Helpers for enumerating the ISO 3166-2 subdivisions defined for a country,
returning either their member names or their `Iso3166Part2Subdivision` values.

```csharp
public static class Iso3166Part2Enum
```

## Remarks

Each method takes an ISO 3166-1 country code (alpha-2 or alpha-3) and returns the
subdivisions defined for that country. An invalid or unknown country code yields
an empty array rather than `null`.

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `GetNames(Iso3166Part1Alpha2Country country)` | `string[]` | Member names of the subdivisions for the country (empty if none). |
| `GetNames(Iso3166Part1Alpha3Country country)` | `string[]` | As above, by alpha-3 country. |
| `GetValues(Iso3166Part1Alpha2Country country)` | `Iso3166Part2Subdivision[]` | The subdivision values for the country (empty if none). |
| `GetValues(Iso3166Part1Alpha3Country country)` | `Iso3166Part2Subdivision[]` | As above, by alpha-3 country. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Use subdivision codes](../how-to/use-subdivision-codes.md)
- [Iso3166Part2Subdivision](Iso3166Part2Subdivision.md)
- [Iso3166Part1Alpha2Country](Iso3166Part1Alpha2Country.md)
- [Iso3166Part1Alpha3Country](Iso3166Part1Alpha3Country.md)
- [Geography API reference](index.md)
</content>
