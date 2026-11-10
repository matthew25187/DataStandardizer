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

This class and its members require the `net10.0` build (they depend on `TimeOnly`)
and are not available on the .NET Standard targets. The whole class is gated
behind `#if NET6_0_OR_GREATER`. `ToUnixTime` returns the seconds since midnight;
`ToDosDateTime` combines the time with a fixed DOS date of 1 January 1980.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `ToDosDateTime()` | `TimeOnly` | `DosDateTime` | *(`net10.0` only)* Combined with a fixed DOS date of 1 January 1980. |
| `ToUnixTime()` | `TimeOnly` | `UnixTime` | *(`net10.0` only)* Seconds since midnight. |

## Applies to

Available only on `net10.0`. **Not** available on `netstandard1.0`
or `netstandard2.0` (requires `TimeOnly`).

## See also

- [Use Unix time](../how-to/use-unix-time.md)
- [Use DOS date/time](../how-to/use-dos-datetime.md)
- [UnixTime](UnixTime.md)
- [DosDateTime](DosDateTime.md)
- [DateOnlyExtensions](DateOnlyExtensions.md)
- [Chronology API reference](index.md)
