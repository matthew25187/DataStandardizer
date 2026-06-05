---
title: CsvFileMapper Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileMapper Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The sealed, read-only collection of field mappings produced from a line model's
declarative attributes. The reader and writer consume it through
[ICsvFileMapper](ICsvFileMapper.md).

**Syntax**

```csharp
public sealed class CsvFileMapper : ICsvFileMapper
```

## Remarks

You do not construct this type directly — its constructor is `internal`. A
`CsvFileMapper` is created by the `CreateMapper` extension on
[CsvExtensions](CsvExtensions.md) from `[CsvFieldMapping]` attributes. Every member
of the `IReadOnlyDictionary<string, CsvFieldMapping>` surface is implemented
**explicitly**, so the collection is consumed through an
`IReadOnlyDictionary<string, CsvFieldMapping>` (or `ICsvFileMapper`) reference.

## Methods

### Explicit implementation

`CsvFileMapper` implements `IReadOnlyDictionary<string, CsvFieldMapping>`
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

- [Field mapping](../concepts/field-mapping.md)
- [ICsvFileMapper](ICsvFileMapper.md)
- [CsvExtensions](CsvExtensions.md)
- [CsvFieldMappingAttribute](CsvFieldMappingAttribute.md)
- [File.CSV API reference](index.md)
</content>
