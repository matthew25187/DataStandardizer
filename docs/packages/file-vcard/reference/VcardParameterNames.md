---
title: VcardParameterNames Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardParameterNames Class

## Definition

Namespace: `DataStandardizer.File.vCard`

The names of the parameters RFC 6350 defines. Names are case-insensitive; these are in the upper case the RFC uses.

**Syntax**

```csharp
public static class VcardParameterNames
```

## Fields

Each is a `const string`.

| Field | Value | Notes |
| --- | --- | --- |
| `AlternativeId` | `ALTID` | RFC 6350 §5.4: tags instances of a property as alternative representations of the same logical property. |
| `CalendarScale` | `CALSCALE` | RFC 6350 §5.8: the calendar system of a date or date-time. |
| `Geo` | `GEO` | RFC 6350 §5.10: a global position specific to an address. |
| `Label` | `LABEL` | RFC 6350 §6.3.1: a delivery address label. |
| `Language` | `LANGUAGE` | RFC 6350 §5.1: the language of a value. |
| `MediaType` | `MEDIATYPE` | RFC 6350 §5.7: a hint at the media type of the resource a URI identifies. |
| `Pid` | `PID` | RFC 6350 §5.5: identifies a specific instance of a property. |
| `Preference` | `PREF` | RFC 6350 §5.3: the level of preference, from 1 (most preferred) to 100. |
| `SortAs` | `SORT-AS` | RFC 6350 §5.9: the strings to sort by. |
| `TimeZone` | `TZ` | RFC 6350 §5.11: a time zone specific to an address. |
| `Type` | `TYPE` | RFC 6350 §5.6: class characteristics of a property. |
| `Value` | `VALUE` | RFC 6350 §5.2: the value type. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardPropertyNames](VcardPropertyNames.md)
- [VcardContentLine](VcardContentLine.md)
