---
title: UnM49Extensions Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# UnM49Extensions Class

## Definition

Namespace: `DataStandardizer.Geography`

Extension methods that read the metadata attached to the UN M49 area code enums,
plus helpers that gather all related M49 codes for an enum type.

```csharp
public static class UnM49Extensions
```

## Remarks

Each metadata accessor exists as an overload extending
`UnM49AreaByAlpha2CountryCode`, `UnM49AreaByAlpha3CountryCode`, and
[UnM49Area](UnM49Area.md), returning the requested value or `null` when it is
unavailable or the code is undefined. The name accessors take an ISO 639 language
code accepting the Part 1 alpha-2 or Part 2 alpha-3 form for English, Chinese,
Russian, French, Spanish, and Arabic.

A code's attribute carries the codes and the names of every level down to and
including the level the code itself occupies, so the ancestor accessors resolve
from any code. `GetName` returns the name of whichever level the code occupies,
which matters for [UnM49Area](UnM49Area.md) because its members span every level
of the hierarchy; for the two country-keyed enumerations it is equivalent to
`GetCountryOrAreaName`.

`GetLevel`, `GetParent`, and `IsWithin` extend [UnM49Area](UnM49Area.md) only, as
the two country-keyed enumerations cannot represent a region as a value.

On .NET Standard targets the `string?` return and parameter types are `string`
annotated with JetBrains `[CanBeNull]` / `[NotNull]`.

## Methods

### Implicit implementation

The `GetM49Codes` helpers are ordinary static methods (no `this` parameter), not
extension methods.

| Method | Returns | Notes |
| --- | --- | --- |
| `GetM49Codes(Type enumType)` | `ushort[]` | All M49 codes (global, region, sub-region, intermediate, and area) for an M49 area enum type. Throws `ArgumentNullException` if `enumType` is `null`; `ArgumentException` if it is not an enum. |
| `GetM49Codes<T>()` *(where `T : struct, Enum`)* | `ushort[]` | Generic form of `GetM49Codes`. |

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `GetCountryOrAreaName(string languageCode)` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` / `UnM49Area` | `string?` | Country or area name for the M49 code. Returns `null` for a `UnM49Area` code above the country or area level. |
| `GetGlobalCode()` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` / `UnM49Area` | `ushort?` | Global code related to the M49 code. |
| `GetGlobalName(string languageCode)` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` / `UnM49Area` | `string?` | Name of the global code. |
| `GetIntermediateRegionCode()` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` / `UnM49Area` | `ushort?` | Intermediate region code related to the M49 code. |
| `GetIntermediateRegionName(string languageCode)` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` / `UnM49Area` | `string?` | Name of the intermediate region code. |
| `GetLevel()` | `UnM49Area` | `UnM49AreaLevel?` | Level of the M49 hierarchy occupied by the code. |
| `GetName(string languageCode)` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` / `UnM49Area` | `string?` | Name of the area at whatever level of the hierarchy it occupies. |
| `GetParent()` | `UnM49Area` | `UnM49Area?` | Parent area in the M49 hierarchy; `null` for the world. |
| `GetRegionCode()` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` / `UnM49Area` | `ushort?` | Region code related to the M49 code. |
| `GetRegionName(string languageCode)` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` / `UnM49Area` | `string?` | Name of the region code. |
| `GetSubRegionCode()` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` / `UnM49Area` | `ushort?` | Sub-region code related to the M49 code. |
| `GetSubRegionName(string languageCode)` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` / `UnM49Area` | `string?` | Name of the sub-region code. |
| `IsWithin(UnM49Area other)` | `UnM49Area` | `bool` | Whether the code falls within another area. An area does not fall within itself. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Access area metadata](../how-to/access-area-metadata.md)
- [Use area codes](../how-to/use-area-codes.md)
- [UnM49Area](UnM49Area.md)
- [UnM49AreaLevel](UnM49AreaLevel.md)
- [UnM49AreaByAlpha2CountryCode](UnM49AreaByAlpha2CountryCode.md)
- [UnM49AreaByAlpha3CountryCode](UnM49AreaByAlpha3CountryCode.md)
- [UnM49AreaCodeAttribute](UnM49AreaCodeAttribute.md)
- [Geography API reference](index.md)
</content>
