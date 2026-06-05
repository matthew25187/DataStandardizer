---
title: UnixTime Struct
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# UnixTime Struct

## Definition

Namespace: `DataStandardizer.Chronology`

A point in time expressed as the number of seconds elapsed since the Unix epoch
(1 January 1970 00:00:00 UTC), stored internally as a signed 64-bit integer.

```csharp
public readonly struct UnixTime : ISystemTime
```

## Remarks

Implicit conversions let a `UnixTime` be used interchangeably with a `long`. The
[SystemTimeExtensions](SystemTimeExtensions.md),
[DateTimeExtensions](DateTimeExtensions.md),
[DateOnlyExtensions](DateOnlyExtensions.md), and
[TimeOnlyExtensions](TimeOnlyExtensions.md) classes provide conversions to and
from the BCL date/time types.

## Constructors

| Constructor | Notes |
| --- | --- |
| `UnixTime(long value)` | Wraps a count of seconds since the Unix epoch. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `JulianDayNumber` | `decimal JulianDayNumber { get; }` | The instant as a Julian Day Number. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Implicit | `implicit operator long(UnixTime)` | Unwrap to the underlying `long`. |
| Implicit | `implicit operator UnixTime(long)` | Wrap a `long`. |

## Extension methods

The following extensions apply to `UnixTime`:

- [`ToDateTime<T>()`](SystemTimeExtensions.md) &mdash; returns `DateTime`.
- [`ToDateOnly<T>()`](SystemTimeExtensions.md) &mdash; returns `DateOnly` *(net6.0+)*.
- [`ToTimeOnly<T>()`](SystemTimeExtensions.md) &mdash; returns `TimeOnly` *(net6.0+)*.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Use Unix time](../how-to/use-unix-time.md)
- [The system-time model](../concepts/system-time-model.md)
- [ISystemTime](ISystemTime.md)
- [SystemTimeExtensions](SystemTimeExtensions.md)
- [DateTimeExtensions](DateTimeExtensions.md)
- [Chronology API reference](index.md)
