---
title: IcalCalendar Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalCalendar Class

## Definition

Namespace: `DataStandardizer.File.iCal`

An iCalendar object (RFC 5545 §3.4): the VCALENDAR component, holding the calendar
properties and the calendar components, such as events, to-dos and time zones.

**Syntax**

```csharp
public sealed class IcalCalendar : IcalComponent
```

## Remarks

The calendar properties include those that RFC 7986 adds: `NAME`, `DESCRIPTION`,
`UID`, `LAST-MODIFIED`, `URL`, `CATEGORIES`, `REFRESH-INTERVAL`, `SOURCE`, `COLOR`
and `IMAGE`.

RFC 5545 defines iCalendar 2.0 only. [IcalReader](IcalReader.md) rejects a calendar
whose `VERSION` does not include 2.0, and [IcalWriter](IcalWriter.md) always writes
`VERSION:2.0`.

The typed properties follow the rules described under *Typed accessors* on
[IcalComponent](IcalComponent.md): a getter reads the first property of its name, a
setter replaces every property of its name in the place of the first, setting
`null` removes them, and a getter throws [IcalFileException](IcalFileException.md)
if the stored value is not valid.

The component collections (`Events`, `Todos` and so on) filter `Components` by type
each time they are enumerated. They see only the components nested directly in the
calendar. Add or remove components through `Components`.

The constructor adds `VERSION:2.0`. A calendar built in code also needs a `PRODID`
(`ProductId`) to pass `Validate`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalCalendar()` | Creates a calendar with a `VERSION:2.0` property and nothing else. |

## Properties

### Components

| Property | Signature | Notes |
| --- | --- | --- |
| `Events` | `IEnumerable<IcalEvent> Events { get; }` | The VEVENT components of the calendar. |
| `FreeBusy` | `IEnumerable<IcalFreeBusy> FreeBusy { get; }` | The VFREEBUSY components of the calendar. |
| `Journals` | `IEnumerable<IcalJournal> Journals { get; }` | The VJOURNAL components of the calendar. |
| `TimeZones` | `IEnumerable<IcalTimeZone> TimeZones { get; }` | The VTIMEZONE components of the calendar. |
| `Todos` | `IEnumerable<IcalTodo> Todos { get; }` | The VTODO components of the calendar. |

### Calendar properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CalendarName` | `string? CalendarName { get; set; }` | `NAME` (RFC 7986 §5.1). TEXT. Throws `IcalFileException` on get if the value has a backslash that does not start a valid escape. `null` removes it. |
| `CalendarScale` | `string? CalendarScale { get; set; }` | `CALSCALE` (RFC 5545 §3.7.1), such as `GREGORIAN`. TEXT, as for `CalendarName`. `null` removes it. |
| `Categories` | `IReadOnlyList<string> Categories { get; set; }` | `CATEGORIES` (RFC 5545 §3.8.1.2; RFC 7986 §5.6), the items of every `CATEGORIES` property. Setting writes one property holding the whole list; an empty list or `null` removes them. Throws `ArgumentException` on set if an item is `null`, or `IcalFileException` on get if an item has an invalid escape. |
| `Color` | `string? Color { get; set; }` | `COLOR` (RFC 7986 §5.9), a CSS3 color name such as `turquoise`. TEXT. `null` removes it. |
| `Description` | `string? Description { get; set; }` | `DESCRIPTION` (RFC 5545 §3.8.1.5; RFC 7986 §5.2). TEXT. `null` removes it. |
| `Images` | `IReadOnlyList<IcalContentLine> Images { get; }` | `IMAGE` (RFC 7986 §5.10), as the content lines themselves. Each is a URI or inline BINARY data, with parameters such as `DISPLAY` and `FMTTYPE`. Change them through `Properties`. |
| `LastModified` | `IcalDateTime? LastModified { get; set; }` | `LAST-MODIFIED` (RFC 5545 §3.8.7.3; RFC 7986 §5.4). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. Throws `IcalFileException` on get if the value is not a valid DATE-TIME, including a `VALUE=DATE`. `null` removes it. |
| `Method` | `string? Method { get; set; }` | `METHOD` (RFC 5545 §3.7.2), the iTIP method, such as `PUBLISH`. TEXT. `null` removes it. |
| `ProductId` | `string? ProductId { get; set; }` | `PRODID` (RFC 5545 §3.7.3), the identifier of the product that created the calendar. TEXT. `null` removes it. |
| `RefreshInterval` | `IcalDuration? RefreshInterval { get; set; }` | `REFRESH-INTERVAL` (RFC 7986 §5.7), the suggested minimum interval between polls. The setter writes the `VALUE=DURATION` parameter the property needs. Throws `IcalFileException` on get if the value is not a valid DURATION. `null` removes it. |
| `Source` | `Uri? Source { get; set; }` | `SOURCE` (RFC 7986 §5.8), where the calendar can be refreshed from. The setter writes the `VALUE=URI` parameter the property needs. Throws `ArgumentException` on set for a relative URI, or `IcalFileException` on get if the value is not a valid absolute URI. `null` removes it. |
| `Uid` | `string? Uid { get; set; }` | `UID` (RFC 5545 §3.8.4.7; RFC 7986 §5.3), a persistent, globally unique identifier. TEXT. `null` removes it. |
| `Url` | `Uri? Url { get; set; }` | `URL` (RFC 5545 §3.8.4.6; RFC 7986 §5.5). Throws `ArgumentException` on set for a relative URI, or `IcalFileException` on get if the value is not a valid absolute URI. `null` removes it. |
| `Version` | `string? Version { get; set; }` | `VERSION` (RFC 5545 §3.7.4), as written: `2.0`, or a range such as `1.0;2.0`. TEXT. `null` removes it, but the writer writes `VERSION:2.0` regardless. |

Every TEXT property removes TEXT escaping on get and adds it on set, and throws
`IcalFileException` on get if the value has a backslash that does not start a
valid escape.

Inherited members `Components`, `LineNumber`, `Name`, `Properties`, `GetProperty`,
`GetProperties`, `SetProperty`, `RemoveProperties`, `Validate` and `ToString` are
documented on [IcalComponent](IcalComponent.md).

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1`, `net8.0`, and
`net10.0` builds.

## See also

- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [Expand recurrences](../how-to/expand-recurrences.md)
- [The layered model](../concepts/layered-model.md)
- [Time-zone resolution](../concepts/time-zone-resolution.md)
- [IcalComponent](IcalComponent.md)
- [IcalEvent](IcalEvent.md)
- [IcalTodo](IcalTodo.md)
- [IcalJournal](IcalJournal.md)
- [IcalFreeBusy](IcalFreeBusy.md)
- [IcalTimeZone](IcalTimeZone.md)
- [IcalReader](IcalReader.md)
- [IcalWriter](IcalWriter.md)
- [File.iCal API reference](index.md)
