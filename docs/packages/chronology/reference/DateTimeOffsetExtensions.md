---
title: DateTimeOffsetExtensions Class
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# DateTimeOffsetExtensions Class

## Definition

Namespace: `DataStandardizer.Chronology`

Extension methods that express a `DateTimeOffset` in a
[TzDataTimezone](TzDataTimezone.md).

```csharp
public static class DateTimeOffsetExtensions
```

## Remarks

`ToOffset` returns the same instant, with the offset in effect in the time zone
at that instant. It works like the built-in `DateTimeOffset.ToOffset(TimeSpan)`,
except that the offset comes from the time zone, as
[`TzDataExtensions.GetUtcOffset`](TzDataExtensions.md) gives it. For any instant
`x`, `x.ToOffset(tz).UtcDateTime` equals `x.UtcDateTime`.

`ToOffset` is the reverse of
[`DateTimeExtensions.ToDateTimeOffset`](DateTimeExtensions.md), which places a
local wall clock time in a time zone.

```csharp
var instant = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

instant.ToOffset(TzDataTimezone.America.New_York);   // 2026-07-01 08:00:00 -04:00
```

`DateTimeOffset` holds offsets only in whole minutes of at most 14 hours. An
offset it can't hold, such as some local mean time offsets before about 1900,
throws `ArgumentOutOfRangeException`. `default(TzDataTimezone)`, or an instance
whose identifier is not that of a known time zone, throws
`InvalidOperationException`.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `ToOffset(TzDataTimezone timezone)` | `DateTimeOffset` | `DateTimeOffset` | The same instant, with the offset in effect in `timezone` at that instant. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Calculate UTC offsets](../how-to/calculate-utc-offsets.md#get-the-offset-at-an-instant)
- [DateTimeExtensions](DateTimeExtensions.md)
- [TzDataExtensions](TzDataExtensions.md)
- [Chronology API reference](index.md)
