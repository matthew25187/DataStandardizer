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

`TModel` must be the record line type of the reader or writer the options are
passed to: a `CsvFileWriter<OrderLine>` calls a `CsvFieldCount<OrderLine>`. The
reader or writer constructor throws `ArgumentException` for a handler of any other
type, such as a `CsvFieldCount<CsvFileRecordLine>` given to a writer of a derived
line type.

The line the delegate is called for is skipped. A reader doesn't return it, and a
writer writes nothing for it. Lines after it are checked against the same field count
as before it.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [CsvFieldContext](CsvFieldContext.md)
- [CsvFileOptions](CsvFileOptions.md)
- [File.CSV API reference](index.md)
