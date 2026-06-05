---
title: ICsvFileLine Interface
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# ICsvFileLine Interface

## Definition

Namespace: `DataStandardizer.File.CSV`

A CSV line as an ordered, name-keyed bag of field values. Index by field name to
read or set a raw value.

**Syntax**

```csharp
public interface ICsvFileLine : IOrderedDictionary
```

## Remarks

`ICsvFileLine` adds no members of its own; it specializes
`System.Collections.Specialized.IOrderedDictionary` so that lines can be enumerated
and indexed in field order. The reader and writer accept and return values typed as
`ICsvFileLine`; the concrete implementations are [CsvFileHeaderLine](CsvFileHeaderLine.md)
and [CsvFileRecordLine](CsvFileRecordLine.md) (via [CsvFileLineBase](CsvFileLineBase.md)).

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The CSV line model](../concepts/csv-line-model.md)
- [CsvFileLineBase](CsvFileLineBase.md)
- [CsvFileRecordLine](CsvFileRecordLine.md)
- [CsvFileHeaderLine](CsvFileHeaderLine.md)
- [File.CSV API reference](index.md)
</content>
