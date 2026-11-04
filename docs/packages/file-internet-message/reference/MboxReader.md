---
title: MboxReader Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# MboxReader Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Represents a reader of an mbox file (RFC 4155): many Internet messages in one file, each after a separator line. Messages are read one at a time, so a mailbox of any size can be processed without loading it whole.

**.NET Standard 1.1:**

```csharp
public sealed class MboxReader : IDisposable
```

**.NET and .NET Standard 2.0 and later:**

```csharp
public sealed class MboxReader : IDisposable, IAsyncDisposable
```

## Remarks

**Variants.** The variants of the format can't be told apart reliably from what a file holds, so a caller says which one a file uses, and the default is [`MboxVariant.Mboxrd`](MboxVariant.md). With [`MboxVariant.Mboxcl`](MboxVariant.md) and [`MboxVariant.Mboxcl2`](MboxVariant.md), the Content-Length field finds the end of each message.

**Line breaks.** RFC 4155 Appendix A requires LF, so a message in a file uses that, which is the format's own rule and doesn't depend on [`ImfOptions.AcceptBareLineFeed`](ImfOptions.md). With [`ImfOptions.AllowMboxCarriageReturnLineFeed`](ImfOptions.md), a file whose first separator line ends in CRLF is read with CRLF line breaks throughout.

**Messages.** A message ends at the empty line before the next separator line or the end of the file, and that empty line is not part of it. Each is read as an [`ImfReader`](ImfReader.md) reads a message, under the same options, so the limits on a message apply to each. An [`ImfFileException`](ImfFileException.md) from a message that the file holds correctly ends only that message: the next read goes on to the next one.

**The format.** A first line that isn't a separator line, a message not ended by an empty line, a Content-Length field that is missing or wrong, a CRLF line break and an octet above 127 each cause an [`ImfFileException`](ImfFileException.md), unless [`ImfOptions.AllowMissingMboxSeparator`](ImfOptions.md), [`ImfOptions.AllowInvalidMboxSeparator`](ImfOptions.md), [`ImfOptions.AllowMissingMboxBlankLine`](ImfOptions.md), [`ImfOptions.AllowInvalidMboxContentLength`](ImfOptions.md), [`ImfOptions.AllowMboxCarriageReturnLineFeed`](ImfOptions.md) or [`ImfOptions.AllowEightBitMboxContent`](ImfOptions.md) is set. Reading then ends, and later reads throw `InvalidOperationException`. RFC 4155 Appendix A prescribes no way to quote a line of a message that starts `From `, so a later line that isn't a full separator line is part of the message, with an [`ImfDiagnostic`](ImfDiagnostic.md).

**Line length.** RFC 4155 Appendix A requires each message to follow the formatting rules of RFC 2822, so RFC 5322 §2.1.1 limits each line in the file to 998 octets. With [`MboxVariant.Mboxrd`](MboxVariant.md), a line whose quote takes it past that, though the line of the message fits once the quote is off, is treated as any long line of the message is: it causes an [`ImfFileException`](ImfFileException.md) for that message, once the message has been read, unless [`ImfOptions.AllowLongLines`](ImfOptions.md) is set, or it is in a body that has no line limit, such as a `binary` one.

