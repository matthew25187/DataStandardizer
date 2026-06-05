---
title: DateOnlyExtensions Class
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# DateOnlyExtensions Class

## Definition

Namespace: `DataStandardizer.Chronology`

Extension methods that convert a `DateOnly` to the system-time types
[UnixTime](UnixTime.md) and [DosDateTime](DosDateTime.md).

```csharp
public static class DateOnlyExtensions
```

## Remarks

This class and its members require .NET 6 or later (they depend on `DateOnly`)
and are not available on the .NET Standard targets. The whole class is gated
behind `#if NET6_0_OR_GREATER`.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `ToDosDateTime()` | `DateOnly` | `DosDateTime` | *(net6.0+)* |
| `ToUnixTime()` | `DateOnly` | `UnixTime` | *(net6.0+)* Time of day taken as midnight. |

## Applies to

Available only on `net8.0` and `net10.0`. **Not** available on `netstandard1.0`
or `netstandard2.0` (requires `DateOnly`, net6.0+).

## See also

- [Use Unix time](../how-to/use-unix-time.md)
- [Use DOS date/time](../how-to/use-dos-datetime.md)
- [UnixTime](UnixTime.md)
- [DosDateTime](DosDateTime.md)
- [TimeOnlyExtensions](TimeOnlyExtensions.md)
- [Chronology API reference](index.md)
