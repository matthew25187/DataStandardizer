---
title: CsvFieldQuoteHandling Enum
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldQuoteHandling Enum

## Definition

Namespace: `DataStandardizer.File.CSV`

How a writer quotes field values. Set on
[CsvFileOptions.QuoteHandling](CsvFileOptions.md); applies to writing only.

**Syntax**

```csharp
public enum CsvFieldQuoteHandling
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Always` | 0 | Always surround field values in double-quotes. |
| `Auto` | 1 | Quote non-numeric (string) values, and any value that contains a line break, double quote or field delimiter, such as `1,000` with a comma delimiter. |
| `Required` | 2 | Quote only when the value contains a line break, double-quote, or delimiter (RFC 4180). |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [Write CSV files](../how-to/write-csv-files.md)
- [CsvFileOptions](CsvFileOptions.md)
- [File.CSV API reference](index.md)
