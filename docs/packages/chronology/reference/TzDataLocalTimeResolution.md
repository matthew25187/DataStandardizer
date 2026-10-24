---
title: TzDataLocalTimeResolution Enum
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataLocalTimeResolution Enum

## Definition

Namespace: `DataStandardizer.Chronology`

Identifies how a time on the local clock of a TZ Database time zone is placed
when it falls in a gap or an overlap.

```csharp
public enum TzDataLocalTimeResolution
```

## Remarks

Used by [`DateTimeExtensions.ToDateTimeOffset`](DateTimeExtensions.md).

- A **gap** is a period that the local clock skips, as when the clocks go
  forward. In America/New_York, 02:00 to 03:00 on 8 March 2026 is a gap.
- An **overlap** is a period that the local clock shows twice, as when the
  clocks go back. In America/New_York, 01:00 to 02:00 on 1 November 2026 is an
  overlap. It is shown first in EDT and then in EST.

A time outside any gap or overlap has exactly one instant, and every value gives
the same result for it.

## Fields

| Field | Value | Gap | Overlap |
| --- | --- | --- | --- |
| `Strict` | `0` | Throws `ArgumentException`. | Throws `ArgumentException`. |
| `Rfc5545` | `1` | Takes the offset in effect before the gap, which moves the time forward by the length of the gap. 02:30 in New York's gap gives 03:30 -04:00. | Takes the first occurrence, which is the earlier instant. 01:30 in New York's overlap gives 01:30 -04:00 (EDT). |

`Rfc5545` follows RFC 5545 (iCalendar), section 3.3.5.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Calculate UTC offsets](../how-to/calculate-utc-offsets.md#convert-a-local-time-to-an-instant)
- [DateTimeExtensions](DateTimeExtensions.md)
- [Chronology API reference](index.md)
