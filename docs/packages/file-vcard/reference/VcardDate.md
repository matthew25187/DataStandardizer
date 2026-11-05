---
title: VcardDate Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardDate Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

A DATE value (RFC 6350 §4.3.1): a calendar date, which may leave out its year, month or day.

**Syntax**

```csharp
public readonly struct VcardDate : IEquatable<VcardDate>, ISpanFormattable
```

## Remarks

| Version | Forms | Standard |
| --- | --- | --- |
| 4.0 | `19850412`, `1985-04`, `1985`, `--0412`, `--04`, `---12` | RFC 6350 §4.3.1 |
| 3.0 | `1996-04-15`, `19960415`, and with one hyphen | RFC 2425 §5.8.4 |
| 2.1 | `1995-04-15`, `19950415` | vCard 2.1 §2.2.4 |

`1985-04-12` is not a vCard 4.0 date, and a reduced date is not a vCard 3.0 or 2.1 date. Each version is held to its own grammar. vCard 3.0 and 2.1 write the extended format.

The year runs from 0 to 9999, so a date can't always become a `DateTime`. A date with no year allows 29 February, and year 0 is a leap year, as in ISO 8601.

**Versions.** Every parse and format method has an overload taking a [VcardVersion](VcardVersion.md). The overload without one uses vCard 4.0. `IsDefinedIn` says whether a version can write the value, and formatting one it can't throws `InvalidOperationException`.

**Forms.** Only `VcardValueFormat.Text` is implemented. `Json` and `Xml` throw `NotSupportedException` until the jCard and xCard work adds them.

**Parsing from a string.** `Parse` and `TryParse` take a `ReadOnlySpan<char>`. A `string` converts implicitly on .NET Core and .NET Standard 2.1. On .NET Framework, call `AsSpan()`.

**Allocation.** `TryParse` from a span and `TryFormat` into one do not allocate (covered by `VcardValueAllocationTests`).

On the `net10.0` build the type implements `ISpanFormattable`. An empty format string or `T` gives the `Text` form.

**Uninitialised instance.** A default instance throws `InvalidOperationException` from `Year`, `Month`, `Day`, `IsComplete` and `IsDefinedIn`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors and factories

| Member | Notes |
| --- | --- |
| `VcardDate(int year, int month, int day)` | A complete date. |
| `FromYear`, `FromYearAndMonth`, `FromMonth`, `FromMonthAndDay`, `FromDay` | The other forms. An out-of-range part throws `ArgumentOutOfRangeException`. |

## Properties

| Property | Notes |
| --- | --- |
| `int? Year`, `int? Month`, `int? Day` | A part, or `null` if the date leaves it out. |
| `bool IsComplete` | Whether it has a year, a month and a day. |

## Methods

| Method | Notes |
| --- | --- |
| `TryParse(value, format, out result)` | Parses a vCard 4.0 value. Returns `false` if it isn't valid. |
| `TryParse(value, format, version, out result)` | Parses a value by the grammar of a version. |
| `Parse(value, format)`, `Parse(value, format, version)` | As `TryParse`, and throws `FormatException` if the value isn't valid. |
| `TryFormat(destination, out charsWritten, format)`, `TryFormat(destination, out charsWritten, format, version)` | Writes the value. Returns `false` if the destination is too short. |
| `ToString(format)`, `ToString(format, version)` | Formats the value as a string. |
| `IsDefinedIn(version)` | Whether a version can write this value. |

See the [API reference](index.md).
