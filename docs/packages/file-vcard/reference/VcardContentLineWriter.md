---
title: VcardContentLineWriter Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardContentLineWriter Class

## Definition

Namespace: `DataStandardizer.File.vCard`

A writer that provides fast, non-cached, forward-only output of the content lines
(RFC 6350 §3.3) of vCard data. It writes [VcardContentLine](VcardContentLine.md)
objects one at a time, with any group as a prefix, folding each and ending it with a
line break. It does not check the structure of a card or the meaning of properties.

**.NET:**

```csharp
public sealed class VcardContentLineWriter : IDisposable, IAsyncDisposable
```

**.NET Standard 1.1:**

```csharp
public sealed class VcardContentLineWriter : IDisposable
```

## Remarks

The writer is constructed over a `Stream`, a `TextWriter`, or, on .NET Standard 2.0
and later targets, a file path. When the writer creates the `StreamWriter` itself, it
uses a 16 KB buffer and, unless `VcardOptions.Encoding` says otherwise, UTF-8 without a
byte order mark that throws on characters it cannot encode. A file path creates or
overwrites the file, shared for reading. A caller-supplied `Stream` is left open. A
caller-supplied `TextWriter` is neither flushed nor closed on disposal, so call `Flush`
first. Supplying an `VcardOptions` whose `Encoding` is set together with a `TextWriter`
throws `ArgumentException`. A file path is opened with `FileOptions.Asynchronous`,
so the asynchronous methods do no synchronous I/O. To write a file only
synchronously, you can pass a `FileStream` opened without it to a `Stream`
constructor.

**Formatting.** Each line is formatted into a pooled buffer, with its group as a prefix
(`item1.TEL`), folded at `VcardOptions.FoldLength` octets of its UTF-8 encoding (75 by
default, not counting the line break), with `VcardOptions.FoldWhitespace` at the start of each continuation line
and `VcardOptions.LineBreak` at the end of each physical line. A fold never splits a
character or surrogate pair. The line is then handed to the underlying `TextWriter` in
a single write. Line breaks never depend on `Environment.NewLine`.

**Parameters and values.** Parameter values are quoted when they hold a `:`, `;` or `,`.
Values are written as they are, with any TEXT escaping already in place. A line read by
[VcardContentLineReader](VcardContentLineReader.md) with its default options reads back
as the same line, though a value that didn't need its quotes loses them, and the folds
can differ.

**Versions.** The writer tracks each card's version from the lines it writes, as the
reader does: `BEGIN:VCARD` forgets the version, a `VERSION` line sets it, and the line
after `END:VCARD` forgets it again. The names match with or without a group, and in any
case. A line that could not be written doesn't count.

| Line | Caret encoding (RFC 6868) | Folding |
| --- | --- | --- |
| In a 4.0 card, from `VERSION:4.0` to `END:VCARD` | `^` becomes `^^`, a double quote `^'`, and a line break `^n` | At `FoldLength` |
| In a 3.0 card, from `VERSION:3.0` to `END:VCARD` | None | At `FoldLength` |
| In a 2.1 card, from `VERSION:2.1` to `END:VCARD` | None | Never |
| Outside a card, or before its `VERSION` line | None | At `FoldLength` |

Where there is no caret encoding, a `^` is an ordinary character, and a parameter value
with a double quote or a line break can't be written faithfully, because RFC 6868 updates
neither RFC 2426 nor vCard 2.1. It throws `VcardFileException`, unless
`VcardOptions.AllowLossyVersionConversion` is set. Then a double quote is left out and a
line break, including a CRLF, becomes one space. The line still follows the grammar, but
reading it back gives a different value.

vCard 2.1 and later versions unfold lines differently, so no fold is read the same way by
every reader. The lines of a 2.1 card are therefore never folded, however long they are.

**Errors.** A line that cannot be written throws `VcardFileException` before anything of
it is written, so writing can continue with the next line. That happens when the
group, the property name or a parameter name is not one or more letters, digits or
hyphens, when a parameter value needs caret encoding outside a 4.0 card, when the value
holds a control character other than tab, or a lone surrogate, or when a
parameter value holds a control character other than tab, carriage return or line
feed, or a lone surrogate. The exception's `Data` holds `LineNumber`, the 1-based
number the line would have had in the output, `PropertyName` once the property name is
valid, `Group` once the group is valid, and `CardIndex` when the line is in a card. See
[VcardFileException](VcardFileException.md).

