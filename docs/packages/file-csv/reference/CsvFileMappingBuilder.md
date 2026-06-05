---
title: CsvFileMappingBuilder Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileMappingBuilder Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The entry point to the fluent mapping pipeline. Its `Property<T>` method selects a
property to map and returns the first step interface of the
[CsvFieldMappingBuilder](CsvFieldMappingBuilder.md) pipeline.

**Syntax**

```csharp
public sealed class CsvFileMappingBuilder<TModel> where TModel : class
```

## Remarks

You obtain a `CsvFileMappingBuilder<TModel>` from the protected `Map()` method of
[CsvFileMapperBase](CsvFileMapperBase.md) or
[CsvFileCustomMapperBase](CsvFileCustomMapperBase.md); its constructors are
`internal`. Call `Property<T>` once per property and chain the returned step
interfaces to configure the field. See
[CsvFieldMappingBuilder](CsvFieldMappingBuilder.md) for the full fluent pipeline.

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Property<T>(Expression<Func<TModel, T>> mappedPropertyExpression)` | `ICsvFieldMappingInitialBuilder<TModel, T>` | Selects the property to map and begins the fluent pipeline. Throws `ArgumentNullException` if the expression is `null`, or `ArgumentException` if it is not a member access. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Field mapping](../concepts/field-mapping.md)
- [Map CSV files](../how-to/map-csv-files.md)
- [CsvFieldMappingBuilder](CsvFieldMappingBuilder.md)
- [CsvFileMapperBase](CsvFileMapperBase.md)
- [CsvFileCustomMapperBase](CsvFileCustomMapperBase.md)
- [File.CSV API reference](index.md)
</content>
