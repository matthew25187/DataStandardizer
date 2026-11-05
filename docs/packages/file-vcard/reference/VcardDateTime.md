---
title: VcardDateTime Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardDateTime Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

A DATE-TIME value (RFC 6350 §4.3.3): a date and a time of day, such as `19961022T140000`, `--1022T1400` or `---22T14`.

**Syntax**

```csharp
public readonly struct VcardDateTime : IEquatable<VcardDateTime>, ISpanFormattable
```

## Remarks

In vCard 4.0 the date may be truncated but not reduced, and the time may be reduced but not truncated. vCard 3.0 and 2.1 have a complete date and a complete time, such as `1995-10-31T22:27:10Z`. vCard 3.0 allows the `T` in either case, since an ABNF string is case-insensitive, and writes it in upper case. vCard 2.1 doesn't mix the basic and extended formats in one value (§2.6.2).

**Versions.** Every parse and format method has an overload taking a [VcardVersion](VcardVersion.md). The overload without one uses vCard 4.0. `IsDefinedIn` says whether a version can write the value, and formatting one it can't throws `InvalidOperationException`.

**Forms.** In `Json`, the form of jCard (RFC 7095 §3.5.5), the date and the time are in the extended format where it applies, such as `1996-10-22T14:00:00` or `--10-22T14:00`. In `Xml`, the form of xCard (RFC 6351 Appendix A), a value is the content of a `date-time` element, in the basic format, such as `19961022T140000`. jCard and xCard represent vCard 4.0 alone, so those forms have no other version.

**Parsing from a string.** `Parse` and `TryParse` take a `ReadOnlySpan<char>`. A `string` converts implicitly on .NET Core and .NET Standard 2.1. On .NET Framework, call `AsSpan()`.

**Allocation.** `TryParse` from a span and `TryFormat` into one do not allocate (covered by `VcardValueAllocationTests`).

On the `net10.0` build the type implements `ISpanFormattable`. An empty format string or `T` gives the `Text` form.

**Uninitialised instance.** A default instance throws `InvalidOperationException` from `Date`, `Time` and `IsDefinedIn`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardDateTime(VcardDate date, VcardTime time)` | Throws `ArgumentException` if either doesn't have the form required, or if no version can write them together. |

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
