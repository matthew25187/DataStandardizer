---
title: IcalEvent Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalEvent Class

## Definition

Namespace: `DataStandardizer.File.iCal`

An event (RFC 5545 §3.6.1): the VEVENT component. It gives typed access to the
properties of an event, and expands its recurrence set into occurrences.

**Syntax**

```csharp
public sealed class IcalEvent : IcalComponent
```

## Remarks

The typed properties follow the rules described under *Typed accessors* on
[IcalComponent](IcalComponent.md): a getter reads the first property of its name, a
setter replaces every property of its name in the place of the first, setting
`null` removes them, and a getter throws [IcalFileException](IcalFileException.md)
if the stored value is not valid.

**Recurrence expansion.** `GetOccurrences` gives the instances of the event's
recurrence set (RFC 5545 §3.8.5) that start in a window, as
[IcalOccurrence\<IcalEvent\>](IcalOccurrence.md) values:

- The recurrence set is `DTSTART`, which is always the first instance even when no
  rule gives it, the occurrences of each `RRULE`, and the dates, date-times and
  periods of each `RDATE`. `DTSTART` counts towards the `COUNT` of a rule that does
  not give it, as RFC 5545 §3.3.10 requires, so such a rule gives one occurrence
  fewer. Each `EXDATE` removes an instance, `DTSTART` among them. An `EXDATE` that
  is a DATE removes every instance on that local date. An instance given more than
  once appears once.
- Each instance ends as RFC 5545 §3.8.5.3 sets out. `DTEND` gives every instance
  the exact duration it gives `DTSTART`, so the local time of the end can change
  across a daylight saving change. `DURATION` is added to each start: its weeks and
  days are nominal and keep the local time of day, and its hours, minutes and
  seconds are exact, so `P1D` and `PT24H` differ across a change. An `RDATE` period
  gives its instance its own end. With neither `DTEND` nor `DURATION`, an instance
  ends when it starts, or a day later for a DATE (RFC 5545 §3.6.1).
- Each event of `calendar` with the same `UID` and a `RECURRENCE-ID` replaces the
  instance it names, with the start and end of its own `DTSTART`, `DTEND` and
  `DURATION`. Of two for the same instance, the one with the higher `SEQUENCE`
  wins, then the later one. With `RANGE=THISANDFUTURE`, it also replaces every
  later instance: each is moved by the change it makes to the start, in the local
  time of `DTSTART`, and takes its duration. An override is matched to an instance
  by its instant, and is ignored if an `EXDATE` removes that instance.
- An event that has a `RECURRENCE-ID` itself gives only its own instance. When
  expanding every event of a calendar, skip those whose recurring event is in it
  too.
- The instances are generated in local time and each is resolved to UTC through
  the resolver, as RFC 5545 §3.3.5 requires. A time in a daylight saving gap is read
  with the offset in use before the gap, and a time in an overlap takes its first
  occurrence. So a daily event at 09:00 stays at 09:00 local time while its time in
  UTC moves by an hour.
- Floating times and DATE values have no instant. Each is compared with the window
  by its wall-clock time, as though it were in UTC, as is a floating `from` or
  `to`.
- An instance is given when its start, after any override, falls in the window:
  `from` is inclusive and `to` exclusive. Either may be `null` for no limit, but a
  `null` `to` needs every `RRULE` to have `COUNT` or `UNTIL`.
- The properties are read, and the time zones of `DTSTART` and the window
  resolved, when `GetOccurrences` is called, so most exceptions are thrown then.
  The instances are generated lazily as they are enumerated. An event with no
  `DTSTART` has no instances.

