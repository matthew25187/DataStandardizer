---
title: IcalTimeZoneObservance Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalTimeZoneObservance Class

## Definition

Namespace: `DataStandardizer.File.iCal`

An observance of a time zone (RFC 5545 §3.6.5): the STANDARD or DAYLIGHT component
of a VTIMEZONE. It says when the observance begins and which UTC offsets are in use
before and during it.

**Syntax**

```csharp
public sealed class IcalTimeZoneObservance : IcalComponent
```

## Remarks

The component name is `STANDARD` or `DAYLIGHT`, set from the kind given to the
constructor. The kind cannot change afterwards.

When `IcalReader` (or `XcalReader`, `JcalReader`) reads a STANDARD or DAYLIGHT
component nested directly in a VTIMEZONE, it creates an `IcalTimeZoneObservance`. A
STANDARD or DAYLIGHT component anywhere else is read as a plain `IcalComponent`.
[IcalTimeZone.Observances](IcalTimeZone.md) lists only the nested components of this
type.

The typed properties read and write the underlying content lines in
[Properties](IcalComponent.md). A getter returns `null` when the property is absent,
and throws `IcalFileException` when its value is not valid. Setting a property to
`null` removes it.

RFC 5545 §3.6.5 requires `DTSTART`, `TZOFFSETFROM` and `TZOFFSETTO` in every
observance. `DTSTART` and `RDATE` are local times, in the offset given by
`TZOFFSETFROM`. When [IcalTimeZoneResolver.Calendar](IcalTimeZoneResolver.md) reads
an observance, it uses only the wall-clock fields of `DTSTART`, whatever its form,
and throws `IcalFileException` if any of the three properties is missing.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalTimeZoneObservance(IcalTimeZoneObservanceKind kind)` | Creates an empty STANDARD or DAYLIGHT component. Throws `ArgumentOutOfRangeException` if `kind` is not an `IcalTimeZoneObservanceKind` member. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `DtStart` | `IcalDateTime? DtStart { get; set; }` | The start of the observance (RFC 5545 §3.8.2.4, `DTSTART`), a DATE-TIME in local time. The `VALUE` and `TZID` parameters are read and written with the value. The getter throws `IcalFileException` if the value is not valid. |
| `Kind` | `IcalTimeZoneObservanceKind Kind { get; }` | Whether the observance is of standard time (STANDARD) or daylight saving time (DAYLIGHT). |
| `OffsetFrom` | `IcalUtcOffset? OffsetFrom { get; set; }` | The UTC offset in use before the observance begins (RFC 5545 §3.8.3.3, `TZOFFSETFROM`). The getter throws `IcalFileException` if the value is not valid. |
| `OffsetTo` | `IcalUtcOffset? OffsetTo { get; set; }` | The UTC offset in use during the observance (RFC 5545 §3.8.3.4, `TZOFFSETTO`). The getter throws `IcalFileException` if the value is not valid. |
| `RecurrenceRules` | `IReadOnlyList<IcalRecurrenceRule> RecurrenceRules { get; set; }` | The recurrence rules (RFC 5545 §3.8.5.3, `RRULE`). Setting a list replaces every `RRULE` property with one per rule; an empty list removes them. The getter throws `IcalFileException` if a rule is not valid. |
| `TimeZoneName` | `string? TimeZoneName { get; set; }` | The customary name of the time zone during the observance (RFC 5545 §3.8.3.2, `TZNAME`), such as `EST`. TEXT escaping is removed by the getter and added by the setter. The getter throws `IcalFileException` if the value has a backslash that does not start a valid escape. |

There is no typed property for `RDATE`. Read and write it through
`GetProperties(IcalPropertyNames.RecurrenceDates)` and `Properties`, as
[IcalTimeZone.FromTzData](IcalTimeZone.md) does.

Inherited members (`Components`, `Name`, `Properties`, `GetProperty`,
`GetProperties`, `RemoveProperties`, `SetProperty`, `Validate` and others) are
documented on [IcalComponent](IcalComponent.md).

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1`, `net8.0`, and
`net10.0` builds.

## See also

- [Time zone resolution](../concepts/time-zone-resolution.md)
- [Daylight saving rules](../concepts/dst-rules.md)
- [IcalTimeZone](IcalTimeZone.md)
- [IcalTimeZoneObservanceKind](IcalTimeZoneObservanceKind.md)
- [IcalUtcOffset](IcalUtcOffset.md)
- [IcalRecurrenceRule](IcalRecurrenceRule.md)
- [IcalComponent](IcalComponent.md)
- [File.iCal API reference](index.md)
