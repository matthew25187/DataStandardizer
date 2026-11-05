---
title: VcardTime Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardTime Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

A TIME value (RFC 6350 §4.3.2): a time of day, which may leave out its hour, minute or second, with an optional zone.

**Syntax**

```csharp
public readonly struct VcardTime : IEquatable<VcardTime>, ISpanFormattable
```

## Remarks

| Version | Forms | Standard |
| --- | --- | --- |
| 4.0 | `102200`, `1022`, `10`, `-2200`, `-22`, `--00`, each with nothing, `Z` or an offset such as `-0800` | RFC 6350 §4.3.2 |
| 3.0 | `10:22:00`, `102200`, with single colons allowed, an optional fraction `10:22:00,5`, and an optional zone | RFC 2425 §5.8.4 |
| 2.1 | `10:22:00` or `102200`, with a zone in the same format | vCard 2.1 §2.6.2, §2.4.5 |

A second of 60 is accepted at any time of day, since the value doesn't say whether a leap second falls there. An hour of 24 is rejected: RFC 6350 §4.3.2 says midnight is always 00.

Two times are equal if they say the same thing the same way, not if they are the same instant: `10Z` and `10+00` differ. A fraction of more than 18 digits keeps its first 18.

**Versions.** Every parse and format method has an overload taking a [VcardVersion](VcardVersion.md). The overload without one uses vCard 4.0. `IsDefinedIn` says whether a version can write the value, and formatting one it can't throws `InvalidOperationException`.

**Forms.** Only `VcardValueFormat.Text` is implemented. `Json` and `Xml` throw `NotSupportedException` until the jCard and xCard work adds them.

**Parsing from a string.** `Parse` and `TryParse` take a `ReadOnlySpan<char>`. A `string` converts implicitly on .NET Core and .NET Standard 2.1. On .NET Framework, call `AsSpan()`.

**Allocation.** `TryParse` from a span and `TryFormat` into one do not allocate (covered by `VcardValueAllocationTests`).

On the `net10.0` build the type implements `ISpanFormattable`. An empty format string or `T` gives the `Text` form.

**Uninitialised instance.** A default instance throws `InvalidOperationException` from its properties and `IsDefinedIn`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardTime(int? hour, int? minute, int? second)` | No zone. A combination with no form, such as an hour and a second but no minute, throws `ArgumentException`. |
| `VcardTime(int? hour, int? minute, int? second, bool isUtc)` | `true` writes the zone as `Z`. |
| `VcardTime(int? hour, int? minute, int? second, VcardUtcOffset offset)` | A [VcardUtcOffset](VcardUtcOffset.md) as the zone. |

## Properties

| Property | Notes |
| --- | --- |
| `int? Hour`, `int? Minute`, `int? Second` | A part, or `null` if the time leaves it out. |
| `decimal? SecondFraction` | The fraction of a second that only vCard 3.0 has, or `null`. |
| `bool IsUtc` | Whether the zone is written `Z`. |
| `VcardUtcOffset? UtcOffset` | The zone if it is an offset, otherwise `null`. |

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