**Disposal.** `Dispose` and `DisposeAsync` may be called more than once and share their
state. They flush and close a stream writer or file the writer created itself, and
return the pooled buffer. After disposal, every member throws `ObjectDisposedException`.
If an operation is in progress, whether or not it has to wait, it completes with
`ObjectDisposedException`, unless it fails for another reason, and the writer is flushed and released once that operation
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
| `VcardContentLineWriter(Stream vcardStream)` | Writes UTF-8 without a byte order mark. The stream is left open. Throws `ArgumentNullException` if `vcardStream` is `null`, or `ArgumentException` if the stream cannot be written. |
| `VcardContentLineWriter(Stream vcardStream, VcardOptions options)` | Honours `options.Encoding`. Throws `ArgumentNullException` if `vcardStream` or `options` is `null`, or `ArgumentException` if the stream cannot be written or `FoldLength`, `FoldWhitespace` or `LineBreak` is not valid. |
| `VcardContentLineWriter(string vcardFilePath)` | *(netstandard2.0+/.NET)* Creates or overwrites the file for asynchronous writing, with a 16 KB `FileStream` buffer, as UTF-8 without a byte order mark. Throws `ArgumentNullException` if `vcardFilePath` is `null`. File system errors come from `FileStream`. |
| `VcardContentLineWriter(string vcardFilePath, VcardOptions options)` | *(netstandard2.0+/.NET)* Honours `options.Encoding`. Throws `ArgumentNullException` if `vcardFilePath` or `options` is `null`, or `ArgumentException` if `FoldLength`, `FoldWhitespace` or `LineBreak` is not valid. |
| `VcardContentLineWriter(TextWriter writer)` | The text writer is neither flushed nor closed on disposal. Throws `ArgumentNullException` if `writer` is `null`. |
| `VcardContentLineWriter(TextWriter writer, VcardOptions options)` | Throws `ArgumentNullException` if `writer` or `options` is `null`, or `ArgumentException` if `options.Encoding` is set or `FoldLength`, `FoldWhitespace` or `LineBreak` is not valid. |

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
| `WriteLine(VcardContentLine line)` | `void` | Writes one line. Throws `ObjectDisposedException` after `Dispose`; `ArgumentNullException` if `line` is `null`; `VcardFileException` if the line cannot be written (nothing has been written); `InvalidOperationException` if another operation is in progress or an earlier asynchronous operation was canceled. |
| `WriteLines(IEnumerable<VcardContentLine> lines, IProgress<VcardWriteProgress>? progress = null)` | `void` | Writes the lines in order. `progress` receives an [VcardWriteProgress](VcardWriteProgress.md) after each line, synchronously on the writing thread; an exception it throws ends the call, and the line it reported has been written. Throws `ArgumentNullException` if `lines` is `null`; `ArgumentException` if `lines` contains `null`, or `VcardFileException` if a line cannot be written, in both cases after the lines before it have been written; `ObjectDisposedException` if the writer is disposed before or during the call; `InvalidOperationException` as `WriteLine`. |
| `Flush()` | `void` | Flushes the underlying text writer, whether the writer created it or it was supplied. Throws `ObjectDisposedException` after `Dispose`, or `InvalidOperationException` as `WriteLine`. |
| `Dispose()` | `void` | Flushes and closes a stream writer or file the writer created itself, and returns the pooled buffer. Calling it again has no effect. |

### Asynchronous *(netstandard2.0+/.NET)*

Errors in how the writer is used (`ObjectDisposedException`, `ArgumentNullException`,
`InvalidOperationException`) are thrown by the call. Errors in the lines
(`VcardFileException`, and `ArgumentException` for a `null` item), I/O errors, and
cancellation (`OperationCanceledException`) are thrown by the task. If the writer is
disposed while an operation is in progress, whether or not it has to wait, the task throws `ObjectDisposedException`, unless the operation fails for another reason. If
an operation is canceled, the writer cannot continue.

| Method | Returns | Notes |
| --- | --- | --- |
| `WriteLineAsync(VcardContentLine line, CancellationToken cancellationToken = default)` | `ValueTask` | As `WriteLine`. Completes synchronously when the underlying text writer takes the line without waiting, as a `StreamWriter` does while the line fits its buffer, unless the writer is disposed during the write; it then completes once the writer has been flushed and released. |
| `WriteLinesAsync(IEnumerable<VcardContentLine> lines, IProgress<VcardWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | As `WriteLines`. |
| `WriteLinesAsync(IAsyncEnumerable<VcardContentLine> lines, IProgress<VcardWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | As `WriteLines`, for an asynchronous sequence. `cancellationToken` is passed to its enumerator. |
| `FlushAsync(CancellationToken cancellationToken = default)` | `Task` | As `Flush`. The token is passed to the text writer on `net10.0`; on .NET Standard it is checked before the flush. |
| `DisposeAsync()` | `ValueTask` | As `Dispose`, flushing asynchronously. Completes once the writer has been flushed and released, or immediately if an operation is in progress. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable`
are available on `netstandard2.0`, `netstandard2.1`, and the .NET targets only; on
`netstandard1.1` construct the writer from a `Stream` or `TextWriter` and write
synchronously. The `IAsyncEnumerable<VcardContentLine>` overload of `WriteLinesAsync`
uses Microsoft.Bcl.AsyncInterfaces on `netstandard2.0`. The nullable reference
annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [VcardContentLine](VcardContentLine.md)
- [VcardContentLineReader](VcardContentLineReader.md)
- [VcardWriteProgress](VcardWriteProgress.md)
- [VcardOptions](VcardOptions.md)
- [File.vCard API reference](index.md)
