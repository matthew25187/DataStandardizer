---
title: CsvFileMapperBase Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileMapperBase Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The base class for an imperative mapper of `CsvFileRecordLine`-derived models.
Subclass it and configure each property's mapping in the constructor, then register
the mapper with a reader or writer.

**Syntax**

```csharp
public abstract class CsvFileMapperBase<TRecordLine> : ICsvFileMapper
    where TRecordLine : CsvFileRecordLine
```

## Remarks

In your subclass constructor, call the `protected CsvFileMappingBuilder<TRecordLine>
Map()` method once per property and chain the fluent
[CsvFileMappingBuilder](CsvFileMappingBuilder.md) pipeline (`Property<T>(…)`,
`HasFieldName`, `ConvertUsing`, and so on). Register the resulting mapper type with
a reader or writer via [CsvFileIoBase](CsvFileIoBase.md)'s `RegisterMapper<TMapper>()`.
Use this base for `CsvFileRecordLine`-derived models; for plain objects use
[CsvFileCustomMapperBase](CsvFileCustomMapperBase.md) instead — the two are not
interchangeable.

Every member of the `IReadOnlyDictionary<string, CsvFieldMapping>` surface is
implemented **explicitly** and is reached through an interface reference.

## Methods

### Explicit implementation

`CsvFileMapperBase` implements `IReadOnlyDictionary<string, CsvFieldMapping>`
explicitly; each member is callable only through the corresponding interface
reference. *(All targets.)*

| Member | Returns | Notes |
| --- | --- | --- |
| `IEnumerable<KeyValuePair<string, CsvFieldMapping>>.GetEnumerator()` | `IEnumerator<KeyValuePair<string, CsvFieldMapping>>` | |
| `IEnumerable.GetEnumerator()` | `IEnumerator` | |
| `IReadOnlyCollection<KeyValuePair<string, CsvFieldMapping>>.Count` | `int` | |
| `IReadOnlyDictionary<string, CsvFieldMapping>.ContainsKey(string key)` | `bool` | |
| `IReadOnlyDictionary<string, CsvFieldMapping>.Keys` | `IEnumerable<string>` | |
| `IReadOnlyDictionary<string, CsvFieldMapping>.this[string key]` | `CsvFieldMapping` | |
| `IReadOnlyDictionary<string, CsvFieldMapping>.TryGetValue(string key, out CsvFieldMapping value)` | `bool` | |
| `IReadOnlyDictionary<string, CsvFieldMapping>.Values` | `IEnumerable<CsvFieldMapping>` | |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Map CSV files](../how-to/map-csv-files.md)
- [Field mapping](../concepts/field-mapping.md)
- [CsvFileCustomMapperBase](CsvFileCustomMapperBase.md)
- [CsvFileMappingBuilder](CsvFileMappingBuilder.md)
- [CsvFileIoBase](CsvFileIoBase.md)
- [File.CSV API reference](index.md)
</content>
