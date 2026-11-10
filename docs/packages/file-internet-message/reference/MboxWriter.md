---
title: MboxWriter Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# MboxWriter Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Represents a writer of mbox files (RFC 4155): many Internet messages in one file, each after a separator line and ended by an empty line.

**.NET Standard 1.1:**

```csharp
public sealed class MboxWriter : IDisposable
```

**.NET and .NET Standard 2.0 and later:**

```csharp
public sealed class MboxWriter : IDisposable, IAsyncDisposable
```

## Remarks

**Messages.** Each message is checked and written as an [`ImfWriter`](ImfWriter.md) does, under the same options, so a message that it refuses leaves nothing in the output, and writing can continue with another. A message read from a file with the same line breaks stays byte for byte as it was, apart from what the variant adds.

**Variants.** With [`MboxVariant.Mboxrd`](MboxVariant.md) and [`MboxVariant.Mboxcl2`](MboxVariant.md), a message reads back exactly. With [`MboxVariant.Mboxo`](MboxVariant.md) and [`MboxVariant.Mboxcl`](MboxVariant.md), the quotes added to lines starting `From ` stay when it is read. With [`MboxVariant.Mboxcl`](MboxVariant.md) and [`MboxVariant.Mboxcl2`](MboxVariant.md), the Content-Length field is set to the length of the body as the file holds it, replacing any that is wrong, and a header section with no empty line gets one. The message itself isn't changed. Other variants leave any Content-Length field as it is.

**Line breaks.** A message in a file uses the line break of the file, which is the LF that RFC 4155 Appendix A requires, unless [`MboxLineBreak.CarriageReturnLineFeed`](MboxLineBreak.md) is given. So one read from an `.eml` file changes when written here. A line that still ends in CRLF in a file of LF, from a file read with [`ImfOptions.AllowMboxCarriageReturnLineFeed`](ImfOptions.md), causes an [`ImfFileException`](ImfFileException.md) unless that option is set. A message that has to change this way can't have a `binary` entity (RFC 2045 §2.9) or an extension mechanism (§6.3), as a line break in it may not be one, which causes an [`ImfFileException`](ImfFileException.md).

**Seven bits.** RFC 4155 Appendix A requires seven-bit characters, so an octet above 127 causes an [`ImfFileException`](ImfFileException.md), unless [`ImfOptions.AllowEightBitMboxContent`](ImfOptions.md) is set. A body written by this package gets a transfer encoding that keeps to seven bits by default.

**Line length.** RFC 4155 Appendix A requires each message to follow the formatting rules of RFC 2822, so RFC 5322 §2.1.1 limits each line in the file to 998 octets. A quote that takes a line past that causes an [`ImfFileException`](ImfFileException.md), unless [`ImfOptions.AllowLongLines`](ImfOptions.md) is set, or it is in a body that has no line limit, such as a `binary` one. A variant that adds no quotes, such as [`MboxVariant.Mboxcl2`](MboxVariant.md), can write the line.

**Last line.** The empty line that ends a message has to be a line of its own, so a message whose last line has no line break gets one. With [`MboxVariant.Mboxcl`](MboxVariant.md) and [`MboxVariant.Mboxcl2`](MboxVariant.md), the Content-Length field doesn't count it, so the message reads back without it. With the others, it reads back with it.

