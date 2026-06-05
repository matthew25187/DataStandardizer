---
title: CsvFieldMappingAttribute Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldMappingAttribute Class

## Definition

Namespace: `DataStandardizer.File.CSV`

Declares a property as mapped to a CSV field. The mapping is discovered
automatically when a mapper is created from a line model's declarative attributes.

**Syntax**

```csharp
[AttributeUsage(AttributeTargets.Property)]
public sealed class CsvFieldMappingAttribute : Attribute
```

## Remarks

Combine with `[TypeConverter]` to attach a type converter to the field. For
imperative configuration use [CsvFileMappingBuilder](CsvFileMappingBuilder.md)
instead.

## Constructors

| Constructor | Notes |
| --- | --- |
| `CsvFieldMappingAttribute()` | Maps by property name. |
| `CsvFieldMappingAttribute(int fieldIndex)` | Maps to a field by index. Throws `ArgumentOutOfRangeException` if `fieldIndex` is negative. |
| `CsvFieldMappingAttribute(string fieldName)` | Maps to a field by name. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `ConstantValue` | `object? ConstantValue { get; set; }` | A constant value for the field. |
| `FieldIndex` | `int? FieldIndex { get; }` | The mapped field index, if set via the constructor. |
| `FieldName` | `string? FieldName { get; }` | The mapped field name, if set via the constructor. |
| `IsOptional` | `bool IsOptional { get; set; }` | `true` if the field is optional. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`. The nullable
reference annotations apply to the `net8.0` and `net10.0` builds.

## See also

- [Map CSV files](../how-to/map-csv-files.md)
- [Field mapping](../concepts/field-mapping.md)
- [CsvFieldAttribute](CsvFieldAttribute.md)
- [CsvFieldMapping](CsvFieldMapping.md)
- [File.CSV API reference](index.md)
</content>
