---
title: IcalOccurrence Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalOccurrence Class

## Definition

Namespace: `DataStandardizer.File.iCal`

One instance of the recurrence set (RFC 5545 §3.8.5) of an event, to-do or journal
entry: when it starts and ends, and the component that describes it. The
`GetOccurrences` methods of [IcalEvent](IcalEvent.md), [IcalTodo](IcalTodo.md) and
[IcalJournal](IcalJournal.md) return these.

**Syntax**

```csharp
public sealed class IcalOccurrence<TComponent>
    where TComponent : IcalComponent
```

`TComponent` is the type of the component: `IcalEvent`, `IcalTodo` or
`IcalJournal`.

## Remarks

`Start` and `End` are local values, in the form the calendar gives them: a DATE for
an all-day instance, a floating DATE-TIME, or one in UTC or in a named time zone.
`StartUtc` and `EndUtc` give the same instants in UTC. They are `null` for a DATE
or a floating time, which has no single instant.

A local time is kept as the recurrence set gives it, even where it falls in a
daylight saving gap. Its instant in UTC follows RFC 5545 §3.3.5, so 02:30 on the day
clocks go forward from 02:00 to 03:00 is read with the offset in use before the
gap.

`RecurrenceId` is the start the recurrence set gives the instance before any
override moves it. It is the value a `RECURRENCE-ID` names the instance by, so use
it to match the instance to an override or to write one. When an override moves
the instance, `Start` can differ from `RecurrenceId`.

The type has no public constructor, and its properties are read-only.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Component` | `TComponent Component { get; }` | The component that describes the instance: the recurring component itself, or the component with a `RECURRENCE-ID` (RFC 5545 §3.8.4.4) that overrides it. |
| `End` | `IcalDateOrDateTime? End { get; }` | The end, as a local value, exclusive, so an all-day event on one date ends on the next. `null` if the instance has none, as for a journal entry or a to-do with neither `DUE` nor `DURATION`. |
| `EndUtc` | `IcalDateTime? EndUtc { get; }` | The end in UTC. `null` if there is no `End`, or it is a DATE or a floating time. |
| `IsOverride` | `bool IsOverride { get; }` | `true` if `Component` overrides the instance, either by naming it in its `RECURRENCE-ID`, or, with `RANGE=THISANDFUTURE`, by naming an earlier instance. |
| `RecurrenceId` | `IcalDateOrDateTime RecurrenceId { get; }` | The start the recurrence set gives the instance, before any override moves it. |
| `Start` | `IcalDateOrDateTime Start { get; }` | The start, as a local value. |
| `StartUtc` | `IcalDateTime? StartUtc { get; }` | The start in UTC. `null` if `Start` is a DATE or a floating time. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | The start and end separated by a solidus, such as `20260301T090000/20260301T100000`, or the start alone if there is no end. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [Expand recurrences](../how-to/expand-recurrences.md)
- [Time-zone resolution](../concepts/time-zone-resolution.md)
- [Daylight saving rules](../concepts/dst-rules.md)
- [IcalEvent](IcalEvent.md)
- [IcalTodo](IcalTodo.md)
- [IcalJournal](IcalJournal.md)
- [IcalDateOrDateTime](IcalDateOrDateTime.md)
- [IcalDateTime](IcalDateTime.md)
- [File.iCal API reference](index.md)
