---
title: JcardReader Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# JcardReader Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Reads jCard data, the JSON form of vCard (RFC 7095), into the card object model. Each call to `ReadCard` returns the next card as a [VcardCard](VcardCard.md), the same card
[VcardReader](VcardReader.md) reads from the same data in `.vcf` form.

**Syntax**

```csharp
public sealed class JcardReader : IDisposable, IAsyncDisposable
```

The data is a JSON document in UTF-8, which may start with a byte order mark: the jCard object of one card, `["vcard", [...]]`, or an array of them (RFC 7095 §3.2). Names are
converted to upper case, the group parameter becomes the group of the property, and each value is converted to the text form of RFC 6350. [Read and write jCard](../how-to/read-write-jcard.md)
has the full mapping. jCard is always UTF-8, so there is no `TextReader` constructor, and `VcardOptions.Encoding` must be `null` or UTF-8.

## Constructors

| Constructor | Notes |
| --- | --- |
| `JcardReader(Stream jcardStream)` | The stream is left open. |
| `JcardReader(Stream jcardStream, VcardOptions options)` | |
| `JcardReader(string jcardFilePath)` | Opens the file for asynchronous, sequential reading, and closes it when disposed. |
| `JcardReader(string jcardFilePath, VcardOptions options)` | |

## Properties

Each throws `ObjectDisposedException` once the reader is disposed.

| Property | Type | Notes |
| --- | --- | --- |
| `CardsRead` | `long` | The cards read so far, including any that failed a conformance check. |
| `PropertiesRead` | `long` | The properties read so far, not counting `version`. |

## Methods

| Method | Notes |
| --- | --- |
| `ReadCard()` | The next card, or `null` at the end of the data. |
| `ReadCards()` | The remaining cards, read as the sequence is enumerated. |
| `ReadCardAsync(CancellationToken)` | Reports problems in the data, and cancellation, through the task. |
| `ReadCardsAsync(CancellationToken)` | The token is combined with any token passed to `GetAsyncEnumerator`. |
| `Dispose()`, `DisposeAsync()` | May be called more than once. |

## Errors

JSON that isn't valid, or doesn't follow the structure of jCard, throws [VcardFileException](VcardFileException.md) with `BytePosition` and, once the value has been read, `JsonPath` in its
`Data`; for JSON that isn't valid, `LineNumber` and `LinePosition` as well. The structure includes the MUSTs of RFC 7095: names in lower case, no `BEGIN` or `END` property, no `value`
parameter, and a `group` of letters, digits and hyphens. A `version` other than `4.0` throws with the diagnostic kind `UnsupportedVersion`. After such an error the reader can't
continue.

Each card is checked once its jCard object has been read, as `VcardCard.Validate()` does, against the same rules and [VcardOptions](VcardOptions.md) as `VcardReader`. The first problem
the options don't accept throws `VcardFileException` with the card index and property in its `Data`, and reading can continue with the next card.

Only one read may be in progress at a time.

## Applies to

Targets `netstandard2.0`, `netstandard2.1`, and `net10.0` only. jCard needs System.Text.Json, so `JcardReader` isn't available on `netstandard1.1`.

## See also

- [Read and write jCard](../how-to/read-write-jcard.md)
- [JcardWriter](JcardWriter.md)
- [VcardReader](VcardReader.md)
- [VcardOptions](VcardOptions.md)
