---
title: SystemTimeWithGregorianCalendar Struct
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# SystemTimeWithGregorianCalendar Struct

## Definition

Namespace: `DataStandardizer.Chronology`

A decorator that adds Gregorian calendar date and time components to any
[ISystemTime](ISystemTime.md). It derives the year, month, day, hour, minute,
and second from the wrapped value's Julian Day Number.

```csharp
public readonly struct SystemTimeWithGregorianCalendar : ISystemTimeWithDateTime
```

## Remarks

Construct it from any `ISystemTime` implementation, such as a
[UnixTime](UnixTime.md) or [DosDateTime](DosDateTime.md). The constructor throws
`ArgumentNullException` when the supplied system time is `null`. The
[SystemTimeExtensions](SystemTimeExtensions.md) `AsUnixTime()` and
`AsDosDateTime()` methods recover the original wrapped value when it was one of
those types.

Every member that reads the wrapped value's Julian Day Number (`JulianDayNumber`,
`Year`, `Month`, `Day`, `Hour`, `Minute` and `Second`) throws
`InvalidOperationException` when the wrapped value is a default instance that
holds no date, such as `default(DosDateTime)`.

A default instance, `default(SystemTimeWithGregorianCalendar)`, wraps no system
time. Those same members, and the `ToDateTime`, `ToDateOnly` and `ToTimeOnly`
extensions, throw `InvalidOperationException` for it. `Equals` (two default
instances are equal), `GetHashCode` and `ToString` don't throw, and
`AsUnixTime()` and `AsDosDateTime()` return `null`.

Two instances are equal when the system times they wrap are equal.
`GetHashCode` returns the wrapped system time's hash code, or 0 for a default
instance, and `ToString` returns the full name of the type.

## Constructors

| Constructor | Notes |
| --- | --- |
| `SystemTimeWithGregorianCalendar(ISystemTime systemTime)` | Wraps an `ISystemTime`. Throws `ArgumentNullException` if `systemTime` is `null`. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Day` | `int Day { get; }` | The day of the month. |
| `Hour` | `int Hour { get; }` | The hour component. |
| `JulianDayNumber` | `decimal JulianDayNumber { get; }` | The wrapped value's Julian Day Number. |
| `Minute` | `int Minute { get; }` | The minute component. |
| `Month` | `int Month { get; }` | The month of the year. |
| `Second` | `int Second { get; }` | The second component. |
| `Year` | `int Year { get; }` | The year component. |

## Extension methods

The following extensions apply to `SystemTimeWithGregorianCalendar`:

- [`AsUnixTime()`](SystemTimeExtensions.md) &mdash; returns `UnixTime?` (non-null only if the wrapped value was a `UnixTime`).
- [`AsDosDateTime()`](SystemTimeExtensions.md) &mdash; returns `DosDateTime?` (non-null only if the wrapped value was a `DosDateTime`).

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, and `net10.0`.

## See also

- [The system-time model](../concepts/system-time-model.md)
- [ISystemTimeWithDateTime](ISystemTimeWithDateTime.md)
- [UnixTime](UnixTime.md)
- [DosDateTime](DosDateTime.md)
- [SystemTimeExtensions](SystemTimeExtensions.md)
- [Chronology API reference](index.md)
