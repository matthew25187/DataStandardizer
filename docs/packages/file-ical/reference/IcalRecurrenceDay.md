---
title: IcalRecurrenceDay Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalRecurrenceDay Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

A value of the BYDAY rule part of a recurrence rule (RFC 5545 §3.3.10): a day of the
week, optionally with an ordinal that picks one occurrence of it within the month or
year, such as `MO`, `1SU` or `-1FR`. Pass these to
[IcalRecurrenceRule.WithByDay](IcalRecurrenceRule.md), and read them back from
`IcalRecurrenceRule.GetByDay`.

**Syntax**

```csharp
public readonly struct IcalRecurrenceDay : IEquatable<IcalRecurrenceDay>
```

## Remarks

The ordinal is positive to count from the start of the month or year, negative to
count from the end, or 0 for every occurrence of the day. It runs from -53 to 53.

Whether an ordinal is allowed depends on the rule. RFC 5545 allows it only when FREQ
is MONTHLY or YEARLY, and not with BYWEEKNO when FREQ is YEARLY. This type doesn't
check that; [IcalRecurrenceRule.IsValid](IcalRecurrenceRule.md) does.

The default value is every Sunday (`SU`).

The type has no `Parse` method. A BYDAY value is parsed as part of a whole rule by
`IcalRecurrenceRule.Parse`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalRecurrenceDay(DayOfWeek weekday)` | Every occurrence of `weekday`. Throws `ArgumentOutOfRangeException` if `weekday` is not a `DayOfWeek` member. |
| `IcalRecurrenceDay(DayOfWeek weekday, int ordinal)` | One occurrence of `weekday`: 1 for the first, -1 for the last, and so on to 53 or -53; or 0 for every occurrence. Throws `ArgumentOutOfRangeException` if `weekday` is not a `DayOfWeek` member, or `ordinal` is not -53 to 53. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Ordinal` | `int Ordinal { get; }` | Which occurrence within the month or year: positive from the start, negative from the end; or 0 for every occurrence. |
| `Weekday` | `DayOfWeek Weekday { get; }` | The day of the week. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(IcalRecurrenceDay other)` | `bool` | `true` for the same day and ordinal. |
| `Equals(object? obj)` | `bool` | `true` when `obj` is an equal `IcalRecurrenceDay`. |
| `GetHashCode()` | `int` | Consistent with `Equals`. |
| `ToString()` | `string` | The value as it appears in a BYDAY rule part, such as `MO`, `1SU` or `-1SU`. A positive ordinal has no `+`. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `==(IcalRecurrenceDay left, IcalRecurrenceDay right)` | `bool` | Same as `left.Equals(right)`. |
| `!=(IcalRecurrenceDay left, IcalRecurrenceDay right)` | `bool` | Same as `!left.Equals(right)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1`, `net8.0`, and
`net10.0` builds.

## See also

- [Expand recurrences](../how-to/expand-recurrences.md)
- [IcalRecurrenceRule](IcalRecurrenceRule.md)
- [IcalRecurrenceFrequency](IcalRecurrenceFrequency.md)
- [File.iCal API reference](index.md)
