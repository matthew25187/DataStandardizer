---
title: The system-time model
parent: Chronology
grand_parent: Packages
nav_order: 10
---

# The system-time model

The Chronology package represents several different *encodings* of an instant —
Unix time and DOS date/time — and lets you convert between them and the standard
.NET date and time types. This is held together by a small set of interfaces and
a shared internal "currency" for points in time.

## The common currency: Julian Day Number

Every encoding ultimately expresses an instant as a **Julian Day Number (JDN)** —
a continuous count of days (with a fractional part for the time of day). Each
time type exposes it as a `decimal JulianDayNumber`. Because every encoding can
produce and consume a JDN, conversions between them are straightforward and
lossless within each format's supported range.

## The interface hierarchy

```text
ISystemTime                         // exposes JulianDayNumber
├── ISystemTimeWithDate             // + date semantics
├── ISystemTimeWithTime             // + time-of-day semantics
└── ISystemTimeWithDateTime         // both date and time
```

- **`ISystemTime`** is the root. Both `UnixTime` and `DosDateTime` implement it —
  they are *encodings* of an instant and nothing more.
- **`ISystemTimeWithDate` / `ISystemTimeWithTime` / `ISystemTimeWithDateTime`**
  add calendar semantics (year, month, day / hour, minute, second).

## Encodings vs. calendar semantics

`UnixTime` and `DosDateTime` deliberately carry **no calendar fields** on their
own — a `UnixTime` is just a 64-bit second count, a `DosDateTime` is just a
packed 32-bit value. To read a year or an hour, you wrap the encoding in the
`SystemTimeWithGregorianCalendar` **decorator**:

```csharp
ISystemTime instant = new UnixTime(946738800L);

var calendar = new SystemTimeWithGregorianCalendar(instant);
var year = calendar.Year;   // 2000
```

The decorator implements `ISystemTimeWithDateTime`, computing each component from
the underlying JDN using the Gregorian calendar. From a decorator you can recover
the original encoding with `AsUnixTime()` or `AsDosDateTime()` (each returns a
nullable value, non-null only when the wrapped instant was of that type).

## Why this design

- **One conversion path.** Every type converts through JDN, so adding a new
  encoding doesn't require N×N conversion methods.
- **Encoding and presentation are separate.** You only pay for calendar
  computation when you actually wrap a value for display.
- **Interchangeability.** Typing a variable as `ISystemTime` lets a single code
  path accept any encoding.

## Related

- [Use Unix time](../how-to/use-unix-time.md)
- [Use DOS date/time](../how-to/use-dos-datetime.md)
- [API reference](../reference/index.md)
