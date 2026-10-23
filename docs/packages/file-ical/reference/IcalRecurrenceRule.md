---
title: IcalRecurrenceRule Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalRecurrenceRule Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

A RECUR value (RFC 5545 §3.3.10): a recurrence rule, such as
`FREQ=MONTHLY;BYDAY=-1FR;COUNT=6`. The type parses, checks and formats a rule, and
`GetOccurrences` expands it into occurrences in local time. The `RecurrenceRules`
property of [IcalEvent](IcalEvent.md), [IcalTodo](IcalTodo.md),
[IcalJournal](IcalJournal.md) and
[IcalTimeZoneObservance](IcalTimeZoneObservance.md) holds values of this type.

**Syntax**

```csharp
public readonly partial struct IcalRecurrenceRule : IEquatable<IcalRecurrenceRule>, ISpanFormattable
```

## Remarks

**Immutable.** A rule is built from a frequency with the `With` methods, each of which
returns a new rule. For example:

```csharp
var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Monthly)
    .WithByDay(new IcalRecurrenceDay(DayOfWeek.Friday, -1))
    .WithCount(6);
```

The `With` methods check each value, but not the constraints between rule parts,
which a rule under construction may break for a while. `IsValid` checks those, and
`Parse`, `TryParse`, `TryFormat`, `ToString(IcalValueFormat)` and `GetOccurrences`
refuse a rule that breaks them.

**Rule parts.**

| Rule part | Read with | Set with | Values |
| --- | --- | --- | --- |
| `FREQ` | `Frequency` | constructor, `WithFrequency` | An [IcalRecurrenceFrequency](IcalRecurrenceFrequency.md). Required. |
| `UNTIL` | `UntilDate`, `UntilDateTime` | `WithUntil`, `WithoutEnd` | A DATE, or a DATE-TIME that is floating or in UTC. |
| `COUNT` | `Count` | `WithCount`, `WithoutEnd` | 1 to `int.MaxValue`. |
| `INTERVAL` | `Interval` | `WithInterval` | 1 to `int.MaxValue`. Defaults to 1. |
| `BYSECOND` | `GetBySecond` | `WithBySecond` | 0 to 60. |
| `BYMINUTE` | `GetByMinute` | `WithByMinute` | 0 to 59. |
| `BYHOUR` | `GetByHour` | `WithByHour` | 0 to 23. |
| `BYDAY` | `GetByDay` | `WithByDay` | [IcalRecurrenceDay](IcalRecurrenceDay.md) values, with an ordinal of -53 to 53. |
| `BYMONTHDAY` | `GetByMonthDay` | `WithByMonthDay` | 1 to 31 or -31 to -1. |
| `BYYEARDAY` | `GetByYearDay` | `WithByYearDay` | 1 to 366 or -366 to -1. |
| `BYWEEKNO` | `GetByWeekNo` | `WithByWeekNo` | ISO 8601 week numbers, 1 to 53 or -53 to -1. |
| `BYMONTH` | `GetByMonth` | `WithByMonth` | 1 to 12. |
| `BYSETPOS` | `GetBySetPos` | `WithBySetPos` | 1 to 366 or -366 to -1. |
| `WKST` | `WeekStart` | `WithWeekStart` | A `DayOfWeek`. Monday when not given. |

UNTIL and COUNT exclude each other: `WithUntil` removes COUNT, and `WithCount`
removes UNTIL.

**Validity.** `IsValid` checks the constraints of RFC 5545 §3.3.10 that apply between
rule parts:

- FREQ is given.
- UNTIL and COUNT are not both given.
- A BYDAY value with an ordinal is given only when FREQ is MONTHLY or YEARLY, and not
  with BYWEEKNO when FREQ is YEARLY.
- BYMONTHDAY is not given when FREQ is WEEKLY.
- BYYEARDAY is not given when FREQ is DAILY, WEEKLY or MONTHLY.
- BYWEEKNO is given only when FREQ is YEARLY.
- BYSETPOS is given only with another BYxxx rule part.

Constraints that depend on DTSTART, such as the value type of UNTIL, are not checked
by `IsValid`. The default value has no frequency, so it is not valid.

