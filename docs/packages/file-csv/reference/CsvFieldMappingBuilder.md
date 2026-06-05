---
title: CsvFieldMappingBuilder Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldMappingBuilder Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The concrete fluent field-mapping builder. It implements the
`ICsvFieldMapping*Builder` step interfaces that shape the order in which a single
field mapping is configured.

**Syntax**

```csharp
public sealed class CsvFieldMappingBuilder<TModel, T>
    : ICsvFieldMappingInitialBuilder<TModel, T>,
      ICsvFieldMappingIdentityNextBuilder<TModel, T>,
      ICsvFieldMappingTransformationNextBuilder<TModel>,
      ICsvFieldMappingValidationNextBuilder
    where TModel : class
```

## Remarks

You rarely name this type directly. A pipeline begins at
[CsvFileMappingBuilder&lt;TModel&gt;.Property&lt;T&gt;](CsvFileMappingBuilder.md),
which returns `ICsvFieldMappingInitialBuilder<TModel, T>`; from there you chain the
step methods below. Every step method is implemented as an **explicit interface
implementation** on `CsvFieldMappingBuilder<TModel, T>`, so each is callable only
through the step interface the previous call returned. This is what enforces the
allowed call order — for example, once you have set an identity you can no longer
set another, and validation follows transformation.

### The step interfaces (folded)

The fluent pipeline is defined by a set of small step interfaces, documented here
rather than on separate pages:

| Step interface | Purpose |
| --- | --- |
| `ICsvFieldMappingInitialBuilder<TModel, in T>` | The pipeline entry point; aggregates identity, constant, variable, transformation, and optional steps. |
| `ICsvFieldMappingIdentityBuilder<TModel, in T>` | Sets the field identity (`HasFieldName` / `HasFieldIndex`). |
| `ICsvFieldMappingIdentityNextBuilder<TModel, in T>` | Continuation after an identity is set: constant, transformation, variable, or optional. |
| `ICsvFieldMappingConstantBuilder<in T>` | Sets a constant field value (`HasConstantValue`). |
| `ICsvFieldMappingVariableBuilder<in T>` | Sets a generated field value (`HasVariableValue`). |
| `ICsvFieldMappingTransformationBuilder<TModel, in T>` | Attaches a converter (`ConvertUsing`). |
| `ICsvFieldMappingTransformationNextBuilder<TModel>` | Continuation after a converter: validation or optional. |
| `ICsvFieldMappingValidationBuilder<TModel>` | Attaches a validator (`ValidateUsing`). |
| `ICsvFieldMappingValidationNextBuilder` | Continuation after validation: optional. |
| `ICsvFieldMappingOptionalBuilder` | Marks the field optional (`IsOptional`). |

### The step methods (explicit on `CsvFieldMappingBuilder<TModel, T>`)

| Step method | Returns | Notes |
| --- | --- | --- |
| `ConvertUsing(CsvFieldConvertFromString<TModel, T> converter)` | `ICsvFieldMappingTransformationNextBuilder<TModel>` | Sets the deserialization converter (reading). |
| `ConvertUsing(CsvFieldConvertToString<TModel> converter)` | `ICsvFieldMappingTransformationNextBuilder<TModel>` | Sets the serialization converter (writing). |
| `ConvertUsing(Type typeConverterType)` | `ICsvFieldMappingTransformationNextBuilder<TModel>` | Sets a `TypeConverter` type. Throws `ArgumentException` if it is not a `TypeConverter`. |
| `ConvertUsing<TConverter>() where TConverter : TypeConverter` | `ICsvFieldMappingTransformationNextBuilder<TModel>` | Sets a `TypeConverter` type by type argument. |
| `HasConstantValue(T value)` | `void` | Sets a constant field value. |
| `HasFieldIndex(int fieldIndex)` | `ICsvFieldMappingIdentityNextBuilder<TModel, T>` | Maps to a field by index. Throws `ArgumentOutOfRangeException` if negative. |
| `HasFieldName(string fieldName)` | `ICsvFieldMappingIdentityNextBuilder<TModel, T>` | Maps to a field by name. Throws `ArgumentNullException` if `null` or `ArgumentException` if all-whitespace. |
| `HasVariableValue(CsvFieldGenerate<T> valueGenerator)` | `void` | Sets a generated field value. |
| `IsOptional()` | `void` | Marks the field optional. |
| `ValidateUsing(CsvFieldValidate<TModel> validator)` | `ICsvFieldMappingValidationNextBuilder` | Sets a validation delegate. |

## Constructors

| Constructor | Notes |
| --- | --- |
| `CsvFieldMappingBuilder(CsvFieldMapping fieldMapping)` | Wraps the mapping the step methods populate. Normally created internally by the pipeline. |

## Methods

### Explicit implementation

`CsvFieldMappingBuilder<TModel, T>` implements the `ICsvFieldMapping*Builder` step
interfaces explicitly; each method is callable only through the step interface that
the previous call returned (see *The step methods* above). *(All targets.)*

| Member | Returns |
| --- | --- |
| `ICsvFieldMappingTransformationBuilder<TModel, T>.ConvertUsing(CsvFieldConvertFromString<TModel, T> converter)` | `ICsvFieldMappingTransformationNextBuilder<TModel>` |
| `ICsvFieldMappingTransformationBuilder<TModel, T>.ConvertUsing(CsvFieldConvertToString<TModel> converter)` | `ICsvFieldMappingTransformationNextBuilder<TModel>` |
| `ICsvFieldMappingTransformationBuilder<TModel, T>.ConvertUsing(Type typeConverterType)` | `ICsvFieldMappingTransformationNextBuilder<TModel>` |
| `ICsvFieldMappingTransformationBuilder<TModel, T>.ConvertUsing<TConverter>()` | `ICsvFieldMappingTransformationNextBuilder<TModel>` |
| `ICsvFieldMappingConstantBuilder<T>.HasConstantValue(T value)` | `void` |
| `ICsvFieldMappingIdentityBuilder<TModel, T>.HasFieldIndex(int fieldIndex)` | `ICsvFieldMappingIdentityNextBuilder<TModel, T>` |
| `ICsvFieldMappingIdentityBuilder<TModel, T>.HasFieldName(string fieldName)` | `ICsvFieldMappingIdentityNextBuilder<TModel, T>` |
| `ICsvFieldMappingVariableBuilder<T>.HasVariableValue(CsvFieldGenerate<T> valueGenerator)` | `void` |
| `ICsvFieldMappingOptionalBuilder.IsOptional()` | `void` |
| `ICsvFieldMappingValidationBuilder<TModel>.ValidateUsing(CsvFieldValidate<TModel> validator)` | `ICsvFieldMappingValidationNextBuilder` |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Field mapping](../concepts/field-mapping.md)
- [Map CSV files](../how-to/map-csv-files.md)
- [CsvFileMappingBuilder](CsvFileMappingBuilder.md)
- [CsvFieldMapping](CsvFieldMapping.md)
- [File.CSV API reference](index.md)
</content>