The overloads without a `resolver` use `IcalTimeZoneResolver.Default`, which is
`IcalTimeZoneResolver.Calendar`: it resolves a `TZID` only from the VTIMEZONE
components of `calendar`, and with no calendar nothing resolves. A zoned
`DTSTART` that does not resolve throws `IcalFileException`. For a calendar that
uses a `TZID` with no VTIMEZONE, fall back to the TZ Database with a chain such
as `IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData)`.
The overloads without `options` use a default [IcalOptions](IcalOptions.md), whose
`MaxRecurrenceIterations` is 100,000.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalEvent()` | Creates an empty VEVENT component, with no properties. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Alarms` | `IEnumerable<IcalAlarm> Alarms { get; }` | The VALARM components (RFC 5545 §3.6.6) nested in the event, filtered from `Components`. |
| `Attendees` | `IReadOnlyList<IcalContentLine> Attendees { get; }` | `ATTENDEE` (RFC 5545 §3.8.4.1), as the content lines themselves. Each is a CAL-ADDRESS with parameters such as `CN` and `PARTSTAT`. Change them through `Properties`. |
| `Categories` | `IReadOnlyList<string> Categories { get; set; }` | `CATEGORIES` (RFC 5545 §3.8.1.2), the items of every `CATEGORIES` property. Setting writes one property holding the whole list; an empty list or `null` removes them. Throws `ArgumentException` on set if an item is `null`. |
| `Classification` | `string? Classification { get; set; }` | `CLASS` (RFC 5545 §3.8.1.3), such as `PUBLIC`. TEXT. `null` removes it. |
| `Color` | `string? Color { get; set; }` | `COLOR` (RFC 7986 §5.9), a CSS3 color name such as `turquoise`. TEXT. `null` removes it. |
| `Conferences` | `IReadOnlyList<IcalContentLine> Conferences { get; }` | `CONFERENCE` (RFC 7986 §5.11), as the content lines themselves. Each is a URI with parameters such as `FEATURE` and `LABEL`. Change them through `Properties`. |
| `Created` | `IcalDateTime? Created { get; set; }` | `CREATED` (RFC 5545 §3.8.7.1). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. `null` removes it. |
| `Description` | `string? Description { get; set; }` | `DESCRIPTION` (RFC 5545 §3.8.1.5). TEXT. `null` removes it. |
| `DtEnd` | `IcalDateOrDateTime? DtEnd { get; set; }` | `DTEND` (RFC 5545 §3.8.2.2), a DATE or a DATE-TIME. The `VALUE` and `TZID` parameters are read and written with the value. `null` removes it. |
| `DtStamp` | `IcalDateTime? DtStamp { get; set; }` | `DTSTAMP` (RFC 5545 §3.8.7.2). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. `null` removes it. |
| `DtStart` | `IcalDateOrDateTime? DtStart { get; set; }` | `DTSTART` (RFC 5545 §3.8.2.4), a DATE or a DATE-TIME. The `VALUE` and `TZID` parameters are read and written with the value. `null` removes it. |
| `Duration` | `IcalDuration? Duration { get; set; }` | `DURATION` (RFC 5545 §3.8.2.5). `null` removes it. |
| `Images` | `IReadOnlyList<IcalContentLine> Images { get; }` | `IMAGE` (RFC 7986 §5.10), as the content lines themselves. Each is a URI or inline BINARY data, with parameters such as `DISPLAY` and `FMTTYPE`. Change them through `Properties`. |
| `LastModified` | `IcalDateTime? LastModified { get; set; }` | `LAST-MODIFIED` (RFC 5545 §3.8.7.3). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. `null` removes it. |
| `Location` | `string? Location { get; set; }` | `LOCATION` (RFC 5545 §3.8.1.7). TEXT. `null` removes it. |
| `Organizer` | `Uri? Organizer { get; set; }` | `ORGANIZER` (RFC 5545 §3.8.4.3), a CAL-ADDRESS such as `mailto:a@example.com`. Throws `ArgumentException` on set for a relative URI. `null` removes it. |
| `Priority` | `int? Priority { get; set; }` | `PRIORITY` (RFC 5545 §3.8.1.9), from 0 (undefined) and 1 (highest) to 9 (lowest). The setter does not check the range; `Validate` does. `null` removes it. |
| `RecurrenceId` | `IcalDateOrDateTime? RecurrenceId { get; set; }` | `RECURRENCE-ID` (RFC 5545 §3.8.4.4), the instance this event overrides, a DATE or a DATE-TIME. The `VALUE` and `TZID` parameters are read and written with the value. `null` removes it. |
| `RecurrenceRules` | `IReadOnlyList<IcalRecurrenceRule> RecurrenceRules { get; set; }` | `RRULE` (RFC 5545 §3.8.5.3), one rule per property. Setting writes one property per rule, in the place of the first; an empty list or `null` removes them. |
| `Resources` | `IReadOnlyList<string> Resources { get; set; }` | `RESOURCES` (RFC 5545 §3.8.1.10), the items of every `RESOURCES` property. Setting writes one property holding the whole list; an empty list or `null` removes them. Throws `ArgumentException` on set if an item is `null`. |
| `Sequence` | `int? Sequence { get; set; }` | `SEQUENCE` (RFC 5545 §3.8.7.4), the revision sequence number. `null` removes it. |
| `Status` | `string? Status { get; set; }` | `STATUS` (RFC 5545 §3.8.1.11), such as `CONFIRMED`. TEXT. `null` removes it. |
| `Summary` | `string? Summary { get; set; }` | `SUMMARY` (RFC 5545 §3.8.1.12). TEXT. `null` removes it. |
| `Transparency` | `string? Transparency { get; set; }` | `TRANSP` (RFC 5545 §3.8.2.7), `OPAQUE` or `TRANSPARENT`. TEXT. `null` removes it. |
| `Uid` | `string? Uid { get; set; }` | `UID` (RFC 5545 §3.8.4.7), a persistent, globally unique identifier. TEXT. `null` removes it. |
| `Url` | `Uri? Url { get; set; }` | `URL` (RFC 5545 §3.8.4.6). Throws `ArgumentException` on set for a relative URI. `null` removes it. |

