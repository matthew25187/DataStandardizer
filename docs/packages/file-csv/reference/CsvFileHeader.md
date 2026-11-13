---
title: CsvFileHeader Delegate
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileHeader Delegate

## Definition

Namespace: `DataStandardizer.File.CSV`

The delegate a reader calls to supply field names when there is no header line
to take them from.

**Syntax**

```csharp
public delegate IReadOnlyList<string> CsvFileHeader(CsvFileHeaderLine? headerLine);
```

## Remarks

Assigned to [CsvFileOptions.HeaderHandler](CsvFileOptions.md); the reader
constructor throws `ArgumentException` for a delegate of any other type, such as
a `Func<CsvFileHeaderLine, IReadOnlyList<string>>`. A writer never calls it and
doesn't check it. Return the field names to use, in order.
The reader calls the delegate only when there is no header line to take names
from, that is when [CsvFileOptions.HeaderHandling](CsvFileOptions.md) is `None`
or `Ignore`, and then once, as it reads the first record. `headerLine` is
`null` on every call. When `HeaderHandling` is `Use`, the names come from the
header line and the reader never calls the delegate. Returning `null` makes `ReadLine` throw
`InvalidOperationException`, and duplicate names make it throw
`CsvFileException`. When the delegate fails, or throws an exception of its own,
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
