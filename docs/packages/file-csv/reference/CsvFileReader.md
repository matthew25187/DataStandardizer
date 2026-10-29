---
title: CsvFileReader Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileReader Class

## Definition

Namespace: `DataStandardizer.File.CSV`

A reader that provides fast, non-cached, forward-only access to CSV data. Each
call to `ReadLine` returns the next line as a `CsvFileHeaderLine` or a
`TRecordLine`, mapping fields to model properties along the way.

**Syntax**

```csharp
public sealed class CsvFileReader<TRecordLine> : CsvFileIoBase<TRecordLine>, IDisposable
    where TRecordLine : CsvFileRecordLine, new()
```

## Remarks

The reader is constructed over a `Stream`, a `TextReader`, or — on .NET Standard 2.0
and later targets — a file path. When the reader creates the underlying
`StreamReader` itself, it sizes the buffers (16 KB) and `Dispose` disposes it; a
caller-supplied `TextReader` is used as given, with its own buffering, and is left
open. Supplying a `CsvFileOptions` whose `Encoding` is set together with an existing
`TextReader` throws `ArgumentException`; specify the encoding when creating the
reader instead. The `TRecordLine` type argument must have a public parameterless
constructor (`new()`).

**Disposal.** `Dispose` may be called more than once. After the reader has been
disposed, `ReadLine`, `Context`, `RegisterMapper` and `UnregisterMapper` throw
`ObjectDisposedException`, including when the reader was constructed over a
caller-supplied `TextReader`.

**Thread safety.** A single reader instance is not thread-safe. Distinct instances
may be used concurrently, including for the same record type. Mapper registration
through `RegisterMapper` is process-wide per record type and visible to every reader
and writer instance.

**Mapper changes.** The reader works out the field names and mappings once and
reuses them for each line. If a mapper is registered or unregistered part way
through a file, the change applies from the next line read. Lines already
returned are not affected.

## Constructors

| Constructor | Notes |
| --- | --- |
| `CsvFileReader(Stream csvStream)` | Reads with a `StreamReader` using a 16 KB buffer, UTF-8 encoding and byte order mark detection. Throws `ArgumentNullException` if `csvStream` is `null`, or `ArgumentException` if the stream cannot be read. |
| `CsvFileReader(Stream csvStream, CsvFileOptions options)` | Honours `options.Encoding`. Throws `ArgumentNullException` if `options` is `null`. |
| `CsvFileReader(string csvFilePath)` | *(netstandard2.0+/.NET)* Opens the file for sequential reading with a 16 KB `FileStream` buffer and reads it with a `StreamReader` using a 16 KB buffer, UTF-8 encoding and byte order mark detection. Throws `ArgumentNullException` if `csvFilePath` is `null`. |
| `CsvFileReader(string csvFilePath, CsvFileOptions options)` | *(netstandard2.0+/.NET)* Honours `options.Encoding`. Throws `ArgumentNullException` if `csvFilePath` or `options` is `null`. |
| `CsvFileReader(TextReader reader)` | Throws `ArgumentNullException` if `reader` is `null`. |
| `CsvFileReader(TextReader reader, CsvFileOptions options)` | Throws `ArgumentException` if `options.Encoding` is set — specify the encoding when creating the reader. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Context` | `CsvContext Context { get; }` | The current mappers and options. Throws `ObjectDisposedException` after `Dispose`. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Disposes the underlying reader only when the reader created it. Calling it again has no effect. |
| `ReadLine()` | `ICsvFileLine?` | Reads the next line as a `CsvFileHeaderLine` or `CsvFileRecordLine`, or `null` at end of stream. Throws `CsvFileException` on field-count, mapping, value, or duplicate-name problems, or `ObjectDisposedException` after `Dispose`. |

Inherited members `RegisterMapper<TMapper>()` and `UnregisterMapper<TMapper>()` are
documented on [CsvFileIoBase](CsvFileIoBase.md).

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The `string` file-path constructors are available on `netstandard2.0`,
`netstandard2.1`, and the .NET targets only; on `netstandard1.3` construct the
reader from a `Stream` or `TextReader`.

## See also

- [Read CSV files](../how-to/read-csv-files.md)
- [Performance](../how-to/performance.md)
- [The CSV line model](../concepts/csv-line-model.md)
- [CsvFileWriter](CsvFileWriter.md)
- [CsvFileIoBase](CsvFileIoBase.md)
- [CsvFileOptions](CsvFileOptions.md)
- [CsvContext](CsvContext.md)
- [File.CSV API reference](index.md)
</content>
