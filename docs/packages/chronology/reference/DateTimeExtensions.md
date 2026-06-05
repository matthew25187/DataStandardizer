---
title: DateTimeExtensions Class
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# DateTimeExtensions Class

## Definition

Namespace: `DataStandardizer.Chronology`

Extension methods that convert a `DateTime` to the system-time types
[UnixTime](UnixTime.md) and [DosDateTime](DosDateTime.md).

```csharp
public static class DateTimeExtensions
```

## Remarks

`ToUnixTime` treats the `DateTime` as UTC; `ToDosDateTime` treats it as local
time and packs the year, month, day, hour, minute, and second into the DOS
format.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `ToDosDateTime()` | `DateTime` | `DosDateTime` | `DateTime` assumed to be local time. |
| `ToUnixTime()` | `DateTime` | `UnixTime` | `DateTime` assumed to be UTC. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Use Unix time](../how-to/use-unix-time.md)
- [Use DOS date/time](../how-to/use-dos-datetime.md)
- [UnixTime](UnixTime.md)
- [DosDateTime](DosDateTime.md)
- [Chronology API reference](index.md)
