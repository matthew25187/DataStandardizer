---
title: CsvFieldGenerate Delegate
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldGenerate Delegate

## Definition

Namespace: `DataStandardizer.File.CSV`

The delegate called to generate a value for a CSV field. `T` is covariant.

**Syntax**

```csharp
public delegate T CsvFieldGenerate<out T>();
```

## Remarks

Attached to a mapping with the `HasVariableValue` step of the fluent
[CsvFieldMappingBuilder](CsvFieldMappingBuilder.md). Return the value to use for the
field each time it is generated.

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Field mapping](../concepts/field-mapping.md)
- [CsvFieldMappingBuilder](CsvFieldMappingBuilder.md)
- [CsvFieldMapping](CsvFieldMapping.md)
- [File.CSV API reference](index.md)
</content>
