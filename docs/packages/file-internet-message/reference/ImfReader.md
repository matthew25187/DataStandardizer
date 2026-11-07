---
title: ImfReader Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfReader Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Represents a reader of a whole Internet message into an [`ImfMessage`](ImfMessage.md), which holds it in memory. To read a message without holding it, use an [`ImfEntityReader`](ImfEntityReader.md).

**.NET Standard 1.1:**

```csharp
public sealed class ImfReader : IDisposable
```

**.NET and .NET Standard 2.0 and later:**

```csharp
public sealed class ImfReader : IDisposable, IAsyncDisposable
```

## Remarks

**Header.** The fields are read as an [`ImfHeaderReader`](ImfHeaderReader.md) reads them, under the same options, and kept with their raw bytes. The obsolete syntax of RFC 5322 §4 is accepted.

**Conformance.** Once the header has been read, it is checked as [`ImfMessage.Validate`](ImfMessage.md) describes. A field body that breaks its grammar causes an [`ImfFileException`](ImfFileException.md), unless [`ImfOptions.AllowInvalidValues`](ImfOptions.md) is set, and then the problem is recorded in `Diagnostics` instead. A field that RFC 5322 §3.6 requires being missing, and a second field of a kind that may appear only once, are always recorded in `Diagnostics`. §3.5 lets a message follow `obs-fields`, which §4.5 defines with no minimum or maximum for any field, and §4 says that a receiver MUST accept the obsolete syntax.

**Body.** The rest of the data is the body, up to [`ImfOptions.MaxEntitySize`](ImfOptions.md), which is held in memory as it is. Its structure is read into [`ImfMessage.Body`](ImfMessage.md) as an [`ImfEntityReader`](ImfEntityReader.md) reads it, under the same options, so a multipart entity that breaks RFC 2046 §5.1 causes an [`ImfFileException`](ImfFileException.md). The bytes of a body are decoded, and checked against the transfer encoding, only when they are asked for. A message inside a `message/rfc822` entity must have one of From, Subject or Date, as RFC 2046 §5.2.1 says, unless [`ImfOptions.AllowMissingRequiredFields`](ImfOptions.md) is set, and the bodies of its fields must follow their grammar, unless [`ImfOptions.AllowInvalidValues`](ImfOptions.md) is set. Call [`ImfMessage.Validate`](ImfMessage.md) on it to check it against the rest of RFC 5322.

The data holds one message, so it can be read only once, and any [`ImfFileException`](ImfFileException.md) ends reading. Later reads throw `InvalidOperationException`. A read started while another is in progress does too.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfReader(Stream messageStream)` | Creates a reader for the message at the current position of a stream. **Throws** `ArgumentNullException`: `messageStream` is `null`. **Throws** `ArgumentException`: `messageStream` does not support reading. |
| `ImfReader(Stream messageStream, ImfOptions options)` | Creates a reader for the message at the current position of a stream. **Throws** `ArgumentNullException`: `messageStream` or `options` is `null`. **Throws** `ArgumentException`: `messageStream` does not support reading. -or- `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `ImfReader(string messageFilePath)` | *(netstandard2.0+/.NET)* Creates a reader for the message in a file. **`messageFilePath`:** The path of the file. The file is opened for asynchronous reading, and is closed when the reader is disposed, even if nothing has been read. **Throws** `ArgumentNullException`: `messageFilePath` is `null`. |
| `ImfReader(string messageFilePath, ImfOptions options)` | *(netstandard2.0+/.NET)* Creates a reader for the message in a file. **`messageFilePath`:** The path of the file. The file is opened for asynchronous reading, and is closed when the reader is disposed, even if nothing has been read. **Throws** `ArgumentNullException`: `messageFilePath` or `options` is `null`. **Throws** `ArgumentException`: `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the number of bytes of the data read into the message so far. **Throws** `ObjectDisposedException`: The reader has been disposed. |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems found so far that did not stop reading, as the options allow. The list is read-only, and grows as the reader finds more. **Throws** `ObjectDisposedException`: The reader has been disposed. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Releases the reader's buffers, and the file it opened itself. A caller-supplied stream is left open. May be called more than once. If a read is in progress, it completes with `ObjectDisposedException`, and the buffers and file are released once it has finished with them. |
| `ReadMessage()` | `ImfMessage` | Reads the message. **Returns:** The message. **Throws** [`ImfFileException`](ImfFileException.md): The message breaks a rule of its format that the options don't accept, or passes a limit. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: Another read is in progress. -or- The message has already been read, or an earlier read failed or was canceled. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `DisposeAsync()` | `ValueTask` | Releases the reader's buffers, and the file it opened itself. A caller-supplied stream is left open. May be called more than once, and shares its state with `Dispose`. If a read is in progress, it completes with `ObjectDisposedException`, and the buffers and file are released once it has finished with them. **Returns:** A task that has already completed. |
| `ReadMessageAsync(CancellationToken cancellationToken = default)` | `Task<ImfMessage>` | Reads the message asynchronously. **Returns:** A task whose result is the message. **`cancellationToken`:** The token to monitor for cancellation requests. The read is canceled only if this token is canceled, and the reader then cannot continue. **Throws** `ObjectDisposedException`: The reader has been disposed. Thrown by this call, or by the task if the reader is disposed while the read is in progress. **Throws** `InvalidOperationException`: Another read is in progress. -or- The message has already been read, or an earlier read failed or was canceled. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the message breaks a rule of its format that the options don't accept, or passes a limit. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled before the call, or the read failed with an `OperationCanceledException`, whatever token it carries, while `cancellationToken` was canceled. The task is canceled. An `OperationCanceledException` from the stream while `cancellationToken` isn't canceled faults the task, as any other exception from it does. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable` are available on `netstandard2.0`, `netstandard2.1`, and `net10.0` only; on `netstandard1.1`, construct it from a `Stream` and work synchronously.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read a message and its attachments](../how-to/read-a-message.md)
- [The layers](../concepts/layers.md)
- [Compliant defaults and the opt-ins](../concepts/compliant-defaults.md)
- [ImfEntityReader](ImfEntityReader.md)
- [ImfFileException](ImfFileException.md)
- [ImfHeaderReader](ImfHeaderReader.md)
- [ImfMessage](ImfMessage.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
