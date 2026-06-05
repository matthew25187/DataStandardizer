---
title: Use DOS date/time
parent: Chronology
grand_parent: Packages
nav_order: 4
---

# Use DOS date/time

Date & time values for the MS-DOS operating system are a pair of packed 16-bit
integers representing the date and time of an event — typically a change to an
object in the file system. Valid values range from 1 January 1980 to
31 December 2107. You can read more about the format in
[Microsoft's documentation](https://learn.microsoft.com/en-us/windows/win32/sysinfo/ms-dos-date-and-time).

DOS date/time values are represented by the `DosDateTime` type. Internally the
value is stored as a packed 32-bit integer, which you can retrieve by casting a
`DosDateTime` to an unsigned 32-bit integer:

```csharp
// Retrieve the packed integer form of a DOS date/time.
var packedDateTime = (uint)dosDateTime;
```

## Create a DosDateTime

You can construct a `DosDateTime` in either of two ways. If you already have a
packed 32-bit integer, pass it to the constructor:

```csharp
// Create DosDateTime from a packed integer.
var dosDateTime = new DosDateTime(0x28217DA5);
```

Alternatively, construct it from the individual date and time components:

```csharp
// Create DosDateTime from date and time components.
var dosDateTime = new DosDateTime(1984, 2, 29, 12, 30, 0);
```

The component constructor also has a date-only form for when you have no time:

```csharp
// Create DosDateTime from date components only.
var dosDate = new DosDateTime(1984, 2, 29);
```

> Component values are validated. Year must be 1980–2107, month 1–12, day 1–31,
> hour 0–23, and minute and second 0–59; out-of-range values throw
> `ArgumentOutOfRangeException`.

## Convert to and from standard .NET types

```csharp
var dosDateTime = new DosDateTime(0x85D63C0);

// Convert DosDateTime to a date object.
var dosDateTimeDateOnly = dosDateTime.ToDateOnly();

// Convert DosDateTime to a date & time object.
var dosDateTimeDateTime = dosDateTime.ToDateTime();

// Convert DosDateTime to a time object.
var dosDateTimeTimeOnly = dosDateTime.ToTimeOnly();
```

Conversions also go the other way:

```csharp
var myDate = new DateOnly(1999, 12, 31);

// Convert to DosDateTime.
var dosDateTime = myDate.ToDosDateTime();
```

## Add date & time semantics

Because `DosDateTime` implements `ISystemTime`, you can wrap it in a
`SystemTimeWithGregorianCalendar` decorator to expose `Year`, `Month`, `Day`,
`Hour`, `Minute`, and `Second` properties. A wrapped value can be used
interchangeably with the original, and you can get the original back:

```csharp
// Getting back the original date/time.
var dosDateTime = decoratedDosDateTime.AsDosDateTime();
```

See [The system-time model](../concepts/system-time-model.md) for how the
decorator fits together.
