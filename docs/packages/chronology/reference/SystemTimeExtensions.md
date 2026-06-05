---
title: SystemTimeExtensions Class
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# SystemTimeExtensions Class

## Definition

Namespace: `DataStandardizer.Chronology`

Extension methods that convert system-time values to the BCL date/time types and
recover the wrapped value from a
[SystemTimeWithGregorianCalendar](SystemTimeWithGregorianCalendar.md).

```csharp
public static class SystemTimeExtensions
```

## Remarks

`ToDateTime`, `ToDateOnly`, and `ToTimeOnly` are generic over any value type
implementing [ISystemTime](ISystemTime.md) (such as [UnixTime](UnixTime.md) and
[DosDateTime](DosDateTime.md)), deriving the result from the value's Julian Day
Number. `ToDateOnly` and `ToTimeOnly` require .NET 6 or later. `AsUnixTime` and
`AsDosDateTime` return a non-null value only when the
`SystemTimeWithGregorianCalendar` wraps that underlying type.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `AsDosDateTime()` | `SystemTimeWithGregorianCalendar` | `DosDateTime?` | Non-null only if the wrapped value was a `DosDateTime`. |
| `AsUnixTime()` | `SystemTimeWithGregorianCalendar` | `UnixTime?` | Non-null only if the wrapped value was a `UnixTime`. |
| `ToDateOnly<T>()` | `T` (`struct, ISystemTime`) | `DateOnly` | *(net6.0+)* |
| `ToDateTime<T>()` | `T` (`struct, ISystemTime`) | `DateTime` | Derived from the Julian Day Number. |
| `ToTimeOnly<T>()` | `T` (`struct, ISystemTime`) | `TimeOnly` | *(net6.0+)* |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. The
`ToDateOnly` and `ToTimeOnly` methods are available only on `net8.0` and
`net10.0` (they require `DateOnly` / `TimeOnly`, net6.0+).

## See also

- [Use Unix time](../how-to/use-unix-time.md)
- [Use DOS date/time](../how-to/use-dos-datetime.md)
- [The system-time model](../concepts/system-time-model.md)
- [ISystemTime](ISystemTime.md)
- [SystemTimeWithGregorianCalendar](SystemTimeWithGregorianCalendar.md)
- [Chronology API reference](index.md)
