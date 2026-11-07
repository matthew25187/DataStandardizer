---
title: JcardWriter Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# JcardWriter Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Writes the card object model as jCard data, the JSON form of vCard (RFC 7095).

**Syntax**

```csharp
public sealed class JcardWriter : IDisposable, IAsyncDisposable
```

The output is compact UTF-8 JSON without a byte order mark. One card is written as its jCard object, `["vcard", [...]]`, and two or more as an array of them, so the first card is held
until a second is written or the writer is disposed; `Flush` doesn't write it. jCard represents vCard 4.0 alone, so each card is written in the form of vCard 4.0, with its `version`
first. [Read and write jCard](../how-to/read-write-jcard.md) has the full mapping. `VcardOptions.Encoding` must be `null` or UTF-8, and the options that concern `.vcf` lines, such as
`LineBreak` and `WriteVersion`, don't apply.

## Constructors

| Constructor | Notes |
| --- | --- |
| `JcardWriter(Stream jcardStream)` | The stream is flushed but left open when the writer is disposed. |
| `JcardWriter(Stream jcardStream, VcardOptions options)` | |
| `JcardWriter(string jcardFilePath)` | Overwrites an existing file, which it opens for asynchronous I/O. |
| `JcardWriter(string jcardFilePath, VcardOptions options)` | |

## Properties

Each throws `ObjectDisposedException` once the writer is disposed.

| Property | Type | Notes |
| --- | --- | --- |
| `CardsWritten` | `long` | The cards written so far, including a first card that is held. |
| `PropertiesWritten` | `long` | The properties written so far, not counting `version`. |

## Methods

| Method | Notes |
| --- | --- |
| `WriteCard(VcardCard card)` | Checks the card, then writes it. |
| `WriteCards(IEnumerable<VcardCard> cards, IProgress<VcardWriteProgress>? progress = null)` | Reports progress after each card. `LinesWritten` counts the lines the cards would have in `.vcf` form. |
| `WriteCardAsync(VcardCard card, CancellationToken)` | Reports problems in the card or the I/O through the task. |
| `WriteCardsAsync(...)` | Takes an `IEnumerable<VcardCard>` or `IAsyncEnumerable<VcardCard>`, with progress. |
| `Flush()`, `FlushAsync(CancellationToken)` | Flushes the stream. |
| `Dispose()`, `DisposeAsync()` | End the document, writing a held card. May be called more than once. |

## Errors

A card is checked as `VcardCard.Validate(VcardVersion.V4_0)` does before it is written. If it breaks a rule the options don't accept, [VcardFileException](VcardFileException.md) is
thrown and nothing of that card is written. A card that jCard can't hold throws too, before any of it is written: a name or group that isn't letters, digits and hyphens, a `BEGIN`
or `END` property, a repeated parameter other than `TYPE`, `PID` or `SORT-AS`, a `GROUP` parameter, or a value with a lone surrogate.

A write to the stream that fails, or a flush of it, leaves what reached it unknown, so the writer can't continue: later calls throw `InvalidOperationException`,
and disposal writes nothing more to the stream. The failed call throws the stream's exception, or its task faults with it, except that an
`OperationCanceledException` cancels the task when the caller's token is canceled.

An asynchronous operation is canceled only when the token passed to it is canceled, whatever token the `OperationCanceledException` carries, or if it carries
none. The task is then canceled, and the writer can't continue: later calls throw `InvalidOperationException`. An `OperationCanceledException` while that token
isn't canceled, whether from the stream, the cards being enumerated or the progress callback, faults the task as an I/O error does. The writer can then continue,
unless it came from a write to the stream or a flush of it. If the document is ended at the end
of an operation, because the writer was disposed during it, or by `DisposeAsync`, and that throws, the task faults with the exception, an
`OperationCanceledException` included.

Only one operation may be in progress at a time.

## Applies to

Targets `netstandard2.0`, `netstandard2.1`, and `net10.0` only. jCard needs System.Text.Json, so `JcardWriter` isn't available on `netstandard1.1`.

## See also

- [Read and write jCard](../how-to/read-write-jcard.md)
- [JcardReader](JcardReader.md)
- [VcardWriter](VcardWriter.md)
- [VcardWriteProgress](VcardWriteProgress.md)
