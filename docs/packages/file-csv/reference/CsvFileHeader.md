---
title: CsvFileHeader Delegate
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileHeader Delegate

## Definition

Namespace: `DataStandardizer.File.CSV`

The delegate a reader calls to supply the field names it uses, in place of any
header line in the file.

**Syntax**

```csharp
public delegate IReadOnlyList<string> CsvFileHeader(CsvFileHeaderLine? headerLine);
```

## Remarks

Assigned to [CsvFileOptions.HeaderHandler](CsvFileOptions.md); the reader
constructor throws `ArgumentException` for a delegate of any other type, such as
a `Func<CsvFileHeaderLine, IReadOnlyList<string>>`. A writer never calls it and
doesn't check it. Return the field names to use, in order;
they replace the names of the file's header line.
The reader calls the delegate at most once, whatever
[CsvFileOptions.HeaderHandling](CsvFileOptions.md) is. With `Use`, it calls it
as it reads the header line, passes that line, and `ReadLine` returns a header
line with the names the delegate gave. With `Ignore` and `None`, it calls it
after it reads the first record, before it returns that record, and passes the
ignored header line, or `null` with `None`; `ReadLine` returns no header line.
For a file with no records, it doesn't call the delegate. A header line can't
hold two fields with the same name, so with `Use`, and with `Ignore` when there
is a delegate, the mapper in effect gives a name that the header line repeats
the names its mappings give with `UseFieldName`, and the delegate is passed the
line with those names. A repeated name that no mapping renames makes `ReadLine`
throw `CsvFileException` as the header line is read, before the delegate is
called. See [header lines that repeat a name](../concepts/field-mapping.md#header-lines-that-repeat-a-name).
The delegate renames fields; it can't add or remove
them. With `Use`, the header line sets how many fields each record is expected
to have; with `Ignore` and `None`, the first record does; returning a different number
of names makes `ReadLine` throw `InvalidOperationException`. Returning
`null`, or a list that contains `null`, makes `ReadLine` throw
`InvalidOperationException`, and duplicate names
make it throw `CsvFileException`. When the delegate fails, or throws an exception of its own,
every later `ReadLine` call throws the same exception. The
nullable annotation on `headerLine` applies to the `netstandard2.1` and
`net10.0` builds.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [CsvFileOptions](CsvFileOptions.md)
- [CsvFileHeaderHandling](CsvFileHeaderHandling.md)
- [CsvFileHeaderLine](CsvFileHeaderLine.md)
- [File.CSV API reference](index.md)
