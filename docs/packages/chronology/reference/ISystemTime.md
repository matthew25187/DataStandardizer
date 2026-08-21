---
title: ISystemTime Interface
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# ISystemTime Interface

## Definition

Namespace: `DataStandardizer.Chronology`

The root abstraction for any encoding of an instant. It exposes a single Julian
Day Number, giving every implementation a common continuous-time representation
that conversions and calculations can build on.

```csharp
public interface ISystemTime
```

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `JulianDayNumber` | `decimal JulianDayNumber { get; }` | A continuous count of days since the start of the Julian Period (1 January 4713 BCE, proleptic Julian calendar). |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The system-time model](../concepts/system-time-model.md)
- [ISystemTimeWithDate](ISystemTimeWithDate.md)
- [ISystemTimeWithTime](ISystemTimeWithTime.md)
- [ISystemTimeWithDateTime](ISystemTimeWithDateTime.md)
- [Chronology API reference](index.md)
