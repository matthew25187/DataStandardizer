---
title: CsvFieldConvertFromString Delegate
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldConvertFromString Delegate

## Definition

Namespace: `DataStandardizer.File.CSV`

The delegate called to deserialize a raw CSV field value into a property value when
reading. `T` is covariant.

**Syntax**

```csharp
public delegate T CsvFieldConvertFromString<TModel, out T>(CsvFieldContext<TModel> context)
    where TModel : class;
```

## Remarks

Attached to a mapping with the `ConvertUsing` step of the fluent
[CsvFieldMappingBuilder](CsvFieldMappingBuilder.md). Read the raw value from the
`context` and return the converted property value. The context's `RawFieldValue`
holds the text being converted, `FieldName` the name of the field the mapping
resolved to on the line, and `FieldIndex` that field's position in the line,
whether the mapping names the field or gives its index. For a constant or generated
value, `FieldIndex` is `null`, since the value didn't come from a field. See
[CsvFieldContext](CsvFieldContext.md). An exception thrown by the delegate propagates
from `ReadLine` unchanged.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [Field mapping](../concepts/field-mapping.md)
- [CsvFieldContext](CsvFieldContext.md)
- [CsvFieldConvertToString](CsvFieldConvertToString.md)
- [CsvFieldMappingBuilder](CsvFieldMappingBuilder.md)
- [File.CSV API reference](index.md)
