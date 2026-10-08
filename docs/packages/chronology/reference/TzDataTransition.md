---
title: TzDataTransition Struct
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTransition Struct

## Definition

Namespace: `DataStandardizer.Chronology`

A change in the offset from universal time in effect in a TZ Database time zone.

```csharp
public readonly struct TzDataTransition : IEquatable<TzDataTransition>
```

## Remarks

`GetTransitions` on [TzDataExtensions](TzDataExtensions.md) returns the
transitions over a period. You don't construct values; the constructor is
internal.

A transition is reported where the offset, the standard offset, daylight saving
time or the abbreviation changes, so `Before.UtcOffset` and `After.UtcOffset`
may be equal. The `After` offset is in effect from `InstantUtc`, and the
`Before` offset until then.

```csharp
var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var to = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

foreach (var transition in TzDataTimezone.America.New_York.GetTransitions(from, to))
{
    Console.WriteLine($"{transition.InstantUtc:u} {transition.Before.Abbreviation} -> {transition.After.Abbreviation}");
}

// 2026-03-08 07:00:00Z EST -> EDT
// 2026-11-01 06:00:00Z EDT -> EST
```

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `After` | `TzDataOffsetInfo After { get; }` | The offset in effect from the transition onwards, as a [TzDataOffsetInfo](TzDataOffsetInfo.md). |
| `Before` | `TzDataOffsetInfo Before { get; }` | The offset in effect immediately before the transition, as a [TzDataOffsetInfo](TzDataOffsetInfo.md). |
| `InstantUtc` | `DateTime InstantUtc { get; }` | The instant of the transition, of kind `Utc`. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(TzDataTransition other)` | `bool` | Equal where every property is equal. |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Equality | `operator ==`, `!=` `(TzDataTransition, TzDataTransition)` | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Calculate UTC offsets](../how-to/calculate-utc-offsets.md#list-the-transitions-over-a-period)
- [TzDataExtensions](TzDataExtensions.md)
- [TzDataOffsetInfo](TzDataOffsetInfo.md)
- [Chronology API reference](index.md)
