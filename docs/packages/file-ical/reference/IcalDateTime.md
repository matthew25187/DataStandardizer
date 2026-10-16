---
title: IcalDateTime Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalDateTime Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

A DATE-TIME value (RFC 5545 §3.3.5): a date and a time of day, which is floating, in
UTC, or in a named time zone. It is made of an [IcalDate](IcalDate.md) and an
[IcalTime](IcalTime.md), plus the time zone identifier of a zoned value.

**Syntax**

```csharp
public readonly struct IcalDateTime : IEquatable<IcalDateTime>, ISpanFormattable
```

## Remarks

**Kinds.** [Kind](IcalDateTimeKind.md) says which form the value has:

- `Floating`: no time zone, such as `19980118T230000`.
- `Utc`: the time is in UTC, such as `19980119T070000Z`.
- `Zoned`: a local time in the zone named by the property's `TZID` parameter.

A zoned value carries the time zone identifier from its `TZID` parameter. The
identifier is not part of the value's lexical form. You supply it to the `Parse` and
`TryParse` overloads that take `timeZoneId`, and `TryFormat` and `ToString` don't
write it. RFC 5545 §3.2.19 says `TZID` must not be applied to a time in UTC, so a
zoned value always has a floating `Time`.

The default value is 0001-01-01 at midnight, floating.

**Forms.** A date-time is parsed from and formatted in one of the forms of
[IcalValueFormat](IcalValueFormat.md):

| Form | Example | Standard |
| --- | --- | --- |
| `Text` | `19980118T230000`, `19980119T070000Z` | RFC 5545 §3.3.5 |
| `Json` | `1998-01-18T23:00:00`, `1998-01-19T07:00:00Z` | RFC 7265 §3.6.5 |
| `Xml` | `1998-01-18T23:00:00`, `1998-01-19T07:00:00Z` | RFC 6321 §3.6.5 |

The `Json` and `Xml` forms are the same. Parsing needs exactly the whole value, with
a real date and no fractional seconds. The `T` separator and `Z` suffix may be upper
or lower case; they are always written in upper case.

**Equality.** Two values are equal when they have the same date, time and kind, and,
for zoned values, the same time zone identifier. Identifiers are compared ordinally,
so identifiers that differ only in case are different zones. A floating value never
equals a value in UTC. The type has no ordering: it does not implement
`IComparable<T>` and has no `<` or `>` operators, because comparing values of
different kinds or zones needs a time zone resolver. See
[Time zone resolution](../concepts/time-zone-resolution.md).

**Leap seconds.** A time with a second of 60 can be parsed and held, but
`ToDateTime` throws `InvalidOperationException` for it, because `DateTime` cannot
represent it.

**Parsing from a string.** `Parse` and `TryParse` take a `ReadOnlySpan<char>`; there
are no `string` overloads. A `string` converts to `ReadOnlySpan<char>` implicitly on
.NET Core 2.1 and later and on .NET Standard 2.1. On .NET Framework, call
`AsSpan()` on the string. The type does not implement `IParsable<T>` or
`ISpanParsable<T>`, because parsing needs an `IcalValueFormat`.

**Formatting.** On the `net8.0` and `net10.0` builds the type implements
`ISpanFormattable`. An empty format string gives the `Text` form; `T`, `J` and `X`
choose a form, in either case.

**Allocation.** `TryParse` from a span does not allocate, including the overload
that takes a time zone identifier, which keeps a reference to the string it is given.
`TryFormat` into a span does not allocate. Both are covered by
`IcalValueAllocationTests`. `ToString` allocates a small buffer and the result
string.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalDateTime(IcalDate date, IcalTime time)` | Creates a floating date-time, or one in UTC if `time.IsUtc` is `true`. |
| `IcalDateTime(IcalDate date, IcalTime time, string timeZoneId)` | Creates a zoned date-time. Throws `ArgumentNullException` if `timeZoneId` is `null`, or `ArgumentException` if `timeZoneId` is empty or `time` is in UTC. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Date` | `IcalDate Date { get; }` | The date. |
| `Kind` | `IcalDateTimeKind Kind { get; }` | `Zoned` if there is a time zone identifier, `Utc` if the time is in UTC, otherwise `Floating`. |
| `Time` | `IcalTime Time { get; }` | The time of day. Its `IsUtc` is `true` only when `Kind` is `Utc`. |
| `TimeZoneId` | `string? TimeZoneId { get; }` | The time zone identifier of a zoned value; or `null` if the value is floating or in UTC. |

