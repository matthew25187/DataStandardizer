---
title: CsvFileOptions Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileOptions Class

## Definition

Namespace: `DataStandardizer.File.CSV`

Options to configure the behaviour of a CSV reader or writer. All defaults are
RFC 4180–compliant. Pass an instance to a reader or writer constructor; read it back
through [ICsvFileOptions](ICsvFileOptions.md).

**.NET:**

```csharp
public sealed record CsvFileOptions : ICsvFileOptions
```

**.NET Standard:**

```csharp
public sealed class CsvFileOptions : ICsvFileOptions
```

## Remarks

On .NET 5 and later the type is a `record` whose properties are `init`-only, so
configure it with an object initializer or a `with` expression. On .NET Standard the
type is a plain `class` whose properties are settable. Construct it with the implicit
parameterless constructor and set the properties you need; the rest keep their
RFC 4180 defaults.

## Constructors

| Constructor | Notes |
| --- | --- |
| `CsvFileOptions()` | Implicit parameterless constructor; all properties take their defaults. |

## Properties

| Property | Signature | Default | Notes |
| --- | --- | --- | --- |
| `BadValueHandler` | `Delegate? BadValueHandler { get; init; }` | `null` | A [CsvFieldBadValue](CsvFieldBadValue.md) delegate. `get; set;` on .NET Standard. |
| `Culture` | `CultureInfo? Culture { get; init; }` | `null` | Culture for value (de)serialization. `get; set;` on .NET Standard. |
| `EmbeddedLineBreak` | `string? EmbeddedLineBreak { get; init; }` | `null` | Replacement for line breaks embedded in field values. `get; set;` on .NET Standard. |
| `Encoding` | `Encoding? Encoding { get; init; }` | `null` | File encoding. `get; set;` on .NET Standard. |
| `FieldDelimiterCharacter` | `char FieldDelimiterCharacter { get; init; }` | `','` | Field separator. `get; set;` on .NET Standard. |
| `HeaderHandler` | `Delegate? HeaderHandler { get; init; }` | `null` | A [CsvFileHeader](CsvFileHeader.md) delegate supplying field names. `get; set;` on .NET Standard. |
| `HeaderHandling` | `CsvFileHeaderHandling HeaderHandling { get; init; }` | `None` | How the header line is handled (reading only). `get; set;` on .NET Standard. |
| `InconsistentFieldCountHandler` | `Delegate? InconsistentFieldCountHandler { get; init; }` | `null` | A [CsvFieldCount](CsvFieldCount.md) delegate. `get; set;` on .NET Standard. |
| `QuoteHandling` | `CsvFieldQuoteHandling QuoteHandling { get; init; }` | `Required` | Field quoting strategy (writing only). `get; set;` on .NET Standard. |
| `SuppressTrailingBlankFields` | `bool SuppressTrailingBlankFields { get; init; }` | `false` | Omit trailing empty fields on write. `get; set;` on .NET Standard. |
| `TerminatorLineBreak` | `string TerminatorLineBreak { get; init; }` | `"\r\n"` | Line terminator (read and write). `get; set;` on .NET Standard. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.
`CsvFileOptions` is a `record` with `init`-only properties on the `net8.0` and
`net10.0` builds, and a plain `class` with settable properties on the
`netstandard1.3` and `netstandard2.0` builds. The nullable reference annotations
apply to the .NET builds.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [ICsvFileOptions](ICsvFileOptions.md)
- [CsvFileHeaderHandling](CsvFileHeaderHandling.md)
- [CsvFieldQuoteHandling](CsvFieldQuoteHandling.md)
- [File.CSV API reference](index.md)
</content>
