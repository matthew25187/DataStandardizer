---
title: VcardUtcOffset Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardUtcOffset Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

A UTC-OFFSET value (RFC 6350 §4.7): a signed difference from UTC in hours and, optionally, minutes.

**Syntax**

```csharp
public readonly struct VcardUtcOffset : IEquatable<VcardUtcOffset>, ISpanFormattable
```

## Remarks

| Version | Forms | Standard |
| --- | --- | --- |
| 4.0 | `-05`, `-0500` | RFC 6350 §4 |
| 3.0 | `-05:00` only | RFC 2426 §2.4.4 |
| 2.1 | `-05`, `-0500`, `-05:00`, with hours up to 24 | vCard 2.1 §2.4.5 |

An offset keeps what was written: `-05` and `-0500` are different values with the same `Offset`. RFC 6350 doesn't forbid a negative zero, so `-00` is valid and differs from `+00`. vCard 3.0 always writes the minutes.

**Versions.** Every parse and format method has an overload taking a [VcardVersion](VcardVersion.md). The overload without one uses vCard 4.0. `IsDefinedIn` says whether a version can write the value, and formatting one it can't throws `InvalidOperationException`.

**Forms.** Only `VcardValueFormat.Text` is implemented. `Json` and `Xml` throw `NotSupportedException` until the jCard and xCard work adds them.

**Parsing from a string.** `Parse` and `TryParse` take a `ReadOnlySpan<char>`. A `string` converts implicitly on .NET Core and .NET Standard 2.1. On .NET Framework, call `AsSpan()`.

**Allocation.** `TryParse` from a span and `TryFormat` into one do not allocate (covered by `VcardValueAllocationTests`).

On the `net10.0` build the type implements `ISpanFormattable`. An empty format string or `T` gives the `Text` form.

**Uninitialised instance.** A default instance throws `InvalidOperationException` from its properties and `IsDefinedIn`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardUtcOffset(bool isNegative, int hours, int? minutes)` | Hours from 0 to 23 and minutes from 0 to 59, or `null` to write only the hours. |

## Properties

| Property | Notes |
| --- | --- |
| `bool IsNegative`, `int Hours`, `int? Minutes` | The parts. |
| `TimeSpan Offset` | The offset. Positive offsets are ahead of UTC. |

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
