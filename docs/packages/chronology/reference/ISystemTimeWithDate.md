---
title: ISystemTimeWithDate Interface
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# ISystemTimeWithDate Interface

## Definition

Namespace: `DataStandardizer.Chronology`

An [ISystemTime](ISystemTime.md) that also exposes the year, month, and day
components of the instant.

```csharp
public interface ISystemTimeWithDate : ISystemTime
```

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Day` | `int Day { get; }` | The day of the month. |
| `JulianDayNumber` | `decimal JulianDayNumber { get; }` | Inherited from `ISystemTime`. |
| `Month` | `int Month { get; }` | The month of the year (1 = January, 12 = December). |
| `Year` | `int Year { get; }` | The year component. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The system-time model](../concepts/system-time-model.md)
- [ISystemTime](ISystemTime.md)
- [ISystemTimeWithTime](ISystemTimeWithTime.md)
- [ISystemTimeWithDateTime](ISystemTimeWithDateTime.md)
- [Chronology API reference](index.md)
