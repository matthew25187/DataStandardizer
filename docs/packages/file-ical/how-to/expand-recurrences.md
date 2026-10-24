---
title: Expand recurrences
parent: File.iCal
grand_parent: Packages
nav_order: 4
---

# Expand recurrences

A recurring event holds one `DTSTART` and rules for repeating it. To get the actual
instances, call `GetOccurrences` on the event, to-do or journal entry. It applies
`RRULE`, `RDATE`, `EXDATE` and any overrides with a `RECURRENCE-ID`, and resolves
each instance to UTC.

## Expand an event in a window

This calendar has a weekly stand-up in New York time. One Monday is removed with
`EXDATE`, a Wednesday is added with `RDATE`, and a second VEVENT with the same `UID`
moves one instance to 10:00:

```text
BEGIN:VEVENT
UID:standup@example.com
DTSTAMP:20261001T000000Z
DTSTART;TZID=America/New_York:20261026T090000
DURATION:PT15M
RRULE:FREQ=WEEKLY;BYDAY=MO
EXDATE;TZID=America/New_York:20261109T090000
RDATE;TZID=America/New_York:20261111T090000
SUMMARY:Stand-up
END:VEVENT
BEGIN:VEVENT
UID:standup@example.com
DTSTAMP:20261001T000000Z
RECURRENCE-ID;TZID=America/New_York:20261116T090000
DTSTART;TZID=America/New_York:20261116T100000
DURATION:PT30M
SUMMARY:Stand-up (moved)
END:VEVENT
```

The calendar also holds the VTIMEZONE for `America/New_York`.

```csharp
using DataStandardizer.File.iCal;

using var reader = new IcalReader("standup.ics");
var calendar = reader.ReadCalendar()!;

var from = new IcalDateTime(new IcalDate(2026, 10, 1), new IcalTime(0, 0, 0, isUtc: true));
var to = new IcalDateTime(new IcalDate(2026, 12, 1), new IcalTime(0, 0, 0, isUtc: true));

foreach (var calendarEvent in calendar.Events)
{
    // An override is given by its recurring event, so skip it here.
    if (calendarEvent.RecurrenceId is not null) continue;

    foreach (var occurrence in calendarEvent.GetOccurrences(calendar, from, to))
    {
        Console.WriteLine($"{occurrence.Start} ({occurrence.StartUtc}) to {occurrence.EndUtc}: " +
            occurrence.Component.Summary);
    }
}
```

```text
20261026T090000 (20261026T130000Z) to 20261026T131500Z: Stand-up
20261102T090000 (20261102T140000Z) to 20261102T141500Z: Stand-up
20261111T090000 (20261111T140000Z) to 20261111T141500Z: Stand-up
20261116T100000 (20261116T150000Z) to 20261116T153000Z: Stand-up (moved)
20261123T090000 (20261123T140000Z) to 20261123T141500Z: Stand-up
20261130T090000 (20261130T140000Z) to 20261130T141500Z: Stand-up
```

The local time stays at 09:00 while the UTC time moves by an hour when daylight
saving time ends on 1 November. 9 November is missing (`EXDATE`), 11 November is
added (`RDATE`), and 16 November comes from the override.

## The arguments

`GetOccurrences` has three overloads on `IcalEvent`, `IcalTodo` and `IcalJournal`:

```csharp
GetOccurrences(calendar, from, to)
GetOccurrences(calendar, from, to, resolver)
GetOccurrences(calendar, from, to, resolver, options)
```

- **`calendar`:** the calendar the component is in. Its VTIMEZONE components are
  where time zones come from, and its components with the same `UID` and a
  `RECURRENCE-ID` are the overrides. It may be `null`.
