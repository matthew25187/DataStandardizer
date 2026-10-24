---
title: IcalRecurrenceFrequency Enum
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalRecurrenceFrequency Enum

## Definition

Namespace: `DataStandardizer.File.iCal`

The FREQ rule part of a recurrence rule (RFC 5545 §3.3.10): the interval at which a
rule repeats. Read it from [IcalRecurrenceRule.Frequency](IcalRecurrenceRule.md).

**Syntax**

```csharp
public enum IcalRecurrenceFrequency
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Secondly` | 1 | Repeats every second or more (`SECONDLY`). |
| `Minutely` | 2 | Repeats every minute or more (`MINUTELY`). |
| `Hourly` | 3 | Repeats every hour or more (`HOURLY`). |
| `Daily` | 4 | Repeats every day or more (`DAILY`). |
| `Weekly` | 5 | Repeats every week or more (`WEEKLY`). |
| `Monthly` | 6 | Repeats every month or more (`MONTHLY`). |
| `Yearly` | 7 | Repeats every year or more (`YEARLY`). |

## Remarks

There is no member with the value 0. The default `IcalRecurrenceRule` has a
`Frequency` of 0, meaning no frequency, and is not a valid rule. Passing a value that
is not a member to the `IcalRecurrenceRule` constructor or to `WithFrequency` throws
`ArgumentOutOfRangeException`.

A rule whose DTSTART is a DATE must have a frequency of `Daily` or above; see
[IcalRecurrenceRule.GetOccurrences](IcalRecurrenceRule.md).

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [Expand recurrences](../how-to/expand-recurrences.md)
- [IcalRecurrenceRule](IcalRecurrenceRule.md)
- [IcalRecurrenceDay](IcalRecurrenceDay.md)
- [File.iCal API reference](index.md)
