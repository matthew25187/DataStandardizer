---
title: IcalContentLineReader Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalContentLineReader Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A reader that provides fast, non-cached, forward-only access to the content lines
(RFC 5545 §3.1) of iCalendar data. Each line is unfolded and its structure is
checked: a name, any parameters and a value. The meaning of properties and values is
not checked, and `BEGIN`/`END` nesting and `VERSION` are not checked either.

**.NET:**

```csharp
public sealed class IcalContentLineReader : IDisposable, IAsyncDisposable
```

**.NET Standard 1.1:**

```csharp
public sealed class IcalContentLineReader : IDisposable
```

## Remarks

The reader is constructed over a `Stream`, a `TextReader`, or, on .NET Standard 2.0
and later targets, a file path. When the reader creates the `StreamReader` itself,
it uses 16 KB buffers, UTF-8 unless `IcalOptions.Encoding` says otherwise, and byte
order mark detection. A file is opened for sequential reading with `FileShare.Read`.
A caller-supplied `Stream` or `TextReader` is left open when the reader is disposed.
Supplying an `IcalOptions` whose `Encoding` is set together with a `TextReader` throws
`ArgumentException`. A byte order mark at the start of the data is skipped.

**Line breaks.** A content line ends with CRLF. A bare line feed is accepted only
with `IcalOptions.AcceptBareLineFeed`; otherwise it throws `IcalFileException`. A bare
carriage return is never a line break: it stays in the line, where parsing rejects
it. The last line need not end with a line break. Line breaks never depend on
`Environment.NewLine`. A fold is a line break followed by one space or tab; both are
removed.

**UTF-8 folds.** When the reader decodes UTF-8 from a `Stream` or file itself, a fold
that splits a multi-octet UTF-8 sequence is repaired before decoding. A
caller-supplied `TextReader` has already decoded the data, so such a fold cannot be
repaired.

**Decoding.** Parameter values have their RFC 6868 caret encoding decoded (`^n`,
`^'` and `^^`; any other caret is kept). Enclosing double quotes are removed. TEXT
unescaping (RFC 5545 §3.3.11) depends on the value type, so it is done only when you
ask for it with `GetTextValue`.

**Errors.** A line that does not follow the content line syntax throws
`IcalFileException` after the line has been consumed, so reading can continue with
the next line. A bare line feed that is not accepted, or a line longer than
`IcalOptions.MaxLineLength`, also throws `IcalFileException`, but then where the line
ends is unknown, so every later read throws `InvalidOperationException`. The
exception's `Data` holds `LineNumber`, `PhysicalLineNumber` and, once the name has
been read, `PropertyName`. See [IcalFileException](IcalFileException.md).

**Zero-copy reads.** `ReadContentLine` and `ReadContentLineAsync` read a line without
allocating; the line is then available as an [IcalContentLineView](IcalContentLineView.md)
from `CurrentLine`. The view, and any span or memory taken from it, is valid only until
the next read starts or the reader is disposed. `ReadLine` and `ReadLineAsync` return
an allocated [IcalContentLine](IcalContentLine.md) that stays valid; the line is also
current, so `CurrentLine` works after them too.

**Memory.** The reader keeps each logical line in one pooled buffer (`ArrayPool<char>.Shared`)
that grows to fit the longest line. RFC 5545 sets no limit on line length, so set
`IcalOptions.MaxLineLength` when reading untrusted data.

**Disposal.** `Dispose` and `DisposeAsync` may be called more than once and share
their state. They return the pooled buffers and close a stream or file the reader
opened itself. After disposal, every member throws `ObjectDisposedException`. If a read
is in progress on another thread, it completes with `ObjectDisposedException`, and the
buffers are released once it has finished with them.