- **`from`:** the earliest start to give, inclusive, or `null` for no limit.
- **`to`:** the end of the window, exclusive, or `null` for no limit.
- **`resolver`:** resolves the `TZID` of zoned values. See
  [Choose a time-zone resolver](#choose-a-time-zone-resolver).
- **`options`:** only `IcalOptions.MaxRecurrenceIterations` applies.

An instance is given when its start, after any override, falls in the window. The
instances come in order of their start.

The properties are read, and the time zones of `DTSTART` and the window resolved,
when you call `GetOccurrences`. So a bad property or an unknown time zone throws
from the call itself. The instances are then generated as you enumerate them. A
component with no `DTSTART` has no instances.

## Each occurrence

`GetOccurrences` returns `IcalOccurrence<TComponent>` objects:

| Member | Holds |
| --- | --- |
| `Start` | The start as a local value, in the form the calendar gives it: a DATE, a floating DATE-TIME, or one in UTC or a named time zone. |
| `StartUtc` | The start in UTC, or `null` for a DATE or a floating time. |
| `End` | The end as a local value, or `null` if there is none. The end is exclusive. |
| `EndUtc` | The end in UTC, or `null` if there is no end or it is a DATE or a floating time. |
| `RecurrenceId` | The start the recurrence set gives the instance, before any override moves it. A `RECURRENCE-ID` names an instance by this value. |
| `IsOverride` | Whether `Component` is an override. |
| `Component` | The recurring component, or the override that replaces this instance. |

## How the instances are found

These rules follow RFC 5545 §3.8.5:

- **`DTSTART`:** always the first instance, even when no rule gives it. It counts
  towards the `COUNT` of a rule.
- **`RRULE`:** each rule adds its occurrences.
- **`RDATE`:** adds each date, date-time or period. A period gives its instance its
  own end.
- **`EXDATE`:** removes an instance, `DTSTART` included. A DATE removes every
  instance on that local date.
- **Duplicates:** an instance given more than once appears once.

### Overrides

A component in `calendar` with the same `UID` and a `RECURRENCE-ID` replaces the
instance it names. It gives the instance its own start and end, and its own
properties through `Component`.

- **Matching:** an override is matched to an instance by its instant.
- **Two for one instance:** the one with the higher `SEQUENCE` wins, then the later
  one in the calendar.
- **`RANGE=THISANDFUTURE`:** the override also replaces every later instance. Each
  is moved by the change it makes to the start, in the local time of `DTSTART`, and
  takes its duration.
- **`EXDATE` wins:** an override is ignored if an `EXDATE` removes its instance.

An override expanded on its own gives only its own instance. So when you expand
every event of a calendar, skip those with a `RecurrenceId` whose recurring event is
in the calendar too, as the sample above does.

### Ends

- **`DTEND` or `DUE`:** each instance gets the exact duration that `DTEND` (or a
  to-do's `DUE`) gives `DTSTART`. So the local time of the end can change across a
  daylight saving change.
- **`DURATION`:** added to each start. Weeks and days are nominal and keep the local
  time of day. Hours, minutes and seconds are exact. So `P1D` and `PT24H` differ
  across a change.
- **Neither:** an event ends when it starts, or a day later if `DTSTART` is a DATE.
  A to-do has no end, and a journal entry never has one.

## Time zones, floating times and dates

The instances are generated in local time, then each is resolved to UTC as RFC 5545
§3.3.5 requires:

- **A time in a daylight saving gap,** such as 02:30 on the day clocks go from
  02:00 to 03:00, is read with the offset in use before the gap. `Start` still shows
  02:30.
- **A time in an overlap** takes its first occurrence.

For more on this, see [DST gaps and overlaps](../concepts/dst-rules.md).

Floating times and DATE values have no instant. They stay floating, or stay dates,
and `StartUtc` and `EndUtc` are `null`. They are compared with the window by their
wall-clock time, as though they were in UTC. A floating `from` or `to` is compared in
the same way.

```text
DTSTART;VALUE=DATE:20261225
RRULE:FREQ=YEARLY;COUNT=2
```

gives `20261225` to `20261226`, then `20271225` to `20271226`, with no UTC values.

## Choose a time-zone resolver

The first two overloads use `IcalTimeZoneResolver.Default`, which is
`IcalTimeZoneResolver.Calendar` alone. It reads the calendar's own VTIMEZONE for each
`TZID`, as RFC 5545 requires. A `TZID` with no VTIMEZONE then throws
`IcalFileException`:

```text
The time zone 'Europe/London' could not be resolved.  RFC 5545 requires a VTIMEZONE for it in the calendar.
```

A calendar like that can only be read with `AllowUndefinedTimeZones`. To expand it,
pass a resolver that fills in the missing zones from the TZ Database:

```csharp
var options = new IcalOptions { AllowUndefinedTimeZones = true };
var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData);

using var reader = new IcalReader("feed.ics", options);
var calendar = reader.ReadCalendar()!;
foreach (var occurrence in calendar.Events.Single().GetOccurrences(calendar, null, null, resolver))
{
    Console.WriteLine($"{occurrence.Start} = {occurrence.StartUtc}");
}
```

`Chain` tries each resolver in turn, so the calendar's own VTIMEZONE still wins
where there is one. `IcalTimeZoneResolver.System`, which uses the operating system's
time zones, is available on .NET Standard 2.0 and later; put it last in a chain. See
[Time-zone resolution](../concepts/time-zone-resolution.md).

## Open-ended windows

`to` may be `null` only if every `RRULE` has a `COUNT` or an `UNTIL`. A rule that
repeats forever with no `to` throws `ArgumentException` from the call itself, even
if you only take a few instances:

```text
A rule with neither COUNT nor UNTIL repeats forever, so the end of a window must be given. (Parameter 'to')
```

The instances end at 9999-12-31. One that falls after that date throws
`ArgumentOutOfRangeException` while you enumerate.

### Rules that never match

Some rules can never give an occurrence, such as
`FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30`. So that one doesn't run for a very long time,
the expansion gives up after `IcalOptions.MaxRecurrenceIterations` consecutive
periods with no occurrence, and throws `InvalidOperationException` while you
enumerate:

```text
The recurrence rule gave no occurrence in 1000 consecutive periods, so it is taken never to match again.
```

A period is one step of the rule's frequency, such as a year for `FREQ=YEARLY`. The
default is 100,000, far more than any rule that can match needs. The value must be
at least 1.

A rule that breaks a constraint listed under `IcalRecurrenceRule.IsValid` throws
`InvalidOperationException` from the call.

## Expand a bare rule

To apply an `RRULE` without a component, call `IcalRecurrenceRule.GetOccurrences`
with a start:

```csharp
var rule = IcalRecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=-1FR;COUNT=4", IcalValueFormat.Text);
var start = new IcalDateTime(new IcalDate(2026, 1, 30), new IcalTime(17, 0, 0));
foreach (var occurrence in rule.GetOccurrences(start, null, null))
{
    Console.WriteLine(occurrence);   // 20260130T170000, 20260227T170000, ...
}
```

This differs from a component's `GetOccurrences`:

- **Local time only:** each occurrence is a wall-clock value with the kind and time
  zone of `start`. Resolving it to UTC, including gaps and overlaps, is left to you.
  `from` and `to` are compared by their wall-clock fields.
- **Only what the rule gives:** if `start` doesn't match the rule, it isn't given.
  `COUNT` counts from `start`, whatever `from` is.
- **No `RDATE`, `EXDATE` or overrides.**
- **An `UNTIL` in UTC** with a `start` that isn't in UTC needs the overload that
  takes a `Func<IcalDateTime, IcalDateTime>` to convert each occurrence to UTC.
  Without it, the call throws `ArgumentException`.

For an all-day rule, pass an `IcalDate`. The overload then returns `IcalDate`
values, and throws `ArgumentException` for a frequency below DAILY, or for BYHOUR,
BYMINUTE or BYSECOND.

```csharp
var rule = IcalRecurrenceRule.Parse("FREQ=YEARLY;COUNT=3", IcalValueFormat.Text);
foreach (var date in rule.GetOccurrences(new IcalDate(2026, 12, 25), null, null))
{
    Console.WriteLine(date);   // 20261225, 20271225, 20281225
}
```

## Related

- [Time-zone resolution](../concepts/time-zone-resolution.md)
- [DST gaps and overlaps](../concepts/dst-rules.md)
- [Read non-compliant feeds](read-non-compliant-feeds.md)
- [IcalOccurrence](../reference/IcalOccurrence.md),
  [IcalRecurrenceRule](../reference/IcalRecurrenceRule.md),
  [IcalTimeZoneResolver](../reference/IcalTimeZoneResolver.md) and
  [IcalOptions](../reference/IcalOptions.md)
