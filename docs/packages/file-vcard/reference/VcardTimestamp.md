---
title: VcardTimestamp Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardTimestamp Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

A TIMESTAMP value (RFC 6350 §4.3.5): a complete date and a complete time with an optional zone, such as `19961022T140000-05`. REV uses it.

**Syntax**

```csharp
public readonly struct VcardTimestamp : IEquatable<VcardTimestamp>, ISpanFormattable
```

## Remarks

vCard 3.0 and 2.1 have no TIMESTAMP type. Their `REV` property is a date-time, in the form [VcardDateTime](VcardDateTime.md) describes for them, and this type reads and writes that form when given one of those versions.

**Versions.** Every parse and format method has an overload taking a [VcardVersion](VcardVersion.md). The overload without one uses vCard 4.0. `IsDefinedIn` says whether a version can write the value, and formatting one it can't throws `InvalidOperationException`.

**Forms.** Only `VcardValueFormat.Text` is implemented. `Json` and `Xml` throw `NotSupportedException` until the jCard and xCard work adds them.

**Parsing from a string.** `Parse` and `TryParse` take a `ReadOnlySpan<char>`. A `string` converts implicitly on .NET Core and .NET Standard 2.1. On .NET Framework, call `AsSpan()`.

**Allocation.** `TryParse` from a span and `TryFormat` into one do not allocate (covered by `VcardValueAllocationTests`).

On the `net10.0` build the type implements `ISpanFormattable`. An empty format string or `T` gives the `Text` form.

**Uninitialised instance.** A default instance throws `InvalidOperationException` from `Date`, `Time` and `IsDefinedIn`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardTimestamp(VcardDate date, VcardTime time)` | Throws `ArgumentException` if either isn't complete. |

## Properties

| Property | Notes |
| --- | --- |
| `VcardDate Date`, `VcardTime Time` | The parts. |

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
