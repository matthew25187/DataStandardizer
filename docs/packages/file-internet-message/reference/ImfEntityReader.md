---
title: ImfEntityReader Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfEntityReader Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Represents a reader that provides fast, non-cached, forward-only access to a whole Internet message as a stream of events: entity start, header field, header end, body chunk and entity end. It follows RFC 2046 §5 into multipart and embedded message entities, and never holds a whole body in memory.

**.NET Standard 1.1:**

```csharp
public sealed class ImfEntityReader : IDisposable
```

**.NET and .NET Standard 2.0 and later:**

```csharp
public sealed class ImfEntityReader : IDisposable, IAsyncDisposable
```

## Remarks

**Structure.** Each entity's header section is read as an [`ImfHeaderReader`](ImfHeaderReader.md) reads one, under the same options, and the limits on a section apply to each section. An entity with no Content-Type field is `text/plain; charset=us-ascii` (RFC 2045 §5.2), or `message/rfc822` if it is a part of a `multipart/digest` entity (RFC 2046 §5.1.5). A message with a MIME field and no MIME-Version field, which RFC 2045 §4 requires, causes an [`ImfFileException`](ImfFileException.md), unless [`ImfOptions.AllowMissingMimeVersion`](ImfOptions.md) is set. A field that is not valid, or a second Content-Type or Content-Transfer-Encoding field, is an error.

**Multipart entities.** A delimiter is a line break, `--`, the boundary, optional spaces and tabs, and a line break. A close delimiter has `--` after the boundary, and may end the data with no line break. The line break before a delimiter belongs to the delimiter, not to the part (RFC 2046 §5.1.1). So a part with no body ends with the empty line that shares its line break with the delimiter, and a part with headers and no empty line before its delimiter is not valid. A line is a delimiter of an open multipart entity only if it matches exactly, the innermost entity first, so a boundary may be a prefix of another, and a line that merely starts with a boundary is content. A line longer than the 998 octets that RFC 5322 §2.1.1 allows is never a delimiter.

A delimiter of an enclosing entity ends every entity inside it. A multipart entity that ends without its close delimiter, because of that or the end of the data, is an error unless [`ImfOptions.AllowMissingBoundaryTerminator`](ImfOptions.md) is set. With that option, a part's header section may end at the end of the data with no empty line, as there is then no delimiter to need one.

**Conformance.** As the header of the message ends, it is checked as [`ImfReader`](ImfReader.md) describes: a field body that breaks its grammar causes an [`ImfFileException`](ImfFileException.md), unless [`ImfOptions.AllowInvalidValues`](ImfOptions.md) is set, and a missing required field or a repeated field is recorded in `Diagnostics`, as RFC 5322 §4 and §4.5 require a receiver to accept them. The header of an embedded message must have one of From, Subject or Date (RFC 2046 §5.2.1), unless [`ImfOptions.AllowMissingRequiredFields`](ImfOptions.md) is set, and its field bodies are held to their grammar in the same way. The header of a part isn't that of a message, so none of this applies to it. The fields of a message's header are held in memory until it ends, up to [`ImfOptions.MaxHeaderSectionLength`](ImfOptions.md).

**Embedded messages.** The body of a `message/rfc822` or `message/global` (RFC 6532) entity is read as a nested entity, except that a `message/global` body with a `quoted-printable` or `base64` transfer encoding, which RFC 6532 §3.7 allows, is given as body chunks. The bodies of `message/partial` and `message/external-body` entities are given as body chunks too, and are not reassembled or followed: a fragment after the first has no header (RFC 2046 §5.2.2).

**Bodies.** Body chunks are raw bytes unless [`ImfOptions.DecodeTransferEncoding`](ImfOptions.md) is set. A transfer encoding other than `7bit`, `8bit` or `binary` on a multipart or message entity is an error (RFC 2045 §6.4), and `message/partial` and `message/external-body` must be `7bit` (RFC 2046 §5.2). The bytes of a body are checked against the rules of RFC 5322 §2.1.1 and §2.3, and its transfer encoding and that of what holds it (RFC 2045 §2.7, §2.8, §6.4, §6.7 and §6.8), unless [`ImfOptions.AllowLongLines`](ImfOptions.md) or [`ImfOptions.AllowInvalidBodyOctets`](ImfOptions.md) is set. The header of a part or an embedded message may hold an octet above 127 only under an `8bit` or `binary` label on the entity that holds it (RFC 2045 §2.7 and §6.4, RFC 6532 §3.7), unless [`ImfOptions.AllowInvalidBodyOctets`](ImfOptions.md) is set.

