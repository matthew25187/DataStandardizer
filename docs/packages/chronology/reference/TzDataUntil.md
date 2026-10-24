---
title: TzDataUntil Struct
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataUntil Struct

## Definition

Namespace: `DataStandardizer.Chronology`

The moment at which a TZ Database zone line stops applying, corresponding to the
`UNTIL` column of the TZ Database source.

```csharp
public readonly struct TzDataUntil : IEquatable<TzDataUntil>
```

## Remarks

The source may leave out the trailing parts of an `UNTIL`. They take their
earliest values, so an `UNTIL` of `1970` denotes midnight wall clock time at the
start of 1 January 1970, and has a `Month` of 1, a `Day` of 1 and a `Time` of
zero.

The moment is a local time, measured against `TimeReference`. It becomes an
instant only with the offset in effect at the end of the zone line, which the
calculation methods of [TzDataExtensions](TzDataExtensions.md) take into
account. Use `GetTransitions` to find the instants at which the offset changes.

You don't construct values; the constructor is internal. Read them through the
`Until` property of a [TzDataZoneLine](TzDataZoneLine.md).

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Day` | `int? Day { get; }` | The day of the month, or the day from which the weekday is counted. `null` where `DayKind` is `LastWeekday`. |
| `DayKind` | `TzDataDayKind DayKind { get; }` | How the day is specified. See [TzDataDayKind](TzDataDayKind.md). |
| `DayOfWeek` | `DayOfWeek? DayOfWeek { get; }` | The weekday. `null` where `DayKind` is `DayOfMonth`. |
| `Month` | `int Month { get; }` | The month, from 1 to 12. |
| `Time` | `TimeSpan Time { get; }` | The time of day, measured against `TimeReference`. |
| `TimeReference` | `TzDataTimeReference TimeReference { get; }` | The clock `Time` is measured against. See [TzDataTimeReference](TzDataTimeReference.md). |
| `Year` | `int Year { get; }` | The year. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(TzDataUntil other)` | `bool` | Equal where every property is equal. |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Equality | `operator ==`, `!=` `(TzDataUntil, TzDataUntil)` | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [TzDataZoneLine](TzDataZoneLine.md)
- [TzDataDayKind](TzDataDayKind.md)
- [TzDataTimeReference](TzDataTimeReference.md)
- [Chronology API reference](index.md)
