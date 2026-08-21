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
| [TzDataTimezone](TzDataTimezone.md) | A TZ Database time zone, exposed through predefined nested static instances. |
| [UnixTime](UnixTime.md) | A point in time as seconds since the Unix epoch, stored as a signed 64-bit integer. |

## Classes

| Type | Description |
| --- | --- |
| [DateOnlyExtensions](DateOnlyExtensions.md) | Extension methods converting `DateOnly` to system-time types. |
| [DateTimeExtensions](DateTimeExtensions.md) | Extension methods converting `DateTime` to system-time types. |
| [SystemTimeExtensions](SystemTimeExtensions.md) | Extension methods converting system-time types to `DateTime`/`DateOnly`/`TimeOnly`. |
| [TimeOnlyExtensions](TimeOnlyExtensions.md) | Extension methods converting `TimeOnly` to system-time types. |
| [TzDataExtensions](TzDataExtensions.md) | Extension methods that read TZ Database time zone metadata. |
| [TzDataTimezoneAttribute](TzDataTimezoneAttribute.md) | Carries the per-zone metadata surfaced by `TzDataExtensions`. |

## Interfaces

| Type | Description |
| --- | --- |
| [ISystemTime](ISystemTime.md) | Root abstraction for any encoding of an instant; exposes a Julian Day Number. |
| [ISystemTimeWithDate](ISystemTimeWithDate.md) | An `ISystemTime` that also exposes date components. |
| [ISystemTimeWithDateTime](ISystemTimeWithDateTime.md) | Combines `ISystemTimeWithDate` and `ISystemTimeWithTime`. |
| [ISystemTimeWithTime](ISystemTimeWithTime.md) | An `ISystemTime` that also exposes time-of-day components. |
