---
title: IcalUtcOffset Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalUtcOffset Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

A UTC-OFFSET value (RFC 5545 §3.3.14): the difference of a local time from UTC, to
the second, such as `-0500` or `+013045`. It is used by the `TZOFFSETFROM` and
`TZOFFSETTO` properties of a time zone observance.

**Syntax**

```csharp
public readonly struct IcalUtcOffset : IEquatable<IcalUtcOffset>, ISpanFormattable
```

## Remarks

Positive offsets are ahead of UTC. An offset is a whole number of seconds less than a
day either side of zero. The default value is no offset, written `+0000`.

**Forms.** An offset is parsed from and formatted in one of the forms of
[IcalValueFormat](IcalValueFormat.md):

| Form | Example | Standard |
| --- | --- | --- |
| `Text` | `-0500`, `+013045` | RFC 5545 §3.3.14 |
| `Json` | `-05:00`, `+01:30:45` | RFC 7265 §3.6.14 |
| `Xml` | `-05:00`, `+01:30:45` | RFC 6321 §3.6.14 |

The `Json` and `Xml` forms are the same. The sign is required. Seconds are written
only when they are not zero. When parsing, the hour must be 0 to 23 and the minute
and second 0 to 59. RFC 5545 forbids a negative zero, so `-0000` and `-000000` (and
`-00:00` in the extended forms) are rejected.

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
| `IcalUtcOffset(TimeSpan offset)` | Creates an offset. Throws `ArgumentOutOfRangeException` if `offset` is a day or more either side of zero, or `ArgumentException` if it is not a whole number of seconds. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Offset` | `TimeSpan Offset { get; }` | The offset. Positive offsets are ahead of UTC. |

## Methods

### Parsing

| Method | Returns | Notes |
| --- | --- | --- |
| `Parse(ReadOnlySpan<char> value, IcalValueFormat format)` | `IcalUtcOffset` | Static. Throws `FormatException` if `value` is not a valid offset in `format`, or `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryParse(ReadOnlySpan<char> value, IcalValueFormat format, out IcalUtcOffset result)` | `bool` | Static. `false` if `value` is not a valid offset in `format`. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. Does not allocate. |

### Formatting

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | The `Text` form, such as `-0500`. |
| `ToString(IcalValueFormat format)` | `string` | The offset in `format`. Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |
| `TryFormat(Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Writes the offset in `format`. Returns `false`, with `charsWritten` 0, if `destination` is too short (up to 7 characters for `Text`, 9 for `Json` and `Xml`). Throws `ArgumentOutOfRangeException` if `format` is not an `IcalValueFormat` member. |

### Equality

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(IcalUtcOffset other)` | `bool` | `true` for the same offset. |
| `Equals(object? obj)` | `bool` | `true` when `obj` is an equal `IcalUtcOffset`. |
| `GetHashCode()` | `int` | Consistent with `Equals`. |

### Explicit interface implementations *(.NET)*

| Member | Returns | Notes |
| --- | --- | --- |
| `ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)` | `bool` | Calls `TryFormat` with the form named by `format`: empty or `T` for `Text`, `J` for `Json`, `X` for `Xml`, in either case. `provider` is ignored. Throws `FormatException` for any other format string. |
| `IFormattable.ToString(string? format, IFormatProvider? formatProvider)` | `string` | Calls `ToString(IcalValueFormat)` with the form named by `format`, as above. Throws `FormatException` for any other format string. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `==(IcalUtcOffset left, IcalUtcOffset right)` | `bool` | Same as `left.Equals(right)`. |
| `!=(IcalUtcOffset left, IcalUtcOffset right)` | `bool` | Same as `!left.Equals(right)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
`ISpanFormattable` and its explicit members are implemented on the `net8.0` and
`net10.0` builds only. The nullable reference annotations apply to the
`netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Daylight saving rules](../concepts/dst-rules.md)
- [IcalTimeZoneObservance](IcalTimeZoneObservance.md)
- [IcalTimeZoneTransition](IcalTimeZoneTransition.md)
- [IcalValueFormat](IcalValueFormat.md)
- [File.iCal API reference](index.md)
