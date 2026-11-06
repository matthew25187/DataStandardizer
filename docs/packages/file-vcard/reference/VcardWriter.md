---
title: VcardWriter Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardWriter Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Writes the card object model as vCard data (RFC 6350).

**Syntax**

```csharp
public sealed class VcardWriter : IDisposable, IAsyncDisposable
```

Each card is written by a [VcardContentLineWriter](VcardContentLineWriter.md), as `BEGIN:VCARD`, a `VERSION` line, its properties in order, and `END:VCARD`. `VcardOptions.WriteVersion` says which version: vCard 4.0 by default, or 3.0 or 2.1, to which the card is converted from the canonical form of the model. See [Versions and canonical forms](../concepts/versions-and-canonical-forms.md). In vCard 2.1 the card an agent holds is written after its `AGENT` line. On `netstandard1.1` there are no asynchronous members, no file-path constructors, and the class doesn't implement `IAsyncDisposable`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardWriter(Stream vcardStream)` | UTF-8 without a byte order mark. The stream is left open. |
| `VcardWriter(Stream vcardStream, VcardOptions options)` | |
| `VcardWriter(string vcardFilePath)` | Overwrites an existing file, which it opens for asynchronous I/O. |
| `VcardWriter(string vcardFilePath, VcardOptions options)` | |
| `VcardWriter(TextWriter writer)` | Neither flushed nor closed when disposed; call `Flush` first. |
| `VcardWriter(TextWriter writer, VcardOptions options)` | `options.Encoding` must be `null`. |

## Properties

Each throws `ObjectDisposedException` once the writer is disposed.

| Property | Type | Notes |
| --- | --- | --- |
| `CardsWritten` | `long` | The cards written so far. |
| `LinesWritten` | `long` | The logical lines written so far. |

## Methods

| Method | Notes |
| --- | --- |
| `WriteCard(VcardCard card)` | Checks the card, then writes it. |
| `WriteCards(IEnumerable<VcardCard> cards, IProgress<VcardWriteProgress>? progress = null)` | Reports progress after each card. |
| `WriteCardAsync(VcardCard card, CancellationToken)` | Reports problems in the card or the I/O through the task. |
| `WriteCardsAsync(...)` | Takes an `IEnumerable<VcardCard>` or `IAsyncEnumerable<VcardCard>`, with progress. |
| `Flush()`, `FlushAsync(CancellationToken)` | Flushes the underlying text writer. |
| `Dispose()`, `DisposeAsync()` | May be called more than once. |

## Errors

A card is checked as `VcardCard.Validate(version)` does before it is written. If it breaks a rule the options don't accept, `VcardFileException` is thrown and nothing of that card is written. The accommodations are `AllowMissingRequiredProperties`, `AllowCardinalityViolations`, `AllowInvalidValues` and `AllowLossyVersionConversion` on [VcardOptions](VcardOptions.md). The forms of vCard 2.1 are never written in a vCard 4.0 or 3.0 card, and neither is a `BEGIN`, `VERSION` or `END` line added to `Properties`.

A check doesn't see every character of a line. A line that can't be written, such as one with a group that isn't valid, throws `VcardFileException` when it is reached, and the lines before it have been written.

Only one operation may be in progress at a time.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardCard](VcardCard.md)
- [VcardReader](VcardReader.md)
- [VcardOptions](VcardOptions.md)
- [VcardWriteProgress](VcardWriteProgress.md)
