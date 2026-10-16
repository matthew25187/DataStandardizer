---
title: IcalContentLineWriter Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalContentLineWriter Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A writer that provides fast, non-cached, forward-only output of the content lines
(RFC 5545 §3.1) of iCalendar data. It writes [IcalContentLine](IcalContentLine.md)
objects one at a time, folding each and ending it with a line break. It does not check
`BEGIN`/`END` nesting or the meaning of properties.

**.NET:**

```csharp
public sealed class IcalContentLineWriter : IDisposable, IAsyncDisposable
```

**.NET Standard 1.1:**

```csharp
public sealed class IcalContentLineWriter : IDisposable
```

## Remarks

The writer is constructed over a `Stream`, a `TextWriter`, or, on .NET Standard 2.0
and later targets, a file path. When the writer creates the `StreamWriter` itself, it
uses a 16 KB buffer and, unless `IcalOptions.Encoding` says otherwise, UTF-8 without a
byte order mark that throws on characters it cannot encode. A file path creates or
overwrites the file, shared for reading. A caller-supplied `Stream` is left open. A
caller-supplied `TextWriter` is neither flushed nor closed on disposal, so call `Flush`
first. Supplying an `IcalOptions` whose `Encoding` is set together with a `TextWriter`
throws `ArgumentException`. For asynchronous file output, pass a `FileStream` opened
with `FileOptions.Asynchronous` to a `Stream` constructor.

**Formatting.** Each line is formatted into a pooled buffer, folded at
`IcalOptions.FoldLength` octets of its UTF-8 encoding (75 by default, not counting the
line break), with `IcalOptions.FoldWhitespace` at the start of each continuation line
and `IcalOptions.LineBreak` at the end of each physical line. A fold never splits a
character or surrogate pair. The line is then handed to the underlying `TextWriter` in
a single write. Line breaks never depend on `Environment.NewLine`.

**Parameters and values.** Parameter values are caret encoded (RFC 6868) and quoted
when they hold a `:`, `;` or `,`. A line read by `IcalContentLineReader` is written
with its parameters exactly as they were read, so only its folding can differ. Values
are written as they are, with any TEXT escaping already in place.

**Errors.** A line that cannot be written throws `IcalFileException` before anything of
it is written, so writing can continue with the next line. That happens when the
property name or a parameter name is not one or more letters, digits or hyphens, when
the value holds a control character other than tab, or a lone surrogate, or when a
parameter value holds a control character other than tab, carriage return or line
feed, or a lone surrogate. The exception's `Data` holds `LineNumber`, the 1-based
number the line would have had in the output, and `PropertyName` once the property
name is valid.

**Disposal.** `Dispose` and `DisposeAsync` may be called more than once and share their
state. They flush and close a stream writer or file the writer created itself, and
return the pooled buffer. After disposal, every member throws `ObjectDisposedException`.
If an operation is in progress on another thread, it completes with
`ObjectDisposedException`, and the writer is flushed and released once that operation
has finished. `DisposeAsync` disposes the writer even if the flush throws.

