---
title: ICsvFileOptions Interface
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# ICsvFileOptions Interface

## Definition

Namespace: `DataStandardizer.File.CSV`

The read-only view of a reader or writer's options. The reader, writer, and the
field-processing contexts receive options through this interface;
[CsvFileOptions](CsvFileOptions.md) is the settable implementation.

**Syntax**

```csharp
public interface ICsvFileOptions
```

## Remarks

Every member is read-only (get-only). All defaults are RFC 4180–compliant; see
[CsvFileOptions](CsvFileOptions.md) for the concrete type, its defaults, and how to
build an instance.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BadValueHandler` | `Delegate? BadValueHandler { get; }` | A [CsvFieldBadValue](CsvFieldBadValue.md) delegate. |
| `Culture` | `CultureInfo? Culture { get; }` | Culture for value (de)serialization. |
| `EmbeddedLineBreak` | `string? EmbeddedLineBreak { get; }` | Replacement for line breaks embedded in field values. |
| `Encoding` | `Encoding? Encoding { get; }` | File encoding. |
| `FieldDelimiterCharacter` | `char FieldDelimiterCharacter { get; }` | Field separator. |
| `HeaderHandler` | `Delegate? HeaderHandler { get; }` | A [CsvFileHeader](CsvFileHeader.md) delegate supplying field names. |
| `HeaderHandling` | `CsvFileHeaderHandling HeaderHandling { get; }` | How the header line is handled (reading only). |
| `InconsistentFieldCountHandler` | `Delegate? InconsistentFieldCountHandler { get; }` | A [CsvFieldCount](CsvFieldCount.md) delegate. |
| `QuoteHandling` | `CsvFieldQuoteHandling QuoteHandling { get; }` | Field quoting strategy (writing only). |
| `SuppressTrailingBlankFields` | `bool SuppressTrailingBlankFields { get; }` | Omit trailing empty fields on write. |
| `TerminatorLineBreak` | `string TerminatorLineBreak { get; }` | Line terminator (read and write). |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`. The nullable
reference annotations apply to the `net8.0` and `net10.0` builds.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [CsvFileOptions](CsvFileOptions.md)
- [File.CSV API reference](index.md)
</content>
