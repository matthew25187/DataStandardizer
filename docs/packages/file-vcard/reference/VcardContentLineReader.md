---
title: VcardContentLineReader Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardContentLineReader Class

## Definition

Namespace: `DataStandardizer.File.vCard`

A reader that provides fast, forward-only access to the content lines (RFC 6350 §3.3)
of vCard data. Lines are unfolded, and split into an optional group, a name, parameters
and a value.

**Syntax**

```csharp
public sealed class VcardContentLineReader : IDisposable, IAsyncDisposable
```

On `netstandard1.1` it implements `IDisposable` only.

## Remarks

The reader checks the structure of each line. It doesn't check the meaning of properties
and values, or the structure of a card, so a line may appear outside a card.

```csharp
using var reader = new VcardContentLineReader("contacts.vcf");
while (reader.ReadContentLine())
{
    var line = reader.CurrentLine;
    if (line.NameEquals("TEL"))
    {
        Console.WriteLine(line.RawValue.ToString());
    }
}
```

### Input

A `Stream` or file is read as strict UTF-8, the only charset of RFC 6350 §3.1. A byte
sequence that isn't valid UTF-8 throws `VcardFileException`, and so does a byte order
mark, which is not in the grammar and never selects an encoding. See
`VcardOptions.Encoding` and `VcardOptions.AcceptByteOrderMark`.

Decoding works on whole blocks of the data, so invalid data can be reported before
earlier lines in the same block have been returned, and the exception names no line.

A fold that splits a multi-octet UTF-8 sequence is repaired before decoding, as RFC 6350
§3.2 says it should be. A caller-supplied `TextReader` has already decoded the data, so
it can't be repaired there.

### Line breaks

Every content line ends with CRLF, including the last (RFC 6350 §3.3). A bare line feed
is accepted only with `VcardOptions.AcceptBareLineFeed`, a last line with no line break
only with `VcardOptions.AcceptMissingFinalLineBreak`, and a bare carriage return is never
a line break. Line breaks never depend on `Environment.NewLine`.

### Caret encoding

RFC 6868 updates RFC 6350, and not RFC 2426 (vCard 3.0) or vCard 2.1. So caret encoding in
parameter values is decoded only on the lines of a vCard 4.0 card, from its `VERSION:4.0`
line up to and including its `END:VCARD` line. The reader tracks this from the lines
themselves:

- `BEGIN:VCARD` forgets the version.
- A `VERSION` line sets it.
- The line after `END:VCARD` forgets it again.

The names match with or without a group, and in any case. A line before `VERSION`, or
outside a card, is not decoded. `VcardOptions.AcceptCaretEncodingInAllVersions` decodes
every line.

### TEXT escaping

Whether TEXT escaping applies depends on the value type, so it is removed only when asked
for, by `GetTextValue` on [VcardContentLine](VcardContentLine.md) or
[VcardContentLineView](VcardContentLineView.md).

Only the escapes of RFC 6350 §3.4 are accepted. Any other backslash, such as the `\:`
that some older software writes, throws `VcardFileException`.

### Errors

| Problem | What happens next |
| --- | --- |
| A line breaks the content line syntax, or the last line has no line break | `VcardFileException`. The line has been consumed, so reading can continue. |
| A bare line feed, a line longer than `MaxLineLength`, or data that can't be decoded | `VcardFileException`. Later reads throw `InvalidOperationException`. |
| The source throws an exception, including an `OperationCanceledException` while the read's token isn't canceled | That exception, which faults the task of an asynchronous read. Later reads throw `InvalidOperationException`, as characters may have been lost. |
| A read is canceled: its token is canceled, before the read or when it fails with an `OperationCanceledException` that carries any token or none | `OperationCanceledException`, and the task is canceled. Later reads throw `InvalidOperationException`. |

During `ReadLinesAsync`, awaiting `MoveNextAsync` throws an `OperationCanceledException` from either of the last two rows, but its task is canceled in both cases, on every target, as an async iterator's always is for an `OperationCanceledException`. Check the token to tell which it was.

### Concurrency

Only one read may be in progress at a time. Synchronous and asynchronous reads may follow
one another, but a read started while another is in progress throws
`InvalidOperationException`. If the reader is disposed during a read, that read completes
with `ObjectDisposedException`.

Asynchronous methods report data and I/O errors through the returned task. Misuse, such
as reading a disposed reader, throws from the call itself.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardContentLineReader(Stream vcardStream)` | The stream is left open when the reader is disposed. |
| `VcardContentLineReader(Stream vcardStream, VcardOptions options)` | As above, with options. |
| `VcardContentLineReader(string vcardFilePath)` | *netstandard2.0+/.NET.* The file is opened for asynchronous I/O, and closed when the reader is disposed, even if it was never read. |
| `VcardContentLineReader(string vcardFilePath, VcardOptions options)` | *netstandard2.0+/.NET.* As above, with options. |
| `VcardContentLineReader(TextReader reader)` | The text reader is left open when the reader is disposed. |
| `VcardContentLineReader(TextReader reader, VcardOptions options)` | As above, with options. `VcardOptions.Encoding` must be `null`. |

Each throws `ArgumentNullException` for a `null` argument, and `ArgumentException` for a
stream that can't be read or a `MaxLineLength` below 1.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CurrentLine` | `VcardContentLineView CurrentLine { get; }` | A view of the line last read. Throws `InvalidOperationException` if no line is current. |
| `LinesRead` | `long LinesRead { get; }` | The logical lines read so far, including any that failed to parse. |
| `PhysicalLinesRead` | `long PhysicalLinesRead { get; }` | The physical lines read so far. |

Each throws `ObjectDisposedException` once the reader is disposed.

## Methods

### Synchronous

| Method | Signature | Notes |
| --- | --- | --- |
| `Dispose` | `void Dispose()` | Releases the buffers, and the stream or file the reader opened itself. May be called more than once. |
| `ReadContentLine` | `bool ReadContentLine()` | Reads the next line without allocating; it is then available from `CurrentLine`. Returns `false` at the end of the data. |
| `ReadLine` | `VcardContentLine? ReadLine()` | Reads the next line, or returns `null` at the end of the data. |
| `ReadLines` | `IEnumerable<VcardContentLine> ReadLines()` | Reads the remaining lines as the sequence is enumerated. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Signature | Notes |
| --- | --- | --- |
| `DisposeAsync` | `ValueTask DisposeAsync()` | As `Dispose`, with which it shares its state. |
| `ReadContentLineAsync` | `ValueTask<bool> ReadContentLineAsync(CancellationToken cancellationToken = default)` | Completes synchronously when the buffer already holds the whole line. |
| `ReadLineAsync` | `ValueTask<VcardContentLine?> ReadLineAsync(CancellationToken cancellationToken = default)` | Completes synchronously when the buffer already holds the whole line. |
| `ReadLinesAsync` | `IAsyncEnumerable<VcardContentLine> ReadLinesAsync(CancellationToken cancellationToken = default)` | The token is combined with any passed to `GetAsyncEnumerator`. |

## Applies to

Targets `netstandard1.1` (synchronous, without the file-path constructors),
`netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardContentLineView](VcardContentLineView.md)
- [VcardContentLine](VcardContentLine.md)
- [VcardOptions](VcardOptions.md)
- [VcardFileException](VcardFileException.md)
- [VcardContentLineWriter](VcardContentLineWriter.md)
