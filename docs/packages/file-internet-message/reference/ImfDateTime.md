---
title: ImfDateTime Struct
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfDateTime Struct

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A date-time (RFC 5322 §3.3), as in a Date or Resent-Date header field or at the end of a Received header field, such as `Fri, 21 Nov 1997 09:55:06 -0600`.

**Syntax**

```csharp
public readonly struct ImfDateTime : IComparable<ImfDateTime>, IEquatable<ImfDateTime>
```

## Remarks

This keeps what `DateTimeOffset` can't represent: a leap second, whether a day of the week was given, a year after 9999, an offset of more than 14 hours, and the difference between `+0000` and `-0000`, which says that the local offset is unknown (RFC 5322 §3.3).

The obsolete syntax (RFC 5322 §4.3) is read: a two- or three-digit year, a zone name, and comments and white space between the parts. A zone name is read as its numeric zone, and a military zone as `-0000`, because its meaning was defined wrongly in RFC 822. The name itself is not kept. No other zone name is in the grammar, so §4.3's advice on them doesn't apply and they are rejected. A year too large for an `Int32` is also rejected.

`ToString` writes the modern form.

Two values are equal only if they would be written the same way, so `+0000` and `-0000` differ, and so do the same moment at two offsets. The ordering is by moment, with a leap second after the second before it and before the one after it. Values at the same moment are then ordered by offset, then with an unknown offset last, then with a day of the week last, so that only equal values compare as 0. A value made with `default` holds no date-time: it equals only another such value, and comes before every other value. Its other members throw `InvalidOperationException`, but for `ToString`, which gives the name of the type.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfDateTime(DateTimeOffset value)` | Initializes a new date-time from a `DateTimeOffset`, with its offset and with no day of the week given. Any fraction of a second is dropped, as RFC 5322 §3.3 has no form for it. An offset of zero is `+0000`, not `-0000`. **Throws** `ArgumentOutOfRangeException`: The year of `value` is before 1900 (RFC 5322 §3.3). |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Day` | `int Day { get; }` | Gets the day of the month, from 1. **Throws** `InvalidOperationException`: This value was made with `default`. |
| `DayOfWeek` | `DayOfWeek DayOfWeek { get; }` | Gets the day of the week implied by the date, whether or not one was given. **Throws** `InvalidOperationException`: This value was made with `default`. |
| `HasDayOfWeek` | `bool HasDayOfWeek { get; }` | Gets a flag indicating if a day of the week was given. **Throws** `InvalidOperationException`: This value was made with `default`. |
| `Hour` | `int Hour { get; }` | Gets the hour, from 0 to 23. **Throws** `InvalidOperationException`: This value was made with `default`. |
| `IsOffsetUnknown` | `bool IsOffsetUnknown { get; }` | Gets a flag indicating if the local offset is unknown, as for `-0000` and the military zones (RFC 5322 §3.3 and §4.3). **Throws** `InvalidOperationException`: This value was made with `default`. |
| `Minute` | `int Minute { get; }` | Gets the minute, from 0 to 59. **Throws** `InvalidOperationException`: This value was made with `default`. |
| `Month` | `int Month { get; }` | Gets the month, from 1 to 12. **Throws** `InvalidOperationException`: This value was made with `default`. |
| `Offset` | `TimeSpan Offset { get; }` | Gets the offset from Universal Time. If `IsOffsetUnknown` is `true`, this is zero, because `-0000` gives the date and time in Universal Time (RFC 5322 §3.3). It is then not the offset of the place the date-time came from, which is unknown, so check `IsOffsetUnknown` before using it as one. **Throws** `InvalidOperationException`: This value was made with `default`. |
| `Second` | `int Second { get; }` | Gets the second, from 0 to 60, where 60 is a leap second. It is 0 if the seconds were left out. **Throws** `InvalidOperationException`: This value was made with `default`. |
| `Year` | `int Year { get; }` | Gets the year, which is 1900 or later. **Throws** `InvalidOperationException`: This value was made with `default`. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `CompareTo(ImfDateTime other)` | `int` | Compares this date-time with another, by moment and then by the differences that make them unequal. **Returns:** A negative number if this one comes first, zero if they are equal, or a positive number if the other comes first. |
| `Equals(ImfDateTime other)` | `bool` | Indicates whether this date-time equals another: they would be written the same way. |
| `static Parse(string value)` | `ImfDateTime` | Parses a date-time. **Returns:** The date-time. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not a valid date-time. |
| `static Parse(ReadOnlySpan<byte> value)` | `ImfDateTime` | Parses a date-time. **Returns:** The date-time. **Throws** `FormatException`: The value is not a valid date-time. |
| `static Parse(string value, ImfOptions options)` | `ImfDateTime` | Parses a date-time. **Returns:** The date-time. **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `FormatException`: The value is not a valid date-time. |
| `static Parse(ReadOnlySpan<byte> value, ImfOptions options)` | `ImfDateTime` | Parses a date-time. **Returns:** The date-time. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `FormatException`: The value is not a valid date-time. |
| `ToDateTimeOffset()` | `DateTimeOffset` | Converts the date-time to a `DateTimeOffset`. A leap second becomes second 59 of the same minute, as `DateTimeOffset` has no leap seconds. **Returns:** The date and time, with their offset. **Throws** `InvalidOperationException`: The local offset is unknown, the date-time is outside what `DateTimeOffset` can represent, or this value was made with `default`. |
| `ToDateTimeOffset(bool treatUnknownOffsetAsUniversalTime)` | `DateTimeOffset` | Converts the date-time to a `DateTimeOffset`. A leap second becomes second 59 of the same minute, as `DateTimeOffset` has no leap seconds. **Returns:** The date and time, with their offset. **Throws** `InvalidOperationException`: The local offset is unknown and `treatUnknownOffsetAsUniversalTime` is `false`, the date-time is outside what `DateTimeOffset` can represent, or this value was made with `default`. |
| `ToString()` | `string` | Formats the date-time in the modern form, such as `Fri, 21 Nov 1997 09:55:06 -0600`, with a day of the week if one was given. **Returns:** The date-time, or the name of this type if this value was made with `default`. |
| `ToString(bool includeDayOfWeek)` | `string` | Formats the date-time in the modern form, such as `Fri, 21 Nov 1997 09:55:06 -0600`. **Returns:** The date-time, or the name of this type if this value was made with `default`. |
| `static TryParse(string? value, out ImfDateTime result)` | `bool` | Parses a date-time. **Returns:** `true` if the value is a valid date-time; otherwise `false`, including for `null`. |
| `static TryParse(ReadOnlySpan<byte> value, out ImfDateTime result)` | `bool` | Parses a date-time. **Returns:** `true` if the value is a valid date-time; otherwise `false`. |
| `static TryParse(string? value, ImfOptions options, out ImfDateTime result)` | `bool` | Parses a date-time. **Returns:** `true` if the value is a valid date-time; otherwise `false`, including for `null`. **Throws** `ArgumentNullException`: `options` is `null`. |
| `static TryParse(ReadOnlySpan<byte> value, ImfOptions options, out ImfDateTime result)` | `bool` | Parses a date-time. **Returns:** `true` if the value is a valid date-time; otherwise `false`. **Throws** `ArgumentNullException`: `options` is `null`. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `operator !=(ImfDateTime left, ImfDateTime right)` | `bool` | Indicates whether two date-times differ. |
| `operator <(ImfDateTime left, ImfDateTime right)` | `bool` | Indicates whether one date-time comes before another. |
| `operator <=(ImfDateTime left, ImfDateTime right)` | `bool` | Indicates whether one date-time comes before another or equals it. |
| `operator ==(ImfDateTime left, ImfDateTime right)` | `bool` | Indicates whether two date-times are equal. |
| `operator >(ImfDateTime left, ImfDateTime right)` | `bool` | Indicates whether one date-time comes after another. |
| `operator >=(ImfDateTime left, ImfDateTime right)` | `bool` | Indicates whether one date-time comes after another or equals it. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [File.InternetMessage API reference](index.md)
