---
title: CsvExtensions Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvExtensions Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The conversion extension methods that build mappers from declarative attributes and
convert between CSV record lines and plain model objects.

**Syntax**

```csharp
public static class CsvExtensions
```

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `CreateMapper<TRecordLine>()` | `TRecordLine` | `ICsvFileMapper` | `where TRecordLine : CsvFileRecordLine`. Builds a mapper from the line model's `[CsvFieldMapping]` (and `[TypeConverter]`) attributes. |
| `ToCsvLine<TModel, TRecordLine>(CsvFileMapperBase<TRecordLine> mapper)` | `TModel` | `TRecordLine` | `where TModel : class` and `where TRecordLine : CsvFileRecordLine, new()`. Converts a plain object to a record line for writing, applying the mapping's field names, field order and constants. Converters and generators apply when a writer with the mapper registered writes the line. |
| `ToObject<TRecordLine, TModel>(CsvFileCustomMapperBase<TModel> mapper)` | `TRecordLine` | `TModel` | `where TRecordLine : CsvFileRecordLine` and `where TModel : class, new()`. Converts a read line to a custom object, applying the mapping's field names and indexes, converters, constants, generators and validators as a reader does. Throws `CsvFileException` when a non-optional property cannot be mapped, a validator rejects a value, or a value can't be converted. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [Map CSV files](../how-to/map-csv-files.md)
- [Field mapping](../concepts/field-mapping.md)
- [CsvFileMapperBase](CsvFileMapperBase.md)
- [CsvFileCustomMapperBase](CsvFileCustomMapperBase.md)
- [ICsvFileMapper](ICsvFileMapper.md)
- [File.CSV API reference](index.md)