## Methods

### Parsing

| Method | Returns | Notes |
| --- | --- | --- |
| `Parse(ReadOnlySpan<char> value, IcalValueFormat format)` | `IcalDateTime` | Static. Parses a floating date-time or one in UTC. Throws `FormatException` if `value` is not a valid date-time in `format`, or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `Parse(ReadOnlySpan<char> value, string? timeZoneId, IcalValueFormat format)` | `IcalDateTime` | Static. Parses a zoned date-time when `timeZoneId` is not `null`; otherwise as above. Throws `FormatException` if `value` is not valid, or is in UTC and `timeZoneId` is given; `ArgumentException` if `timeZoneId` is empty; or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalDateTime result)` | `bool` | Static. `false` if `value` is not a valid date-time in `format`. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. Does not allocate. |
| `TryParse(ReadOnlySpan<char> value, string? timeZoneId, IcalValueFormat format, out IcalDateTime result)` | `bool` | Static. As above, giving a zoned value when `timeZoneId` is not `null`. `false` if `value` is in UTC and `timeZoneId` is given. Throws `ArgumentException` if `timeZoneId` is empty, or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. Does not allocate. |

### Formatting

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | The `Text` form, such as `19980119T070000Z`. The time zone identifier is not written. |
| `ToString(IcalValueFormat format)` | `string` | The date-time in `format`, without the time zone identifier. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Writes the date-time in `format`, without the time zone identifier, which belongs in a `TZID` parameter. Returns `false`, with `charsWritten` 0, if `destination` is too short (up to 16 characters for `Text`, 20 for `Json` and `Xml`). Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. Does not allocate. |

### Conversion

| Method | Returns | Notes |
| --- | --- | --- |
| `FromDateTime(DateTime value)` | `IcalDateTime` | Static. A date-time in UTC if `value.Kind` is `DateTimeKind.Utc`, otherwise a floating one. Fractions of a second are dropped. |
| `FromDateTime(DateTime value, string timeZoneId)` | `IcalDateTime` | Static. A zoned date-time from the wall-clock time of `value`. Fractions of a second and `value.Kind` are ignored. Throws `ArgumentNullException` if `timeZoneId` is `null`, or `ArgumentException` if it is empty. |
| `ToDateTime()` | `DateTime` | Of kind `DateTimeKind.Utc` for a value in UTC; otherwise of kind `DateTimeKind.Unspecified` holding the wall-clock time. A zoned value is not converted to UTC. Throws `InvalidOperationException` if the second is 60. |

### Equality

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(IcalDateTime other)` | `bool` | `true` for the same date, time and kind, and for zoned values the same time zone identifier, compared ordinally. |
| `Equals(object? obj)` | `bool` | `true` when `obj` is an equal `IcalDateTime`. |
| `GetHashCode()` | `int` | Consistent with `Equals`. |

### Explicit interface implementations *(.NET)*

| Member | Returns | Notes |
| --- | --- | --- |
| `ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)` | `bool` | Calls `TryFormat` with the form named by `format`: empty or `T` for `Text`, `J` for `Json`, `X` for `Xml`, in either case. `provider` is ignored. Throws `FormatException` for any other format string. |
| `IFormattable.ToString(string? format, IFormatProvider? formatProvider)` | `string` | Calls `ToString(IcalValueFormat)` with the form named by `format`, as above. Throws `FormatException` for any other format string. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `==(IcalDateTime left, IcalDateTime right)` | `bool` | Same as `left.Equals(right)`. |
| `!=(IcalDateTime left, IcalDateTime right)` | `bool` | Same as `!left.Equals(right)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
`ISpanFormattable` and its explicit members are implemented on the `net8.0` and
`net10.0` builds only. The nullable reference annotations, such as `string?` on
`TimeZoneId` and on the `timeZoneId` parameters, apply to the `netstandard2.1`,
`net8.0`, and `net10.0` builds.

## See also

- [Time zone resolution](../concepts/time-zone-resolution.md)
- [Expand recurrences](../how-to/expand-recurrences.md)
- [IcalDate](IcalDate.md)
- [IcalTime](IcalTime.md)
- [IcalDateTimeKind](IcalDateTimeKind.md)
- [IcalDateOrDateTime](IcalDateOrDateTime.md)
- [IcalTimeZoneResolver](IcalTimeZoneResolver.md)
- [IcalValueFormat](IcalValueFormat.md)
- [File.iCal API reference](index.md)
