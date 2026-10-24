---
title: IcalFreeBusy Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalFreeBusy Class

## Definition

Namespace: `DataStandardizer.File.iCal`

Free or busy time information (RFC 5545 §3.6.4): the VFREEBUSY component. It gives
typed access to the properties of a free/busy request, reply or publication.

**Syntax**

```csharp
public sealed class IcalFreeBusy : IcalComponent
```

## Remarks

The typed properties follow the rules described under *Typed accessors* on
[IcalComponent](IcalComponent.md): a getter reads the first property of its name, a
setter replaces every property of its name in the place of the first, setting
`null` removes them, and a getter throws [IcalFileException](IcalFileException.md)
if the stored value is not valid.

`Periods` gathers the periods of every `FREEBUSY` property into one list, so it
loses the `FBTYPE` parameter that says whether each period is free or busy. To
read `FBTYPE`, call `GetProperties("FREEBUSY")` and read each content line. To
change the periods, change the `FREEBUSY` properties through `Properties`.

A VFREEBUSY has no recurrence set, so there is no `GetOccurrences`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalFreeBusy()` | Creates an empty VFREEBUSY component, with no properties. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Attendees` | `IReadOnlyList<IcalContentLine> Attendees { get; }` | `ATTENDEE` (RFC 5545 §3.8.4.1), as the content lines themselves. Each is a CAL-ADDRESS with parameters such as `CN` and `PARTSTAT`. Change them through `Properties`. |
| `Contact` | `string? Contact { get; set; }` | `CONTACT` (RFC 5545 §3.8.4.2). TEXT. Throws `IcalFileException` on get if the value has a backslash that does not start a valid escape. `null` removes it. |
| `DtEnd` | `IcalDateOrDateTime? DtEnd { get; set; }` | `DTEND` (RFC 5545 §3.8.2.2), a DATE or a DATE-TIME. The `VALUE` and `TZID` parameters are read and written with the value. Throws `IcalFileException` on get if the value is not valid. `null` removes it. |
| `DtStamp` | `IcalDateTime? DtStamp { get; set; }` | `DTSTAMP` (RFC 5545 §3.8.7.2). A DATE-TIME, which RFC 5545 requires to be in UTC; the accessor does not enforce this. Throws `IcalFileException` on get if the value is not a valid DATE-TIME, including a `VALUE=DATE`. `null` removes it. |
| `DtStart` | `IcalDateOrDateTime? DtStart { get; set; }` | `DTSTART` (RFC 5545 §3.8.2.4), a DATE or a DATE-TIME. The `VALUE` and `TZID` parameters are read and written with the value. Throws `IcalFileException` on get if the value is not valid. `null` removes it. |
| `Organizer` | `Uri? Organizer { get; set; }` | `ORGANIZER` (RFC 5545 §3.8.4.3), a CAL-ADDRESS such as `mailto:a@example.com`. Throws `ArgumentException` on set for a relative URI, or `IcalFileException` on get if the value is not a valid absolute URI. `null` removes it. |
| `Periods` | `IReadOnlyList<IcalPeriod> Periods { get; }` | `FREEBUSY` (RFC 5545 §3.8.2.6), the periods of every `FREEBUSY` property, each split on commas, as a new list. Empty if there are none. Throws `IcalFileException` if a period is not valid. Read-only. |
| `Uid` | `string? Uid { get; set; }` | `UID` (RFC 5545 §3.8.4.7), a persistent, globally unique identifier. TEXT. `null` removes it. |
| `Url` | `Uri? Url { get; set; }` | `URL` (RFC 5545 §3.8.4.6). Throws `ArgumentException` on set for a relative URI, or `IcalFileException` on get if the value is not a valid absolute URI. `null` removes it. |

Inherited members `Components`, `LineNumber`, `Name`, `Properties`, `GetProperty`,
`GetProperties`, `SetProperty`, `RemoveProperties`, `Validate` and `ToString` are
documented on [IcalComponent](IcalComponent.md).

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1`, `net8.0`, and
`net10.0` builds.

## See also

- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [IcalComponent](IcalComponent.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalPeriod](IcalPeriod.md)
- [IcalContentLine](IcalContentLine.md)
- [File.iCal API reference](index.md)
