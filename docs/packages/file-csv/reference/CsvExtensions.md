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
| `ToCsvLine<TModel, TRecordLine>(CsvFileMapperBase<TRecordLine> mapper)` | `TModel` | `TRecordLine` | `where TModel : class` and `where TRecordLine : CsvFileRecordLine, new()`. Converts a plain object to a record line for writing. |
| `ToObject<TRecordLine, TModel>(CsvFileCustomMapperBase<TModel> mapper)` | `TRecordLine` | `TModel` | `where TRecordLine : CsvFileRecordLine` and `where TModel : class, new()`. Converts a read line to a custom object. Throws `CsvFileException` when a non-optional property cannot be mapped. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Map CSV files](../how-to/map-csv-files.md)
- [Field mapping](../concepts/field-mapping.md)
- [CsvFileMapperBase](CsvFileMapperBase.md)
- [CsvFileCustomMapperBase](CsvFileCustomMapperBase.md)
- [ICsvFileMapper](ICsvFileMapper.md)
- [File.CSV API reference](index.md)
</content>
