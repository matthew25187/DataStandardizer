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
[UnixTime](UnixTime.md) and [DosDateTime](DosDateTime.md), and that place a
local wall clock time in a [TzDataTimezone](TzDataTimezone.md).

```csharp
public static class DateTimeExtensions
```

## Remarks

`ToUnixTime` treats the `DateTime` as UTC; `ToDosDateTime` treats it as local
time and packs the year, month, day, hour, minute, and second into the DOS
format.

### Placing a local time in a time zone

`ToDateTimeOffset` treats the `DateTime` as a time on the local clock of the
time zone. It returns a `DateTimeOffset` that keeps that time, with the offset
in effect at the instant the clock shows it. The result's `UtcDateTime` gives
the instant.

- The `DateTime` must be of kind `Unspecified`. The method appears on every
  `DateTime`, but one of kind `Utc` or `Local` throws `ArgumentException`.
- Unlike `DateTime.ToUniversalTime()`, which uses the machine's time zone,
  `ToDateTimeOffset` uses the rules of the time zone passed to it.
- A [TzDataLocalTimeResolution](TzDataLocalTimeResolution.md) decides what
  happens when the time falls in a gap, which the clock skips, or an overlap,
  which the clock shows twice:
  - `Rfc5545` follows RFC 5545, section 3.3.5. A time in a gap takes the
    offset in effect before the gap, which moves it forward by the length of
    the gap. A time in an overlap takes its first occurrence.
  - `Strict` throws `ArgumentException` in either case.
- `DateTimeOffset` holds offsets only in whole minutes of at most 14 hours. An
  offset it can't hold, such as some local mean time offsets before about 1900,
  throws `ArgumentOutOfRangeException`.
- `default(TzDataTimezone)`, or an instance whose identifier is not that of a
  known time zone, throws `InvalidOperationException`.

For example, in America/New_York:

| Local time | Result with `Rfc5545` | UTC |
| --- | --- | --- |
| 2026-07-01 12:00 | 2026-07-01 12:00 -04:00 | 16:00 |
| 2026-03-08 02:30 (gap) | 2026-03-08 03:30 -04:00 | 07:30 |
| 2026-11-01 01:30 (overlap) | 2026-11-01 01:30 -04:00 | 05:30 |

To list every offset in effect at a local time instead, use
[`TzDataExtensions.GetOffsetInfosForLocal`](TzDataExtensions.md).

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `ToDateTimeOffset(TzDataTimezone timezone, TzDataLocalTimeResolution resolution)` | `DateTime` | `DateTimeOffset` | `DateTime` must be of kind `Unspecified`, and is a time on the local clock of `timezone`. Throws `ArgumentException` for any other kind, and when `resolution` is `Strict` and the time falls in a gap or an overlap. |
| `ToDosDateTime()` | `DateTime` | `DosDateTime` | `DateTime` assumed to be local time. |
| `ToUnixTime()` | `DateTime` | `UnixTime` | `DateTime` assumed to be UTC. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Calculate UTC offsets](../how-to/calculate-utc-offsets.md#convert-a-local-time-to-an-instant)
- [Use Unix time](../how-to/use-unix-time.md)
- [Use DOS date/time](../how-to/use-dos-datetime.md)
- [UnixTime](UnixTime.md)
- [DosDateTime](DosDateTime.md)
- [TzDataLocalTimeResolution](TzDataLocalTimeResolution.md)
- [DateTimeOffsetExtensions](DateTimeOffsetExtensions.md)
- [Chronology API reference](index.md)
