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

Combine with `[TypeConverter]` to attach a type converter to the field. A reader
parses the numeric, date, time, `bool`, `Guid` and enum types without one; see
[converting field values](../concepts/field-mapping.md#converting-field-values).
For imperative configuration use [CsvFileMappingBuilder](CsvFileMappingBuilder.md)
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
| `ConstantValue` | `object? ConstantValue { get; set; }` | A constant value for the field. A reader converts a number to the property's number type if that type holds it as it is written, and throws `CsvFileException` if it can't; see [Field mapping](../concepts/field-mapping.md). |
| `FieldIndex` | `int? FieldIndex { get; }` | The mapped field index, if set via the constructor. |
| `FieldName` | `string? FieldName { get; }` | The mapped field name, if set via the constructor. |
| `IsOptional` | `bool IsOptional { get; set; }` | `true` if the field is optional. |
| `UseFieldName` | `string? UseFieldName { get; set; }` | The name the field is known by once read, in place of its name in the file, or `null` if it keeps that name. Properties mapped by a name that the header line repeats take its fields in declaration order, and each must set this. Setting `null` throws `ArgumentNullException`; an empty or all-whitespace name throws `ArgumentException`. See [header lines that repeat a name](../concepts/field-mapping.md#header-lines-that-repeat-a-name). |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the
`netstandard2.1` and `net10.0` builds.

## See also

- [Map CSV files](../how-to/map-csv-files.md)
- [Field mapping](../concepts/field-mapping.md)
- [CsvFieldAttribute](CsvFieldAttribute.md)
- [CsvFieldMapping](CsvFieldMapping.md)
- [File.CSV API reference](index.md)
