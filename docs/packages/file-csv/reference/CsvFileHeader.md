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
The reader calls the delegate once, whatever
[CsvFileOptions.HeaderHandling](CsvFileOptions.md) is. With `Use`, it calls it
as it reads the header line, passes that line, and `ReadLine` returns a header
line with the names the delegate gave. With `Ignore`, it calls it as it reads
the header line and passes that line, and `ReadLine` returns no header line.
With `None`, it calls it as it reads the first record and passes `null`. A
header line read from the file is checked for duplicate names before the
delegate is called; duplicates throw `CsvFileException`. The delegate renames fields; it can't add or remove
them. When there is a header line, returning a different number of names from
its number of fields makes `ReadLine` throw `InvalidOperationException`, and the
header line sets how many fields each record is expected to have. Returning
`null` makes `ReadLine` throw `InvalidOperationException`, and duplicate names
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
