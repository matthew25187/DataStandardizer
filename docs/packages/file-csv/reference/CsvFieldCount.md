---
title: CsvFieldCount Delegate
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldCount Delegate

## Definition

Namespace: `DataStandardizer.File.CSV`

The delegate called when a line has a field count inconsistent with the other lines
in the CSV file, so you can handle it instead of letting a `CsvFileException` be
thrown.

**Syntax**

```csharp
public delegate void CsvFieldCount<TModel>(CsvFieldContext<TModel> context)
    where TModel : class;
```

## Remarks

Assigned to [CsvFileOptions.InconsistentFieldCountHandler](CsvFileOptions.md). The
`context` carries the offending line's model and header. See
[CsvFieldContext](CsvFieldContext.md).

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [CsvFieldContext](CsvFieldContext.md)
- [CsvFileOptions](CsvFileOptions.md)
- [File.CSV API reference](index.md)
</content>
