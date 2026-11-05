---
title: VcardReader Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardReader Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Reads vCard data (RFC 6350) into the card object model. A stream can hold many cards.

**Syntax**

```csharp
public sealed class VcardReader : IDisposable, IAsyncDisposable
```

The data is read with a [VcardContentLineReader](VcardContentLineReader.md), so the same options and rules apply to its content lines. On `netstandard1.1` there are no asynchronous members, no file-path constructors, and the class doesn't implement `IAsyncDisposable`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardReader(Stream vcardStream)` | Read as UTF-8. The stream is left open. |
| `VcardReader(Stream vcardStream, VcardOptions options)` | |
| `VcardReader(string vcardFilePath)` | Opens the file for asynchronous I/O, and closes it when disposed, even before the first read. |
| `VcardReader(string vcardFilePath, VcardOptions options)` | |
| `VcardReader(TextReader reader)` | The text reader is left open. |
| `VcardReader(TextReader reader, VcardOptions options)` | `options.Encoding` must be `null`. |

## Properties

Each throws `ObjectDisposedException` once the reader is disposed.

| Property | Type | Notes |
| --- | --- | --- |
| `CardsRead` | `long` | The cards read so far, including any that failed a conformance check. |
| `LinesRead` | `long` | The logical lines read so far. |
| `PhysicalLinesRead` | `long` | The physical lines read so far. |

## Methods

| Method | Notes |
| --- | --- |
| `ReadCard()` | The next card, or `null` at the end of the data. |
| `ReadCards()` | The remaining cards, read as the sequence is enumerated. |
| `ReadCardAsync(CancellationToken)` | Reports problems in the data through the task. |
| `ReadCardsAsync(CancellationToken)` | The remaining cards as an `IAsyncEnumerable<VcardCard>`. |
| `Dispose()`, `DisposeAsync()` | May be called more than once. |

## Errors

vCard 4.0, 3.0 and 2.1 are read, and one stream can hold any of them. A vCard 3.0 or 2.1 card is checked by the rules of its version, then converted to the canonical form of the model; see [Converting between versions](../version-conversion.md). The card an `AGENT` of a 2.1 card holds, on the lines after it, is read with it. Any other `VERSION` throws `VcardFileException` as soon as it is read.

| Problem | What happens |
| --- | --- |
| A line that isn't valid; a `BEGIN` or `END` that isn't paired; a nested `BEGIN`; another `VERSION`; data with no card | `VcardFileException`, and the reader can't continue. |
| A problem found in a card once it has been read, which the options don't accept | `VcardFileException`, with the card index and the property in `Data`. Reading can continue with the next card. |

Each card is checked as `VcardCard.Validate` does, and for the structure of its `BEGIN`, `VERSION` and `END` lines. The accommodations are `AllowVersionNotFirst`, `AllowMissingRequiredProperties`, `AllowCardinalityViolations`, `AllowInvalidValues` and `AcceptLegacySyntax` on [VcardOptions](VcardOptions.md).

Only one read may be in progress at a time.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardCard](VcardCard.md)
- [VcardWriter](VcardWriter.md)
- [VcardOptions](VcardOptions.md)
- [VcardFileException](VcardFileException.md)
