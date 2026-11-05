---
title: XcardReader Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# XcardReader Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Reads xCard data, the XML form of vCard (RFC 6351), into the card object model. Each call to `ReadCard` returns the next card as a [VcardCard](VcardCard.md), the same card
[VcardReader](VcardReader.md) reads from the same data in `.vcf` form.

**Syntax**

```csharp
public sealed class XcardReader : IDisposable, IAsyncDisposable
```

The data is an XML document whose root is the `vcards` element in the namespace `urn:ietf:params:xml:ns:vcard-4.0`, which RFC 6351 §5 says MUST be present even for one card. Each
`vcard` element in it is a card of vCard 4.0, given `VERSION:4.0`, since the namespace plays its role (§5.1). Names are converted to upper case, a `group` element gives its properties
its name, and each value is converted to the text form of RFC 6350. A property in another namespace becomes an `XML` property (§6). [Read and write xCard](../how-to/read-write-xcard.md)
has the full mapping.

## Constructors

| Constructor | Notes |
| --- | --- |
| `XcardReader(Stream xcardStream)` | Reads the encoding the XML declaration names, or UTF-8. The stream is left open. |
| `XcardReader(Stream xcardStream, VcardOptions options)` | `VcardOptions.Encoding`, if set, is used in place of the declared encoding, unless the stream starts with a byte order mark. |
| `XcardReader(string xcardFilePath)` | Opens the file for asynchronous, sequential reading, and closes it when disposed. Not on `netstandard1.1`. |
| `XcardReader(string xcardFilePath, VcardOptions options)` | Not on `netstandard1.1`. |
| `XcardReader(TextReader reader)` | The text reader is left open. |
| `XcardReader(TextReader reader, VcardOptions options)` | `VcardOptions.Encoding` must be `null`. |
| `XcardReader(XmlReader reader)` | Starts at the XML reader's current node, and stops at the end of the `vcards` element. The XML reader is left open. |
| `XcardReader(XmlReader reader, VcardOptions options)` | `VcardOptions.Encoding` must be `null`. |

## Properties

Each throws `ObjectDisposedException` once the reader is disposed.

| Property | Type | Notes |
| --- | --- | --- |
| `CardsRead` | `long` | The cards read so far, including any that failed a conformance check. |
| `PropertiesRead` | `long` | The properties read so far, not counting the `VERSION` each card is given. |

## Methods

| Method | Notes |
| --- | --- |
| `ReadCard()` | The next card, or `null` at the end of the `vcards` element. |
| `ReadCards()` | The remaining cards, read as the sequence is enumerated. |
| `ReadCardAsync(CancellationToken)` | Reports problems in the data, and cancellation, through the task. Needs an XML reader whose `Async` setting is `true`. Not on `netstandard1.1`. |
| `ReadCardsAsync(CancellationToken)` | The token is combined with any token passed to `GetAsyncEnumerator`. Not on `netstandard1.1`. |
| `Dispose()`, `DisposeAsync()` | May be called more than once. `DisposeAsync` is not on `netstandard1.1`. |

## Errors

XML that isn't well-formed, or doesn't follow the structure of xCard, throws [VcardFileException](VcardFileException.md) with the `LineNumber` and `LinePosition` of the XML document
in its `Data`. The structure includes the root of RFC 6351 §5, names in lower case (§5.1), no `begin`, `end` or `version` property, no `value` parameter, the `parameters` element
first, a named `group` that holds no group, and the components of a structured value in order. A document type definition throws, even from an XML reader that would parse it. After
such an error the reader can't continue.

A value that isn't in the form RFC 6351 gives its type, such as a BOOLEAN of `TRUE` or a DATE of a year alone, is kept and reported as `InvalidValue`. Each card is checked once its
`vcard` element has been read, as `VcardCard.Validate()` does, against the same rules and [VcardOptions](VcardOptions.md) as `VcardReader`. The first problem the options don't accept
throws `VcardFileException` with the card index and property in its `Data`, and reading can continue with the next card.

Only one read may be in progress at a time.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`. The asynchronous members and the file-path constructors need `netstandard2.0` or later.

## See also

- [Read and write xCard](../how-to/read-write-xcard.md)
- [XcardWriter](XcardWriter.md)
- [VcardReader](VcardReader.md)
- [VcardOptions](VcardOptions.md)
