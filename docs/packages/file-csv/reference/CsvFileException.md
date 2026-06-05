---
title: CsvFileException Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileException Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The exception thrown for abnormal CSV processing — unexpected field counts,
unmappable properties, invalid field values, duplicate field names, or invalid
field names on write.

**Syntax**

```csharp
public sealed class CsvFileException : Exception
```

## Remarks

Additional contextual details (field names, indexes, expected/actual counts) are
attached to the exception's `Data` dictionary.

## Constructors

| Constructor | Notes |
| --- | --- |
| `CsvFileException()` | |
| `CsvFileException(string message)` | |
| `CsvFileException(string message, Exception innerException)` | |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `FilePath` | `string? FilePath { get; init; }` | The path to the CSV file. `get;` only (internal setter) on `netstandard1.3` / `netstandard2.0`. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`. `FilePath` is
`init`-settable on the `net8.0` and `net10.0` builds; on the .NET Standard builds it
is publicly get-only.

## See also

- [Read CSV files](../how-to/read-csv-files.md)
- [Write CSV files](../how-to/write-csv-files.md)
- [File.CSV API reference](index.md)
</content>
