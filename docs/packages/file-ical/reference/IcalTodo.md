---
title: IcalTodo Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalTodo Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A to-do (RFC 5545 §3.6.2): the VTODO component. It gives typed access to the
properties of a to-do, and expands its recurrence set into occurrences.

**Syntax**

```csharp
public sealed class IcalTodo : IcalComponent
```

## Remarks

The typed properties follow the rules described under *Typed accessors* on
[IcalComponent](IcalComponent.md): a getter reads the first property of its name, a
setter replaces every property of its name in the place of the first, setting
`null` removes them, and a getter throws [IcalFileException](IcalFileException.md)
if the stored value is not valid.

**Recurrence expansion.** `GetOccurrences` finds the instances as
[IcalEvent](IcalEvent.md) does, with the to-dos of `calendar` as the overrides.
The differences are in how each instance ends (RFC 5545 §3.8.5.3):

- `DUE` gives every instance the exact duration it gives `DTSTART`.
- `DURATION` is added to each start, its weeks and days nominal and the rest exact.
- With neither, an instance has no end, so `IcalOccurrence.End` is `null`.

The properties are read, and the time zones of `DTSTART` and the window resolved,
when `GetOccurrences` is called. The instances are generated lazily as they are
enumerated. A to-do with no `DTSTART` has no instances, even if it has a `DUE`.

The overloads without a `resolver` use `IcalTimeZoneResolver.Default`, which is
`IcalTimeZoneResolver.Calendar`: it resolves a `TZID` only from the VTIMEZONE
components of `calendar`. The overloads without `options` use a default
[IcalOptions](IcalOptions.md).

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalTodo()` | Creates an empty VTODO component, with no properties. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Alarms` | `IEnumerable<IcalAlarm> Alarms { get; }` | The VALARM components (RFC 5545 §3.6.6) nested in the to-do, filtered from `Components`. |
| `Attendees` | `IReadOnlyList<IcalContentLine> Attendees { get; }` | `ATTENDEE` (RFC 5545 §3.8.4.1), as the content lines themselves. Each is a CAL-ADDRESS with parameters such as `CN` and `PARTSTAT`. Change them through `Properties`. |
| `Categories` | `IReadOnlyList<string> Categories { get; set; }` | `CATEGORIES` (RFC 5545 §3.8.1.2), the items of every `CATEGORIES` property. Setting writes one property holding the whole list; an empty list or `null` removes them. Throws `ArgumentException` on set if an item is `null`. |
| `Classification` | `string? Classification { get; set; }` | `CLASS` (RFC 5545 §3.8.1.3), such as `PUBLIC`. TEXT. `null` removes it. |
| `Color` | `string? Color { get; set; }` | `COLOR` (RFC 7986 §5.9), a CSS3 color name such as `turquoise`. TEXT. `null` removes it. |
| `Completed` | `IcalDateTime? Completed { get; set; }` | `COMPLETED` (RFC 5545 §3.8.2.1). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. `null` removes it. |
| `Conferences` | `IReadOnlyList<IcalContentLine> Conferences { get; }` | `CONFERENCE` (RFC 7986 §5.11), as the content lines themselves. Each is a URI with parameters such as `FEATURE` and `LABEL`. Change them through `Properties`. |
| `Created` | `IcalDateTime? Created { get; set; }` | `CREATED` (RFC 5545 §3.8.7.1). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. `null` removes it. |
| `Description` | `string? Description { get; set; }` | `DESCRIPTION` (RFC 5545 §3.8.1.5). TEXT. `null` removes it. |
| `DtStamp` | `IcalDateTime? DtStamp { get; set; }` | `DTSTAMP` (RFC 5545 §3.8.7.2). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. `null` removes it. |
| `DtStart` | `IcalDateOrDateTime? DtStart { get; set; }` | `DTSTART` (RFC 5545 §3.8.2.4), a DATE or a DATE-TIME. The `VALUE` and `TZID` parameters are read and written with the value. `null` removes it. |
| `Due` | `IcalDateOrDateTime? Due { get; set; }` | `DUE` (RFC 5545 §3.8.2.3), a DATE or a DATE-TIME. The `VALUE` and `TZID` parameters are read and written with the value. `null` removes it. |
| `Duration` | `IcalDuration? Duration { get; set; }` | `DURATION` (RFC 5545 §3.8.2.5). `null` removes it. |
| `Images` | `IReadOnlyList<IcalContentLine> Images { get; }` | `IMAGE` (RFC 7986 §5.10), as the content lines themselves. Each is a URI or inline BINARY data, with parameters such as `DISPLAY` and `FMTTYPE`. Change them through `Properties`. |
| `LastModified` | `IcalDateTime? LastModified { get; set; }` | `LAST-MODIFIED` (RFC 5545 §3.8.7.3). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. `null` removes it. |
| `Location` | `string? Location { get; set; }` | `LOCATION` (RFC 5545 §3.8.1.7). TEXT. `null` removes it. |
| `Organizer` | `Uri? Organizer { get; set; }` | `ORGANIZER` (RFC 5545 §3.8.4.3), a CAL-ADDRESS such as `mailto:a@example.com`. Throws `ArgumentException` on set for a relative URI. `null` removes it. |
| `PercentComplete` | `int? PercentComplete { get; set; }` | `PERCENT-COMPLETE` (RFC 5545 §3.8.1.8), from 0 to 100. The setter does not check the range. `null` removes it. |
| `Priority` | `int? Priority { get; set; }` | `PRIORITY` (RFC 5545 §3.8.1.9), from 0 (undefined) and 1 (highest) to 9 (lowest). The setter does not check the range; `Validate` does. `null` removes it. |
| `RecurrenceId` | `IcalDateOrDateTime? RecurrenceId { get; set; }` | `RECURRENCE-ID` (RFC 5545 §3.8.4.4), the instance this to-do overrides, a DATE or a DATE-TIME. The `VALUE` and `TZID` parameters are read and written with the value. `null` removes it. |
| `RecurrenceRules` | `IReadOnlyList<IcalRecurrenceRule> RecurrenceRules { get; set; }` | `RRULE` (RFC 5545 §3.8.5.3), one rule per property. Setting writes one property per rule, in the place of the first; an empty list or `null` removes them. |
| `Resources` | `IReadOnlyList<string> Resources { get; set; }` | `RESOURCES` (RFC 5545 §3.8.1.10), the items of every `RESOURCES` property. Setting writes one property holding the whole list; an empty list or `null` removes them. Throws `ArgumentException` on set if an item is `null`. |
| `Sequence` | `int? Sequence { get; set; }` | `SEQUENCE` (RFC 5545 §3.8.7.4), the revision sequence number. `null` removes it. |
| `Status` | `string? Status { get; set; }` | `STATUS` (RFC 5545 §3.8.1.11), such as `NEEDS-ACTION` or `COMPLETED`. TEXT. `null` removes it. |
| `Summary` | `string? Summary { get; set; }` | `SUMMARY` (RFC 5545 §3.8.1.12). TEXT. `null` removes it. |
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
| `GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to)` | `IEnumerable<IcalOccurrence<IcalTodo>>` | Expands the recurrence set with `IcalTimeZoneResolver.Default` and default options. |
| `GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to, IIcalTimeZoneResolver resolver)` | `IEnumerable<IcalOccurrence<IcalTodo>>` | Expands with `resolver` and default options. Throws `ArgumentNullException` if `resolver` is `null`. |
| `GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to, IIcalTimeZoneResolver resolver, IcalOptions options)` | `IEnumerable<IcalOccurrence<IcalTodo>>` | Expands with `resolver`; of `options`, only `MaxRecurrenceIterations` applies. Throws `ArgumentNullException` if `resolver` or `options` is `null`, or `ArgumentException` if `options.MaxRecurrenceIterations` is less than 1. |

