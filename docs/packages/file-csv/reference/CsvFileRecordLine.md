---
title: CsvFileRecordLine Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileRecordLine Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The base type for a CSV data record. Used directly it behaves as a name-keyed bag of
field values; subclass it to expose fields as strongly-typed properties.

**Syntax**

```csharp
public class CsvFileRecordLine : CsvFileLineBase
```

## Remarks

To expose typed fields, override property getters and setters to call the
`protected virtual` helpers `GetPropertyValue<T>([CallerMemberName] string?
propertyName = null)` and `SetPropertyValue<T>(T value, [CallerMemberName] string?
propertyName = null)`. Each helper resolves the backing field name from the
property (honouring any [CsvFieldAttribute](CsvFieldAttribute.md)) and reads or
writes the field bag inherited from [CsvFileLineBase](CsvFileLineBase.md). These
helpers are `protected` and therefore not part of the public surface; the public
`Add` and the explicit `IOrderedDictionary` members are inherited from
`CsvFileLineBase`.

`CsvFileReader<TRecordLine>` requires a `CsvFileRecordLine`-derived type with a
public parameterless constructor; `CsvFileWriter<TRecordLine>` does not.

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The CSV line model](../concepts/csv-line-model.md)
- [CsvFileLineBase](CsvFileLineBase.md)
- [CsvFileHeaderLine](CsvFileHeaderLine.md)
- [CsvFieldAttribute](CsvFieldAttribute.md)
- [CsvFileReader](CsvFileReader.md)
- [File.CSV API reference](index.md)
</content>
