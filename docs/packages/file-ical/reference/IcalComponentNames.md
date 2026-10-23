---
title: IcalComponentNames Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalComponentNames Class

## Definition

Namespace: `DataStandardizer.File.iCal`

The names of the components that RFC 5545 defines, as `string` constants.

**Syntax**

```csharp
public static class IcalComponentNames
```

## Remarks

Component names are case-insensitive. These constants are in the upper case the RFC
uses. Compare them with `IcalComponent.Name`, or use them to create an
[IcalComponent](IcalComponent.md). The readers convert the lower-case names of xCal
and jCal to upper case, so these constants match whatever format a calendar was read
from.

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Calendar` | `"VCALENDAR"` | An iCalendar object (RFC 5545 §3.4). |
| `Event` | `"VEVENT"` | An event (RFC 5545 §3.6.1). |
| `Todo` | `"VTODO"` | A to-do (RFC 5545 §3.6.2). |
| `Journal` | `"VJOURNAL"` | A journal entry (RFC 5545 §3.6.3). |
| `FreeBusy` | `"VFREEBUSY"` | Free or busy time information (RFC 5545 §3.6.4). |
| `TimeZone` | `"VTIMEZONE"` | A time zone definition (RFC 5545 §3.6.5). |
| `Standard` | `"STANDARD"` | The standard time observance of a time zone (RFC 5545 §3.6.5). |
| `Daylight` | `"DAYLIGHT"` | The daylight saving time observance of a time zone (RFC 5545 §3.6.5). |
| `Alarm` | `"VALARM"` | An alarm (RFC 5545 §3.6.6). |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [The layered model](../concepts/layered-model.md)
- [IcalComponent](IcalComponent.md)
- [IcalPropertyNames](IcalPropertyNames.md)
- [IcalParameterNames](IcalParameterNames.md)
- [File.iCal API reference](index.md)
