---
title: TzDataDayKind Enum
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataDayKind Enum

## Definition

Namespace: `DataStandardizer.Chronology`

Identifies how the day of a TZ Database rule transition or zone line `UNTIL` is
specified.

```csharp
public enum TzDataDayKind
```

## Remarks

Used by the `DayKind` property of [TzDataRule](TzDataRule.md) and
[TzDataUntil](TzDataUntil.md). The kind determines which of their `Day` and
`DayOfWeek` properties are set.

## Fields

| Field | Source form | `Day` | `DayOfWeek` | Meaning |
| --- | --- | --- | --- | --- |
| `DayOfMonth` | `5` | Set | `null` | A fixed day of the month. |
| `LastWeekday` | `lastSun` | `null` | Set | The last given weekday of the month. |
| `WeekdayOnOrAfter` | `Sun>=8` | Set | Set | The first given weekday on or after a day of the month. |
| `WeekdayOnOrBefore` | `Sun<=25` | Set | Set | The last given weekday on or before a day of the month. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [TzDataRule](TzDataRule.md)
- [TzDataUntil](TzDataUntil.md)
- [Chronology API reference](index.md)