**Errors.** Any [`ImfFileException`](ImfFileException.md) ends reading, so later reads throw `InvalidOperationException`. Its [`ImfFileException.DataItemName.EntityPath`](ImfFileException.md) item says which entity it was found in.

Only one read may be in progress at a time. Synchronous and asynchronous reads may follow one another, but a read started while another is in progress throws `InvalidOperationException`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfEntityReader(Stream messageStream)` | Creates a reader for the message at the current position of a stream. **Throws** `ArgumentNullException`: `messageStream` is `null`. **Throws** `ArgumentException`: `messageStream` does not support reading. |
| `ImfEntityReader(Stream messageStream, ImfOptions options)` | Creates a reader for the message at the current position of a stream. **Throws** `ArgumentNullException`: `messageStream` or `options` is `null`. **Throws** `ArgumentException`: `messageStream` does not support reading. -or- `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `ImfEntityReader(string messageFilePath)` | *(netstandard2.0+/.NET)* Creates a reader for the message at the start of a file. **Throws** `ArgumentNullException`: `messageFilePath` is `null`. |
| `ImfEntityReader(string messageFilePath, ImfOptions options)` | *(netstandard2.0+/.NET)* Creates a reader for the message at the start of a file. **Throws** `ArgumentNullException`: `messageFilePath` or `options` is `null`. **Throws** `ArgumentException`: `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the number of bytes of the data that the events read so far take up. Decoded or not, this counts the raw bytes. **Throws** `ObjectDisposedException`: The reader has been disposed. |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems found so far that did not stop reading. The list is read-only, and grows as the reader finds more. **Throws** `ObjectDisposedException`: The reader has been disposed. |
| `EntitiesRead` | `long EntitiesRead { get; }` | Gets the number of entities found so far, including the message itself. **Throws** `ObjectDisposedException`: The reader has been disposed. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Releases the reader's buffers, and the file it opened itself. A caller-supplied stream is left open. May be called more than once. If a read is in progress, it completes with `ObjectDisposedException`, and the buffers and file are released once it has finished with them. |
| `ReadEvent()` | `ImfEntityEvent?` | Reads the next event, without allocation for a body chunk. **Returns:** A view of the event, valid until the next read; or `null` if the message has ended. **Throws** [`ImfFileException`](ImfFileException.md): The message breaks a rule of its format, or passes a limit. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: Another read is in progress. -or- An earlier read found an error, or was canceled, so the reader cannot continue. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `DisposeAsync()` | `ValueTask` | Releases the reader's buffers, and the file it opened itself. A caller-supplied stream is left open. May be called more than once, and shares its state with `Dispose`. If a read is in progress, it completes with `ObjectDisposedException`, and the buffers and file are released once it has finished with them. **Returns:** A task that has already completed. |
| `ReadEventAsync(CancellationToken cancellationToken = default)` | `ValueTask<ImfEntityEvent?>` | Reads the next event asynchronously, without allocation for a body chunk when the task completes synchronously, which it does when the reader's buffer already holds what the event needs. **Returns:** A task whose result is a view of the event, valid until the next read; or `null` if the message has ended. **Throws** `ObjectDisposedException`: The reader has been disposed. Thrown by this call, or by the task if the reader is disposed while the read is in progress. **Throws** `InvalidOperationException`: Another read is in progress. -or- An earlier read found an error, or was canceled, so the reader cannot continue. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the message breaks a rule of its format, or passes a limit. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable` are available on `netstandard2.0`, `netstandard2.1`, and `net10.0` only; on `netstandard1.1`, construct it from a `Stream` and work synchronously.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Stream a large message](../how-to/stream-a-large-message.md)
- [The layers](../concepts/layers.md)
- [ImfFileException](ImfFileException.md)
- [ImfHeaderReader](ImfHeaderReader.md)
- [ImfOptions](ImfOptions.md)
- [ImfReader](ImfReader.md)
- [File.InternetMessage API reference](index.md)