**Thread safety and async rules.** A single writer instance is not thread-safe. Only
one operation may be in progress at a time. Synchronous and asynchronous calls may
follow one another, but a call made while another is in progress throws
`InvalidOperationException`. A canceled asynchronous operation leaves the writer
unable to continue, and later operations throw `InvalidOperationException`. In the bulk
methods, the whole call is one operation, so neither the lines being enumerated nor
the progress callback may call the writer.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalContentLineWriter(Stream icalStream)` | Writes UTF-8 without a byte order mark. The stream is left open. Throws `ArgumentNullException` if `icalStream` is `null`, or `ArgumentException` if the stream cannot be written. |
| `IcalContentLineWriter(Stream icalStream, IcalOptions options)` | Honours `options.Encoding`. Throws `ArgumentNullException` if `icalStream` or `options` is `null`, or `ArgumentException` if the stream cannot be written or `FoldLength`, `FoldWhitespace` or `LineBreak` is not valid. |
| `IcalContentLineWriter(string icalFilePath)` | *(netstandard2.0+/.NET)* Creates or overwrites the file, with a 16 KB `FileStream` buffer, as UTF-8 without a byte order mark. Throws `ArgumentNullException` if `icalFilePath` is `null`. File system errors come from `FileStream`. |
| `IcalContentLineWriter(string icalFilePath, IcalOptions options)` | *(netstandard2.0+/.NET)* Honours `options.Encoding`. Throws `ArgumentNullException` if `icalFilePath` or `options` is `null`, or `ArgumentException` if `FoldLength`, `FoldWhitespace` or `LineBreak` is not valid. |
| `IcalContentLineWriter(TextWriter writer)` | The text writer is neither flushed nor closed on disposal. Throws `ArgumentNullException` if `writer` is `null`. |
| `IcalContentLineWriter(TextWriter writer, IcalOptions options)` | Throws `ArgumentNullException` if `writer` or `options` is `null`, or `ArgumentException` if `options.Encoding` is set or `FoldLength`, `FoldWhitespace` or `LineBreak` is not valid. |

The options are valid when `FoldLength` is at least 4, `FoldWhitespace` is a space or
a horizontal tab, and `LineBreak` is `"\r\n"` or `"\n"`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `LinesWritten` | `long LinesWritten { get; }` | The number of logical (unfolded) lines written so far. Throws `ObjectDisposedException` after `Dispose`. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `WriteLine(IcalContentLine line)` | `void` | Writes one line. Throws `ObjectDisposedException` after `Dispose`; `ArgumentNullException` if `line` is `null`; `IcalFileException` if the line cannot be written (nothing has been written); `InvalidOperationException` if another operation is in progress or an earlier asynchronous operation was canceled. |
| `WriteLines(IEnumerable<IcalContentLine> lines, IProgress<IcalWriteProgress>? progress = null)` | `void` | Writes the lines in order. `progress` receives an [IcalWriteProgress](IcalWriteProgress.md) after each line, synchronously on the writing thread; an exception it throws ends the call, and the line it reported has been written. Throws `ArgumentNullException` if `lines` is `null`; `ArgumentException` if `lines` contains `null`, or `IcalFileException` if a line cannot be written, in both cases after the lines before it have been written; `ObjectDisposedException` if the writer is disposed before or during the call; `InvalidOperationException` as `WriteLine`. |
| `Flush()` | `void` | Flushes the underlying text writer, whether the writer created it or it was supplied. Throws `ObjectDisposedException` after `Dispose`, or `InvalidOperationException` as `WriteLine`. |
| `Dispose()` | `void` | Flushes and closes a stream writer or file the writer created itself, and returns the pooled buffer. Calling it again has no effect. |

### Asynchronous *(netstandard2.0+/.NET)*

Errors in how the writer is used (`ObjectDisposedException`, `ArgumentNullException`,
`InvalidOperationException`) are thrown by the call. Errors in the lines
(`IcalFileException`, and `ArgumentException` for a `null` item), I/O errors, and
cancellation (`OperationCanceledException`) are thrown by the task. If the writer is
disposed while an operation is pending, the task throws `ObjectDisposedException`. If
an operation is canceled, the writer cannot continue.

| Method | Returns | Notes |
| --- | --- | --- |
| `WriteLineAsync(IcalContentLine line, CancellationToken cancellationToken = default)` | `ValueTask` | As `WriteLine`. Completes synchronously when the underlying text writer takes the line without waiting, as a `StreamWriter` does while the line fits its buffer. |
| `WriteLinesAsync(IEnumerable<IcalContentLine> lines, IProgress<IcalWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | As `WriteLines`. |
| `WriteLinesAsync(IAsyncEnumerable<IcalContentLine> lines, IProgress<IcalWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | As `WriteLines`, for an asynchronous sequence. `cancellationToken` is passed to its enumerator. |
| `FlushAsync(CancellationToken cancellationToken = default)` | `Task` | As `Flush`. The token is passed to the text writer on .NET 8 and later; on .NET Standard it is checked before the flush. |
| `DisposeAsync()` | `ValueTask` | As `Dispose`, flushing asynchronously. Completes once the writer has been flushed and released. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable`
are available on `netstandard2.0`, `netstandard2.1`, and the .NET targets only; on
`netstandard1.1` construct the writer from a `Stream` or `TextWriter` and write
synchronously. The `IAsyncEnumerable<IcalContentLine>` overload of `WriteLinesAsync`
uses Microsoft.Bcl.AsyncInterfaces on `netstandard2.0`. The nullable reference
annotations apply to the `netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [The layered model](../concepts/layered-model.md)
- [Standards compliance](../concepts/standards-compliance.md)
- [IcalContentLine](IcalContentLine.md)
- [IcalContentLineReader](IcalContentLineReader.md)
- [IcalWriteProgress](IcalWriteProgress.md)
- [IcalWriter](IcalWriter.md)
- [IcalOptions](IcalOptions.md)
- [File.iCal API reference](index.md)
