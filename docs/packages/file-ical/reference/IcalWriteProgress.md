---
title: IcalWriteProgress Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalWriteProgress Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

The progress of a writer, reported through `IProgress<IcalWriteProgress>` by the bulk
write methods.

**Syntax**

```csharp
public readonly struct IcalWriteProgress : IEquatable<IcalWriteProgress>
```

## Remarks

[IcalContentLineWriter](IcalContentLineWriter.md) reports it after each line that
`WriteLines` or `WriteLinesAsync` hands to the underlying `TextWriter`. Its
`ComponentsWritten` and `CalendarsWritten` are then always 0.
[IcalWriter](IcalWriter.md) reports it after each calendar that `WriteCalendars` or
`WriteCalendarsAsync` writes, with all three counts. [XcalWriter](XcalWriter.md) and
[JcalWriter](JcalWriter.md) report it after each calendar too; their `LinesWritten` is
the number of content lines the calendars written so far would have as .ics data, not
a count of XML or JSON lines.

Counts are cumulative for the writer, not for the call that reports them. They count
what has been handed to the underlying `TextWriter`, which may not have been flushed
yet.

The report is made synchronously on the writing thread. An `IProgress<T>` such as
`Progress<T>` may then pass it to another thread. Being a small value type, it is safe
to keep.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalWriteProgress(long linesWritten)` | `ComponentsWritten` and `CalendarsWritten` are 0. |
| `IcalWriteProgress(long linesWritten, long componentsWritten, long calendarsWritten)` | Sets all three counts. No argument is checked. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CalendarsWritten` | `long CalendarsWritten { get; }` | The number of `VCALENDAR` objects written so far; always 0 for `IcalContentLineWriter`. |
| `ComponentsWritten` | `long ComponentsWritten { get; }` | The number of components written so far, at any depth and not counting the `VCALENDAR` objects; always 0 for `IcalContentLineWriter`. |
| `LinesWritten` | `long LinesWritten { get; }` | The number of logical (unfolded) content lines written so far. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(IcalWriteProgress other)` | `bool` | `true` if all three counts are equal. |
| `Equals(object? obj)` | `bool` | `true` if `obj` is an `IcalWriteProgress` with all three counts equal. |
| `GetHashCode()` | `int` | Combines the three counts. |
| `ToString()` | `string` | `LinesWritten = n, ComponentsWritten = n, CalendarsWritten = n`, formatted with the invariant culture. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `operator ==(IcalWriteProgress left, IcalWriteProgress right)` | `bool` | Same as `left.Equals(right)`. |
| `operator !=(IcalWriteProgress left, IcalWriteProgress right)` | `bool` | Same as `!left.Equals(right)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable annotation on `Equals(object?)` applies to the `netstandard2.1`, `net8.0`,
and `net10.0` builds.

## See also

- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [IcalContentLineWriter](IcalContentLineWriter.md)
- [IcalWriter](IcalWriter.md)
- [XcalWriter](XcalWriter.md)
- [JcalWriter](JcalWriter.md)
- [File.iCal API reference](index.md)