Mailbox locking and changing a mailbox in place are not supported. Only one read may be in progress at a time, and a read started while another is in progress throws `InvalidOperationException`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MboxReader(Stream mailboxStream)` | Creates a reader of an mbox file in the [`MboxVariant.Mboxrd`](MboxVariant.md) variant, at the current position of a stream. **Throws** `ArgumentNullException`: `mailboxStream` is `null`. **Throws** `ArgumentException`: `mailboxStream` does not support reading. |
| `MboxReader(Stream mailboxStream, MboxVariant variant, ImfOptions options)` | Creates a reader of an mbox file, at the current position of a stream. **Throws** `ArgumentNullException`: `mailboxStream` or `options` is `null`. **Throws** `ArgumentOutOfRangeException`: `variant` is not a variant. **Throws** `ArgumentException`: `mailboxStream` does not support reading. -or- `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `MboxReader(string mailboxFilePath)` | *(netstandard2.0+/.NET)* Creates a reader of an mbox file in the [`MboxVariant.Mboxrd`](MboxVariant.md) variant. **Throws** `ArgumentNullException`: `mailboxFilePath` is `null`. |
| `MboxReader(string mailboxFilePath, MboxVariant variant, ImfOptions options)` | *(netstandard2.0+/.NET)* Creates a reader of an mbox file. **Throws** `ArgumentNullException`: `mailboxFilePath` or `options` is `null`. **Throws** `ArgumentOutOfRangeException`: `variant` is not a variant. **Throws** `ArgumentException`: `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the number of bytes of the file read so far. **Throws** `ObjectDisposedException`: The reader has been disposed. |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems with the format of the file found so far in the current message, and before the first, that did not stop reading. The list is read-only, and starts again with each message, so that a mailbox of any size takes the same memory. The problems in the message itself are in [`MboxEntry.Diagnostics`](MboxEntry.md), or the [`ImfEntityReader.Diagnostics`](ImfEntityReader.md) of the reader that `ReadEntityEvents` gave. **Throws** `ObjectDisposedException`: The reader has been disposed. |
| `Envelope` | `MboxEnvelope? Envelope { get; }` | Gets the separator line of the current message, or `null` before the first. **Throws** `ObjectDisposedException`: The reader has been disposed. |
| `MessagesRead` | `long MessagesRead { get; }` | Gets the number of messages found so far, including the current one. **Throws** `ObjectDisposedException`: The reader has been disposed. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Releases the reader's buffers, the reader that `ReadEntityEvents` last gave, and the file it opened itself. A caller-supplied stream is left open. May be called more than once. If a read is in progress, it completes with `ObjectDisposedException`, and the buffers and file are released once it has finished with them. |
| `ReadEntityEvents()` | `ImfEntityReader?` | Moves to the next message, skipping what is left of the current one, and gives a reader of its events, which never holds a whole body in memory. `Envelope` then gives the separator line. The reader that this gives is disposed by the next read of this object. Each read of its events is a read of this object, so only one may be in progress at a time. Problems with the format of the file come from reads of its events, in the same way as those of the message. **Returns:** A reader of the events of the message, or `null` at the end of the file. **Throws** [`ImfFileException`](ImfFileException.md): The file breaks a rule of its format that the options don't accept, or passes a limit. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: Another read is in progress. -or- An earlier read found an error in the format of the file, or was canceled, so the reader cannot continue. |
| `ReadMessage()` | `MboxEntry?` | Reads the next message, skipping what is left of the current one. **Returns:** The message with its separator line, or `null` at the end of the file. **Throws** [`ImfFileException`](ImfFileException.md): The message or the file breaks a rule of its format that the options don't accept, or passes a limit. Its [`ImfFileException.DataItemName.MessageIndex`](ImfFileException.md) item says which message. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: Another read is in progress. -or- An earlier read found an error in the format of the file, or was canceled, so the reader cannot continue. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `DisposeAsync()` | `ValueTask` | Releases the reader's buffers, the reader that `ReadEntityEvents` last gave, and the file it opened itself. A caller-supplied stream is left open. May be called more than once, and shares its state with `Dispose`. If a read is in progress, it completes with `ObjectDisposedException`, and the buffers and file are released once it has finished with them. **Returns:** A task that has already completed. |
| `ReadEntityEventsAsync(CancellationToken cancellationToken = default)` | `Task<ImfEntityReader?>` | Moves to the next message asynchronously, skipping what is left of the current one, and gives a reader of its events, as `ReadEntityEvents` describes. **Returns:** A task whose result is a reader of the events of the message, or `null` at the end of the file. **Throws** `ObjectDisposedException`: The reader has been disposed. Thrown by this call, or by the task if the reader is disposed while the read is in progress. **Throws** `InvalidOperationException`: Another read is in progress. -or- An earlier read found an error in the format of the file, or was canceled, so the reader cannot continue. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the file breaks a rule of its format that the options don't accept, or passes a limit. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled. |
| `ReadMessageAsync(CancellationToken cancellationToken = default)` | `Task<MboxEntry?>` | Reads the next message asynchronously, skipping what is left of the current one. **Returns:** A task whose result is the message with its separator line, or `null` at the end of the file. **Throws** `ObjectDisposedException`: The reader has been disposed. Thrown by this call, or by the task if the reader is disposed while the read is in progress. **Throws** `InvalidOperationException`: Another read is in progress. -or- An earlier read found an error in the format of the file, or was canceled, so the reader cannot continue. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the message or the file breaks a rule of its format that the options don't accept, or passes a limit. Its [`ImfFileException.DataItemName.MessageIndex`](ImfFileException.md) item says which message. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable` are available on `netstandard2.0`, `netstandard2.1`, and `net10.0` only; on `netstandard1.1`, construct it from a `Stream` and work synchronously.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read, filter and rewrite an mbox file](../how-to/read-and-rewrite-mbox.md)
- [One message format, several file formats](../concepts/file-formats.md)
- [ImfDiagnostic](ImfDiagnostic.md)
- [ImfEntityReader](ImfEntityReader.md)
- [ImfFileException](ImfFileException.md)
- [ImfOptions](ImfOptions.md)
- [ImfReader](ImfReader.md)
- [MboxEntry](MboxEntry.md)
- [MboxVariant](MboxVariant.md)
- [File.InternetMessage API reference](index.md)
