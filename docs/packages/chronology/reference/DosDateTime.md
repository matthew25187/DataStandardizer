---
title: DosDateTime Struct
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# DosDateTime Struct

## Definition

Namespace: `DataStandardizer.Chronology`

An MS-DOS packed date and time, stored internally as an unsigned 32-bit integer
in the compact format used by the DOS file system. The supported range is
1 January 1980 to 31 December 2107.

```csharp
public readonly struct DosDateTime : ISystemTime
```

## Remarks

A `DosDateTime` converts implicitly to its packed `uint` value, and a packed
value converts to a `DosDateTime` with an explicit cast. Every constructor
validates its input, so a `DosDateTime` always holds a real date and time: the
month is 1 to 12, the day is within the length of that month in that year, the
hour is 0 to 23, the minute is 0 to 59 and the seconds field is 0 to 29 (0 to
58 seconds). An out-of-range component, or a packed value that breaks any of
these rules, throws `ArgumentOutOfRangeException` from the constructor, and
`InvalidCastException` from the explicit cast. Seconds are stored at two-second
resolution, as in the DOS format.

A default instance, `default(DosDateTime)`, holds the packed value 0, which is
not a valid date. Its `JulianDayNumber` throws `InvalidOperationException`, and
its conversion to `uint` returns 0.

## Constructors

| Constructor | Notes |
| --- | --- |
| `DosDateTime(uint value)` | Wraps a packed 32-bit value. Throws `ArgumentOutOfRangeException` when it isn't a valid DOS date and time. |
| `DosDateTime(ushort year, ushort month, ushort day)` | Date only. Throws `ArgumentOutOfRangeException` for out-of-range components. |
| `DosDateTime(ushort year, ushort month, ushort day, ushort hour, ushort minute, ushort second)` | Date and time. Throws `ArgumentOutOfRangeException` for out-of-range components. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `JulianDayNumber` | `decimal JulianDayNumber { get; }` | The instant as a Julian Day Number. Throws `InvalidOperationException` for a default instance. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Implicit | `implicit operator uint(DosDateTime)` | Unwrap to the packed 32-bit value. |
| Explicit | `explicit operator DosDateTime(uint)` | Wrap a packed value. Throws `InvalidCastException` when it isn't a valid DOS date and time. |

## Extension methods

The following extensions apply to `DosDateTime`:

- [`ToDateTime<T>()`](SystemTimeExtensions.md) &mdash; returns `DateTime`.
- [`ToDateOnly<T>()`](SystemTimeExtensions.md) &mdash; returns `DateOnly` *(`net10.0` only)*.
- [`ToTimeOnly<T>()`](SystemTimeExtensions.md) &mdash; returns `TimeOnly` *(`net10.0` only)*.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, and `net10.0`.

## See also

- [Use DOS date/time](../how-to/use-dos-datetime.md)
- [The system-time model](../concepts/system-time-model.md)
- [ISystemTime](ISystemTime.md)
- [SystemTimeExtensions](SystemTimeExtensions.md)
- [DateTimeExtensions](DateTimeExtensions.md)
- [Chronology API reference](index.md)
