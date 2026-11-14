---
title: CsvFieldValidate Delegate
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldValidate Delegate

## Definition

Namespace: `DataStandardizer.File.CSV`

The delegate called to validate an incoming CSV field value.

**Syntax**

```csharp
public delegate bool CsvFieldValidate<TModel>(CsvFieldContext<TModel> context)
    where TModel : class;
```

## Remarks

Attached to a mapping with the `ValidateUsing` step of the fluent
[CsvFieldMappingBuilder](CsvFieldMappingBuilder.md). Return `true` if the field value
is valid; `false` triggers the [CsvFieldBadValue](CsvFieldBadValue.md) handler if one
is set, otherwise a `CsvFileException`. The context's `RawFieldValue` holds the
field's text as read from the file, even when an earlier mapping of the same field
has converted it, `FieldName` the name of the field the mapping resolved to on the
line, and `FieldIndex` that field's position in the record as read. Both
`RawFieldValue` and `FieldIndex` are `null` when the file has no such field, such
as one that an earlier mapping's constant value added to the line. When the validator
returns `false`, the bad-value handler is given the same context. See
[CsvFieldContext](CsvFieldContext.md).

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [Field mapping](../concepts/field-mapping.md)
- [CsvFieldContext](CsvFieldContext.md)
- [CsvFieldMappingBuilder](CsvFieldMappingBuilder.md)
- [CsvFieldBadValue](CsvFieldBadValue.md)
- [File.CSV API reference](index.md)
