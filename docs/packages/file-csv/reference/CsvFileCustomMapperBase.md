---
title: CsvFileCustomMapperBase Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileCustomMapperBase Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The base class for an imperative mapper of plain (non-line) model objects. Subclass
it to map an arbitrary class to and from CSV lines via the `ToObject` / `ToCsvLine`
conversions.

**Syntax**

```csharp
public abstract class CsvFileCustomMapperBase<TModel> : ICsvFileMapper
    where TModel : class
```

## Remarks

In your subclass constructor, call the `protected CsvFileMappingBuilder<TModel>
Map()` method once per property and chain the fluent
[CsvFileMappingBuilder](CsvFileMappingBuilder.md) pipeline. Pass an instance of the
mapper to the `ToObject` / `ToCsvLine` extensions on
[CsvExtensions](CsvExtensions.md). Use this base for plain objects; for
`CsvFileRecordLine`-derived models use [CsvFileMapperBase](CsvFileMapperBase.md)
instead — the two are not interchangeable.

Every member of the `IReadOnlyDictionary<string, CsvFieldMapping>` surface is
implemented **explicitly** and is reached through an interface reference.

## Methods

### Explicit implementation

`CsvFileCustomMapperBase` implements `IReadOnlyDictionary<string, CsvFieldMapping>`
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
- [CsvFileMapperBase](CsvFileMapperBase.md)
- [CsvFileMappingBuilder](CsvFileMappingBuilder.md)
- [CsvExtensions](CsvExtensions.md)
- [File.CSV API reference](index.md)
</content>
