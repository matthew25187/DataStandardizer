---
title: IcalDuration Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalDuration Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

A DURATION value (RFC 5545 §3.3.6): a number of weeks, or a number of days and an
exact time, such as `P15DT5H0M20S`, `-PT15M` or `P7W`.

**Syntax**

```csharp
public readonly struct IcalDuration : IEquatable<IcalDuration>, ISpanFormattable
```

## Remarks

**Nominal and exact parts.** Weeks and days are nominal: added to a date-time, they
move it by calendar days, which across a daylight saving change can be 23 or 25
hours. Hours, minutes and seconds are exact. So `P1D` and `PT24H` are different
durations, and this type keeps them apart: they are not equal. For the same reason a
duration in weeks (`P1W`) is not equal to the same number of days (`P7D`), although
the two have the same effect.

**Sign.** The whole duration has one sign, so the parts are all negative or all
positive. A zero duration is never negative, so `-PT0S` parses to the same value as
`PT0S`. A leading `+` is accepted when parsing and never written.

**Normal form.** The exact time is held as a number of seconds and written in hours,
minutes and seconds, so `PT90M` is written back as `PT1H30M`. A zero minute between
an hour and a second is written, as RFC 5545 requires (`PT1H0M5S`). The default value
is zero, written `PT0S`.

**Forms.** A duration has the same lexical form in every
[IcalValueFormat](IcalValueFormat.md): `Text` (RFC 5545 §3.3.6), `Json`
(RFC 7265 §3.6.6) and `Xml` (RFC 6321 §3.6.6). The `format` argument is checked but
does not change the result. Parsing follows the RFC 5545 grammar: the designator
letters may be upper or lower case, and the time parts must come in the order H, M,
S with none skipped after the first, so `PT1H20S` is rejected. They are always written
in upper case. White space is not trimmed, in any form.

**Range.** Weeks and days are each limited to `int.MaxValue`. The exact time is
limited to what a `TimeSpan` can hold.

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
| `IcalDuration(int days, TimeSpan time)` | Creates a duration of nominal days and an exact time. Throws `ArgumentException` if `days` and `time` have opposite signs, or `time` is not a whole number of seconds; or `ArgumentOutOfRangeException` if `days` is `int.MinValue`. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Days` | `int Days { get; }` | The number of nominal days, negative if the duration is negative. Does not include `Weeks`. |
| `IsNegative` | `bool IsNegative { get; }` | `true` if the duration is negative. Always `false` for a zero duration. |
| `IsWeeks` | `bool IsWeeks { get; }` | `true` if the duration is given in weeks. If so, `Days` and `Time` are zero. |
| `Time` | `TimeSpan Time { get; }` | The exact time, negative if the duration is negative. |
| `Weeks` | `int Weeks { get; }` | The number of nominal weeks, negative if the duration is negative. |

## Methods

### Parsing

| Method | Returns | Notes |
| --- | --- | --- |
| `Parse(ReadOnlySpan<char> value, IcalValueFormat format)` | `IcalDuration` | Static. Throws `FormatException` if `value` is not a valid duration, or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalDuration result)` | `bool` | Static. `false` if `value` is not a valid duration. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. Does not allocate. |

### Formatting

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | The `Text` form, such as `P15DT5H0M20S`. |
| `ToString(IcalValueFormat format)` | `string` | The duration, which is the same in every form. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Writes the duration. Returns `false`, with `charsWritten` 0, if `destination` is too short. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |

### Construction and arithmetic

| Method | Returns | Notes |
| --- | --- | --- |
| `FromWeeks(int weeks)` | `IcalDuration` | Static. A duration of nominal weeks. Throws `ArgumentOutOfRangeException` if `weeks` is `int.MinValue`. |
| `Negate()` | `IcalDuration` | The same duration with the opposite sign. A zero duration stays non-negative. |

### Equality

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(IcalDuration other)` | `bool` | `true` for the same sign, the same weeks, days and seconds, and the same `IsWeeks`. Nominal and exact parts are compared separately, so `P1D` does not equal `PT24H`, and `P1W` does not equal `P7D`. |
| `Equals(object? obj)` | `bool` | `true` when `obj` is an equal `IcalDuration`. |
| `GetHashCode()` | `int` | Consistent with `Equals`. |

### Explicit interface implementations *(.NET)*

| Member | Returns | Notes |
| --- | --- | --- |
| `ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)` | `bool` | Calls `TryFormat` with the form named by `format`: empty or `T` for `Text`, `J` for `Json`, `X` for `Xml`, in either case. `provider` is ignored. Throws `FormatException` for any other format string. |
| `IFormattable.ToString(string? format, IFormatProvider? formatProvider)` | `string` | Calls `ToString(IcalValueFormat)` with the form named by `format`, as above. Throws `FormatException` for any other format string. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `==(IcalDuration left, IcalDuration right)` | `bool` | Same as `left.Equals(right)`. |
| `!=(IcalDuration left, IcalDuration right)` | `bool` | Same as `!left.Equals(right)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
`ISpanFormattable` and its explicit members are implemented on the `net8.0` and
`net10.0` builds only. The nullable reference annotations apply to the
`netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Daylight saving rules](../concepts/dst-rules.md)
- [IcalPeriod](IcalPeriod.md)
- [IcalValueFormat](IcalValueFormat.md)
- [File.iCal API reference](index.md)
