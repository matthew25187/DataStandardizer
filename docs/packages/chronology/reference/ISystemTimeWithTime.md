---
title: ISystemTimeWithTime Interface
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# ISystemTimeWithTime Interface

## Definition

Namespace: `DataStandardizer.Chronology`

An [ISystemTime](ISystemTime.md) that also exposes the hour, minute, and second
components of the instant.

```csharp
public interface ISystemTimeWithTime : ISystemTime
```

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Hour` | `int Hour { get; }` | The hour component (0&ndash;23). |
| `JulianDayNumber` | `decimal JulianDayNumber { get; }` | Inherited from `ISystemTime`. |
| `Minute` | `int Minute { get; }` | The minute component (0&ndash;59). |
| `Second` | `int Second { get; }` | The second component (0&ndash;59). |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The system-time model](../concepts/system-time-model.md)
- [ISystemTime](ISystemTime.md)
- [ISystemTimeWithDate](ISystemTimeWithDate.md)
- [ISystemTimeWithDateTime](ISystemTimeWithDateTime.md)
- [Chronology API reference](index.md)
