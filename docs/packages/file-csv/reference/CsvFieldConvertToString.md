---
title: CsvFieldConvertToString Delegate
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldConvertToString Delegate

## Definition

Namespace: `DataStandardizer.File.CSV`

The delegate called to serialize a property value into a raw CSV field value when
writing.

**Syntax**

```csharp
public delegate string CsvFieldConvertToString<TModel>(CsvFieldContext<TModel> context)
    where TModel : class;
```

## Remarks

Attached to a mapping with the `ConvertUsing` step of the fluent
[CsvFieldMappingBuilder](CsvFieldMappingBuilder.md). Read the model value from the
`context` and return the string to write. See [CsvFieldContext](CsvFieldContext.md).

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Field mapping](../concepts/field-mapping.md)
- [CsvFieldContext](CsvFieldContext.md)
- [CsvFieldConvertFromString](CsvFieldConvertFromString.md)
- [CsvFieldMappingBuilder](CsvFieldMappingBuilder.md)
- [File.CSV API reference](index.md)
</content>
