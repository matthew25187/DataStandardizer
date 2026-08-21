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

Implicit conversions let a `DosDateTime` be used interchangeably with the packed
`uint` value. The component constructors validate each field and throw
`ArgumentOutOfRangeException` for out-of-range values. Seconds are stored at
two-second resolution, as in the DOS format.

## Constructors

| Constructor | Notes |
| --- | --- |
| `DosDateTime(uint value)` | Wraps a packed 32-bit value. |
| `DosDateTime(ushort year, ushort month, ushort day)` | Date only. Throws `ArgumentOutOfRangeException` for out-of-range components. |
| `DosDateTime(ushort year, ushort month, ushort day, ushort hour, ushort minute, ushort second)` | Date and time. Throws `ArgumentOutOfRangeException` for out-of-range components. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `JulianDayNumber` | `decimal JulianDayNumber { get; }` | The instant as a Julian Day Number. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Implicit | `implicit operator uint(DosDateTime)` | Unwrap to the packed 32-bit value. |
| Implicit | `implicit operator DosDateTime(uint)` | Wrap a packed value. |

## Extension methods

The following extensions apply to `DosDateTime`:

- [`ToDateTime<T>()`](SystemTimeExtensions.md) &mdash; returns `DateTime`.
- [`ToDateOnly<T>()`](SystemTimeExtensions.md) &mdash; returns `DateOnly` *(net6.0+)*.
- [`ToTimeOnly<T>()`](SystemTimeExtensions.md) &mdash; returns `TimeOnly` *(net6.0+)*.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Use DOS date/time](../how-to/use-dos-datetime.md)
- [The system-time model](../concepts/system-time-model.md)
- [ISystemTime](ISystemTime.md)
- [SystemTimeExtensions](SystemTimeExtensions.md)
- [DateTimeExtensions](DateTimeExtensions.md)
- [Chronology API reference](index.md)
