---
title: Use Unix time
parent: Chronology
grand_parent: Packages
nav_order: 3
---

# Use Unix time

Unix time is the means by which time is represented in the Unix operating system.
It measures the number of seconds elapsed since the Unix epoch, 1 January 1970.
It has since become widely used in many other contexts, and the
*DataStandardizer.Chronology* package lets you work with it in your own .NET
applications.

A Unix time value is represented by the `UnixTime` type. Internally the time is
stored as a signed 64-bit integer. To create a value, use the constructor:

```csharp
// Unix time for 1 January 2000 15:00
var unixTime = new UnixTime(946738800L);
```

Because the type defines implicit conversion operators, you can also cast an
integer to `UnixTime`:

```csharp
// Unix time for 1 January 2000 15:00 by casting
var unixTime = (UnixTime)946738800L;
```

## Convert to standard .NET date & time types

With a `UnixTime` value you can convert to the standard .NET types:

```csharp
var unixTime = new UnixTime(946738800L);

// Convert to a DateTime.
var unixTimeAsDateTime = unixTime.ToDateTime();

// Convert to a DateOnly.
var unixTimeAsDateOnly = unixTime.ToDateOnly();

// Convert to a TimeOnly.
var unixTimeAsTimeOnly = unixTime.ToTimeOnly();
```

Conversions also go the other way — starting from a standard .NET date or time
type, convert to Unix time:

```csharp
// Convert DateTime to UnixTime.
var dateTime = new DateTime(2000, 1, 1, 15, 0, 0);
var unixTime = dateTime.ToUnixTime();
```

## Add date & time semantics

By itself, a `UnixTime` instance carries no calendar semantics. You can add date
& time capabilities by wrapping it in a `SystemTimeWithGregorianCalendar`
decorator:

```csharp
// Unix time with date & time
var unixTimeWithDateTime = new SystemTimeWithGregorianCalendar(unixTime);
var year = unixTimeWithDateTime.Year;
var month = unixTimeWithDateTime.Month;
var day = unixTimeWithDateTime.Day;
var hour = unixTimeWithDateTime.Hour;
var minute = unixTimeWithDateTime.Minute;
var second = unixTimeWithDateTime.Second;
```

Because both `UnixTime` and `SystemTimeWithGregorianCalendar` implement
`ISystemTime`, you can use either interchangeably by typing your variable as
`ISystemTime`. See [The system-time model](../concepts/system-time-model.md)
for more.

From a decorated value you can get the original Unix time back:

```csharp
// Get back Unix time from the decorated value.
var unixTime = unixTimeWithDateTime.AsUnixTime();
```
