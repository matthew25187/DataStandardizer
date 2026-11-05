---
title: VcardWriteProgress Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardWriteProgress Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

The progress of a writer, reported through `IProgress<VcardWriteProgress>` by the bulk
write methods.

**Syntax**

```csharp
public readonly struct VcardWriteProgress : IEquatable<VcardWriteProgress>
```

## Remarks

[VcardContentLineWriter](VcardContentLineWriter.md) reports it after each line that
`WriteLines` or `WriteLinesAsync` hands to the underlying `TextWriter`, and
[VcardWriter](VcardWriter.md) after each card that `WriteCards` or `WriteCardsAsync`
hands to it.

Counts are cumulative for the writer, not for the call that reports them. They count
what has been handed to the underlying `TextWriter`, which may not have been flushed
yet.

The report is made synchronously on the writing thread. An `IProgress<T>` such as
`Progress<T>` may then pass it to another thread. Being a small value type, it is safe
to keep.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardWriteProgress(long linesWritten)` | Sets the count of lines, with no cards. The argument is not checked. |
| `VcardWriteProgress(long linesWritten, long cardsWritten)` | Sets both counts. The arguments are not checked. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CardsWritten` | `long CardsWritten { get; }` | The number of cards written so far. It stays 0 for a `VcardContentLineWriter`, which doesn't deal in cards. |
| `LinesWritten` | `long LinesWritten { get; }` | The number of logical (unfolded) content lines written so far. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(VcardWriteProgress other)` | `bool` | `true` if both counts are equal. |
| `Equals(object? obj)` | `bool` | `true` if `obj` is a `VcardWriteProgress` with equal counts. |
| `GetHashCode()` | `int` | The hash code of the counts. |
| `ToString()` | `string` | `LinesWritten = n, CardsWritten = n`, formatted with the invariant culture. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `operator ==(VcardWriteProgress left, VcardWriteProgress right)` | `bool` | Same as `left.Equals(right)`. |
| `operator !=(VcardWriteProgress left, VcardWriteProgress right)` | `bool` | Same as `!left.Equals(right)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable annotation on `Equals(object?)` applies to the `netstandard2.1`
and `net10.0` builds.

## See also

- [VcardContentLineWriter](VcardContentLineWriter.md)
- [VcardWriter](VcardWriter.md)
- [File.vCard API reference](index.md)
