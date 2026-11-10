---
title: ImfWriter Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfWriter Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Represents a writer of whole Internet messages from [`ImfMessage`](ImfMessage.md) objects.

**.NET Standard 1.1:**

```csharp
public sealed class ImfWriter : IDisposable
```

**.NET and .NET Standard 2.0 and later:**

```csharp
public sealed class ImfWriter : IDisposable, IAsyncDisposable
```

## Remarks

**All at once.** The writer checks a whole message before it writes any of it, so a message it refuses leaves nothing in the output, and writing can continue with another.

**Conformance.** The header is checked as [`ImfMessage.Validate`](ImfMessage.md) describes, under the options of the writer. A missing required field, a second field of a kind that may appear only once, and a field body that breaks its grammar each cause an [`ImfFileException`](ImfFileException.md), unless [`ImfOptions.AllowMissingRequiredFields`](ImfOptions.md), [`ImfOptions.AllowCardinalityViolations`](ImfOptions.md) or [`ImfOptions.AllowInvalidValues`](ImfOptions.md) is set. With them set, the problem is recorded in `Diagnostics` instead. A message inside a `message/rfc822` entity has only to have one of From, Subject or Date, as RFC 2046 §5.2.1 says. A message with a MIME field and no MIME-Version field (RFC 2045 §4), and a body that hasn't changed and that breaks the rules of RFC 5322 §2.1.1 and §2.3 or of its transfer encoding, are refused the same way, unless [`ImfOptions.AllowMissingMimeVersion`](ImfOptions.md), [`ImfOptions.AllowLongLines`](ImfOptions.md) or [`ImfOptions.AllowInvalidBodyOctets`](ImfOptions.md) is set.

**Unchanged fields.** A field that was read is written byte for byte, whichever options it was read under, so that signatures over it stay valid. That includes any obsolete syntax it holds. So does the line break of the empty line that ended the header, and its absence if the data ended first and there is still no body.

**Other fields.** A field that was set is formatted in the modern form, as [`ImfHeaderWriter`](ImfHeaderWriter.md) formats it, with CRLF line breaks, and with the [`ImfOptions.HeaderEncoding`](ImfOptions.md) of the writer.

**Body.** A body that hasn't changed is written from [`ImfMessage.RawBody`](ImfMessage.md), as it is. If anything in [`ImfMessage.Body`](ImfMessage.md) has changed, only what has changed is written again, as [`ImfEntityWriter`](ImfEntityWriter.md) writes it. So an entity whose content was set gets a transfer encoding of its own, and everything else, including the delimiter lines of a multipart entity whose parts are all still there, is written byte for byte. A multipart entity that was read with no close delimiter gets one.

**Messages from containers.** A message read from an mbox file with LF line breaks is written with CRLF line breaks, as RFC 5322 §2.1 requires, so everything in it changes in that way. One that has a `binary` entity (RFC 2045 §2.9), or one with an extension mechanism (§6.3), whose line breaks may not be line breaks, can't change like that, which causes an [`ImfFileException`](ImfFileException.md). Such a message is written in blocks, with the progress reported after each, and its fields are counted once, at the end.

Only one operation may be in progress at a time. Synchronous and asynchronous calls may follow one another, but a call made while another is in progress throws `InvalidOperationException`. After a write fails once output has started, a message is incomplete, so the writer cannot continue, and the same goes after any write is canceled, whether or not output had started. A write is canceled only when the token passed to it is canceled, whatever token the `OperationCanceledException` carries, or if it carries none. An `OperationCanceledException` while that token isn't canceled, such as one from the stream or the progress callback, is handled as any other exception is.

