---
title: EmlxWriter Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# EmlxWriter Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Represents a writer of an Apple Mail EMLX file from an [`EmlxEntry`](EmlxEntry.md): the byte count, the message and the property list of metadata.

**.NET Standard 1.1:**

```csharp
public sealed class EmlxWriter : IDisposable
```

**.NET and .NET Standard 2.0 and later:**

```csharp
public sealed class EmlxWriter : IDisposable, IAsyncDisposable
```

## Remarks

**Format.** EMLX is proprietary and undocumented, so this follows the layout that [`EmlxReader`](EmlxReader.md) describes.

**Byte count.** The count is worked out from what is written. An entry that was read keeps its first line if the count is still right, and otherwise the width of that line, so an unchanged file is written back byte for byte.

**Message.** The message is checked and written as an [`ImfWriter`](ImfWriter.md) does, under the same options, so a message that it refuses leaves nothing in the output. It is written with LF line breaks, so one read from an `.eml` file changes in that way. A message that has to change this way can't have a `binary` entity (RFC 2045 §2.9) or an extension mechanism (§6.3), as a line break in it may not be one, which causes an [`ImfFileException`](ImfFileException.md).

**Metadata.** Metadata that was read and hasn't changed is written byte for byte. A property list that was missing or not well formed when read causes an [`ImfFileException`](ImfFileException.md), unless [`ImfOptions.AllowInvalidEmlxMetadata`](ImfOptions.md) is set, which writes its bytes back with a diagnostic. Values that nest more deeply than [`ImfOptions.MaxNestingDepth`](ImfOptions.md) are treated the same way, and a property list longer than [`ImfOptions.MaxEmlxMetadataLength`](ImfOptions.md) always causes an [`ImfFileException`](ImfFileException.md), as a reader with the same options wouldn't accept either.

A file holds one entry, so a second write throws `InvalidOperationException`, though a write that was refused with nothing written can be tried again with another entry. Only one operation may be in progress at a time. After a write fails once output has started, the file is incomplete, so the writer cannot continue, and the same goes after any write is canceled.

## Constructors

| Constructor | Notes |
| --- | --- |
| `EmlxWriter(Stream messageStream)` | Creates a writer of a file to a stream. **Throws** `ArgumentNullException`: `messageStream` is `null`. **Throws** `ArgumentException`: `messageStream` does not support writing. |
| `EmlxWriter(Stream messageStream, ImfOptions options)` | Creates a writer of a file to a stream. **Throws** `ArgumentNullException`: `messageStream` or `options` is `null`. **Throws** `ArgumentException`: `messageStream` does not support writing. -or- `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `EmlxWriter(string messageFilePath)` | *(netstandard2.0+/.NET)* Creates a writer of a file. An existing file is overwritten. **Throws** `ArgumentNullException`: `messageFilePath` is `null`. |
| `EmlxWriter(string messageFilePath, ImfOptions options)` | *(netstandard2.0+/.NET)* Creates a writer of a file. An existing file is overwritten. **Throws** `ArgumentNullException`: `messageFilePath` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the number of bytes written so far. **Throws** `ObjectDisposedException`: The writer has been disposed. |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems found so far that did not stop writing, as the options allow. The list is read-only, and grows as the writer finds more. **Throws** `ObjectDisposedException`: The writer has been disposed. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Closes the file the writer opened itself. A caller-supplied stream is left open. May be called more than once. If an operation is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. |
| `Flush()` | `void` | Flushes the stream, whether the writer opened it or it was supplied by the caller. **Throws** `ObjectDisposedException`: The writer has been disposed. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. |
| `WriteEntry(EmlxEntry entry, IProgress<ImfWriteProgress>? progress = null)` | `void` | Writes the file. **Throws** `ObjectDisposedException`: The writer has been disposed, either before this call or during it. **Throws** `ArgumentNullException`: `entry` is `null`. **Throws** [`ImfFileException`](ImfFileException.md): The entry can't be written. Nothing has been written. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An entry has already been written, or an earlier write failed or was canceled. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `DisposeAsync()` | `ValueTask` | Closes the file the writer opened itself, asynchronously. A caller-supplied stream is left open. May be called more than once, and shares its state with `Dispose`. If an operation is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. **Returns:** A task that completes once any operation in progress has finished, and the file has been closed. |
| `FlushAsync(CancellationToken cancellationToken = default)` | `Task` | Flushes the stream asynchronously, whether the writer opened it or it was supplied by the caller. **Returns:** A task that completes once the stream has been flushed. **Throws** `ObjectDisposedException`: The writer has been disposed. Thrown by this call, or by the task if the writer is disposed while the flush is in progress. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled. |
| `WriteEntryAsync(EmlxEntry entry, IProgress<ImfWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | Writes the file asynchronously. **Returns:** A task that completes once the whole file has been handed to the stream. **Throws** `ObjectDisposedException`: The writer has been disposed. Thrown by this call, or by the task if the writer is disposed while the write is in progress. **Throws** `ArgumentNullException`: `entry` is `null`. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An entry has already been written, or an earlier write failed or was canceled. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the entry can't be written. Nothing has been written. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable` are available on `netstandard2.0`, `netstandard2.1`, and `net10.0` only; on `netstandard1.1`, construct it from a `Stream` and work synchronously.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read Apple Mail EMLX files](../how-to/read-emlx.md)
- [One message format, several file formats](../concepts/file-formats.md)
- [EmlxEntry](EmlxEntry.md)
- [EmlxReader](EmlxReader.md)
- [ImfFileException](ImfFileException.md)
- [ImfOptions](ImfOptions.md)
- [ImfWriter](ImfWriter.md)
- [File.InternetMessage API reference](index.md)
