---
title: UnM49Area Enum
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# UnM49Area Enum

## Definition

Namespace: `DataStandardizer.Geography`

UN M49 area codes for every level of the M49 hierarchy. Each member is named with
an underscore followed by the three-digit M49 code, and the member's underlying
value is the numeric M49 area code.

```csharp
public enum UnM49Area : ushort
```

## Remarks

Unlike [UnM49AreaByAlpha2CountryCode](UnM49AreaByAlpha2CountryCode.md) and
[UnM49AreaByAlpha3CountryCode](UnM49AreaByAlpha3CountryCode.md), which represent
countries and areas only, this enumeration represents **every level** of the M49
hierarchy as a member: the world, regions, sub-regions, intermediate regions, and
countries or areas.

Per-member metadata is carried by `UnM49AreaCodeAttribute` and read through
[UnM49Extensions](UnM49Extensions.md). Each member's attribute carries the codes
and the names of every level down to and including the level the member itself
occupies, so an ancestor's name is available directly from any member:

```csharp
UnM49Area._894.GetName("en");              // "Zambia"
UnM49Area._894.GetRegionName("en");        // "Africa"
UnM49Area._894.GetLevel();                 // UnM49AreaLevel.CountryOrArea
UnM49Area._894.GetParent();                // UnM49Area._014 (Eastern Africa)
UnM49Area._894.IsWithin(UnM49Area._002);   // true (Africa)
```

Because a code above the country or area level does not identify a country or
area, `GetCountryOrAreaName` returns `null` for such codes. Use
`GetName` to retrieve the name of whichever level a code occupies.

The M49 hierarchy is sparse. Not every country sits within an intermediate
region, and Antarctica (`_010`) sits directly within the world, so `GetParent`
returns the nearest populated ancestor rather than assuming a fixed depth.

## Fields

Members are named `_001` through `_894`, where the digits are the three-digit M49
code. For example `_001` is the world, `_002` is Africa, `_015` is Northern
Africa, `_014` is Eastern Africa, and `_894` is Zambia.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Use area codes](../how-to/use-area-codes.md)
- [UnM49AreaLevel](UnM49AreaLevel.md)
- [UnM49AreaByAlpha2CountryCode](UnM49AreaByAlpha2CountryCode.md)
- [UnM49AreaByAlpha3CountryCode](UnM49AreaByAlpha3CountryCode.md)
- [UnM49Extensions](UnM49Extensions.md)
- [UnM49AreaCodeAttribute](UnM49AreaCodeAttribute.md)
- [Geography API reference](index.md)
