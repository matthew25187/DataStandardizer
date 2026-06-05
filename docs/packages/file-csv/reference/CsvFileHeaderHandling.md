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
| `Use` | 1 | Read the first line as the header and use it for field names. |
| `Ignore` | 2 | Skip the header line; supply field names via a [CsvFileHeader](CsvFileHeader.md) delegate. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [CsvFileOptions](CsvFileOptions.md)
- [CsvFileHeader](CsvFileHeader.md)
- [File.CSV API reference](index.md)
</content>