A file-path constructor opens the file with `FileOptions.Asynchronous`, so the asynchronous methods do no synchronous I/O. To write a file only synchronously, without the small cost that option adds on Windows, pass a `FileStream` opened without it to a `Stream` constructor.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfWriter(Stream messageStream)` | Creates a writer of messages to a stream. **Throws** `ArgumentNullException`: `messageStream` is `null`. **Throws** `ArgumentException`: `messageStream` does not support writing. |
| `ImfWriter(Stream messageStream, ImfOptions options)` | Creates a writer of messages to a stream. **Throws** `ArgumentNullException`: `messageStream` or `options` is `null`. **Throws** `ArgumentException`: `messageStream` does not support writing. -or- `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `ImfWriter(string messageFilePath)` | *(netstandard2.0+/.NET)* Creates a writer of messages to a file. An existing file is overwritten. **`messageFilePath`:** The path of the file. The file is opened for asynchronous writing, and is closed when the writer is disposed. **Throws** `ArgumentNullException`: `messageFilePath` is `null`. |
| `ImfWriter(string messageFilePath, ImfOptions options)` | *(netstandard2.0+/.NET)* Creates a writer of messages to a file. An existing file is overwritten. **`messageFilePath`:** The path of the file. The file is opened for asynchronous writing, and is closed when the writer is disposed. **Throws** `ArgumentNullException`: `messageFilePath` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the number of bytes written so far. **Throws** `ObjectDisposedException`: The writer has been disposed. |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems found so far that did not stop writing, as the options allow. The list is read-only, and grows as the writer finds more. **Throws** `ObjectDisposedException`: The writer has been disposed. |
| `EntitiesWritten` | `long EntitiesWritten { get; }` | Gets the number of messages written so far. **Throws** `ObjectDisposedException`: The writer has been disposed. |
| `FieldsWritten` | `long FieldsWritten { get; }` | Gets the number of header fields written so far. **Throws** `ObjectDisposedException`: The writer has been disposed. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Closes the file the writer opened itself. A caller-supplied stream is left open. May be called more than once. If an operation is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. |
| `Flush()` | `void` | Flushes the stream, whether the writer opened it or it was supplied by the caller. **Throws** `ObjectDisposedException`: The writer has been disposed. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. |
| `WriteMessage(ImfMessage message, IProgress<ImfWriteProgress>? progress = null)` | `void` | Writes a whole message. The whole call is one operation, so `progress` may not call the writer. Any number of messages can be written, one after another, with nothing between them. **`progress`:** Receives the progress after each field and each block of the body has been handed to the stream, synchronously on the writing thread; or `null`. An exception from `IProgress.Report` ends the call, leaving the message incomplete. `Progress` runs its handler later, so writing doesn't wait for the handler, and an exception from the handler doesn't end the call. **Throws** `ObjectDisposedException`: The writer has been disposed, either before this call or during it. **Throws** `ArgumentNullException`: `message` is `null`. **Throws** [`ImfFileException`](ImfFileException.md): The message can't be written. Nothing has been written. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `DisposeAsync()` | `ValueTask` | Closes the file the writer opened itself, asynchronously. A caller-supplied stream is left open. May be called more than once, and shares its state with `Dispose`. If an operation is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. **Returns:** A task that completes once any operation in progress has finished, and the file has been closed. |
| `FlushAsync(CancellationToken cancellationToken = default)` | `Task` | Flushes the stream asynchronously, whether the writer opened it or it was supplied by the caller. **Returns:** A task that completes once the stream has been flushed. **`cancellationToken`:** The token to monitor for cancellation requests. The flush is canceled only if this token is canceled, and the writer then cannot continue. **Throws** `ObjectDisposedException`: The writer has been disposed. Thrown by this call, or by the task if the writer is disposed while the flush is in progress. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled before the call, or the operation failed with an `OperationCanceledException`, whatever token it carries, while `cancellationToken` was canceled. The task is canceled. An `OperationCanceledException` while `cancellationToken` isn't canceled faults the task, as any other exception does. |
| `WriteMessageAsync(ImfMessage message, IProgress<ImfWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | Writes a whole message asynchronously. The whole call is one operation, so `progress` may not call the writer. Any number of messages can be written, one after another, with nothing between them. **Returns:** A task that completes once the whole message has been handed to the stream. **`progress`:** Receives the progress after each field and each block of the body has been handed to the stream, synchronously on the writing thread; or `null`. An exception from `IProgress.Report` ends the call, leaving the message incomplete. `Progress` runs its handler later, so writing doesn't wait for the handler, and an exception from the handler doesn't end the call. **`cancellationToken`:** The token to monitor for cancellation requests. The write is canceled only if this token is canceled, and the writer then cannot continue. **Throws** `ObjectDisposedException`: The writer has been disposed. Thrown by this call, or by the task if the writer is disposed while the write is in progress. **Throws** `ArgumentNullException`: `message` is `null`. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the message can't be written. Nothing has been written. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled before the call, or the operation failed with an `OperationCanceledException`, whatever token it carries, while `cancellationToken` was canceled. The task is canceled. An `OperationCanceledException` while `cancellationToken` isn't canceled faults the task, as any other exception does. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable` are available on `netstandard2.0`, `netstandard2.1`, and `net10.0` only; on `netstandard1.1`, construct it from a `Stream` and work synchronously.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Build and write a message](../how-to/build-and-write-a-message.md)
- [Edit one header without breaking a signature](../how-to/edit-a-header.md)
- [Byte-exact round trips](../concepts/round-trips.md)
- [ImfEntityWriter](ImfEntityWriter.md)
- [ImfFileException](ImfFileException.md)
- [ImfHeaderWriter](ImfHeaderWriter.md)
- [ImfMessage](ImfMessage.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