Every getter throws `IcalFileException` if the stored value is not valid for its
type: a TEXT value or list item with a backslash that does not start a valid
escape, a DATE-TIME property with `VALUE=DATE`, a value that does not parse, or a
URI that is not absolute. TEXT properties remove TEXT escaping on get and add it on
set.

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to)` | `IEnumerable<IcalOccurrence<IcalEvent>>` | Expands the recurrence set with `IcalTimeZoneResolver.Default` and default options. See *Recurrence expansion* and the exceptions below. |
| `GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to, IIcalTimeZoneResolver resolver)` | `IEnumerable<IcalOccurrence<IcalEvent>>` | Expands with `resolver` and default options. Throws `ArgumentNullException` if `resolver` is `null`. |
| `GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to, IIcalTimeZoneResolver resolver, IcalOptions options)` | `IEnumerable<IcalOccurrence<IcalEvent>>` | Expands with `resolver`; of `options`, only `MaxRecurrenceIterations` applies. Throws `ArgumentNullException` if `resolver` or `options` is `null`, or `ArgumentException` if `options.MaxRecurrenceIterations` is less than 1. |

`calendar` is the calendar the event is in. It holds the VTIMEZONE components that
time zones are resolved from, and the events that override instances. Pass `null`
if there is none.

All three overloads throw:

- `ArgumentException` if `to` is `null` and an `RRULE` has neither `COUNT` nor
  `UNTIL`, or if an `RRULE` has a frequency below `DAILY`, or `BYHOUR`, `BYMINUTE`
  or `BYSECOND`, and `DTSTART` is a DATE.
- `InvalidOperationException` if an `RRULE` breaks a constraint listed under
  `IcalRecurrenceRule.IsValid`. Also thrown while enumerating, when more consecutive
  periods than `MaxRecurrenceIterations` give no occurrence.
- `IcalFileException` if a property the instances are found from is not valid, or
  a time zone cannot be resolved.
- `ArgumentOutOfRangeException` while enumerating, if an instance or its end falls
  after 9999-12-31.

Inherited members `Components`, `LineNumber`, `Name`, `Properties`, `GetProperty`,
`GetProperties`, `SetProperty`, `RemoveProperties`, `Validate` and `ToString` are
documented on [IcalComponent](IcalComponent.md).

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations, such as `IcalCalendar? calendar`, apply to the
`netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Expand recurrences](../how-to/expand-recurrences.md)
- [Time-zone resolution](../concepts/time-zone-resolution.md)
- [Daylight saving rules](../concepts/dst-rules.md)
- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [IcalComponent](IcalComponent.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalOccurrence](IcalOccurrence.md)
- [IcalAlarm](IcalAlarm.md)
- [IcalTodo](IcalTodo.md)
- [IcalRecurrenceRule](IcalRecurrenceRule.md)
- [IcalTimeZoneResolver](IcalTimeZoneResolver.md)
- [IcalOptions](IcalOptions.md)
- [File.iCal API reference](index.md)
