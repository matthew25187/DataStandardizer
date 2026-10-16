---
title: IcalDateOrDateTime Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalDateOrDateTime Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

The value of a property that may be either a DATE or a DATE-TIME (RFC 5545 §3.3.4
and §3.3.5), such as `DTSTART`, `DTEND` or `RECURRENCE-ID`. It holds either an
[IcalDate](IcalDate.md) or an [IcalDateTime](IcalDateTime.md).

**Syntax**

```csharp
public readonly struct IcalDateOrDateTime : IEquatable<IcalDateOrDateTime>
```

## Remarks

The property's `VALUE` parameter says which value type it holds, and a zoned
date-time takes its time zone from the `TZID` parameter. The typed accessors of the
components, such as [IcalEvent](IcalEvent.md), read and write both parameters, so
you rarely parse or format this type yourself.

Both `IcalDate` and `IcalDateTime` convert to `IcalDateOrDateTime` implicitly, so
you can assign either one to a property of this type.

The default value is the DATE 0001-01-01.

The type has no `Parse`, `TryParse` or `TryFormat` methods, and does not implement
`ISpanFormattable`. Parse or format the `IcalDate` or `IcalDateTime` it holds
instead.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalDateOrDateTime(IcalDate date)` | Creates a DATE value. |
| `IcalDateOrDateTime(IcalDateTime dateTime)` | Creates a DATE-TIME value. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Date` | `IcalDate Date { get; }` | The value itself if it is a DATE, or the date part of a DATE-TIME. |
| `DateTime` | `IcalDateTime DateTime { get; }` | The date-time. Throws `InvalidOperationException` if the value is a DATE. |
| `IsDate` | `bool IsDate { get; }` | `true` if the value is a DATE, with no time of day. |
| `ValueType` | `IcalValueType ValueType { get; }` | `IcalValueType.Date` or `IcalValueType.DateTime`. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(IcalDateOrDateTime other)` | `bool` | `true` when both are DATE values for the same date, or both are equal DATE-TIME values. A DATE never equals a DATE-TIME. |
| `Equals(object? obj)` | `bool` | `true` when `obj` is an equal `IcalDateOrDateTime`. |
| `GetHashCode()` | `int` | Consistent with `Equals`. |
| `ToString()` | `string` | The `Text` form, such as `19970714` or `19970714T133000Z`, without the time zone of a zoned date-time. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `implicit operator IcalDateOrDateTime(IcalDate date)` | `IcalDateOrDateTime` | Converts a date to a DATE value. |
| `implicit operator IcalDateOrDateTime(IcalDateTime dateTime)` | `IcalDateOrDateTime` | Converts a date-time to a DATE-TIME value. |
| `==(IcalDateOrDateTime left, IcalDateOrDateTime right)` | `bool` | Same as `left.Equals(right)`. |
| `!=(IcalDateOrDateTime left, IcalDateOrDateTime right)` | `bool` | Same as `!left.Equals(right)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1`, `net8.0`, and
`net10.0` builds.

## See also

- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [IcalDate](IcalDate.md)
- [IcalDateTime](IcalDateTime.md)
- [IcalValueType](IcalValueType.md)
- [IcalEvent](IcalEvent.md)
- [File.iCal API reference](index.md)
