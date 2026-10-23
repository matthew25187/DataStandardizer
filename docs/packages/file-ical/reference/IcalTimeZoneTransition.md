---
title: IcalTimeZoneTransition Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalTimeZoneTransition Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

A change in the UTC offset of a time zone: the onset of an observance (RFC 5545
§3.6.5). [IcalTimeZoneRules.GetTransitions](IcalTimeZoneRules.md) returns these.

**Syntax**

```csharp
public readonly struct IcalTimeZoneTransition : IEquatable<IcalTimeZoneTransition>
```

## Remarks

A transition is immutable. Its instant is always of kind `DateTimeKind.Utc`: the
constructor treats a time of kind `DateTimeKind.Unspecified` as UTC and rejects one of
kind `DateTimeKind.Local`.

`OffsetFrom` and `OffsetTo` may be equal. That happens when only the kind or the
name changes, or when a VTIMEZONE observance has the same `TZOFFSETFROM` and
`TZOFFSETTO`.

The transitions of [IcalTimeZoneResolver.System](IcalTimeZoneResolver.md) have no
`Name`, because `TimeZoneInfo` gives no customary abbreviation.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalTimeZoneTransition(DateTime instantUtc, IcalUtcOffset offsetFrom, IcalUtcOffset offsetTo, IcalTimeZoneObservanceKind kind, string? name)` | `instantUtc` is the instant of the transition; `offsetFrom` is the offset in use before it and `offsetTo` the offset in use from it onwards; `kind` says whether standard or daylight saving time is in use from it onwards; `name` is the customary name from it onwards, such as `EST`, or `null`. Throws `ArgumentException` if `instantUtc` is of kind `DateTimeKind.Local`, or `ArgumentOutOfRangeException` if `kind` is not an `IcalTimeZoneObservanceKind` member. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `InstantUtc` | `DateTime InstantUtc { get; }` | The instant of the transition, of kind `DateTimeKind.Utc`. |
| `Kind` | `IcalTimeZoneObservanceKind Kind { get; }` | Whether standard time or daylight saving time is in use from the transition onwards. |
| `Name` | `string? Name { get; }` | The customary name of the time zone from the transition onwards, such as `EST`; or `null` if there is none. |
| `OffsetFrom` | `IcalUtcOffset OffsetFrom { get; }` | The UTC offset in use before the transition. |
| `OffsetTo` | `IcalUtcOffset OffsetTo { get; }` | The UTC offset in use from the transition onwards. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(IcalTimeZoneTransition other)` | `bool` | `true` when the instant, both offsets, the kind and the name are the same. Names are compared ordinally. |
| `Equals(object? obj)` | `bool` | `true` when `obj` is an equal `IcalTimeZoneTransition`. |
| `GetHashCode()` | `int` | Consistent with `Equals`. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `==(IcalTimeZoneTransition left, IcalTimeZoneTransition right)` | `bool` | Same as `left.Equals(right)`. |
| `!=(IcalTimeZoneTransition left, IcalTimeZoneTransition right)` | `bool` | Same as `!left.Equals(right)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1`, `net8.0`, and
`net10.0` builds.

## See also

- [Time zone resolution](../concepts/time-zone-resolution.md)
- [Daylight saving rules](../concepts/dst-rules.md)
- [IcalTimeZoneRules](IcalTimeZoneRules.md)
- [IcalTimeZoneObservanceKind](IcalTimeZoneObservanceKind.md)
- [IcalUtcOffset](IcalUtcOffset.md)
- [File.iCal API reference](index.md)
