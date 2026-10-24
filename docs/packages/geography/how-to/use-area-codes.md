---
title: Use area codes
parent: Geography
grand_parent: Packages
nav_order: 4
---

# Use area codes

UN M49 area codes are exposed through three enums, all with a `ushort`
underlying value equal to the numeric M49 code:

| Enum | Members | Covers |
| --- | --- | --- |
| `UnM49Area` | `_001` … `_894` | Every level of the hierarchy: the world, regions, sub-regions, intermediate regions, and countries or areas. |
| `UnM49AreaByAlpha2CountryCode` | ISO 3166-1 alpha-2 codes, e.g. `CH` | Countries or areas only. |
| `UnM49AreaByAlpha3CountryCode` | ISO 3166-1 alpha-3 codes, e.g. `CHE` | Countries or areas only. |

UN M49 defines area codes only in numeric form, but a .NET identifier cannot
start with a digit, so `UnM49Area` members are named with an underscore
followed by the three-digit code.

## Use any area by its M49 code

`UnM49Area` contains the entire M49 code set, so use it when you know the
numeric code, or when you need an area above the country or area level:

```csharp
var world = UnM49Area._001;
var europe = UnM49Area._150;
var westernEurope = UnM49Area._155;
var switzerland = UnM49Area._756;

var switzerlandAreaCode = (ushort)UnM49Area._756; // 756
```

The members also support navigating the hierarchy:

```csharp
UnM49Area._756.GetLevel();              // UnM49AreaLevel.CountryOrArea
UnM49Area._756.GetParent();             // UnM49Area._155 (Western Europe)
UnM49Area._756.IsWithin(UnM49Area._150); // true (Europe)
```

## Use a country or area by its country code

When you know the ISO 3166-1 country code instead, use one of the
country-keyed enums:

```csharp
var switzerlandAreaCode = (ushort)UnM49AreaByAlpha2CountryCode.CH; // 756
```

Or by its alpha-3 country code:

```csharp
var switzerlandAreaCode = (ushort)UnM49AreaByAlpha3CountryCode.CHE; // 756
```

These enums represent countries or areas only. The world, regions, sub-regions
and intermediate regions have no country code, so they are reachable from these
enums only as metadata (see [Access area metadata](access-area-metadata.md)).

## Convert between the enums

Because all three enums share the numeric M49 code as their underlying value,
a country or area converts between them with a cast:

```csharp
var switzerland = (UnM49Area)UnM49AreaByAlpha2CountryCode.CH; // UnM49Area._756
```

Converting from `UnM49Area` to a country-keyed enum is only meaningful for a
country or area; check with `Enum.IsDefined` or `GetLevel` first. To get the
country code itself as a string, use `GetIso3166Part1Alpha2Code` or
`GetIso3166Part1Alpha3Code`.

## See also

- [Access area metadata](access-area-metadata.md)
- [UnM49Area](../reference/UnM49Area.md)
- [UnM49AreaLevel](../reference/UnM49AreaLevel.md)
- [UnM49Extensions](../reference/UnM49Extensions.md)
