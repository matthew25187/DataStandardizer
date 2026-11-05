---
title: VcardDateAndOrTime Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardDateAndOrTime Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

A DATE-AND-OR-TIME value (RFC 6350 §4.3.4): a date and time, a date, or a time written with a `T` before it. BDAY and ANNIVERSARY use it.

**Syntax**

```csharp
public readonly struct VcardDateAndOrTime : IEquatable<VcardDateAndOrTime>, ISpanFormattable
```

## Remarks

vCard 3.0 and 2.1 have no such type. Their `BDAY` property is a date or a date-time, so in those versions this type reads and writes those two and has no form for a time alone.

**Versions.** Every parse and format method has an overload taking a [VcardVersion](VcardVersion.md). The overload without one uses vCard 4.0. `IsDefinedIn` says whether a version can write the value, and formatting one it can't throws `InvalidOperationException`.

**Forms.** Only `VcardValueFormat.Text` is implemented. `Json` and `Xml` throw `NotSupportedException` until the jCard and xCard work adds them.

**Parsing from a string.** `Parse` and `TryParse` take a `ReadOnlySpan<char>`. A `string` converts implicitly on .NET Core and .NET Standard 2.1. On .NET Framework, call `AsSpan()`.

**Allocation.** `TryParse` from a span and `TryFormat` into one do not allocate (covered by `VcardValueAllocationTests`).

On the `net10.0` build the type implements `ISpanFormattable`. An empty format string or `T` gives the `Text` form.

**Uninitialised instance.** A default instance throws `InvalidOperationException` from `Kind`, `Date`, `Time` and `IsDefinedIn`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardDateAndOrTime(VcardDate)`, `VcardDateAndOrTime(VcardTime)`, `VcardDateAndOrTime(VcardDateTime)` | Throws `ArgumentException` if the argument isn't initialised. |

## Properties

| Property | Notes |
| --- | --- |
| `VcardDateAndOrTimeKind Kind` | What it holds. See [VcardDateAndOrTimeKind](VcardDateAndOrTimeKind.md). |
| `VcardDate? Date`, `VcardTime? Time` | The parts it holds, otherwise `null`. |

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
