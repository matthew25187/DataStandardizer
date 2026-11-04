---
title: ImfEntityWriter Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfEntityWriter Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Represents a writer that provides fast, non-cached, forward-only output of whole Internet messages, with their MIME entities (RFC 2045, RFC 2046), from a description of each message.

**.NET Standard 1.1:**

```csharp
public sealed class ImfEntityWriter : IDisposable
```

**.NET and .NET Standard 2.0 and later:**

```csharp
public sealed class ImfEntityWriter : IDisposable, IAsyncDisposable
```

## Remarks

**All at once.** The writer checks a whole message before it writes any of it, so a message it refuses leaves nothing in the output, and writing can continue with another. It reads a body from a stream block by block, and doesn't hold it in memory.

**Header fields.** Fields are written as [`ImfHeaderWriter`](ImfHeaderWriter.md) writes them, in the order they were added. After them, unless they include the same field, the writer adds `MIME-Version: 1.0` to the header of a message (RFC 2045 §4), then the Content-Type field, then a Content-Transfer-Encoding field unless the encoding is a `7bit` that was not asked for (RFC 2045 §6.1).

**Transfer encoding.** Unless [`ImfEntitySource.TransferEncoding`](ImfEntitySource.md) says which to use, each body gets the lightest that fits. `7bit` fits if its lines are at most 998 octets with CRLF line breaks, and it has no NULs or octets above 127 (RFC 2045 §2.7). `8bit`, which also allows octets above 127, is next if [`ImfOptions.AllowEightBitTransferEncoding`](ImfOptions.md) is set. `binary`, which fits anything, is next if [`ImfOptions.AllowBinaryTransferEncoding`](ImfOptions.md) is set. Otherwise it is `quoted-printable` or `base64`, whichever an estimate of their sizes with line breaks says is smaller, with `quoted-printable` winning a tie. The estimate can differ from the output by a few bytes, as an encoded octet is never split across a soft line break.

A body that is encoded decodes to exactly the bytes given, with one exception. In `quoted-printable`, a CRLF in a `text` body is written as a line break, as RFC 2045 §6.7 rule 4 says; if the body also holds a bare CR or LF, or is not `text`, each CR and LF is encoded, with only soft line breaks. The exception is a `text` body from a stream that doesn't support seeking, which can't be looked at first: it is treated as text in the canonical form of RFC 2046 §4.1.1, so a bare LF in it is written as a line break, and decodes to a CRLF.

A multipart entity or embedded message gets the identity encoding that covers what it holds (RFC 2045 §6.4), so one with an `8bit` part is labelled `8bit`. Such an entity has no encoding of its own, so a body of content with a multipart or message type can only be given an identity encoding, except `message/global` (RFC 6532 §3.7).

**Boundaries.** A boundary that was not given is random, 34 characters from the RFC 2046 §5.1.1 `bchars`, and holds `=_`, which `quoted-printable` output can't, as RFC 2045 §6.7 has every `=` followed by two hexadecimal digits or a line break. It is checked against the enclosing boundaries, and against every line of the content it encloses that could hold it. If it collides, a new one is picked. A boundary that was given is checked in the same way, and a collision is refused.

**Raw passthrough.** An entity from [`ImfEntitySource.FromRaw`](ImfEntitySource.md) is copied byte for byte, which keeps a `multipart/signed` or DKIM signature over it valid. The same goes for a field added with [`ImfEntitySource.AddField`](ImfEntitySource.md) or [`ImfEntitySource.AddRawField`](ImfEntitySource.md).

**Limits.** [`ImfOptions.MaxNestingDepth`](ImfOptions.md) and [`ImfOptions.MaxPartCount`](ImfOptions.md) apply, as to a reader, which also keeps a tree that holds itself from being followed for ever.

Only one operation may be in progress at a time. Synchronous and asynchronous calls may follow one another, but a call made while another is in progress throws `InvalidOperationException`. After a write fails once output has started, a message is incomplete, so the writer cannot continue, and the same goes after any write is canceled, whether or not output had started.