`calendar` is the calendar the to-do is in, which holds the VTIMEZONE components
and the overriding to-dos, or `null`. `from` is inclusive and `to` exclusive; either
may be `null` for no limit, but a `null` `to` needs every `RRULE` to have `COUNT`
or `UNTIL`. All three overloads throw the same exceptions as
[IcalEvent.GetOccurrences](IcalEvent.md#methods): `ArgumentException`,
`InvalidOperationException`, `IcalFileException` and, while enumerating,
`ArgumentOutOfRangeException`.

Inherited members `Components`, `LineNumber`, `Name`, `Properties`, `GetProperty`,
`GetProperties`, `SetProperty`, `RemoveProperties`, `Validate` and `ToString` are
documented on [IcalComponent](IcalComponent.md).

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1`, `net8.0`, and
`net10.0` builds.

## See also

- [Expand recurrences](../how-to/expand-recurrences.md)
- [Time-zone resolution](../concepts/time-zone-resolution.md)
- [IcalComponent](IcalComponent.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalEvent](IcalEvent.md)
- [IcalOccurrence](IcalOccurrence.md)
- [IcalAlarm](IcalAlarm.md)
- [IcalRecurrenceRule](IcalRecurrenceRule.md)
- [IcalTimeZoneResolver](IcalTimeZoneResolver.md)
- [File.iCal API reference](index.md)
