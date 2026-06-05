---
title: CsvFileWriter Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileWriter Class

## Definition

Namespace: `DataStandardizer.File.CSV`

A writer that serializes CSV lines to a stream, text writer, or file. Each call to
`WriteLine` writes a header line or a record line, applying the configured quoting,
delimiter, and line-termination rules.

**Syntax**

```csharp
public sealed class CsvFileWriter<TRecordLine> : CsvFileIoBase<TRecordLine>, IDisposable
    where TRecordLine : CsvFileRecordLine
```

## Remarks

Unlike `CsvFileReader<TRecordLine>`, the writer's `TRecordLine` argument is **not**
required to have a parameterless constructor. The writer is constructed over a
`Stream`, a `TextWriter`, or — on .NET Standard 2.0 and later targets — a file path.
When the writer creates the underlying `StreamWriter` itself, `Dispose` disposes it;
a caller-supplied `TextWriter` is left open. Supplying a `CsvFileOptions` whose
`Encoding` is set together with an existing `TextWriter` throws `ArgumentException`;
specify the encoding when creating the writer instead.

## Constructors

| Constructor | Notes |
| --- | --- |
| `CsvFileWriter(Stream csvStream)` | Writes with a default `StreamWriter`. Throws `ArgumentNullException` if `csvStream` is `null`. |
| `CsvFileWriter(Stream csvStream, CsvFileOptions options)` | Honours `options.Encoding`. |
| `CsvFileWriter(string csvFilePath)` | *(netstandard2.0+/.NET)* Creates the file with a default `StreamWriter`. Throws `ArgumentNullException` if `csvFilePath` is `null`. |
| `CsvFileWriter(string csvFilePath, CsvFileOptions options)` | *(netstandard2.0+/.NET)* Honours `options.Encoding`. |
| `CsvFileWriter(TextWriter writer)` | Throws `ArgumentNullException` if `writer` is `null`. |
| `CsvFileWriter(TextWriter writer, CsvFileOptions options)` | Throws `ArgumentException` if `options.Encoding` is set — specify the encoding when creating the writer. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Disposes the underlying writer only when the writer created it. |
| `WriteLine(ICsvFileLine csvLine)` | `void` | Writes a header or record line. Throws `CsvFileException` on an invalid field name or inconsistent field count. |

Inherited members `RegisterMapper<TMapper>()` and `UnregisterMapper<TMapper>()` are
documented on [CsvFileIoBase](CsvFileIoBase.md).

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`. The `string`
file-path constructors are available on `netstandard2.0` and the .NET targets only;
on `netstandard1.3` construct the writer from a `Stream` or `TextWriter`.

## See also

- [Write CSV files](../how-to/write-csv-files.md)
- [The CSV line model](../concepts/csv-line-model.md)
- [CsvFileReader](CsvFileReader.md)
- [CsvFileIoBase](CsvFileIoBase.md)
- [CsvFileOptions](CsvFileOptions.md)
- [File.CSV API reference](index.md)
</content>
