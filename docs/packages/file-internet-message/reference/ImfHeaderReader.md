---
title: ImfHeaderReader Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfHeaderReader Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Represents a reader that provides fast, non-cached, forward-only access to the fields of one header section of an Internet message (RFC 5322 §2.2).

**.NET Standard 1.1:**

```csharp
public sealed class ImfHeaderReader : IDisposable
```

**.NET and .NET Standard 2.0 and later:**

```csharp
public sealed class ImfHeaderReader : IDisposable, IAsyncDisposable
```

## Remarks

This reads one header section, which may be that of a part, so the rules for the header of a whole message, such as the fields that RFC 5322 §3.6 requires, don't apply to it. Use [`ImfReader`](ImfReader.md) or [`ImfEntityReader`](ImfEntityReader.md) for those.

The section ends at an empty line or at the end of the data, since a message may have no body. The bytes after the empty line are not read as fields. The reader reads ahead, so the position of a stream afterwards is not the start of the body; that is at `BytePosition` from where the reader started.

Each field is a name of printable US-ASCII characters, a colon and a body. White space between the name and the colon is accepted, as RFC 5322 §4.5 requires of a reader. The rest of the syntax of a field body, including its obsolete forms, is left to the code that reads the body, with the exception of line breaks: CR and LF may appear only together, as a CRLF. A bare line feed is accepted only with [`ImfOptions.AcceptBareLineFeed`](ImfOptions.md), and a bare carriage return is never accepted, although RFC 5322 §4.1 lists both in its obsolete unstructured text, because where a line ends would then be unclear. Every field must end with a line break, including the last (RFC 5322 §3.6.8).

A line over 998 octets (RFC 5322 §2.1.1) is rejected unless [`ImfOptions.AllowLongLines`](ImfOptions.md) is set. A field body must be valid UTF-8 (RFC 6532) unless [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md) is set. [`ImfOptions.MaxHeaderFieldCount`](ImfOptions.md) and [`ImfOptions.MaxHeaderSectionLength`](ImfOptions.md) limit what hostile input can make the reader do.

If a field's name or the UTF-8 of its body is at fault, an [`ImfFileException`](ImfFileException.md) is thrown after the field has been consumed, so reading can continue with the next field. If a line break, line length or limit is at fault, or the data ends inside a field, where the field ends is unknown, so later reads throw `InvalidOperationException`.

Only one read may be in progress at a time. Synchronous and asynchronous reads may follow one another, but a read started while another is in progress throws `InvalidOperationException`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfHeaderReader(Stream messageStream)` | Creates a reader for the header section at the current position of a stream. **Throws** `ArgumentNullException`: `messageStream` is `null`. **Throws** `ArgumentException`: `messageStream` does not support reading. |
| `ImfHeaderReader(Stream messageStream, ImfOptions options)` | Creates a reader for the header section at the current position of a stream. **Throws** `ArgumentNullException`: `messageStream` or `options` is `null`. **Throws** `ArgumentException`: `messageStream` does not support reading. -or- `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `ImfHeaderReader(string messageFilePath)` | *(netstandard2.0+/.NET)* Creates a reader for the header section at the start of a file. **`messageFilePath`:** The path of the file. The file is opened for asynchronous reading, and is closed when the reader is disposed, even if nothing has been read. **Throws** `ArgumentNullException`: `messageFilePath` is `null`. |
| `ImfHeaderReader(string messageFilePath, ImfOptions options)` | *(netstandard2.0+/.NET)* Creates a reader for the header section at the start of a file. **`messageFilePath`:** The path of the file. The file is opened for asynchronous reading, and is closed when the reader is disposed, even if nothing has been read. **Throws** `ArgumentNullException`: `messageFilePath` or `options` is `null`. **Throws** `ArgumentException`: `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the number of bytes of the data that the fields read so far take up, including the empty line that ends the section once it has been read. At the end of the section, this is the offset of the body from where the reader started. **Throws** `ObjectDisposedException`: The reader has been disposed. |
| `FieldsRead` | `long FieldsRead { get; }` | Gets the number of fields read so far, including any that failed to parse. **Throws** `ObjectDisposedException`: The reader has been disposed. |
| `PhysicalLinesRead` | `long PhysicalLinesRead { get; }` | Gets the number of physical lines read so far. A folded field spans several physical lines. **Throws** `ObjectDisposedException`: The reader has been disposed. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Releases the reader's buffers, and the file it opened itself. A caller-supplied stream is left open. May be called more than once. If a read is in progress, it completes with `ObjectDisposedException`, and the buffers and file are released once it has finished with them. |
| `ReadField()` | `ImfHeaderFieldView?` | Reads the next field of the section, without allocation. **Returns:** A view of the field, valid until the next read; or `null` if the section has ended. **Throws** [`ImfFileException`](ImfFileException.md): The field does not follow the syntax of RFC 5322, has a line break that is not accepted, or passes a limit. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: Another read is in progress. -or- An earlier read found a line break, line length or limit error, or the data ended inside a field, or a read was canceled, so the reader cannot continue. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `DisposeAsync()` | `ValueTask` | Releases the reader's buffers, and the file it opened itself. A caller-supplied stream is left open. May be called more than once, and shares its state with `Dispose`. If a read is in progress, it completes with `ObjectDisposedException`, and the buffers and file are released once it has finished with them. **Returns:** A task that has already completed. |
| `ReadFieldAsync(CancellationToken cancellationToken = default)` | `ValueTask<ImfHeaderFieldView?>` | Reads the next field of the section asynchronously, without allocation when the task completes synchronously, which it does when the reader's buffer already holds the whole field. **Returns:** A task whose result is a view of the field, valid until the next read; or `null` if the section has ended. **`cancellationToken`:** The token to monitor for cancellation requests. If the read is canceled, the reader cannot continue. **Throws** `ObjectDisposedException`: The reader has been disposed. Thrown by this call, or by the task if the reader is disposed while the read is in progress. **Throws** `InvalidOperationException`: Another read is in progress. -or- An earlier read found a line break, line length or limit error, or the data ended inside a field, or a read was canceled, so the reader cannot continue. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the field does not follow the syntax of RFC 5322, has a line break that is not accepted, or passes a limit. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable` are available on `netstandard2.0`, `netstandard2.1`, and `net10.0` only; on `netstandard1.1`, construct it from a `Stream` and work synchronously.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Edit one header without breaking a signature](../how-to/edit-a-header.md)
- [The layers](../concepts/layers.md)
- [ImfEntityReader](ImfEntityReader.md)
- [ImfFileException](ImfFileException.md)
- [ImfOptions](ImfOptions.md)
- [ImfReader](ImfReader.md)
- [File.InternetMessage API reference](index.md)
