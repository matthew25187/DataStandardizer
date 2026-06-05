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
is set, otherwise a `CsvFileException`. See [CsvFieldContext](CsvFieldContext.md).

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Field mapping](../concepts/field-mapping.md)
- [CsvFieldContext](CsvFieldContext.md)
- [CsvFieldMappingBuilder](CsvFieldMappingBuilder.md)
- [CsvFieldBadValue](CsvFieldBadValue.md)
- [File.CSV API reference](index.md)
</content>
