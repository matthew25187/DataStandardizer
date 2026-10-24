---
title: UnM49AreaLevel Enum
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# UnM49AreaLevel Enum

## Definition

Namespace: `DataStandardizer.Geography`

Level of the UN M49 hierarchy occupied by an area.

```csharp
public enum UnM49AreaLevel
```

## Remarks

Returned by `GetLevel` on [UnM49Area](UnM49Area.md), which represents every level
of the M49 hierarchy as a member.

A member's level is determined from the numeric codes on its
`UnM49AreaCodeAttribute`: if the member's own value appears among those codes,
the level corresponds to that code's position; if it does not appear, the member
necessarily identifies a country or area, as a country or area is deliberately
not represented by a constructor parameter or a property on the attribute.

## Fields

| Field | Description |
| --- | --- |
| `Unknown` | The level of the area could not be determined. |
| `Global` | The world. |
| `Region` | A region. |
| `SubRegion` | A sub-region. |
| `IntermediateRegion` | An intermediate region. |
| `CountryOrArea` | A country or area. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [UnM49Area](UnM49Area.md)
- [UnM49Extensions](UnM49Extensions.md)
- [UnM49AreaCodeAttribute](UnM49AreaCodeAttribute.md)
- [Geography API reference](index.md)