**Thread safety and async rules.** A single reader instance is not thread-safe. Only
one read may be in progress at a time. Synchronous and asynchronous reads may follow
one another, but a read started while another is in progress throws
`InvalidOperationException`. A canceled asynchronous read leaves the reader unable to
continue, and later reads throw `InvalidOperationException`. The asynchronous methods
complete synchronously when the buffer already holds the whole line.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalContentLineReader(Stream icalStream)` | Reads as UTF-8 unless the stream starts with a byte order mark. The stream is left open. Throws `ArgumentNullException` if `icalStream` is `null`, or `ArgumentException` if the stream cannot be read. |
| `IcalContentLineReader(Stream icalStream, IcalOptions options)` | Honours `options.Encoding`. Throws `ArgumentNullException` if `icalStream` or `options` is `null`, or `ArgumentException` if the stream cannot be read or `options.MaxLineLength` is less than 1. |
| `IcalContentLineReader(string icalFilePath)` | *(netstandard2.0+/.NET)* Opens the file for asynchronous, sequential reading with a 16 KB `FileStream` buffer and reads it as UTF-8 unless it starts with a byte order mark. Throws `ArgumentNullException` if `icalFilePath` is `null`. File system errors come from `FileStream`. |
| `IcalContentLineReader(string icalFilePath, IcalOptions options)` | *(netstandard2.0+/.NET)* Honours `options.Encoding`. Throws `ArgumentNullException` if `icalFilePath` or `options` is `null`, or `ArgumentException` if `options.MaxLineLength` is less than 1. |
| `IcalContentLineReader(TextReader reader)` | The text reader is left open. Throws `ArgumentNullException` if `reader` is `null`. |
| `IcalContentLineReader(TextReader reader, IcalOptions options)` | Throws `ArgumentNullException` if `reader` or `options` is `null`, or `ArgumentException` if `options.Encoding` is set or `options.MaxLineLength` is less than 1. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CurrentLine` | `IcalContentLineView CurrentLine { get; }` | A view of the line last read by `ReadContentLine`, `ReadLine` or their async forms, without allocation. Throws `InvalidOperationException` if no line is current: none has been read yet, the last read reached the end of the data, or it threw. Throws `ObjectDisposedException` after `Dispose`. |
| `LinesRead` | `long LinesRead { get; }` | The number of logical (unfolded) lines read so far, including any that failed to parse. Throws `ObjectDisposedException` after `Dispose`. |
| `PhysicalLinesRead` | `long PhysicalLinesRead { get; }` | The number of physical lines read so far. A folded line spans several. Throws `ObjectDisposedException` after `Dispose`. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `ReadContentLine()` | `bool` | Reads the next line without allocation and makes it current. `false` at the end of the data. Throws `IcalFileException` for a line that is not valid, a bare line feed that is not accepted, or a line longer than `MaxLineLength`; `ObjectDisposedException` after `Dispose`; `InvalidOperationException` if another read is in progress, or an earlier read found a line break or line length error or was canceled. |
| `ReadLine()` | `IcalContentLine?` | Reads the next line as an allocated `IcalContentLine`, or `null` at the end of the data. The line is also current. Throws as `ReadContentLine`. |
| `ReadLines()` | `IEnumerable<IcalContentLine>` | The remaining lines, read as the sequence is enumerated. Throws `ObjectDisposedException` from the call itself if the reader is disposed; during enumeration, throws as `ReadLine`. |
| `Dispose()` | `void` | Returns the pooled buffers and closes a stream or file the reader opened itself. Calling it again has no effect. |

### Asynchronous *(netstandard2.0+/.NET)*

Errors in how the reader is used (`ObjectDisposedException`, `InvalidOperationException`)
are thrown by the call. Errors in the data (`IcalFileException`) and cancellation
(`OperationCanceledException`) are thrown by the task. If the reader is disposed while
a read is pending, the task throws `ObjectDisposedException`.

| Method | Returns | Notes |
| --- | --- | --- |
| `ReadContentLineAsync(CancellationToken cancellationToken = default)` | `ValueTask<bool>` | As `ReadContentLine`. If canceled, the reader cannot continue. |
| `ReadLineAsync(CancellationToken cancellationToken = default)` | `ValueTask<IcalContentLine?>` | As `ReadLine`. If canceled, the reader cannot continue. |
| `ReadLinesAsync(CancellationToken cancellationToken = default)` | `IAsyncEnumerable<IcalContentLine>` | The remaining lines. The token is combined with any token passed to `GetAsyncEnumerator`. Throws `ObjectDisposedException` from the call itself if the reader is disposed. |
| `DisposeAsync()` | `ValueTask` | Same as `Dispose`; returns a completed task. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable`
are available on `netstandard2.0`, `netstandard2.1`, and the .NET targets only; on
`netstandard1.1` construct the reader from a `Stream` or `TextReader` and read
synchronously. The nullable reference annotations apply to the `netstandard2.1`,
`net8.0`, and `net10.0` builds.

## See also

- [Read zero-copy](../how-to/read-zero-copy.md)
- [Read asynchronously](../how-to/read-asynchronously.md)
- [Read non-compliant feeds](../how-to/read-non-compliant-feeds.md)
- [The layered model](../concepts/layered-model.md)
- [IcalContentLineView](IcalContentLineView.md)
- [IcalContentLine](IcalContentLine.md)
- [IcalContentLineWriter](IcalContentLineWriter.md)
- [IcalReader](IcalReader.md)
- [IcalOptions](IcalOptions.md)
- [File.iCal API reference](index.md)
