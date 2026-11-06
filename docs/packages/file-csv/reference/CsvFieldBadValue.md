---
title: CsvFieldBadValue Delegate
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldBadValue Delegate

## Definition

Namespace: `DataStandardizer.File.CSV`

The delegate called when a bad CSV field value is encountered, so you can handle it
instead of letting a `CsvFileException` be thrown.

**Syntax**

```csharp
public delegate void CsvFieldBadValue<TModel>(CsvFieldContext<TModel> context)
    where TModel : class;
```

## Remarks

Assigned to [CsvFileOptions.BadValueHandler](CsvFileOptions.md). The `context`
carries the field's identity, raw value, and the model. See
[CsvFieldContext](CsvFieldContext.md).

A reader calls it for a field it can't read, such as an unquoted field that holds a
quote, for a field a mapping's validator rejects, and for a field it can't convert
to the mapped property's type. See
[converting field values](../concepts/field-mapping.md#converting-field-values).
An exception thrown by a [CsvFieldConvertFromString](CsvFieldConvertFromString.md)
delegate isn't a bad value: it propagates unchanged, and the handler isn't called.

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [CsvFieldContext](CsvFieldContext.md)
- [CsvFileOptions](CsvFileOptions.md)
- [File.CSV API reference](index.md)
