---
title: MhtmlWriter Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# MhtmlWriter Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Represents a writer of MHTML files (`.mht`, `.mhtml`), or any other aggregate documents (RFC 2557), from [`MhtmlDocument`](MhtmlDocument.md) objects.

**.NET Standard 1.1:**

```csharp
public sealed class MhtmlWriter : IDisposable
```

**.NET and .NET Standard 2.0 and later:**

```csharp
public sealed class MhtmlWriter : IDisposable, IAsyncDisposable
```

## Remarks

This writes the message of a document as an [`ImfWriter`](ImfWriter.md) does, under the same options, so anything that hasn't changed is written byte for byte, and with the same checks.

**Structure.** The message as it now is, not as it was when the document was made, is checked as [`MhtmlDocument`](MhtmlDocument.md) describes, under the options of the writer and with no base URI, and a message that it refuses leaves nothing in the output.

Only one operation may be in progress at a time. After a write fails once output has started, a document is incomplete, so the writer cannot continue, and the same goes after any write is canceled.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MhtmlWriter(Stream messageStream)` | Creates a writer of documents to a stream. **Throws** `ArgumentNullException`: `messageStream` is `null`. **Throws** `ArgumentException`: `messageStream` does not support writing. |
| `MhtmlWriter(Stream messageStream, ImfOptions options)` | Creates a writer of documents to a stream. **Throws** `ArgumentNullException`: `messageStream` or `options` is `null`. **Throws** `ArgumentException`: `messageStream` does not support writing. -or- `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `MhtmlWriter(string messageFilePath)` | *(netstandard2.0+/.NET)* Creates a writer of documents to a file. An existing file is overwritten. **Throws** `ArgumentNullException`: `messageFilePath` is `null`. |
| `MhtmlWriter(string messageFilePath, ImfOptions options)` | *(netstandard2.0+/.NET)* Creates a writer of documents to a file. An existing file is overwritten. **Throws** `ArgumentNullException`: `messageFilePath` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the number of bytes written so far. **Throws** `ObjectDisposedException`: The writer has been disposed. |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems found so far that did not stop writing. The list is read-only, and grows as the writer finds more. **Throws** `ObjectDisposedException`: The writer has been disposed. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Closes the file the writer opened itself. A caller-supplied stream is left open. May be called more than once. If an operation is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. |
| `Flush()` | `void` | Flushes the stream, whether the writer opened it or it was supplied by the caller. **Throws** `ObjectDisposedException`: The writer has been disposed. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. |
| `WriteDocument(MhtmlDocument document, IProgress<ImfWriteProgress>? progress = null)` | `void` | Writes a whole document. **Throws** `ObjectDisposedException`: The writer has been disposed, either before this call or during it. **Throws** `ArgumentNullException`: `document` is `null`. **Throws** [`ImfFileException`](ImfFileException.md): The document can't be written. Nothing has been written. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `DisposeAsync()` | `ValueTask` | Closes the file the writer opened itself, asynchronously. A caller-supplied stream is left open. May be called more than once, and shares its state with `Dispose`. If an operation is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. **Returns:** A task that completes once any operation in progress has finished, and the file has been closed. |
| `FlushAsync(CancellationToken cancellationToken = default)` | `Task` | Flushes the stream asynchronously, whether the writer opened it or it was supplied by the caller. **Returns:** A task that completes once the stream has been flushed. **Throws** `ObjectDisposedException`: The writer has been disposed. Thrown by this call, or by the task if the writer is disposed while the flush is in progress. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled. |
| `WriteDocumentAsync(MhtmlDocument document, IProgress<ImfWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | Writes a whole document asynchronously. **Returns:** A task that completes once the whole document has been handed to the stream. **Throws** `ObjectDisposedException`: The writer has been disposed. Thrown by this call, or by the task if the writer is disposed while the write is in progress. **Throws** `ArgumentNullException`: `document` is `null`. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the document can't be written. Nothing has been written. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable` are available on `netstandard2.0`, `netstandard2.1`, and `net10.0` only; on `netstandard1.1`, construct it from a `Stream` and work synchronously.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Open a saved web page](../how-to/open-a-saved-web-page.md)
- [ImfFileException](ImfFileException.md)
- [ImfOptions](ImfOptions.md)
- [ImfWriter](ImfWriter.md)
- [MhtmlDocument](MhtmlDocument.md)
- [File.InternetMessage API reference](index.md)
