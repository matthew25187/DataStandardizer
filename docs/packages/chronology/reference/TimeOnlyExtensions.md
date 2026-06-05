---
title: TimeOnlyExtensions Class
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TimeOnlyExtensions Class

## Definition

Namespace: `DataStandardizer.Chronology`

Extension methods that convert a `TimeOnly` to the system-time types
[UnixTime](UnixTime.md) and [DosDateTime](DosDateTime.md).

```csharp
public static class TimeOnlyExtensions
```

## Remarks

This class and its members require .NET 6 or later (they depend on `TimeOnly`)
and are not available on the .NET Standard targets. The whole class is gated
behind `#if NET6_0_OR_GREATER`. `ToUnixTime` returns the seconds since midnight;
`ToDosDateTime` combines the time with a fixed DOS date of 1 January 1980.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `ToDosDateTime()` | `TimeOnly` | `DosDateTime` | *(net6.0+)* Combined with a fixed DOS date of 1 January 1980. |
| `ToUnixTime()` | `TimeOnly` | `UnixTime` | *(net6.0+)* Seconds since midnight. |

## Applies to

Available only on `net8.0` and `net10.0`. **Not** available on `netstandard1.0`
or `netstandard2.0` (requires `TimeOnly`, net6.0+).

## See also

- [Use Unix time](../how-to/use-unix-time.md)
- [Use DOS date/time](../how-to/use-dos-datetime.md)
- [UnixTime](UnixTime.md)
- [DosDateTime](DosDateTime.md)
- [DateOnlyExtensions](DateOnlyExtensions.md)
- [Chronology API reference](index.md)
