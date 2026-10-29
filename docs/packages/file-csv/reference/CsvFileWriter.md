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
When the writer creates the underlying `StreamWriter` itself, it sizes the buffers
(16K characters) and `Dispose` disposes it; a caller-supplied `TextWriter` is used as
given, with its own buffering, and is left open. Supplying a `CsvFileOptions` whose
`Encoding` is set together with an existing `TextWriter` throws `ArgumentException`;
specify the encoding when creating the writer instead.

**Disposal.** `Dispose` may be called more than once. After the writer has been
disposed, `WriteLine`, `RegisterMapper` and `UnregisterMapper` throw
`ObjectDisposedException`, including when the writer was constructed over a
caller-supplied `TextWriter`.

**Thread safety.** A single writer instance is not thread-safe. Distinct instances
may be used concurrently, including for the same record type. Mapper registration
through `RegisterMapper` is process-wide per record type and visible to every reader
and writer instance.

**Mapper changes.** The writer works out the output field order and mappings once and
reuses them for each line. If a mapper is registered or unregistered part way
through a file, the change applies from the next line written. The new field count
is still checked against the previous line.

**Failed lines.** All of a line's field values are converted before any of them is
written. If a converter, value generator or type converter throws, or `WriteLine`
throws `CsvFileException`, nothing from that line is written.

## Constructors

| Constructor | Notes |
| --- | --- |
| `CsvFileWriter(Stream csvStream)` | Writes with a `StreamWriter` using a 16K-character buffer and UTF-8 encoding without a byte order mark. Throws `ArgumentNullException` if `csvStream` is `null`. |
| `CsvFileWriter(Stream csvStream, CsvFileOptions options)` | Honours `options.Encoding`. Throws `ArgumentNullException` if `options` is `null`. |
| `CsvFileWriter(string csvFilePath)` | *(netstandard2.0+/.NET)* Creates (or overwrites) the file with a 16 KB `FileStream` buffer and writes it with a `StreamWriter` using a 16K-character buffer and UTF-8 encoding without a byte order mark. Throws `ArgumentNullException` if `csvFilePath` is `null`. |
| `CsvFileWriter(string csvFilePath, CsvFileOptions options)` | *(netstandard2.0+/.NET)* Honours `options.Encoding`. Throws `ArgumentNullException` if `csvFilePath` or `options` is `null`. |
| `CsvFileWriter(TextWriter writer)` | Throws `ArgumentNullException` if `writer` is `null`. |
| `CsvFileWriter(TextWriter writer, CsvFileOptions options)` | Throws `ArgumentException` if `options.Encoding` is set — specify the encoding when creating the writer. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Disposes the underlying writer only when the writer created it. Calling it again has no effect. |
| `WriteLine(ICsvFileLine csvLine)` | `void` | Writes a header or record line. Throws `CsvFileException` on an invalid field name or inconsistent field count, or `ObjectDisposedException` after `Dispose`. |

Inherited members `RegisterMapper<TMapper>()` and `UnregisterMapper<TMapper>()` are
documented on [CsvFileIoBase](CsvFileIoBase.md).

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The `string` file-path constructors are available on `netstandard2.0`,
`netstandard2.1`, and the .NET targets only; on `netstandard1.3` construct the
writer from a `Stream` or `TextWriter`.

## See also

- [Write CSV files](../how-to/write-csv-files.md)
- [Performance](../how-to/performance.md)
- [The CSV line model](../concepts/csv-line-model.md)
- [CsvFileReader](CsvFileReader.md)
- [CsvFileIoBase](CsvFileIoBase.md)
- [CsvFileOptions](CsvFileOptions.md)
- [File.CSV API reference](index.md)
</content>
