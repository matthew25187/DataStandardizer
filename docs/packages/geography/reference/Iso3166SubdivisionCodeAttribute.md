---
title: Iso3166SubdivisionCodeAttribute Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166SubdivisionCodeAttribute Class

## Definition

Namespace: `DataStandardizer.Geography`

Describes an ISO 3166-2 subdivision code (`Iso3166Part2Subdivision`) with its
category identifier, code, and parent code.

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class Iso3166SubdivisionCodeAttribute : CodeAttributeBase
```

## Remarks

Applied to each subdivision field; its constructors are `internal`. You normally
read the metadata through the [Iso3166Extensions](Iso3166Extensions.md) accessors.
`CodeAttributeBase` is defined in `DataStandardizer.Core`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `SubdivisionCategoryIdentifier` | `ushort SubdivisionCategoryIdentifier { get; set; }` | Identifier of the subdivision's category. |
| `SubdivisionCode` | `string SubdivisionCode { get; set; }` | The ISO 3166-2 code. |
| `SubdivisionParentCode` | `string? SubdivisionParentCode { get; set; }` | Parent subdivision code, if any. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso3166Extensions](Iso3166Extensions.md)
- [Iso3166Part2Subdivision](Iso3166Part2Subdivision.md)
- [Geography API reference](index.md)
</content>
