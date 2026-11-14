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
public sealed class CsvFileOptions : ICsvFileOptions, IEquatable<CsvFileOptions>
```

## Remarks

On `net10.0` the type is a `record` whose properties are `init`-only, so
configure it with an object initializer or a `with` expression. On .NET Standard the
type is a `class` whose properties are settable. Construct it with the implicit
parameterless constructor and set the properties you need; the rest keep their
RFC 4180 defaults.

On every target, two options objects are equal when each of their properties is equal,
and `==`, `!=`, `GetHashCode` and `ToString` behave as they do for a record. The .NET
Standard class implements `IEquatable<CsvFileOptions>` to match the `net10.0` record.

The handler properties are typed `Delegate`, but a reader or writer calls only a
delegate of the type each property names, with the reader's or writer's record line
type as `TModel`. Its constructor throws `ArgumentException` for a handler it calls
that is of any other type, so that a handler is never silently ignored. A writer
calls only `InconsistentFieldCountHandler`, so it checks only that one. The options are checked
when they are passed to the constructor; a handler set on a .NET Standard options
object after that isn't checked.

## Constructors

| Constructor | Notes |
| --- | --- |
| `CsvFileOptions()` | Implicit parameterless constructor; all properties take their defaults. |

## Properties

| Property | Signature | Default | Notes |
| --- | --- | --- | --- |
| `BadValueHandler` | `Delegate? BadValueHandler { get; init; }` | `null` | A [CsvFieldBadValue](CsvFieldBadValue.md) delegate for the reader's record line type. `get; set;` on .NET Standard. |
| `Culture` | `CultureInfo? Culture { get; init; }` | `null` | Culture for value (de)serialization. When `null`, values are converted with the invariant culture, never the current culture. `get; set;` on .NET Standard. |
| `EmbeddedLineBreak` | `string? EmbeddedLineBreak { get; init; }` | `null` | Replacement for line breaks embedded in field values. `get; set;` on .NET Standard. |
| `Encoding` | `Encoding? Encoding { get; init; }` | `null` | File encoding. Its character set is the one whose printable characters a field may hold (RFC 4180 section 3, `charset`); see [Characters allowed in a field](../how-to/configure-csv.md#characters-allowed-in-a-field). `get; set;` on .NET Standard. |
| `FieldDelimiterCharacter` | `char FieldDelimiterCharacter { get; init; }` | `','` | Field separator. A writer rejects a double quote or the first character of `TerminatorLineBreak`. `get; set;` on .NET Standard. |
| `HeaderHandler` | `Delegate? HeaderHandler { get; init; }` | `null` | A [CsvFileHeader](CsvFileHeader.md) delegate supplying field names. `get; set;` on .NET Standard. |
| `HeaderHandling` | `CsvFileHeaderHandling HeaderHandling { get; init; }` | `None` | How the header line is handled (reading only). `get; set;` on .NET Standard. |
| `InconsistentFieldCountHandler` | `Delegate? InconsistentFieldCountHandler { get; init; }` | `null` | A [CsvFieldCount](CsvFieldCount.md) delegate for the reader's or writer's record line type. `get; set;` on .NET Standard. |
| `QuoteHandling` | `CsvFieldQuoteHandling QuoteHandling { get; init; }` | `Required` | Field quoting strategy (writing only). A writer rejects a value `CsvFieldQuoteHandling` doesn't define. `get; set;` on .NET Standard. |
| `SuppressTrailingBlankFields` | `bool SuppressTrailingBlankFields { get; init; }` | `false` | Omit trailing empty fields on write. `get; set;` on .NET Standard. |
| `TerminatorLineBreak` | `string TerminatorLineBreak { get; init; }` | `"\r\n"` | Line terminator (read and write). Must have at least one character; a writer also rejects one that starts with a double quote. `get; set;` on .NET Standard. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
`CsvFileOptions` is a `record` with `init`-only properties on the `net10.0` build, and a `class` with settable properties on the
`netstandard1.1`, `netstandard2.0`, and `netstandard2.1` builds. The nullable
reference annotations apply to the `netstandard2.1` and `net10.0`
builds.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [ICsvFileOptions](ICsvFileOptions.md)
- [CsvFileHeaderHandling](CsvFileHeaderHandling.md)
- [CsvFieldQuoteHandling](CsvFieldQuoteHandling.md)
- [File.CSV API reference](index.md)
