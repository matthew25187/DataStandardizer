---
title: XcardWriter Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# XcardWriter Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Writes the card object model as xCard data, the XML form of vCard (RFC 6351).

**Syntax**

```csharp
public sealed class XcardWriter : IDisposable, IAsyncDisposable
```

The cards are written in a `vcards` element in the namespace `urn:ietf:params:xml:ns:vcard-4.0`, which starts with the first card and ends when the writer is disposed; if no card is
written, nothing is. xCard represents vCard 4.0 alone, so each card is written in the form of vCard 4.0, without its `VERSION`, whose role the namespace plays (RFC 6351 §5.1).
Names are written in lower case, a group as a `group` element, and each value in its typed value element. [Read and write xCard](../how-to/read-write-xcard.md) has the full mapping.
The output is UTF-8 without a byte order mark by default, indented by two spaces, with the line breaks of `VcardOptions.LineBreak`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `XcardWriter(Stream xcardStream)` | The stream is flushed but left open when the writer is disposed. |
| `XcardWriter(Stream xcardStream, VcardOptions options)` | `VcardOptions.Encoding`, if set, is the encoding of the output. |
| `XcardWriter(string xcardFilePath)` | Overwrites an existing file, which it opens for asynchronous I/O. Not on `netstandard1.1`. |
| `XcardWriter(string xcardFilePath, VcardOptions options)` | Not on `netstandard1.1`. |
| `XcardWriter(TextWriter writer)` | The text writer is flushed but left open. |
| `XcardWriter(TextWriter writer, VcardOptions options)` | `VcardOptions.Encoding` must be `null`. |
| `XcardWriter(XmlWriter writer)` | Writes the `vcards` element at the XML writer's current position. The XML writer is neither flushed nor closed. |
| `XcardWriter(XmlWriter writer, VcardOptions options)` | `VcardOptions.Encoding` must be `null`. |

## Properties

Each throws `ObjectDisposedException` once the writer is disposed.

| Property | Type | Notes |
| --- | --- | --- |
| `CardsWritten` | `long` | The cards written so far. |
| `PropertiesWritten` | `long` | The properties written so far. `VERSION` isn't written, so it isn't counted. |

## Methods

| Method | Notes |
| --- | --- |
| `WriteCard(VcardCard card)` | Checks the card, then writes it. |
| `WriteCards(IEnumerable<VcardCard> cards, IProgress<VcardWriteProgress>? progress = null)` | Reports progress after each card. `LinesWritten` counts the lines the cards would have in `.vcf` form. |
| `WriteCardAsync(VcardCard card, CancellationToken)` | Reports problems in the card or the I/O through the task. Needs an XML writer whose `Async` setting is `true`. Not on `netstandard1.1`. |
| `WriteCardsAsync(...)` | Takes an `IEnumerable<VcardCard>` or `IAsyncEnumerable<VcardCard>`, with progress. Not on `netstandard1.1`. |
| `Flush()`, `FlushAsync(CancellationToken)` | Flushes the XML writer. The `vcards` element isn't ended. `FlushAsync` is not on `netstandard1.1`. |
| `Dispose()`, `DisposeAsync()` | End the `vcards` element. May be called more than once. `DisposeAsync` is not on `netstandard1.1`. |

## Errors

A card is checked as `VcardCard.Validate(VcardVersion.V4_0)` does before it is written. If it breaks a rule the options don't accept, [VcardFileException](VcardFileException.md) is
thrown and nothing of that card is written. A card that xCard can't hold throws too, before any of it is written: a name or group that isn't letters, digits and hyphens, a `BEGIN`,
`END` or `GROUP` property, a property with two `VALUE` parameters, or a value with a character XML can't hold. A write that fails leaves the writer unable to continue.

Only one operation may be in progress at a time.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`. The asynchronous members and the file-path constructors need `netstandard2.0` or later.

## See also

- [Read and write xCard](../how-to/read-write-xcard.md)
- [XcardReader](XcardReader.md)
- [VcardWriter](VcardWriter.md)
- [VcardWriteProgress](VcardWriteProgress.md)
