---
title: IcalTime Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalTime Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

A TIME value (RFC 5545 §3.3.12): a time of day to the second, either floating or in
UTC. It is the time part of an [IcalDateTime](IcalDateTime.md).

**Syntax**

```csharp
public readonly struct IcalTime : IEquatable<IcalTime>, ISpanFormattable
```

## Remarks

A second of 60 is allowed, for a positive leap second. The default value is
midnight, floating.

A time carries no time zone. A zoned [IcalDateTime](IcalDateTime.md) holds a
floating `IcalTime` and keeps the time zone identifier itself.

**Forms.** A time is parsed from and formatted in one of the forms of
[IcalValueFormat](IcalValueFormat.md):

| Form | Example | Standard |
| --- | --- | --- |
| `Text` | `230000`, `070000Z` | RFC 5545 §3.3.12 |
| `Json` | `23:00:00`, `07:00:00Z` | RFC 7265 §3.6.12 |
| `Xml` | `23:00:00`, `07:00:00Z` | RFC 6321 §3.6.12 |

The `Json` and `Xml` forms are the same. Parsing needs exactly the whole value, with
no fractional seconds. The `Z` suffix may be upper or lower case; `Z` is always
written in upper case.

**Equality.** A floating time never equals a time in UTC, even with the same hour,
minute and second. The type has no ordering: it does not implement `IComparable<T>`
and has no `<` or `>` operators.

**Parsing from a string.** `Parse` and `TryParse` take a `ReadOnlySpan<char>`; there
are no `string` overloads. A `string` converts to `ReadOnlySpan<char>` implicitly on
.NET Core 2.1 and later and on .NET Standard 2.1. On .NET Framework, call
`AsSpan()` on the string. The type does not implement `IParsable<T>` or
`ISpanParsable<T>`, because parsing needs an `IcalValueFormat`.

**Formatting.** On the `net8.0` and `net10.0` builds the type implements
`ISpanFormattable`. An empty format string gives the `Text` form; `T`, `J` and `X`
choose a form, in either case.

**Allocation.** `TryParse` from a span does not allocate (covered by
`IcalValueAllocationTests`). `TryFormat` writes straight into the destination span.
`ToString` allocates a small buffer and the result string.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalTime(int hour, int minute, int second)` | Creates a floating time. Throws `ArgumentOutOfRangeException` if `hour` is not 0 to 23, `minute` is not 0 to 59, or `second` is not 0 to 60. |
| `IcalTime(int hour, int minute, int second, bool isUtc)` | Creates a time in UTC when `isUtc` is `true`, otherwise a floating time. Throws `ArgumentOutOfRangeException` as above. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Hour` | `int Hour { get; }` | The hour, from 0 to 23. |
| `IsUtc` | `bool IsUtc { get; }` | `true` if the time is in UTC; `false` if it is floating. |
| `Minute` | `int Minute { get; }` | The minute, from 0 to 59. |
| `Second` | `int Second { get; }` | The second, from 0 to 60. 60 is a positive leap second. |

## Methods

### Parsing

| Method | Returns | Notes |
| --- | --- | --- |
| `Parse(ReadOnlySpan<char> value, IcalValueFormat format)` | `IcalTime` | Static. Throws `FormatException` if `value` is not a valid time in `format`, or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalTime result)` | `bool` | Static. `false` if `value` is not a valid time in `format`. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. Does not allocate. |

### Formatting

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | The `Text` form, such as `070000Z`. |
| `ToString(IcalValueFormat format)` | `string` | The time in `format`. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Writes the time in `format`. Returns `false`, with `charsWritten` 0, if `destination` is too short (up to 7 characters for `Text`, 9 for `Json` and `Xml`). Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |

### Equality

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(IcalTime other)` | `bool` | `true` for the same hour, minute, second and `IsUtc`. |
| `Equals(object? obj)` | `bool` | `true` when `obj` is an equal `IcalTime`. |
| `GetHashCode()` | `int` | Consistent with `Equals`. |

### Explicit interface implementations *(.NET)*

| Member | Returns | Notes |
| --- | --- | --- |
| `ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)` | `bool` | Calls `TryFormat` with the form named by `format`: empty or `T` for `Text`, `J` for `Json`, `X` for `Xml`, in either case. `provider` is ignored. Throws `FormatException` for any other format string. |
| `IFormattable.ToString(string? format, IFormatProvider? formatProvider)` | `string` | Calls `ToString(IcalValueFormat)` with the form named by `format`, as above. Throws `FormatException` for any other format string. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `==(IcalTime left, IcalTime right)` | `bool` | Same as `left.Equals(right)`. |
| `!=(IcalTime left, IcalTime right)` | `bool` | Same as `!left.Equals(right)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
`ISpanFormattable` and its explicit members are implemented on the `net8.0` and
`net10.0` builds only. The nullable reference annotations apply to the
`netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Standards compliance](../concepts/standards-compliance.md)
- [IcalDateTime](IcalDateTime.md)
- [IcalDateTimeKind](IcalDateTimeKind.md)
- [IcalValueFormat](IcalValueFormat.md)
- [File.iCal API reference](index.md)
