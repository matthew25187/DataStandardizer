---
title: CsvFieldMapping Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldMapping Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The details of a single mapping between a CSV field and a model property: the field
identity, optionality, constant or generated value, converters, and the property
type.

**Syntax**

```csharp
public sealed class CsvFieldMapping
```

## Remarks

You do not construct or set this type directly — its constructor and property
setters are `internal`. Mappings are built from
[CsvFieldMappingAttribute](CsvFieldMappingAttribute.md) declarations or through the
fluent [CsvFileMappingBuilder](CsvFileMappingBuilder.md), and surfaced (read-only)
through a [CsvFileMapper](CsvFileMapper.md) / [ICsvFileMapper](ICsvFileMapper.md).
The properties below are all publicly readable.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `ConstantValue` | `object? ConstantValue { get; }` | A constant value for the field. |
| `FieldIndex` | `int? FieldIndex { get; }` | The mapped field index. Setting a negative value throws `ArgumentOutOfRangeException`. |
| `FieldName` | `string? FieldName { get; }` | The mapped field name. |
| `FromStringConverter` | `Delegate? FromStringConverter { get; }` | A [CsvFieldConvertFromString](CsvFieldConvertFromString.md) delegate (reading only). |
| `IsOptional` | `bool IsOptional { get; }` | `true` if the field is optional. |
| `PropertyType` | `Type PropertyType { get; }` | The type of the mapped property. |
| `ToStringConverter` | `Delegate? ToStringConverter { get; }` | A [CsvFieldConvertToString](CsvFieldConvertToString.md) delegate (writing only). |
| `TypeConverterType` | `Type? TypeConverterType { get; }` | The `TypeConverter` type used for (de)serialization. |
| `Validator` | `Delegate? Validator { get; }` | A [CsvFieldValidate](CsvFieldValidate.md) delegate. |
| `VariableValueGenerator` | `Delegate? VariableValueGenerator { get; }` | A [CsvFieldGenerate](CsvFieldGenerate.md) delegate. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`. The nullable
reference annotations apply to the `net8.0` and `net10.0` builds.

## See also

- [Field mapping](../concepts/field-mapping.md)
- [CsvFieldMappingAttribute](CsvFieldMappingAttribute.md)
- [CsvFileMappingBuilder](CsvFileMappingBuilder.md)
- [ICsvFileMapper](ICsvFileMapper.md)
- [File.CSV API reference](index.md)
</content>
