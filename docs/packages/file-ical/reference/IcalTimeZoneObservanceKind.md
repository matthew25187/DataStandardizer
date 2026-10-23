---
title: IcalTimeZoneObservanceKind Enum
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalTimeZoneObservanceKind Enum

## Definition

Namespace: `DataStandardizer.File.iCal`

The kinds of observance of a time zone (RFC 5545 §3.6.5). Gives the
[Kind](IcalTimeZoneObservance.md) of a STANDARD or DAYLIGHT component, and the
[Kind](IcalTimeZoneTransition.md) of a transition.

**Syntax**

```csharp
public enum IcalTimeZoneObservanceKind
```

## Remarks

The members start at 1, so `default(IcalTimeZoneObservanceKind)` (0) is not a
member. The `IcalTimeZoneObservance` and `IcalTimeZoneTransition` constructors
throw `ArgumentOutOfRangeException` for a value that is not `Standard` or
`Daylight`.

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Standard` | 1 | Standard time: the STANDARD component. |
| `Daylight` | 2 | Daylight saving time: the DAYLIGHT component. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [Time zone resolution](../concepts/time-zone-resolution.md)
- [IcalTimeZoneObservance](IcalTimeZoneObservance.md)
- [IcalTimeZoneTransition](IcalTimeZoneTransition.md)
- [File.iCal API reference](index.md)
