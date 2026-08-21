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
public readonly struct TzDataTimezone : IComparable, IEquatable<TzDataTimezone>
```

## Remarks

Per-zone metadata (ISO country codes, latitude, longitude, comment) is read
through the extension methods on [TzDataExtensions](TzDataExtensions.md). See
[Use time zones](../how-to/use-timezones.md).

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

## Methods

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
| Explicit | `explicit operator TzDataTimezone(string)` | Wraps an identifier string. |
| Implicit | `implicit operator string(TzDataTimezone)` | Unwraps to the identifier string. |
| Equality | `operator ==`, `!=` `(TzDataTimezone, TzDataTimezone)` | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Use time zones](../how-to/use-timezones.md)
- [Access time zone metadata](../how-to/access-timezone-metadata.md)
- [TzDataExtensions](TzDataExtensions.md)
- [Chronology API reference](index.md)
