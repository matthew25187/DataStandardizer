---
title: CsvFileIoBase Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileIoBase Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The shared abstract base of [CsvFileReader](CsvFileReader.md) and
[CsvFileWriter](CsvFileWriter.md). It holds the mapper registration API common to
both and the protected (de)serialization helpers they use internally.

**Syntax**

```csharp
public abstract class CsvFileIoBase<TRecordLine> : CsvFileCacheRepositoryBase
    where TRecordLine : CsvFileRecordLine
```

## Remarks

You do not derive from this type directly; use the concrete reader and writer.
Imperative mappers are registered against a reader or writer through the public
`RegisterMapper` / `UnregisterMapper` methods documented here. The (de)serialization
helpers (`DeserializeCsvLineFieldValue`, `SerializeCsvLineFieldValue`,
`GetCsvLineMappedFieldValue`, `GetMapper`, `BuildException`, and so on) are
`protected` and are not part of the public surface.

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `RegisterMapper<TMapper>()` | `void` | `where TMapper : CsvFileMapperBase<TRecordLine>, new()`. Registers an imperative mapper; a no-op if one is already registered. |
| `UnregisterMapper<TMapper>()` | `void` | `where TMapper : CsvFileMapperBase<TRecordLine>, new()`. Removes a registered mapper. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Map CSV files](../how-to/map-csv-files.md)
- [CsvFileReader](CsvFileReader.md)
- [CsvFileWriter](CsvFileWriter.md)
- [CsvFileMapperBase](CsvFileMapperBase.md)
- [File.CSV API reference](index.md)
</content>
