---
title: TzDataTimezone Struct
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimezone Struct

## Definition

Namespace: `DataStandardizer.Chronology`

A TZ Database time zone. You don't construct these; instead use the predefined
static instances, grouped by region and named after the database's hierarchical
identifiers — for example `TzDataTimezone.Europe.Berlin` and
`TzDataTimezone.America.Argentina.Buenos_Aires`.

```csharp
public readonly struct TzDataTimezone : IComparable, IEquatable<TzDataTimezone>, IParsable<TzDataTimezone>
```

`IParsable<TzDataTimezone>` is implemented on `net8.0` and later only.

## Remarks

To get a time zone from its identifier string, use `Parse` or `TryParse`. They
return the predefined instance with that identifier, and reject identifiers
that are not those of a predefined instance. Identifiers are case-sensitive.
Prefer them to the explicit cast, which accepts any string.

Per-zone location metadata (latitude, longitude, ISO country codes, comment) is
read through the `Latitude`, `Longitude`, `IsoCountryCodes` and `Comment`
properties. These replace the `GetLatitude`, `GetLongitude`,
`GetIsoCountryCodes` and `GetComment` extension methods on
[TzDataExtensions](TzDataExtensions.md), which are deprecated. See
[Access time zone metadata](../how-to/access-timezone-metadata.md).

An instance created by explicit cast from a string finds its metadata through the
predefined instance with the same identifier. The properties throw
`InvalidOperationException` for the `default` value and for an identifier that
is not that of a predefined instance.

## Fields

The time zone instances are grouped by region:

- [Africa](TzDataTimezone.Africa.md) — 19 time zones
- [America](TzDataTimezone.America.md) — 121 time zones
- [Antarctica](TzDataTimezone.Antarctica.md) — 8 time zones
- [Asia](TzDataTimezone.Asia.md) — 74 time zones
- [Atlantic](TzDataTimezone.Atlantic.md) — 8 time zones
- [Australia](TzDataTimezone.Australia.md) — 11 time zones
- [Europe](TzDataTimezone.Europe.md) — 38 time zones
- [Indian](TzDataTimezone.Indian.md) — 3 time zones
- [Pacific](TzDataTimezone.Pacific.md) — 30 time zones

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Comment` | `string? Comment { get; }` | The zone's comment from `zone1970.tab`, or `null` if it has none. Declared as `string` (`[CanBeNull]`) on the .NET Standard targets. |
| `IsoCountryCodes` | `IReadOnlyList<string> IsoCountryCodes { get; }` | ISO 3166-1 Alpha-2 codes for the countries the zone covers, in `zone1970.tab` order. |
| `Latitude` | `double Latitude { get; }` | Latitude of the zone's principal location, in decimal degrees. |
| `Longitude` | `double Longitude { get; }` | Longitude of the zone's principal location, in decimal degrees. |
| `ZoneLines` | `IReadOnlyList<TzDataZoneLine> ZoneLines { get; }` | The zone's full history of offsets from universal time, in chronological order. |

## Methods

### Static

| Method | Returns | Notes |
| --- | --- | --- |
| `Parse(string s)` | `TzDataTimezone` | Returns the predefined instance with identifier `s`. Throws `ArgumentNullException` if `s` is `null`, and `FormatException` if it is not a known identifier. |
| `Parse(string s, IFormatProvider? provider)` | `TzDataTimezone` | `IParsable<TzDataTimezone>` implementation; `net8.0` and later only. `provider` is ignored. |
| `TryParse(string? s, out TzDataTimezone result)` | `bool` | Returns `false`, with `result` set to `default`, if `s` is `null` or not a known identifier. |
| `TryParse(string? s, IFormatProvider? provider, out TzDataTimezone result)` | `bool` | `IParsable<TzDataTimezone>` implementation; `net8.0` and later only. `provider` is ignored. |

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CompareTo(object obj)` | `int` | |
| `Equals(TzDataTimezone other)` | `bool` | |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |
| `ToString()` | `string` | Override. Returns the identifier. |
| `ToString(IFormatProvider provider)` | `string` | |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Explicit | `explicit operator TzDataTimezone(string)` | Wraps an identifier string without validating it. Prefer `Parse` or `TryParse`. |
| Implicit | `implicit operator string(TzDataTimezone)` | Unwraps to the identifier string. |
| Equality | `operator ==`, `!=` `(TzDataTimezone, TzDataTimezone)` | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Use time zones](../how-to/use-timezones.md)
- [Access time zone metadata](../how-to/access-timezone-metadata.md)
- [TzDataExtensions](TzDataExtensions.md)
- [Chronology API reference](index.md)
