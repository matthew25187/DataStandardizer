---
title: IcalTimeZone Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalTimeZone Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A time zone definition (RFC 5545 §3.6.5): the VTIMEZONE component. It holds the
`TZID` that `TZID` parameters refer to, and the STANDARD and DAYLIGHT observances
that define the zone's UTC offsets. `FromTzData` builds one from the TZ Database.

**Syntax**

```csharp
public sealed class IcalTimeZone : IcalComponent
```

## Remarks

RFC 5545 §3.2.19 requires a calendar to hold a VTIMEZONE for every `TZID` it uses.
The readers create an `IcalTimeZone` for each VTIMEZONE they read, and
[IcalCalendar.TimeZones](IcalCalendar.md) lists them. To add one, add it to the
calendar's [Components](IcalComponent.md).

[IcalTimeZoneResolver.Calendar](IcalTimeZoneResolver.md), which is also
`IcalTimeZoneResolver.Default`, turns a VTIMEZONE into
[IcalTimeZoneRules](IcalTimeZoneRules.md). It reads the observances when the `TZID`
is resolved, so later changes to the component do not affect rules already
returned.

The typed properties read and write the underlying content lines in
[Properties](IcalComponent.md). A getter returns `null` when the property is absent,
and throws `IcalFileException` when its value is not valid. Setting a property to
`null` removes it.

**Building from the TZ Database.** `FromTzData` defines a time zone over a period
you choose. It has no default range of years: the observances cover exactly the
transitions from `from` (included) to `to` (excluded). Choose a period that covers
the calendar's times, including the occurrences of any recurring components.

- The first observance starts at `from`, in local time. It has the offset then in
  use as both its `TZOFFSETFROM` and its `TZOFFSETTO`, with the kind and name in use
  at `from`.
- Each later observance gathers the transitions that share a kind, a pair of offsets
  and a name. Its `DTSTART` is the first of them, and there is one `RDATE` property for
  each of the rest. `DTSTART` and `RDATE` are floating times, in the local time before the
  transition, as RFC 5545 §3.6.5 requires. `RRULE` is not used.
- A transition that changes neither the offset, the kind nor the name, such as a
  change to the standard offset alone, is left out.
- Before `from`, the definition gives the offset in use at `from`. From `to`
  onwards, it gives the offset that follows the last transition in the period.
- Times are written to the whole second. A fraction of a second in `from` is
  dropped.

For example, `America/New_York` from 2020 to 2023 gives three observances: a
STANDARD starting at `from`, a DAYLIGHT with `DTSTART:20200308T020000` and `RDATE`s
for 2021 and 2022, and a STANDARD with `DTSTART:20201101T020000` and `RDATE`s for
2021 and 2022. A zone with no transitions in the period, such as `Asia/Kolkata`,
gives one observance.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalTimeZone()` | Creates an empty VTIMEZONE component. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `LastModified` | `IcalDateTime? LastModified { get; set; }` | When the information was last revised (RFC 5545 §3.8.7.3, `LAST-MODIFIED`), in UTC. The `VALUE` and `TZID` parameters are read and written with the value. The getter throws `IcalFileException` if the value is not valid. |
| `Observances` | `IEnumerable<IcalTimeZoneObservance> Observances { get; }` | The STANDARD and DAYLIGHT observances: the nested components of type `IcalTimeZoneObservance`, in order. Evaluated lazily over `Components`. |
| `TimeZoneId` | `string? TimeZoneId { get; set; }` | The time zone identifier (RFC 5545 §3.8.3.1, `TZID`), which `TZID` parameters refer to. TEXT escaping is removed by the getter and added by the setter. The getter throws `IcalFileException` if the value has a backslash that does not start a valid escape. |
| `TimeZoneUrl` | `Uri? TimeZoneUrl { get; set; }` | The location of a published definition of the time zone (RFC 5545 §3.8.3.5, `TZURL`). The getter throws `IcalFileException` if the value is not a valid absolute URI. The setter throws `ArgumentException` if the URI is not absolute. |

Inherited members (`Components`, `Name`, `Properties`, `GetProperty`,
`GetProperties`, `RemoveProperties`, `SetProperty`, `Validate` and others) are
documented on [IcalComponent](IcalComponent.md).

## Methods

### Static

| Method | Returns | Notes |
| --- | --- | --- |
| `FromTzData(TzDataTimezone timezone, DateTimeOffset from, DateTimeOffset to)` | `IcalTimeZone` | Builds a VTIMEZONE from the TZ Database over a period, as described under Remarks. `timezone` is the [TzDataTimezone](../../chronology/reference/TzDataTimezone.md); its identifier becomes the `TZID`. `from` is the start of the period, which is included; `to` is the end, which is excluded. Throws `ArgumentOutOfRangeException` if `to` precedes `from`, or `InvalidOperationException` if `timezone` is the default value or its identifier is not that of a known time zone. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1`, `net8.0`, and
`net10.0` builds. `TzDataTimezone` comes from the DataStandardizer.Chronology
package, which this package depends on.

## See also

- [Time zone resolution](../concepts/time-zone-resolution.md)
- [Daylight saving rules](../concepts/dst-rules.md)
- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [IcalTimeZoneObservance](IcalTimeZoneObservance.md)
- [IcalTimeZoneResolver](IcalTimeZoneResolver.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalComponent](IcalComponent.md)
- [TzDataTimezone](../../chronology/reference/TzDataTimezone.md)
- [File.iCal API reference](index.md)
