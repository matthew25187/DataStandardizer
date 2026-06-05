---
title: CsvFileHeader Delegate
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileHeader Delegate

## Definition

Namespace: `DataStandardizer.File.CSV`

The delegate called when the header line for a CSV file is being prepared, to supply
field names in lieu of (or alongside) a header line read from the file.

**Syntax**

```csharp
public delegate IReadOnlyList<string> CsvFileHeader(CsvFileHeaderLine? headerLine);
```

## Remarks

Assigned to [CsvFileOptions.HeaderHandler](CsvFileOptions.md). `headerLine` is the
header read from the file, if any; return the field names to use, in order. The
nullable annotation on `headerLine` applies to the `net8.0` and `net10.0` builds.

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [CsvFileOptions](CsvFileOptions.md)
- [CsvFileHeaderHandling](CsvFileHeaderHandling.md)
- [CsvFileHeaderLine](CsvFileHeaderLine.md)
- [File.CSV API reference](index.md)
</content>
