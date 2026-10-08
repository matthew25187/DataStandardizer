---
title: TzDataRule Class
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataRule Class

## Definition

Namespace: `DataStandardizer.Chronology`

A daylight saving rule from the TZ Database, corresponding to one `Rule` line of
the TZ Database source.

```csharp
public sealed class TzDataRule
```

## Remarks

A rule describes a transition that recurs once in each year from `FromYear` to
`ToYear` inclusive. Once it has occurred, `Save` is added to the standard offset
of any zone line that uses the rule, until the next rule of the set takes
effect.

You don't construct rules; the constructor is internal. Read them through the
`Rules` property of a [TzDataZoneLine](TzDataZoneLine.md). Instances are
immutable.

The properties correspond to the columns of the source:

```text
# Rule NAME  FROM  TO   -  IN   ON       AT    SAVE  LETTER/S
Rule   US    2007  max  -  Mar  Sun>=8   2:00  1:00  D
Rule   US    2007  max  -  Nov  Sun>=1   2:00  0     S
```

| Column | Property |
| --- | --- |
| `FROM` | `FromYear` |
| `TO` | `ToYear` |
| `IN` | `Month` |
| `ON` | `DayKind`, `Day` and `DayOfWeek` |
| `AT` | `AtTime` and `AtTimeReference` |
| `SAVE` | `Save` and `IsDaylight` |
| `LETTER/S` | `Letter` |

The rule's name isn't exposed, because the TZ Database doesn't treat rule names
as stable.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `AtTime` | `TimeSpan AtTime { get; }` | The time of day of the transition, measured against `AtTimeReference`. May be 24 hours or more, denoting a time on a following day. |
| `AtTimeReference` | `TzDataTimeReference AtTimeReference { get; }` | The clock `AtTime` is measured against: wall clock, standard or universal time. See [TzDataTimeReference](TzDataTimeReference.md). |
| `Day` | `int? Day { get; }` | The day of the month of the transition, or the day from which the weekday is counted. `null` where `DayKind` is `LastWeekday`. |
| `DayKind` | `TzDataDayKind DayKind { get; }` | How the day of the transition is specified. See [TzDataDayKind](TzDataDayKind.md). |
| `DayOfWeek` | `DayOfWeek? DayOfWeek { get; }` | The weekday of the transition. `null` where `DayKind` is `DayOfMonth`. |
| `FromYear` | `int FromYear { get; }` | The first year in which the rule applies. |
| `IsDaylight` | `bool IsDaylight { get; }` | `true` if the time in effect after the transition is daylight saving time. The TZ Database decides this, so it doesn't always follow from `Save`: Europe/Dublin's negative winter save is daylight saving time. |
| `Letter` | `string Letter { get; }` | The variable part of the zone abbreviation, substituted for `%s` in a zone line `Format`, for example `D` or `S`. Empty where the source gives `-`. |
| `Month` | `int Month { get; }` | The month of the transition, from 1 to 12. |
| `Save` | `TimeSpan Save { get; }` | The amount added to the standard offset after the transition. May be negative, as for Europe/Dublin, or less than an hour, as for Australia/Lord_Howe. |
| `ToYear` | `int ToYear { get; }` | The last year in which the rule applies, or `int.MaxValue` where the rule applies indefinitely (`max`). |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetTransitionDate(int year)` | `DateTime` | The local date of the transition in `year`, at midnight and of kind `Unspecified`. The date is read against `AtTimeReference`, and `AtTime` is not added to it. A weekday counted from a day of the month may fall outside `Month`: `Fri<=1` falls in the preceding month unless the first is a Friday. Throws `ArgumentOutOfRangeException` if `year` is outside `FromYear` to `ToYear` or the range of `DateTime`, or if the rule names 29 February and `year` isn't a leap year. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Calculate UTC offsets](../how-to/calculate-utc-offsets.md#look-at-the-underlying-zone-lines)
- [TzDataZoneLine](TzDataZoneLine.md)
- [TzDataDayKind](TzDataDayKind.md)
- [TzDataTimeReference](TzDataTimeReference.md)
- [Chronology API reference](index.md)
