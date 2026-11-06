---
title: Read asynchronously
parent: File.vCard
grand_parent: Packages
nav_order: 2
---

# Read and write asynchronously

`VcardReader` and `VcardWriter` have asynchronous members alongside the synchronous
ones, and so do the content-line reader and writer they are built on, and the jCard
and xCard readers and writers. Use them to read cards from the network, or a large
file, without blocking a thread.

> **Applies to:** .NET Standard 2.0 and later, and .NET 10. There are no
> asynchronous members on .NET Standard 1.1.

## Read cards

`ReadCardsAsync` returns an `IAsyncEnumerable<VcardCard>`:

```csharp
using DataStandardizer.File.vCard;

await using var reader = new VcardReader("contacts.vcf");
await foreach (var card in reader.ReadCardsAsync(cancellationToken))
{
    Console.WriteLine(card.FormattedNames.GetPreferred()?.Value ?? "(no name)");
}
```

The token you pass is combined with any token passed to `GetAsyncEnumerator`, for
example by `WithCancellation`.

To read one card at a time, call `ReadCardAsync`. Its result is `null` at the end of
the data:

```csharp
await using var reader = new VcardReader(stream);
var card = await reader.ReadCardAsync(cancellationToken);
```

The checks are the same as for `ReadCard`, and a card that breaks a rule faults the
task with `VcardFileException`. See [Read and write .vcf files](read-write-vcf-files.md).

## Read content lines

`VcardContentLineReader`, the lower layer, has three asynchronous reads:

| Member | Returns |
| --- | --- |
| `ReadContentLineAsync` | `ValueTask<bool>`. The line is then in `CurrentLine`, without allocation. |
| `ReadLineAsync` | `ValueTask<VcardContentLine?>`: a copy of the line, or `null` at the end. |
| `ReadLinesAsync` | `IAsyncEnumerable<VcardContentLine>`. |

```csharp
await using var reader = new VcardContentLineReader("contacts.vcf");
while (await reader.ReadContentLineAsync(cancellationToken))
{
    var line = reader.CurrentLine;
    if (line.NameEquals("FN"))
    {
        Console.WriteLine(line.GetTextValue().ToString());
    }
}
```

`CurrentLine` is a view of the reader's buffer. A call to `ReadContentLineAsync`
moves the reader on as soon as it is made, before its task completes, so take what
you need from a view before you start the next read. The `Memory` members of the
view, such as `RawValueMemory`, can be held across an `await` that isn't a read. See
[Read with zero copies](read-zero-copy.md).

## Reads complete synchronously when the data is buffered

The read methods of the content-line reader return `ValueTask`. When the reader's
buffer already holds the whole line, the task completes synchronously and nothing is
allocated for it. Only a read that has to fetch more data from the stream waits.

## No synchronous I/O

The asynchronous members do no synchronous I/O. They fill the buffer with the
`ReadAsync` of the underlying text reader, and write with its `WriteAsync` and
`FlushAsync`.

A file that a reader or writer opens from a path is opened with
`FileOptions.Asynchronous`, so its asynchronous members do overlapped file I/O. The
synchronous members still work on such a file, at a small cost on Windows. If you
only read or write a file synchronously, you can open a `FileStream` without
`FileOptions.Asynchronous` yourself and pass it to a `Stream` constructor.

A stream that you pass in is used as it is.

## One operation at a time

A reader or writer runs one operation at a time. Synchronous and asynchronous calls
may follow one another, but a call made while another is in progress throws
`InvalidOperationException`. So await each read before you start the next.

A `WriteCardsAsync` call is one operation from start to end, so neither the cards it
enumerates nor its progress callback may call the writer.

## Cancellation

Pass a `CancellationToken` to any asynchronous member. If a read or write is
canceled, the task completes with an `OperationCanceledException`, and the reader or
writer can't continue: later calls throw `InvalidOperationException`. Dispose it and
start again.

On .NET Standard 2.0, the `TextReader.ReadAsync` and `TextWriter.WriteAsync`
overloads take no token. The token is then checked before each read and each write,
rather than passed to them.

## Write cards

```csharp
await using var writer = new VcardWriter("contacts.vcf");
await writer.WriteCardsAsync(cards, progress, cancellationToken);
```

- **`WriteCardAsync`:** writes one card.
- **`WriteCardsAsync`:** writes an `IEnumerable<VcardCard>` or an
  `IAsyncEnumerable<VcardCard>`. It checks for cancellation between cards.
- **`FlushAsync`:** flushes the underlying text writer.

Each card is checked before any of it is written, as it is for `WriteCard`.

Because `WriteCardsAsync` accepts an `IAsyncEnumerable<VcardCard>`, you can stream
cards from a reader straight to a writer, one at a time:

```csharp
await using var reader = new VcardReader("contacts.vcf");
await using var writer = new VcardWriter("copy.vcf");
await writer.WriteCardsAsync(reader.ReadCardsAsync(cancellationToken), null, cancellationToken);
```

### Report progress

`WriteCardsAsync` takes an optional `IProgress<VcardWriteProgress>`. It is called
synchronously, on the writing thread, after each card has been handed to the
underlying text writer. Its counts, `CardsWritten` and `LinesWritten`, are totals for
the writer, not for the call.

```csharp
var progress = new Progress<VcardWriteProgress>(
    value => Console.WriteLine($"{value.CardsWritten} cards written"));
await writer.WriteCardsAsync(cards, progress, cancellationToken);
```

`Progress<T>` posts each report to the synchronization context it was created on, so
a report can arrive after the call has returned.

## Dispose asynchronously

The readers and writers implement `IAsyncDisposable`, so use `await using`.

- **Reader:** `DisposeAsync` does the same as `Dispose`.
- **Writer:** `DisposeAsync` flushes and closes the file or stream writer it created
  itself, asynchronously. `Dispose` would flush it synchronously. A `TextWriter` you
  supplied is neither flushed nor closed, and a `Stream` you supplied is left open.

If you dispose a reader or writer while an operation is in progress, that operation
completes with `ObjectDisposedException`.

## jCard and xCard

`JcardReader`, `JcardWriter`, `XcardReader` and `XcardWriter` have the same
asynchronous members as `VcardReader` and `VcardWriter`. An xCard reader or writer
you build on your own `XmlReader` or `XmlWriter` needs its `Async` setting to be
`true`. See [Convert between .vcf, jCard and xCard](convert-formats.md).

## Related

- [Read and write .vcf files](read-write-vcf-files.md)
- [Read with zero copies](read-zero-copy.md)
- [VcardReader](../reference/VcardReader.md),
  [VcardWriter](../reference/VcardWriter.md),
  [VcardContentLineReader](../reference/VcardContentLineReader.md) and
  [VcardWriteProgress](../reference/VcardWriteProgress.md)
