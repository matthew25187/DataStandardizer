---
title: CsvFieldAttribute Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldAttribute Class

## Definition

Namespace: `DataStandardizer.File.CSV`

Maps a property on a CSV line model to a named field, so direct field access uses
the given field name rather than the property name.

**Syntax**

```csharp
[AttributeUsage(AttributeTargets.Property)]
public sealed class CsvFieldAttribute : Attribute
```

## Remarks

Apply to a property of a [CsvFileRecordLine](CsvFileRecordLine.md) subclass. The
field-access helpers and the extension converters resolve the backing field name
from this attribute when present.

## Constructors

| Constructor | Notes |
| --- | --- |
| `CsvFieldAttribute(string fieldName)` | The name of the field the property accesses. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `FieldName` | `string FieldName { get; }` | The name of the field accessed by the property. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Map CSV files](../how-to/map-csv-files.md)
- [Field mapping](../concepts/field-mapping.md)
- [CsvFieldMappingAttribute](CsvFieldMappingAttribute.md)
- [CsvFileRecordLine](CsvFileRecordLine.md)
- [File.CSV API reference](index.md)
</content>
