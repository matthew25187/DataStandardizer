---
title: IcalPeriod Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalPeriod Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

A PERIOD value (RFC 5545 §3.3.9): a start and either an end or a positive duration,
such as `19970101T180000Z/19970102T070000Z` or `19970101T180000Z/PT5H30M`.
[IcalFreeBusy.Periods](IcalFreeBusy.md) holds values of this type.

**Syntax**

```csharp
public readonly struct IcalPeriod : IEquatable<IcalPeriod>, ISpanFormattable
```

## Remarks

**Start, end and duration.** A period has a start ([IcalDateTime](IcalDateTime.md))
and either an end or a duration ([IcalDuration](IcalDuration.md)). `End` is `null`
when the period has a duration, and `Duration` is `null` when it has an end.

**Validity.** RFC 5545 §3.3.9 says the start must be before the end, and the
duration must be positive. The constructors and the parsers check this:

- An end is checked against the start only when both have the same kind and time
  zone, comparing their wall-clock fields. Values of different kinds or zones can't be
  compared without resolving the zone, so they are accepted.
- A duration must not be negative or zero (`PT0S`, `P0D` and `P0W` are all rejected).

The default value starts and ends at 0001-01-01 at midnight, floating, which is not a
valid period. It can be formatted, but not parsed back.

**Time zones.** A `TZID` parameter on the property applies to both the start and the
end. Pass it as `timeZoneId` when parsing; neither date-time may then be in UTC. The
time zone identifier is not written when formatting; it belongs in a `TZID`
parameter.

**Forms.** A period is parsed from and formatted in one of the forms of
[IcalValueFormat](IcalValueFormat.md). Every form joins the two parts with a solidus
(`/`). The date-times use the form's date-time syntax, and the duration is the same
in every form.

| Form | Example | Standard |
| --- | --- | --- |
| `Text` | `19970101T180000Z/PT5H30M` | RFC 5545 §3.3.9 |
| `Json` | `1997-01-01T18:00:00Z/PT5H30M` | RFC 7265 §3.6.9 |
| `Xml` | `1997-01-01T18:00:00Z/PT5H30M` | RFC 6321 §3.6.9 |

In a jCal document a period is a JSON array of two strings, and in an xCal document a
`period` element holding `start` and `end` or `duration` elements. The `Json` and
`Xml` forms are the two parts joined with a solidus; [JcalReader](JcalReader.md),
[JcalWriter](JcalWriter.md), [XcalReader](XcalReader.md) and
[XcalWriter](XcalWriter.md) map them to and from the array and the elements.

**Parsing from a string.** `Parse` and `TryParse` take a `ReadOnlySpan<char>`; there
are no `string` overloads. A `string` converts to `ReadOnlySpan<char>` implicitly on
.NET Core 2.1 and later and on .NET Standard 2.1. On .NET Framework, call
`AsSpan()` on the string. The type does not implement `IParsable<T>` or
`ISpanParsable<T>`, because parsing needs an `IcalValueFormat`.

**Formatting.** On the `net8.0` and `net10.0` builds the type implements
`ISpanFormattable`. An empty format string gives the `Text` form; `T`, `J` and `X`
choose a form, in either case.

**Allocation.** `TryParse` from a span and `TryFormat` into a span do not allocate
(covered by `IcalValueAllocationTests`). `ToString` allocates a small buffer and the
result string.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalPeriod(IcalDateTime start, IcalDateTime end)` | Creates a period with a start and an end. Throws `ArgumentException` if `end` has the same kind and time zone as `start` and is not later. |
| `IcalPeriod(IcalDateTime start, IcalDuration duration)` | Creates a period with a start and a duration. Throws `ArgumentException` if `duration` is zero or negative. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Duration` | `IcalDuration? Duration { get; }` | The duration; or `null` if the period has an end instead. |
| `End` | `IcalDateTime? End { get; }` | The end; or `null` if the period has a duration instead. |
| `Start` | `IcalDateTime Start { get; }` | The start. |

## Methods

### Parsing

| Method | Returns | Notes |
| --- | --- | --- |
| `Parse(ReadOnlySpan<char> value, IcalValueFormat format)` | `IcalPeriod` | Static. Parses a period whose date-times are floating or in UTC. Throws `FormatException` if `value` is not a valid period in `format`, or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `Parse(ReadOnlySpan<char> value, string? timeZoneId, IcalValueFormat format)` | `IcalPeriod` | Static. Parses a period in the named time zone when `timeZoneId` is not `null`. Throws `FormatException` if `value` is not a valid period (including a date-time in UTC when `timeZoneId` is given); `ArgumentException` if `timeZoneId` is empty; or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalPeriod result)` | `bool` | Static. `false` if `value` is not a valid period in `format`. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. Does not allocate. |
| `TryParse(ReadOnlySpan<char> value, string? timeZoneId, IcalValueFormat format, out IcalPeriod result)` | `bool` | Static. As above, in the named time zone when `timeZoneId` is not `null`. Throws `ArgumentException` if `timeZoneId` is empty, or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |

### Formatting

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | The `Text` form. The time zone identifier is not written. |
| `ToString(IcalValueFormat format)` | `string` | The period in `format`, without the time zone identifier. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Writes the period in `format`, without the time zone identifier. Returns `false`, with `charsWritten` 0, if `destination` is too short. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. Does not allocate. |

### Equality

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(IcalPeriod other)` | `bool` | `true` for equal starts and equal ends or durations. A period with an end never equals one with a duration. |
| `Equals(object? obj)` | `bool` | `true` when `obj` is an equal `IcalPeriod`. |
| `GetHashCode()` | `int` | Consistent with `Equals`. |

### Explicit interface implementations *(.NET)*

| Member | Returns | Notes |
| --- | --- | --- |
| `ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)` | `bool` | Calls `TryFormat` with the form named by `format`: empty or `T` for `Text`, `J` for `Json`, `X` for `Xml`, in either case. `provider` is ignored. Throws `FormatException` for any other format string. |
| `IFormattable.ToString(string? format, IFormatProvider? formatProvider)` | `string` | Calls `ToString(IcalValueFormat)` with the form named by `format`, as above. Throws `FormatException` for any other format string. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `==(IcalPeriod left, IcalPeriod right)` | `bool` | Same as `left.Equals(right)`. |
| `!=(IcalPeriod left, IcalPeriod right)` | `bool` | Same as `!left.Equals(right)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
`ISpanFormattable` and its explicit members are implemented on the `net8.0` and
`net10.0` builds only. The nullable reference annotations, such as `string?` on the
`timeZoneId` parameters, apply to the `netstandard2.1`, `net8.0`, and `net10.0`
builds. `JcalReader` and `JcalWriter` are not available on `netstandard1.1`, but the
`Json` form of this type is.

## See also

- [Read and write jCal](../how-to/read-write-jcal.md)
- [Read and write xCal](../how-to/read-write-xcal.md)
- [IcalDateTime](IcalDateTime.md)
- [IcalDuration](IcalDuration.md)
- [IcalFreeBusy](IcalFreeBusy.md)
- [IcalValueFormat](IcalValueFormat.md)
- [File.iCal API reference](index.md)
