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

Each metadata accessor exists as an overload extending both
`UnM49AreaByAlpha2CountryCode` and `UnM49AreaByAlpha3CountryCode`, returning the
requested value or `null` when it is unavailable or the code is undefined. The
name accessors take an ISO 639 language code accepting the Part 1 alpha-2 or Part
2 alpha-3 form for English, Chinese, Russian, French, Spanish, and Arabic.

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
| `GetCountryOrAreaName(string languageCode)` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` | `string?` | Country or area name for the M49 code. |
| `GetGlobalCode()` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` | `ushort?` | Global code related to the M49 code. |
| `GetGlobalName(string languageCode)` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` | `string?` | Name of the global code. |
| `GetIntermediateRegionCode()` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` | `ushort?` | Intermediate region code related to the M49 code. |
| `GetIntermediateRegionName(string languageCode)` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` | `string?` | Name of the intermediate region code. |
| `GetRegionCode()` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` | `ushort?` | Region code related to the M49 code. |
| `GetRegionName(string languageCode)` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` | `string?` | Name of the region code. |
| `GetSubRegionCode()` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` | `ushort?` | Sub-region code related to the M49 code. |
| `GetSubRegionName(string languageCode)` | `UnM49AreaByAlpha2CountryCode` / `UnM49AreaByAlpha3CountryCode` | `string?` | Name of the sub-region code. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Access area metadata](../how-to/access-area-metadata.md)
- [Use area codes](../how-to/use-area-codes.md)
- [UnM49AreaByAlpha2CountryCode](UnM49AreaByAlpha2CountryCode.md)
- [UnM49AreaByAlpha3CountryCode](UnM49AreaByAlpha3CountryCode.md)
- [UnM49AreaCodeAttribute](UnM49AreaCodeAttribute.md)
- [Geography API reference](index.md)
</content>
