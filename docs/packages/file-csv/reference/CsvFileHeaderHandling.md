---
title: CsvFileHeaderHandling Enum
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileHeaderHandling Enum

## Definition

Namespace: `DataStandardizer.File.CSV`

How a reader handles the header line in a CSV file. Set on
[CsvFileOptions.HeaderHandling](CsvFileOptions.md); applies to reading only.

**Syntax**

```csharp
public enum CsvFileHeaderHandling
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `None` | 0 | The file has no header line. |
| `Use` | 1 | Read the first line as the header and use it for field names. A [CsvFileHeader](CsvFileHeader.md) delegate, if set, is passed the line and its names are used instead. |
| `Ignore` | 2 | Skip the header line, as if the file had none. A [CsvFileHeader](CsvFileHeader.md) delegate, if set, is passed the skipped line and supplies the field names. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [CsvFileOptions](CsvFileOptions.md)
- [CsvFileHeader](CsvFileHeader.md)
- [File.CSV API reference](index.md)
