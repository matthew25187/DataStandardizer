---
title: IcalJournal Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalJournal Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A journal entry (RFC 5545 §3.6.3): the VJOURNAL component. It gives typed access to
the properties of a journal entry, and expands its recurrence set into occurrences.

**Syntax**

```csharp
public sealed class IcalJournal : IcalComponent
```

## Remarks

The typed properties follow the rules described under *Typed accessors* on
[IcalComponent](IcalComponent.md): a getter reads the first property of its name, a
setter replaces every property of its name in the place of the first, setting
`null` removes them, and a getter throws [IcalFileException](IcalFileException.md)
if the stored value is not valid.

RFC 5545 lets a VJOURNAL have more than one `DESCRIPTION`. So `Descriptions` gives
the content lines themselves, and has no setter. Change them through `Properties`,
or replace them all with one by calling `SetProperty`.

**Recurrence expansion.** `GetOccurrences` finds the instances as
[IcalEvent](IcalEvent.md) does, with the journal entries of `calendar` as the
overrides. A journal entry has no end, so no instance has one, and
`IcalOccurrence.End` is always `null`. The properties are read, and the time zones
of `DTSTART` and the window resolved, when `GetOccurrences` is called. The
instances are generated lazily as they are enumerated. A journal entry with no
`DTSTART` has no instances.

The overloads without a `resolver` use `IcalTimeZoneResolver.Default`, which is
`IcalTimeZoneResolver.Calendar`: it resolves a `TZID` only from the VTIMEZONE
components of `calendar`. The overloads without `options` use a default
[IcalOptions](IcalOptions.md).

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalJournal()` | Creates an empty VJOURNAL component, with no properties. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Attendees` | `IReadOnlyList<IcalContentLine> Attendees { get; }` | `ATTENDEE` (RFC 5545 §3.8.4.1), as the content lines themselves. Each is a CAL-ADDRESS with parameters such as `CN` and `PARTSTAT`. Change them through `Properties`. |
| `Categories` | `IReadOnlyList<string> Categories { get; set; }` | `CATEGORIES` (RFC 5545 §3.8.1.2), the items of every `CATEGORIES` property. Setting writes one property holding the whole list; an empty list or `null` removes them. Throws `ArgumentException` on set if an item is `null`. |
| `Classification` | `string? Classification { get; set; }` | `CLASS` (RFC 5545 §3.8.1.3), such as `PUBLIC`. TEXT. `null` removes it. |
| `Color` | `string? Color { get; set; }` | `COLOR` (RFC 7986 §5.9), a CSS3 color name such as `turquoise`. TEXT. `null` removes it. |
| `Created` | `IcalDateTime? Created { get; set; }` | `CREATED` (RFC 5545 §3.8.7.1). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. `null` removes it. |
| `Descriptions` | `IReadOnlyList<IcalContentLine> Descriptions { get; }` | `DESCRIPTION` (RFC 5545 §3.8.1.5), as the content lines themselves. The values keep their TEXT escaping; call `IcalContentLine.GetTextValue` to remove it. |
| `DtStamp` | `IcalDateTime? DtStamp { get; set; }` | `DTSTAMP` (RFC 5545 §3.8.7.2). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. `null` removes it. |
| `DtStart` | `IcalDateOrDateTime? DtStart { get; set; }` | `DTSTART` (RFC 5545 §3.8.2.4), a DATE or a DATE-TIME. The `VALUE` and `TZID` parameters are read and written with the value. `null` removes it. |
| `Images` | `IReadOnlyList<IcalContentLine> Images { get; }` | `IMAGE` (RFC 7986 §5.10), as the content lines themselves. Each is a URI or inline BINARY data, with parameters such as `DISPLAY` and `FMTTYPE`. Change them through `Properties`. |
| `LastModified` | `IcalDateTime? LastModified { get; set; }` | `LAST-MODIFIED` (RFC 5545 §3.8.7.3). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. `null` removes it. |
| `Organizer` | `Uri? Organizer { get; set; }` | `ORGANIZER` (RFC 5545 §3.8.4.3), a CAL-ADDRESS such as `mailto:a@example.com`. Throws `ArgumentException` on set for a relative URI. `null` removes it. |
| `RecurrenceId` | `IcalDateOrDateTime? RecurrenceId { get; set; }` | `RECURRENCE-ID` (RFC 5545 §3.8.4.4), the instance this journal entry overrides, a DATE or a DATE-TIME. The `VALUE` and `TZID` parameters are read and written with the value. `null` removes it. |
| `RecurrenceRules` | `IReadOnlyList<IcalRecurrenceRule> RecurrenceRules { get; set; }` | `RRULE` (RFC 5545 §3.8.5.3), one rule per property. Setting writes one property per rule, in the place of the first; an empty list or `null` removes them. |
| `Sequence` | `int? Sequence { get; set; }` | `SEQUENCE` (RFC 5545 §3.8.7.4), the revision sequence number. `null` removes it. |
| `Status` | `string? Status { get; set; }` | `STATUS` (RFC 5545 §3.8.1.11), such as `DRAFT` or `FINAL`. TEXT. `null` removes it. |
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
| `GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to)` | `IEnumerable<IcalOccurrence<IcalJournal>>` | Expands the recurrence set with `IcalTimeZoneResolver.Default` and default options. |
| `GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to, IIcalTimeZoneResolver resolver)` | `IEnumerable<IcalOccurrence<IcalJournal>>` | Expands with `resolver` and default options. Throws `ArgumentNullException` if `resolver` is `null`. |
| `GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to, IIcalTimeZoneResolver resolver, IcalOptions options)` | `IEnumerable<IcalOccurrence<IcalJournal>>` | Expands with `resolver`; of `options`, only `MaxRecurrenceIterations` applies. Throws `ArgumentNullException` if `resolver` or `options` is `null`, or `ArgumentException` if `options.MaxRecurrenceIterations` is less than 1. |

`calendar` is the calendar the journal entry is in, which holds the VTIMEZONE
components and the overriding journal entries, or `null`. `from` is inclusive and
`to` exclusive; either may be `null` for no limit, but a `null` `to` needs every
`RRULE` to have `COUNT` or `UNTIL`. All three overloads throw the same exceptions
as [IcalEvent.GetOccurrences](IcalEvent.md#methods): `ArgumentException`,
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
- [IcalComponent](IcalComponent.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalEvent](IcalEvent.md)
- [IcalOccurrence](IcalOccurrence.md)
- [IcalContentLine](IcalContentLine.md)
- [IcalRecurrenceRule](IcalRecurrenceRule.md)
- [File.iCal API reference](index.md)