A file-path constructor opens the file with `FileOptions.Asynchronous`, so the asynchronous methods do no synchronous I/O. To write a file only synchronously, without the small cost that option adds on Windows, pass a `FileStream` opened without it to a `Stream` constructor.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfEntityWriter(Stream messageStream)` | Creates a writer of messages to a stream. **Throws** `ArgumentNullException`: `messageStream` is `null`. **Throws** `ArgumentException`: `messageStream` does not support writing. |
| `ImfEntityWriter(Stream messageStream, ImfOptions options)` | Creates a writer of messages to a stream. **Throws** `ArgumentNullException`: `messageStream` or `options` is `null`. **Throws** `ArgumentException`: `messageStream` does not support writing. -or- `options` has a limit that is less than 1. |
| `ImfEntityWriter(string messageFilePath)` | *(netstandard2.0+/.NET)* Creates a writer of messages to a file. An existing file is overwritten. **Throws** `ArgumentNullException`: `messageFilePath` is `null`. |
| `ImfEntityWriter(string messageFilePath, ImfOptions options)` | *(netstandard2.0+/.NET)* Creates a writer of messages to a file. An existing file is overwritten. **Throws** `ArgumentNullException`: `messageFilePath` or `options` is `null`. **Throws** `ArgumentException`: `options` has a limit that is less than 1. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the number of bytes written so far. **Throws** `ObjectDisposedException`: The writer has been disposed. |
| `EntitiesWritten` | `long EntitiesWritten { get; }` | Gets the number of entities written so far, at any depth, including each message. An entity written from its raw bytes counts as one, whatever it holds. **Throws** `ObjectDisposedException`: The writer has been disposed. |
| `FieldsWritten` | `long FieldsWritten { get; }` | Gets the number of header fields written so far, including those the writer adds. The fields inside an entity written from its raw bytes are not counted. **Throws** `ObjectDisposedException`: The writer has been disposed. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Closes the file the writer opened itself. A caller-supplied stream is left open. May be called more than once. If an operation is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. |
| `Flush()` | `void` | Flushes the stream, whether the writer opened it or it was supplied by the caller. **Throws** `ObjectDisposedException`: The writer has been disposed. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. |
| `WriteMessage(ImfEntitySource message, IProgress<ImfWriteProgress>? progress = null)` | `void` | Writes a whole message. The whole call is one operation, so `progress` may not call the writer. Any number of messages can be written, one after another, with nothing between them. **Throws** `ObjectDisposedException`: The writer has been disposed, either before this call or during it. **Throws** `ArgumentNullException`: `message` is `null`. **Throws** [`ImfFileException`](ImfFileException.md): The message can't be written: its [`ImfFileException.DataItemName.EntityPath`](ImfFileException.md) item says which entity is at fault. Nothing has been written. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `DisposeAsync()` | `ValueTask` | Closes the file the writer opened itself, asynchronously. A caller-supplied stream is left open. May be called more than once, and shares its state with `Dispose`. If an operation is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. **Returns:** A task that completes once any operation in progress has finished, and the file has been closed. |
| `FlushAsync(CancellationToken cancellationToken = default)` | `Task` | Flushes the stream asynchronously, whether the writer opened it or it was supplied by the caller. **Returns:** A task that completes once the stream has been flushed. **Throws** `ObjectDisposedException`: The writer has been disposed. Thrown by this call, or by the task if the writer is disposed while the flush is in progress. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled. |
| `WriteMessageAsync(ImfEntitySource message, IProgress<ImfWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | Writes a whole message asynchronously. The whole call is one operation, so `progress` may not call the writer. Any number of messages can be written, one after another, with nothing between them. **Returns:** A task that completes once the whole message has been handed to the stream. **Throws** `ObjectDisposedException`: The writer has been disposed. Thrown by this call, or by the task if the writer is disposed while the write is in progress. **Throws** `ArgumentNullException`: `message` is `null`. **Throws** `InvalidOperationException`: Another operation is in progress. -or- An earlier write failed or was canceled, so the writer cannot continue. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the message can't be written: its [`ImfFileException.DataItemName.EntityPath`](ImfFileException.md) item says which entity is at fault. Nothing has been written. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable` are available on `netstandard2.0`, `netstandard2.1`, and `net10.0` only; on `netstandard1.1`, construct it from a `Stream` and work synchronously.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Stream a large message](../how-to/stream-a-large-message.md)
- [The layers](../concepts/layers.md)
- [Byte-exact round trips](../concepts/round-trips.md)
- [ImfEntitySource](ImfEntitySource.md)
- [ImfFileException](ImfFileException.md)
- [ImfHeaderWriter](ImfHeaderWriter.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