**Parsing.** Rule part names and values are not case-sensitive. Each rule part may
appear once. Parsing is strict: an empty rule part (such as a trailing `;`), white
space, an unknown rule part (including extensions such as `RSCALE` from RFC 7529),
a COUNT or INTERVAL of 0, or an out-of-range value makes the whole value invalid.
A parsed rule must also pass `IsValid`.

**Normal form.** The values of each BYxxx rule part are held as a set, so their order
and any repeats are not kept. They are written in a fixed order: positive values in
increasing order, then negative ones in decreasing order (`1,2,-1,-2`), and BYDAY
values grouped by day from Sunday. The rule parts are written in a fixed order too,
starting with FREQ as RFC 5545 requires: FREQ, UNTIL, COUNT, INTERVAL, BYSECOND,
BYMINUTE, BYHOUR, BYDAY, BYMONTHDAY, BYYEARDAY, BYWEEKNO, BYMONTH, BYSETPOS, WKST.
An INTERVAL of 1 is written if it was given, and left out if it was not.

**Forms.** A rule has only the `Text` form of [IcalValueFormat](IcalValueFormat.md).
In a jCal document (RFC 7265 §3.6.10) a rule is a JSON object with a member for each
rule part, and in an xCal document (RFC 6321 §3.6.10) a `recur` element with a child
element for each rule part. So parsing or formatting in the `Json` or `Xml` form
throws `NotSupportedException`. [JcalReader](JcalReader.md),
[JcalWriter](JcalWriter.md), [XcalReader](XcalReader.md) and
[XcalWriter](XcalWriter.md) map the object and the elements to and from the text form.

**Parsing from a string.** `Parse` and `TryParse` take a `ReadOnlySpan<char>`; there
are no `string` overloads. A `string` converts to `ReadOnlySpan<char>` implicitly on
.NET Core 2.1 and later and on .NET Standard 2.1. On .NET Framework, call
`AsSpan()` on the string. The type does not implement `IParsable<T>` or
`ISpanParsable<T>`, because parsing needs an `IcalValueFormat`.

**Formatting.** `ToString()` doesn't check the rule, so it also shows a rule that is
not valid. `ToString(IcalValueFormat)` and `TryFormat` throw
`InvalidOperationException` for a rule that is not valid. On the `net8.0` and
`net10.0` builds the type implements `ISpanFormattable`, and an interpolated string
such as `$"{rule}"` formats through `ISpanFormattable.TryFormat`. So, on those
builds, interpolating a rule that is not valid throws `InvalidOperationException`,
where `rule.ToString()` does not.

**Allocation.** The rule parts are held inline, so `TryParse` from a span and
`TryFormat` into a span do not allocate (covered by `IcalValueAllocationTests`). The
`Get` methods return a new array on each call. `ToString` allocates a buffer, starting
at 128 characters and doubling until the rule fits, and the result string.

**Expanding occurrences.** `GetOccurrences` applies the rule in local time
throughout. Occurrences are wall-clock values with the kind and time zone of `start`,
and resolving them to UTC, including any that fall in a daylight saving gap or
overlap, is left to the caller. The components' own `GetOccurrences` methods, such as
`IcalEvent.GetOccurrences`, do that, and add RDATE and EXDATE. Specifically:

- The BYxxx rule parts are applied as the table in RFC 5545 §3.3.10 sets out,
  followed by BYSETPOS, WKST, COUNT and UNTIL. Where no rule part gives the day or
  time, it is taken from `start`.
- An invalid date is skipped, not clamped, so `FREQ=MONTHLY;BYMONTHDAY=30` has no
  occurrence in February. A second of 60 is never generated.
- Only the occurrences the rule generates are given. RFC 5545 counts DTSTART as the
  first occurrence of a recurrence set, but if `start` does not match the rule it is
  not given here, and adding it is left to the caller.
- `from` is inclusive and `to` is exclusive. Both are compared with occurrences by
  their wall-clock fields, whatever their kind.
- COUNT counts from `start`, whatever `from` is.
- UNTIL is compared by its wall-clock fields when it is floating, or when it and
  `start` are both in UTC. A DATE UNTIL includes the whole of that day. When UNTIL is
  in UTC and `start` is not, use the overload that takes `toUtc`, which converts each
  occurrence to UTC to compare it with UNTIL. For a DATE start, a DATE-TIME UNTIL is
  compared by its date.
