---
title: TzDataOffsetInfo Struct
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataOffsetInfo Struct

## Definition

Namespace: `DataStandardizer.Chronology`

The offset from universal time in effect in a TZ Database time zone over a
period, with the abbreviation by which it is known.

```csharp
public readonly struct TzDataOffsetInfo : IEquatable<TzDataOffsetInfo>
```

## Remarks

`GetOffsetInfo` on [TzDataExtensions](TzDataExtensions.md) returns the offset
in effect at an instant, and the `Before` and `After` properties of
[TzDataTransition](TzDataTransition.md) return the offsets either side of a
transition. You don't construct values; the constructor is internal.

`UtcOffset` is always `StandardOffset` plus `DaylightSavings`. The TZ Database
decides which time is daylight saving time: in winter, Europe/Dublin has a
`DaylightSavings` of -1 hour, and `IsDaylightSavingTime` is `true`.

```csharp
var info = TzDataTimezone.America.New_York.GetOffsetInfo(
    new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));

// info.UtcOffset            -04:00:00
// info.StandardOffset       -05:00:00
// info.DaylightSavings      01:00:00
// info.IsDaylightSavingTime true
// info.Abbreviation         EDT
// info.ValidFromUtc         2026-03-08 07:00:00 (UTC)
// info.ValidUntilUtc        2026-11-01 06:00:00 (UTC)
```

The period from `ValidFromUtc` to `ValidUntilUtc` is the one over which the
offset, its parts and the abbreviation all stay the same: it runs from the
previous transition to the next.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Abbreviation` | `string Abbreviation { get; }` | The TZ Database abbreviation, for example `EST`, `BST` or `+0530`. Never `null`; empty for the `default` value. |
| `DaylightSavings` | `TimeSpan DaylightSavings { get; }` | The amount added to the standard offset. May be negative, as for Europe/Dublin in winter. |
| `IsDaylightSavingTime` | `bool IsDaylightSavingTime { get; }` | `true` if daylight saving time is in effect; otherwise, `false`. |
| `StandardOffset` | `TimeSpan StandardOffset { get; }` | The offset of standard time from universal time. |
| `UtcOffset` | `TimeSpan UtcOffset { get; }` | The total offset from universal time: `StandardOffset` plus `DaylightSavings`. |
| `ValidFromUtc` | `DateTime? ValidFromUtc { get; }` | The instant, of kind `Utc`, from which the offset is in effect, or `null` where it has been in effect since the beginning of time. |
| `ValidUntilUtc` | `DateTime? ValidUntilUtc { get; }` | The instant, of kind `Utc`, at which the offset stops being in effect, or `null` where it stays in effect indefinitely. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(TzDataOffsetInfo other)` | `bool` | Equal where every property is equal. `Abbreviation` is compared ordinally. |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Equality | `operator ==`, `!=` `(TzDataOffsetInfo, TzDataOffsetInfo)` | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Calculate UTC offsets](../how-to/calculate-utc-offsets.md#get-everything-at-once)
- [TzDataExtensions](TzDataExtensions.md)
- [TzDataTransition](TzDataTransition.md)
- [Chronology API reference](index.md)
