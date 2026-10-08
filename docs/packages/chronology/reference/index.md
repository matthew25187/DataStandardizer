---
title: API reference
parent: Chronology
grand_parent: Packages
nav_order: 20
---

# DataStandardizer.Chronology API reference

The public types of **DataStandardizer.Chronology**. All types are in the
`DataStandardizer.Chronology` namespace.

## Structures

| Type | Description |
| --- | --- |
| [DosDateTime](DosDateTime.md) | An MS-DOS packed date/time (1980&ndash;2107) stored as an unsigned 32-bit integer. |
| [SystemTimeWithGregorianCalendar](SystemTimeWithGregorianCalendar.md) | A decorator that adds Gregorian calendar date &amp; time components to any `ISystemTime`. |
| [TzDataOffsetInfo](TzDataOffsetInfo.md) | The offset from universal time in effect in a TZ Database time zone over a period, with its abbreviation. |
| [TzDataTimezone](TzDataTimezone.md) | A TZ Database time zone, exposed through predefined nested static instances. |
| [TzDataTransition](TzDataTransition.md) | A change in the offset from universal time in effect in a TZ Database time zone. |
| [TzDataUntil](TzDataUntil.md) | The moment at which a TZ Database zone line stops applying (the `UNTIL` column). |
| [UnixTime](UnixTime.md) | A point in time as seconds since the Unix epoch, stored as a signed 64-bit integer. |

## Classes

| Type | Description |
| --- | --- |
| [DateOnlyExtensions](DateOnlyExtensions.md) | Extension methods converting `DateOnly` to system-time types. |
| [DateTimeExtensions](DateTimeExtensions.md) | Extension methods converting `DateTime` to system-time types. |
| [SystemTimeExtensions](SystemTimeExtensions.md) | Extension methods converting system-time types to `DateTime`/`DateOnly`/`TimeOnly`. |
| [TimeOnlyExtensions](TimeOnlyExtensions.md) | Extension methods converting `TimeOnly` to system-time types. |
| [TzDataExtensions](TzDataExtensions.md) | Extension methods calculating the offsets, transitions and abbreviations of TZ Database time zones, and the deprecated metadata accessors. |
| [TzDataRule](TzDataRule.md) | A TZ Database daylight saving rule (a `Rule` line). |
| [TzDataTimezoneAttribute](TzDataTimezoneAttribute.md) | Deprecated. Carries the per-zone metadata now exposed by the properties of `TzDataTimezone`. |
| [TzDataZoneLine](TzDataZoneLine.md) | A period in the offset history of a TZ Database time zone (a line of a `Zone` entry). |

## Enumerations

| Type | Description |
| --- | --- |
| [TzDataDayKind](TzDataDayKind.md) | How the day of a TZ Database rule transition or `UNTIL` is specified. |
| [TzDataTimeReference](TzDataTimeReference.md) | The clock against which a TZ Database time of day is measured. |
| [TzDataZoneRuleKind](TzDataZoneRuleKind.md) | What the `RULES` column of a TZ Database zone line contains. |

## Interfaces

| Type | Description |
| --- | --- |
| [ISystemTime](ISystemTime.md) | Root abstraction for any encoding of an instant; exposes a Julian Day Number. |
| [ISystemTimeWithDate](ISystemTimeWithDate.md) | An `ISystemTime` that also exposes date components. |
| [ISystemTimeWithDateTime](ISystemTimeWithDateTime.md) | Combines `ISystemTimeWithDate` and `ISystemTimeWithTime`. |
| [ISystemTimeWithTime](ISystemTimeWithTime.md) | An `ISystemTime` that also exposes time-of-day components. |