- The occurrences end at 9999-12-31, the last date.
- The occurrences are generated as they are enumerated, in increasing order.

The arguments and the rule are checked when `GetOccurrences` is called, not when the
result is enumerated.

**MaxRecurrenceIterations.** A rule that steps through more consecutive periods than
[IcalOptions.MaxRecurrenceIterations](IcalOptions.md) without an occurrence throws
`InvalidOperationException` while it is enumerated. A period is one step of the rule's
frequency, such as a year for `FREQ=YEARLY`. So a rule that can never match, such as
`FREQ=SECONDLY;BYMONTH=2;BYMONTHDAY=30`, ends with an exception rather than running
for a very long time. The default is 100,000, and the value must be at least 1. The
overloads without `options` use the default.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalRecurrenceRule(IcalRecurrenceFrequency frequency)` | Creates a rule with a frequency and no other rule parts. Throws `ArgumentOutOfRangeException` if `frequency` is not an `IcalRecurrenceFrequency` member. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Count` | `int? Count { get; }` | The number of occurrences (COUNT); or `null` if the rule does not give one. |
| `Frequency` | `IcalRecurrenceFrequency Frequency { get; }` | The frequency (FREQ); or 0 for the default value, which has none. |
| `Interval` | `int Interval { get; }` | The interval (INTERVAL), in units of `Frequency`. 1 if the rule does not give one. |
| `IsValid` | `bool IsValid { get; }` | `true` if the rule meets the constraints listed under Validity. |
| `UntilDate` | `IcalDate? UntilDate { get; }` | The last date (UNTIL) when it is a DATE; or `null` if the rule has no UNTIL or it is a DATE-TIME. |
| `UntilDateTime` | `IcalDateTime? UntilDateTime { get; }` | The last date-time (UNTIL) when it is a DATE-TIME, which is floating or in UTC; or `null` if the rule has no UNTIL or it is a DATE. |
| `WeekStart` | `DayOfWeek? WeekStart { get; }` | The day the week starts on (WKST); or `null` if the rule does not give one, in which case it is Monday. |

## Methods

### Parsing

