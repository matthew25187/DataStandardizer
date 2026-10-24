---
title: TzDataTimezoneAttribute Class
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimezoneAttribute Class

> **Deprecated.** This attribute is marked `[Obsolete]` and will be removed in
> version 2.0. Use the `Latitude`, `Longitude`, `IsoCountryCodes` and `Comment`
> properties of [TzDataTimezone](TzDataTimezone.md) instead.

## Definition

Namespace: `DataStandardizer.Chronology`

Carries the per-zone metadata for a TZ Database time zone: latitude, longitude,
the ISO 3166 country codes the zone covers, and an optional comment. It is
applied to the static fields of [TzDataTimezone](TzDataTimezone.md).

```csharp
[Obsolete("Use the Latitude, Longitude, IsoCountryCodes and Comment members of TzDataTimezone instead.")]
[AttributeUsage(AttributeTargets.Field)]
public class TzDataTimezoneAttribute : Attribute
```

## Remarks

The attribute is still applied to the static fields of `TzDataTimezone`, so
code that reads it by reflection keeps working until it is removed. Reading the
metadata through the properties of `TzDataTimezone` needs no reflection, and
also works for an instance created by explicit cast from a string. On `net8.0`
and `net10.0` the `Comment` property is nullable (`string?`); on the .NET
Standard targets it is annotated `[CanBeNull]`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `TzDataTimezoneAttribute(double latitude, double longitude, params string[] isoCountryCodes)` | |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Comment` | `string? Comment { get; set; }` | Optional timezone comment. Declared as `string` (`[CanBeNull]`) on the .NET Standard targets. Use `TzDataTimezone.Comment` instead. |
| `IsoCountryCodes` | `string[] IsoCountryCodes { get; }` | ISO 3166-1 Alpha-2 codes for the countries the zone covers. Use `TzDataTimezone.IsoCountryCodes` instead. |
| `Latitude` | `double Latitude { get; }` | Latitude of the zone's principal location. Use `TzDataTimezone.Latitude` instead. |
| `Longitude` | `double Longitude { get; }` | Longitude of the zone's principal location. Use `TzDataTimezone.Longitude` instead. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. The
`Comment` property is nullable-annotated (`string?`) only on `net8.0` and
`net10.0`.

## See also

- [Access time zone metadata](../how-to/access-timezone-metadata.md)
- [TzDataExtensions](TzDataExtensions.md)
- [TzDataTimezone](TzDataTimezone.md)
- [Chronology API reference](index.md)
