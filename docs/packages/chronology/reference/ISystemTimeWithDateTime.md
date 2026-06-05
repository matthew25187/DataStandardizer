---
title: ISystemTimeWithDateTime Interface
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# ISystemTimeWithDateTime Interface

## Definition

Namespace: `DataStandardizer.Chronology`

Combines [ISystemTimeWithDate](ISystemTimeWithDate.md) and
[ISystemTimeWithTime](ISystemTimeWithTime.md) into a single abstraction exposing
both date and time components of an instant.

```csharp
public interface ISystemTimeWithDateTime : ISystemTimeWithDate, ISystemTimeWithTime
```

## Remarks

This interface adds no members of its own; it merely unifies the date and time
interfaces. The inherited members are `Day`, `Month`, `Year` (from
`ISystemTimeWithDate`), `Hour`, `Minute`, `Second` (from `ISystemTimeWithTime`),
and `JulianDayNumber` (from `ISystemTime`).

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The system-time model](../concepts/system-time-model.md)
- [ISystemTime](ISystemTime.md)
- [ISystemTimeWithDate](ISystemTimeWithDate.md)
- [ISystemTimeWithTime](ISystemTimeWithTime.md)
- [SystemTimeWithGregorianCalendar](SystemTimeWithGregorianCalendar.md)
- [Chronology API reference](index.md)