| Method | Returns | Notes |
| --- | --- | --- |
| `Parse(ReadOnlySpan<char> value, IcalValueFormat format)` | `IcalRecurrenceRule` | Static. Parses a rule and checks it against the constraints under Validity. Throws `FormatException` if `value` is not a valid rule; `NotSupportedException` if `format` is `Json` or `Xml`; or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalRecurrenceRule result)` | `bool` | Static. `false` if `value` is not a valid rule. Throws `NotSupportedException` if `format` is `Json` or `Xml`, or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. Does not allocate. |

### Formatting

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | The `Text` form. Does not check the rule, so it also shows a rule that is not valid. |
| `ToString(IcalValueFormat format)` | `string` | The rule in `format`. Throws `InvalidOperationException` if the rule is not valid; `NotSupportedException` if `format` is `Json` or `Xml`; or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Writes the rule in `format`. Returns `false`, with `charsWritten` 0, if `destination` is too short. Throws `InvalidOperationException` if the rule is not valid; `NotSupportedException` if `format` is `Json` or `Xml`; or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. Does not allocate. |

### Reading the BYxxx rule parts

Each method returns a new array of the values in the order they are written, or an
empty array if the rule does not give the rule part.

| Method | Returns | Notes |
| --- | --- | --- |
| `GetByDay()` | `IcalRecurrenceDay[]` | BYDAY, grouped by day from Sunday. |
| `GetByHour()` | `int[]` | BYHOUR, 0 to 23. |
| `GetByMinute()` | `int[]` | BYMINUTE, 0 to 59. |
| `GetByMonth()` | `int[]` | BYMONTH, 1 to 12. |
| `GetByMonthDay()` | `int[]` | BYMONTHDAY, 1 to 31 or -31 to -1. |
| `GetBySecond()` | `int[]` | BYSECOND, 0 to 60. |
| `GetBySetPos()` | `int[]` | BYSETPOS, 1 to 366 or -366 to -1. |
| `GetByWeekNo()` | `int[]` | BYWEEKNO, 1 to 53 or -53 to -1. |
| `GetByYearDay()` | `int[]` | BYYEARDAY, 1 to 366 or -366 to -1. |

### Building a rule

Each method returns a copy of the rule with the change; the rule it is called on is
not changed. For the BYxxx methods, the values replace the rule part's values, and
passing `null` or no values removes the rule part. Repeated values are kept once.

| Method | Returns | Notes |
| --- | --- | --- |
| `WithByDay(params IcalRecurrenceDay[] values)` | `IcalRecurrenceRule` | Sets BYDAY. |
| `WithByHour(params int[] values)` | `IcalRecurrenceRule` | Sets BYHOUR. Throws `ArgumentOutOfRangeException` if a value is not 0 to 23. |
| `WithByMinute(params int[] values)` | `IcalRecurrenceRule` | Sets BYMINUTE. Throws `ArgumentOutOfRangeException` if a value is not 0 to 59. |
| `WithByMonth(params int[] values)` | `IcalRecurrenceRule` | Sets BYMONTH. Throws `ArgumentOutOfRangeException` if a value is not 1 to 12. |
| `WithByMonthDay(params int[] values)` | `IcalRecurrenceRule` | Sets BYMONTHDAY. Throws `ArgumentOutOfRangeException` if a value is not 1 to 31 or -31 to -1. |
| `WithBySecond(params int[] values)` | `IcalRecurrenceRule` | Sets BYSECOND. Throws `ArgumentOutOfRangeException` if a value is not 0 to 60. |
| `WithBySetPos(params int[] values)` | `IcalRecurrenceRule` | Sets BYSETPOS. Throws `ArgumentOutOfRangeException` if a value is not 1 to 366 or -366 to -1. |
| `WithByWeekNo(params int[] values)` | `IcalRecurrenceRule` | Sets BYWEEKNO. Throws `ArgumentOutOfRangeException` if a value is not 1 to 53 or -53 to -1. |
| `WithByYearDay(params int[] values)` | `IcalRecurrenceRule` | Sets BYYEARDAY. Throws `ArgumentOutOfRangeException` if a value is not 1 to 366 or -366 to -1. |
| `WithCount(int count)` | `IcalRecurrenceRule` | Sets COUNT and removes UNTIL. Throws `ArgumentOutOfRangeException` if `count` is less than 1. |
| `WithFrequency(IcalRecurrenceFrequency frequency)` | `IcalRecurrenceRule` | Sets FREQ. Throws `ArgumentOutOfRangeException` if `frequency` is not an `IcalRecurrenceFrequency` member. |
| `WithInterval(int interval)` | `IcalRecurrenceRule` | Sets INTERVAL. An interval of 1 is written, although it is the default. Throws `ArgumentOutOfRangeException` if `interval` is less than 1. |
| `WithoutEnd()` | `IcalRecurrenceRule` | Removes UNTIL and COUNT, so the rule repeats forever. |
| `WithUntil(IcalDate until)` | `IcalRecurrenceRule` | Sets UNTIL to a DATE and removes COUNT. RFC 5545 requires a DATE UNTIL when DTSTART is a DATE. |
| `WithUntil(IcalDateTime until)` | `IcalRecurrenceRule` | Sets UNTIL to a DATE-TIME and removes COUNT. It should be floating if DTSTART is floating, and otherwise in UTC. Throws `ArgumentException` if `until` is zoned. |
| `WithWeekStart(DayOfWeek? weekStart)` | `IcalRecurrenceRule` | Sets WKST, or removes it when `weekStart` is `null`. Throws `ArgumentOutOfRangeException` if `weekStart` is not a `DayOfWeek` member. |

### Expanding occurrences

All overloads return the occurrences in increasing order, generated as they are
enumerated. See *Expanding occurrences* under Remarks for how the rule is applied.

| Method | Returns | Notes |
| --- | --- | --- |
| `GetOccurrences(IcalDateTime start, IcalDateTime? from, IcalDateTime? to)` | `IEnumerable<IcalDateTime>` | Occurrences from a date-time `start` (DTSTART), with its kind and time zone. `from` is the earliest occurrence to give, inclusive, or `null` to start at `start`; `to` is the end, exclusive, or `null` for none. Uses the default `MaxRecurrenceIterations`. Throws `InvalidOperationException` if the rule is not valid; `ArgumentException` if the rule has neither COUNT nor UNTIL and `to` is `null`, or its UNTIL is in UTC and `start` is not. |
| `GetOccurrences(IcalDateTime start, IcalDateTime? from, IcalDateTime? to, IcalOptions options)` | `IEnumerable<IcalDateTime>` | As above, with `options.MaxRecurrenceIterations`. Also throws `ArgumentNullException` if `options` is `null`, or `ArgumentException` if `options.MaxRecurrenceIterations` is less than 1. |
| `GetOccurrences(IcalDateTime start, IcalDateTime? from, IcalDateTime? to, Func<IcalDateTime, IcalDateTime> toUtc, IcalOptions options)` | `IEnumerable<IcalDateTime>` | As above, and handles a UNTIL in UTC when `start` is not in UTC. `toUtc` converts an occurrence to UTC, and the wall-clock fields of what it returns are taken as UTC. It is called only in that case. For a VTIMEZONE observance, whose DTSTART is floating, it subtracts the observance's TZOFFSETFROM; for a zoned start it resolves the time zone. Throws `ArgumentNullException` if `toUtc` or `options` is `null`; `InvalidOperationException` if the rule is not valid; or `ArgumentException` if the rule has neither COUNT nor UNTIL and `to` is `null`, or `options.MaxRecurrenceIterations` is less than 1. |
| `GetOccurrences(IcalDate start, IcalDate? from, IcalDate? to)` | `IEnumerable<IcalDate>` | Occurrences from an all-day DTSTART. Uses the default `MaxRecurrenceIterations`. Throws `InvalidOperationException` if the rule is not valid; `ArgumentException` if the rule has neither COUNT nor UNTIL and `to` is `null`, or it has a frequency below DAILY, or BYHOUR, BYMINUTE or BYSECOND, none of which a date can have. |
| `GetOccurrences(IcalDate start, IcalDate? from, IcalDate? to, IcalOptions options)` | `IEnumerable<IcalDate>` | As above, with `options.MaxRecurrenceIterations`. Also throws `ArgumentNullException` if `options` is `null`, or `ArgumentException` if `options.MaxRecurrenceIterations` is less than 1. |

Enumerating any of these throws `InvalidOperationException` when more consecutive
periods than `MaxRecurrenceIterations` give no occurrence.

### Equality

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(IcalRecurrenceRule other)` | `bool` | `true` for the same rule parts with the same sets of values. Order and repeats of BYxxx values don't matter. An INTERVAL of 1 that was given doesn't equal one left out, and a DATE UNTIL doesn't equal a DATE-TIME UNTIL. |
| `Equals(object? obj)` | `bool` | `true` when `obj` is an equal `IcalRecurrenceRule`. |
| `GetHashCode()` | `int` | Consistent with `Equals`. |

