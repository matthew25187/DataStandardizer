---
title: ICsvFileMapper Interface
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# ICsvFileMapper Interface

## Definition

Namespace: `DataStandardizer.File.CSV`

A read-only set of field mappings keyed by property name. The reader and writer
consume a mapper to translate between CSV fields and model properties.

**Syntax**

```csharp
public interface ICsvFileMapper : IReadOnlyDictionary<string, CsvFieldMapping>
```

## Remarks

`ICsvFileMapper` adds no members of its own; it specializes
`IReadOnlyDictionary<string, CsvFieldMapping>`. The sealed
[CsvFileMapper](CsvFileMapper.md) is the concrete collection produced from
declarative attributes; [CsvFileMapperBase](CsvFileMapperBase.md) and
[CsvFileCustomMapperBase](CsvFileCustomMapperBase.md) are the imperative base
classes that implement it.

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Field mapping](../concepts/field-mapping.md)
- [CsvFileMapper](CsvFileMapper.md)
- [CsvFileMapperBase](CsvFileMapperBase.md)
- [CsvFileCustomMapperBase](CsvFileCustomMapperBase.md)
- [CsvFieldMapping](CsvFieldMapping.md)
- [File.CSV API reference](index.md)
</content>
