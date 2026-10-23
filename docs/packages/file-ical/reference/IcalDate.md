---
title: IcalDate Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalDate Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

A DATE value (RFC 5545 §3.3.4): a calendar date with no time of day. It is the date
part of an [IcalDateTime](IcalDateTime.md), and one of the two forms of an
[IcalDateOrDateTime](IcalDateOrDateTime.md).

**Syntax**

```csharp
public readonly struct IcalDate : IEquatable<IcalDate>, IComparable<IcalDate>, ISpanFormattable
```

## Remarks

The year runs from 1 to 9999, the range of `DateTime`. The default value is
0001-01-01, which is a valid date.

**Forms.** A date is parsed from and formatted in one of the forms of
[IcalValueFormat](IcalValueFormat.md):

| Form | Example | Standard |
| --- | --- | --- |
| `Text` | `19970714` | RFC 5545 §3.3.4 |
| `Json` | `1997-07-14` | RFC 7265 §3.6.4 |
| `Xml` | `1997-07-14` | RFC 6321 §3.6.4 |

The `Json` and `Xml` forms are the same. Parsing needs exactly the whole value: no
white space, no time part, and a real date (`20260230` is rejected).

**Parsing from a string.** `Parse` and `TryParse` take a `ReadOnlySpan<char>`; there
are no `string` overloads. A `string` converts to `ReadOnlySpan<char>` implicitly on
.NET Core 2.1 and later and on .NET Standard 2.1. On .NET Framework, call
`AsSpan()` on the string. The type does not implement `IParsable<T>` or
`ISpanParsable<T>`, because parsing needs an `IcalValueFormat`.

**Formatting.** On the `net8.0` and `net10.0` builds the type implements
`ISpanFormattable`, so it can be used in interpolated strings and with
`string.Format`. An empty format string gives the `Text` form; `T`, `J` and `X`
choose a form, in either case. See the explicit interface implementations under
Methods.

**Allocation.** `TryParse` from a span does not allocate (covered by
`IcalValueAllocationTests`). `TryFormat` writes straight into the destination span.
`ToString` allocates a small buffer and the result string.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalDate(int year, int month, int day)` | Creates a date. Throws `ArgumentOutOfRangeException` if `year` is not 1 to 9999, `month` is not 1 to 12, or `day` is not 1 to the number of days in the month. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Day` | `int Day { get; }` | The day of the month. |
| `DayOfWeek` | `DayOfWeek DayOfWeek { get; }` | The day of the week. |
| `Month` | `int Month { get; }` | The month, from 1 to 12. |
| `Year` | `int Year { get; }` | The year, from 1 to 9999. |

## Methods

### Parsing

| Method | Returns | Notes |
| --- | --- | --- |
| `Parse(ReadOnlySpan<char> value, IcalValueFormat format)` | `IcalDate` | Static. Throws `FormatException` if `value` is not a valid date in `format`, or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalDate result)` | `bool` | Static. `false` if `value` is not a valid date in `format`. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. Does not allocate. |

### Formatting

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | The `Text` form, such as `19970714`. |
| `ToString(IcalValueFormat format)` | `string` | The date in `format`. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Writes the date in `format`. Returns `false`, with `charsWritten` 0, if `destination` is too short (8 characters for `Text`, 10 for `Json` and `Xml`). Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |

### Conversion

| Method | Returns | Notes |
| --- | --- | --- |
| `FromDateTime(DateTime value)` | `IcalDate` | Static. The date part of `value`. Its time of day and `Kind` are ignored. |
| `ToDateTime()` | `DateTime` | Midnight on the date, of kind `DateTimeKind.Unspecified`. |

### Equality and comparison

| Method | Returns | Notes |
| --- | --- | --- |
| `CompareTo(IcalDate other)` | `int` | Less than zero if this date is earlier, zero if the same, greater than zero if later. |
| `Equals(IcalDate other)` | `bool` | `true` for the same date. |
| `Equals(object? obj)` | `bool` | `true` when `obj` is an equal `IcalDate`. |
| `GetHashCode()` | `int` | Consistent with `Equals`. |

### Explicit interface implementations *(.NET)*

| Member | Returns | Notes |
| --- | --- | --- |
| `ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)` | `bool` | Calls `TryFormat` with the form named by `format`: empty or `T` for `Text`, `J` for `Json`, `X` for `Xml`, in either case. `provider` is ignored. Throws `FormatException` for any other format string. |
| `IFormattable.ToString(string? format, IFormatProvider? formatProvider)` | `string` | Calls `ToString(IcalValueFormat)` with the form named by `format`, as above. Throws `FormatException` for any other format string. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `==(IcalDate left, IcalDate right)` | `bool` | Same as `left.Equals(right)`. |
| `!=(IcalDate left, IcalDate right)` | `bool` | Same as `!left.Equals(right)`. |
| `<(IcalDate left, IcalDate right)` | `bool` | `true` if `left` is earlier. |
| `<=(IcalDate left, IcalDate right)` | `bool` | `true` if `left` is no later. |
| `>(IcalDate left, IcalDate right)` | `bool` | `true` if `left` is later. |
| `>=(IcalDate left, IcalDate right)` | `bool` | `true` if `left` is no earlier. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
`ISpanFormattable` and its explicit members are implemented on the `net8.0` and
`net10.0` builds only. The nullable reference annotations apply to the
`netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Standards compliance](../concepts/standards-compliance.md)
- [Convert between formats](../how-to/convert-formats.md)
- [IcalDateTime](IcalDateTime.md)
- [IcalDateOrDateTime](IcalDateOrDateTime.md)
- [IcalValueFormat](IcalValueFormat.md)
- [File.iCal API reference](index.md)
