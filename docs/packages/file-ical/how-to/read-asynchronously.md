---
title: Read asynchronously
parent: File.iCal
grand_parent: Packages
nav_order: 2
---

# Read and write asynchronously

`IcalReader` and `IcalWriter` have asynchronous members alongside the synchronous
ones, and so do the content-line reader and writer they are built on. Use them to
read a feed from the network, or a large file, without blocking a thread.

> **Applies to:** .NET Standard 2.0 and later, and .NET 8 and .NET 10. There are no
> asynchronous members on .NET Standard 1.1.

## Read calendars

`ReadCalendarsAsync` returns an `IAsyncEnumerable<IcalCalendar>`:

```csharp
using DataStandardizer.File.iCal;

await using var reader = new IcalReader("calendar.ics");
await foreach (var calendar in reader.ReadCalendarsAsync(cancellationToken))
{
    foreach (var calendarEvent in calendar.Events)
    {
        Console.WriteLine($"{calendarEvent.DtStart}: {calendarEvent.Summary}");
    }
}
```

The token you pass is combined with any token passed to `GetAsyncEnumerator`, for
example by `WithCancellation`.

To read one calendar at a time, call `ReadCalendarAsync`. Its result is `null` at
the end of the data:

```csharp
await using var reader = new IcalReader(stream);
var calendar = await reader.ReadCalendarAsync(cancellationToken);
```

The checks are the same as for `ReadCalendar`. See
[Read and write .ics files](read-write-ics-files.md).

## Read content lines

`IcalContentLineReader`, the lower layer, has three asynchronous reads:

| Member | Returns |
| --- | --- |
| `ReadContentLineAsync` | `ValueTask<bool>`. The line is then in `CurrentLine`, without allocation. |
| `ReadLineAsync` | `ValueTask<IcalContentLine?>`: a copy of the line, or `null` at the end. |
| `ReadLinesAsync` | `IAsyncEnumerable<IcalContentLine>`. |

```csharp
await using var reader = new IcalContentLineReader("calendar.ics");
while (await reader.ReadContentLineAsync(cancellationToken))
{
    var line = reader.CurrentLine;
    if (line.NameEquals("SUMMARY"))
    {
        Console.WriteLine(line.GetTextValue().ToString());
    }
}
```

`CurrentLine` is a view of the reader's buffer. A call to `ReadContentLineAsync`
moves the reader on as soon as it is made, before its task completes, so take what
you need from a view before you start the next read. See
[Read with zero copies](read-zero-copy.md).

## Reads complete synchronously when the data is buffered

The read methods return `ValueTask`. When the reader's buffer already holds the
whole line, or the whole calendar, the task completes synchronously and nothing is
allocated for it. Only a read that has to fetch more data from the stream waits.

## No synchronous I/O

The asynchronous members do no synchronous I/O. They fill the buffer with the
`ReadAsync` of the underlying text reader, and write with its `WriteAsync` and
`FlushAsync`.

A file that a reader or writer opens from a path is opened with
`FileOptions.Asynchronous`, so its asynchronous members do overlapped file I/O.
The synchronous members still work on such a file, at a small cost on Windows. If
you only read or write a file synchronously, you can open a `FileStream` without
`FileOptions.Asynchronous` yourself and pass it to a `Stream` constructor.

A stream that you pass in is used as it is.

## One operation at a time

A reader or writer runs one operation at a time. Synchronous and asynchronous calls
may follow one another, but a call made while another is in progress throws
`InvalidOperationException`. So await each read before you start the next.

A `WriteCalendarsAsync` call is one operation from start to end, so neither the
calendars it enumerates nor its progress callback may call the writer.

## Cancellation

Pass a `CancellationToken` to any asynchronous member. If a read or write is
canceled, the task completes with an `OperationCanceledException`, and the reader
or writer can't continue: later calls throw `InvalidOperationException`. Dispose it
and start again.

On .NET Standard 2.0, the `TextReader.ReadAsync` and `TextWriter.WriteAsync`
overloads take no token. The token is then checked before each read and each write,
rather than passed to them.

## Write calendars

```csharp
await using var writer = new IcalWriter("calendar.ics");
await writer.WriteCalendarsAsync(calendars, progress, cancellationToken);
```

- **`WriteCalendarAsync`:** writes one calendar. It completes synchronously when the
  underlying text writer takes every line without waiting, as a `StreamWriter` does
  while the lines fit in its buffer.
- **`WriteCalendarsAsync`:** writes an `IEnumerable<IcalCalendar>` or an
  `IAsyncEnumerable<IcalCalendar>`. It checks for cancellation between calendars.
- **`FlushAsync`:** flushes the underlying text writer.

Each calendar is checked against RFC 5545 before any of it is written, as it is
for `WriteCalendar`.

Because `WriteCalendarsAsync` accepts an `IAsyncEnumerable<IcalCalendar>`, you can
stream calendars from a reader straight to a writer:

```csharp
await using var reader = new IcalReader("calendar.ics");
await using var writer = new IcalWriter("copy.ics");
await writer.WriteCalendarsAsync(reader.ReadCalendarsAsync(cancellationToken), null, cancellationToken);
```

### Report progress

`WriteCalendarsAsync` takes an optional `IProgress<IcalWriteProgress>`. It is called
synchronously, on the writing thread, after each calendar has been handed to the
underlying text writer. Its counts (`CalendarsWritten`, `ComponentsWritten` and
`LinesWritten`) are totals for the writer, not for the call.

```csharp
var progress = new Progress<IcalWriteProgress>(
    value => Console.WriteLine($"{value.CalendarsWritten} calendars written"));
await writer.WriteCalendarsAsync(calendars, progress, cancellationToken);
```

`Progress<T>` posts each report to the synchronization context it was created on,
so a report can arrive after the call has returned. If your `Report` throws, the
call ends; the calendar it was reporting has been written.

## Dispose asynchronously

Both types implement `IAsyncDisposable`, so use `await using`.

- **Reader:** `DisposeAsync` does the same as `Dispose` and completes at once.
- **Writer:** `DisposeAsync` flushes and closes the file or stream writer it
  created itself, asynchronously. `Dispose` would flush it synchronously. A
  `TextWriter` you supplied is neither flushed nor closed, and a `Stream` you
  supplied is left open.

If you dispose a reader or writer while an operation is in progress, that operation
completes with `ObjectDisposedException`. The buffers are released once it has
finished with them.

## Related

- [Read and write .ics files](read-write-ics-files.md)
- [Read with zero copies](read-zero-copy.md)
- [Convert between .ics, jCal and xCal](convert-formats.md): `XcalReader`,
  `XcalWriter`, `JcalReader` and `JcalWriter` have the same asynchronous members.
- [IcalReader](../reference/IcalReader.md),
  [IcalWriter](../reference/IcalWriter.md),
  [IcalContentLineReader](../reference/IcalContentLineReader.md) and
  [IcalWriteProgress](../reference/IcalWriteProgress.md)