A stream that already holds an mbox file can be appended to by positioning it at the end, and a file-path constructor overwrites the file. Mailbox locking and changing a mailbox in place are not supported. Only one operation may be in progress at a time. After a write fails once output has started, the file is incomplete, so the writer cannot continue, and the same goes after any write is canceled. A write is canceled only when the token passed to it is canceled, whatever token the `OperationCanceledException` carries, or if it carries none. An `OperationCanceledException` while that token isn't canceled, such as one from the stream or the progress callback, is handled as any other exception is.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MboxWriter(Stream mailboxStream)` | Creates a writer of an mbox file in the [`MboxVariant.Mboxrd`](MboxVariant.md) variant, at the current position of a stream. **Throws** `ArgumentNullException`: `mailboxStream` is `null`. **Throws** `ArgumentException`: `mailboxStream` does not support writing. |
| `MboxWriter(Stream mailboxStream, MboxVariant variant, ImfOptions options)` | Creates a writer of an mbox file with LF line breaks, at the current position of a stream. **Throws** `ArgumentNullException`: `mailboxStream` or `options` is `null`. **Throws** `ArgumentOutOfRangeException`: `variant` is not a variant. **Throws** `ArgumentException`: `mailboxStream` does not support writing. -or- `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `MboxWriter(Stream mailboxStream, MboxVariant variant, ImfOptions options, MboxLineBreak lineBreak)` | Creates a writer of an mbox file, at the current position of a stream. **Throws** `ArgumentNullException`: `mailboxStream` or `options` is `null`. **Throws** `ArgumentOutOfRangeException`: `variant` is not a variant, or `lineBreak` is not a line break. **Throws** `ArgumentException`: `mailboxStream` does not support writing. -or- `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `MboxWriter(string mailboxFilePath)` | *(netstandard2.0+/.NET)* Creates a writer of an mbox file in the [`MboxVariant.Mboxrd`](MboxVariant.md) variant. An existing file is overwritten. **`mailboxFilePath`:** The path of the file. The file is opened for asynchronous writing, and is closed when the writer is disposed. **Throws** `ArgumentNullException`: `mailboxFilePath` is `null`. |
| `MboxWriter(string mailboxFilePath, MboxVariant variant, ImfOptions options)` | *(netstandard2.0+/.NET)* Creates a writer of an mbox file with LF line breaks. An existing file is overwritten. **`mailboxFilePath`:** The path of the file. The file is opened for asynchronous writing, and is closed when the writer is disposed. **Throws** `ArgumentNullException`: `mailboxFilePath` or `options` is `null`. **Throws** `ArgumentOutOfRangeException`: `variant` is not a variant. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the number of bytes written so far. **Throws** `ObjectDisposedException`: The writer has been disposed. |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems found so far that did not stop writing, as the options allow. The list is read-only, and grows as the writer finds more. **Throws** `ObjectDisposedException`: The writer has been disposed. |
| `MessagesWritten` | `long MessagesWritten { get; }` | Gets the number of messages written so far. **Throws** `ObjectDisposedException`: The writer has been disposed. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Closes the file the writer opened itself. A caller-supplied stream is left open. May be called more than once. If an operation is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. |
| `Flush()` | `void` | Flushes the stream, whether the writer opened it or it was supplied by the caller. **Throws** `ObjectDisposedException`: The writer has been disposed. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. |
| `WriteMessage(MboxEnvelope envelope, ImfMessage message)` | `void` | Writes a message. **Throws** `ObjectDisposedException`: The writer has been disposed, either before this call or during it. **Throws** `ArgumentNullException`: `envelope` or `message` is `null`. **Throws** [`ImfFileException`](ImfFileException.md): The message or separator line can't be written. Nothing has been written. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. |
| `WriteMessages(IEnumerable<MboxEntry> entries, IProgress<ImfWriteProgress>? progress = null)` | `void` | Writes many messages, one after another. The whole call is one operation, so neither `entries` nor `progress` may call the writer. Entries are taken one at a time, so an enumerator that reads them from another mailbox takes the same memory for any number of them. **`progress`:** Receives the progress after each message has been handed to the stream, synchronously on the writing thread; or `null`. Its [`ImfWriteProgress.EntitiesWritten`](ImfWriteProgress.md) counts messages. **Throws** `ObjectDisposedException`: The writer has been disposed, either before this call or during it. **Throws** `ArgumentNullException`: `entries` is `null`. **Throws** `ArgumentException`: `entries` gave a `null` entry. The ones before it have been written. **Throws** [`ImfFileException`](ImfFileException.md): A message or separator line can't be written. The ones before it have been written, and nothing of it has. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `DisposeAsync()` | `ValueTask` | Closes the file the writer opened itself, asynchronously. A caller-supplied stream is left open. May be called more than once, and shares its state with `Dispose`. If an operation is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. **Returns:** A task that completes once any operation in progress has finished, and the file has been closed. |
| `FlushAsync(CancellationToken cancellationToken = default)` | `Task` | Flushes the stream asynchronously, whether the writer opened it or it was supplied by the caller. **Returns:** A task that completes once the stream has been flushed. **`cancellationToken`:** The token to monitor for cancellation requests. The flush is canceled only if this token is canceled, and the writer then cannot continue. **Throws** `ObjectDisposedException`: The writer has been disposed. Thrown by this call, or by the task if the writer is disposed while the flush is in progress. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled before the call, or the operation failed with an `OperationCanceledException`, whatever token it carries, while `cancellationToken` was canceled. The task is canceled. An `OperationCanceledException` while `cancellationToken` isn't canceled faults the task, as any other exception does. |
| `WriteMessageAsync(MboxEnvelope envelope, ImfMessage message, CancellationToken cancellationToken = default)` | `Task` | Writes a message asynchronously. **Returns:** A task that completes once the whole message has been handed to the stream. **`cancellationToken`:** The token to monitor for cancellation requests. The write is canceled only if this token is canceled, and the writer then cannot continue. **Throws** `ObjectDisposedException`: The writer has been disposed. Thrown by this call, or by the task if the writer is disposed while the write is in progress. **Throws** `ArgumentNullException`: `envelope` or `message` is `null`. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the message or separator line can't be written. Nothing has been written. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled before the call, or the operation failed with an `OperationCanceledException`, whatever token it carries, while `cancellationToken` was canceled. The task is canceled. An `OperationCanceledException` while `cancellationToken` isn't canceled faults the task, as any other exception does. |
| `WriteMessagesAsync(IEnumerable<MboxEntry> entries, IProgress<ImfWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | Writes many messages asynchronously, one after another. The whole call is one operation, so neither `entries` nor `progress` may call the writer. **Returns:** A task that completes once every message has been handed to the stream. **`progress`:** Receives the progress after each message has been handed to the stream, synchronously on the writing thread; or `null`. Its [`ImfWriteProgress.EntitiesWritten`](ImfWriteProgress.md) counts messages. **`cancellationToken`:** The token to monitor for cancellation requests. The write is canceled only if this token is canceled, and the writer then cannot continue. **Throws** `ObjectDisposedException`: The writer has been disposed. Thrown by this call, or by the task if the writer is disposed while the write is in progress. **Throws** `ArgumentNullException`: `entries` is `null`. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. **Throws** `ArgumentException`: By the task: `entries` gave a `null` entry. The ones before it have been written. **Throws** [`ImfFileException`](ImfFileException.md): By the task: a message or separator line can't be written. The ones before it have been written, and nothing of it has. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled before the call, or the operation failed with an `OperationCanceledException`, whatever token it carries, while `cancellationToken` was canceled. The task is canceled. An `OperationCanceledException` while `cancellationToken` isn't canceled faults the task, as any other exception does. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable` are available on `netstandard2.0`, `netstandard2.1`, and `net10.0` only; on `netstandard1.1`, construct it from a `Stream` and work synchronously.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read, filter and rewrite an mbox file](../how-to/read-and-rewrite-mbox.md)
- [One message format, several file formats](../concepts/file-formats.md)
- [ImfFileException](ImfFileException.md)
- [ImfOptions](ImfOptions.md)
- [ImfWriteProgress](ImfWriteProgress.md)
- [ImfWriter](ImfWriter.md)
- [MboxLineBreak](MboxLineBreak.md)
- [MboxVariant](MboxVariant.md)
- [File.InternetMessage API reference](index.md)