### Explicit interface implementations *(.NET)*

| Member | Returns | Notes |
| --- | --- | --- |
| `ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)` | `bool` | Calls `TryFormat` with the form named by `format`: empty or `T` for `Text`, in either case. `J` and `X` name the `Json` and `Xml` forms, which throw `NotSupportedException`. `provider` is ignored. Throws `FormatException` for any other format string, or `InvalidOperationException` if the rule is not valid. |
| `IFormattable.ToString(string? format, IFormatProvider? formatProvider)` | `string` | Calls `ToString(IcalValueFormat)` with the form named by `format`, as above. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `==(IcalRecurrenceRule left, IcalRecurrenceRule right)` | `bool` | Same as `left.Equals(right)`. |
| `!=(IcalRecurrenceRule left, IcalRecurrenceRule right)` | `bool` | Same as `!left.Equals(right)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
`ISpanFormattable` and its explicit members are implemented on the `net8.0` and
`net10.0` builds only. The nullable reference annotations apply to the
`netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Expand recurrences](../how-to/expand-recurrences.md)
- [Time zone resolution](../concepts/time-zone-resolution.md)
- [Daylight saving rules](../concepts/dst-rules.md)
- [IcalRecurrenceFrequency](IcalRecurrenceFrequency.md)
- [IcalRecurrenceDay](IcalRecurrenceDay.md)
- [IcalOptions](IcalOptions.md)
- [IcalEvent](IcalEvent.md)
- [IcalOccurrence](IcalOccurrence.md)
- [File.iCal API reference](index.md)
