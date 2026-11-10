---
title: EmlxReader Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# EmlxReader Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Represents a reader of an Apple Mail EMLX file (`.emlx`, `.partial.emlx`) into an [`EmlxEntry`](EmlxEntry.md): one message, with a byte count before it and a property list of metadata after it.

**.NET Standard 1.1:**

```csharp
public sealed class EmlxReader : IDisposable
```

**.NET and .NET Standard 2.0 and later:**

```csharp
public sealed class EmlxReader : IDisposable, IAsyncDisposable
```

## Remarks

**Format.** EMLX is proprietary and undocumented, so this follows the layout that open-source readers agree on. The first line is the length of the message in bytes, in ASCII digits, which may have spaces after them, and then a line feed. The message follows, with LF line breaks, and then an XML property list. Anything in the property list that this package doesn't know is kept, not rejected.

**Errors.** A byte count that isn't a number or doesn't fit the file causes an [`ImfFileException`](ImfFileException.md), unless [`ImfOptions.AllowInvalidEmlxByteCount`](ImfOptions.md) is set. A property list that is missing or not well formed does too, unless [`ImfOptions.AllowInvalidEmlxMetadata`](ImfOptions.md) is set. With them set, the problem is recorded in `Diagnostics` instead.

**Message.** The message is read as an [`ImfReader`](ImfReader.md) reads it, under the same options and with the same checks, except that a line feed is its line break. A message with CRLF line breaks therefore causes an [`ImfFileException`](ImfFileException.md). Writing the message with an [`ImfWriter`](ImfWriter.md) gives it CRLF line breaks, so it can be saved as an `.eml` file.

**Partial files.** Apple Mail may keep a large part of a message in a `.emlxpart` file of its own, leaving an entity with an `X-Apple-Content-Length` field and an empty body. Such an entity is found by its content, whatever the name of the file. With no [`EmlxPartResolver`](EmlxPartResolver.md), or one that returns `null`, the message is read without the content, with an [`ImfDiagnosticKind.MissingEmlxPart`](ImfDiagnosticKind.md) diagnostic and an entry in [`EmlxEntry.MissingParts`](EmlxEntry.md) for the part, and no opt-in is needed. A resolver's content goes into the message at that entity, and any CRLF line breaks in it become LF, unless the part is `binary` or has an extension mechanism (RFC 2045 §2.9 and §6.3). The message is then whole, and so it is what a writer writes.

**Memory.** The whole file is held in memory while it is read, up to the sum of [`ImfOptions.MaxEntitySize`](ImfOptions.md), [`ImfOptions.MaxHeaderSectionLength`](ImfOptions.md) and [`ImfOptions.MaxEmlxMetadataLength`](ImfOptions.md), and the message then takes as much again. The layout is checked before the message, so that a wrong count is reported as one, and not as whatever it does to the message.

The data holds one entry, so it can be read only once, and any [`ImfFileException`](ImfFileException.md) ends reading. Later reads throw `InvalidOperationException`. A read started while another is in progress does too.

## Constructors

| Constructor | Notes |
| --- | --- |
| `EmlxReader(Stream messageStream)` | Creates a reader for the file at the current position of a stream. **Throws** `ArgumentNullException`: `messageStream` is `null`. **Throws** `ArgumentException`: `messageStream` does not support reading. |
| `EmlxReader(Stream messageStream, ImfOptions options)` | Creates a reader for the file at the current position of a stream. **Throws** `ArgumentNullException`: `messageStream` or `options` is `null`. **Throws** `ArgumentException`: `messageStream` does not support reading. -or- `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `EmlxReader(string messageFilePath)` | *(netstandard2.0+/.NET)* Creates a reader for a file. **`messageFilePath`:** The path of the file. The file is opened for asynchronous reading, and is closed when the reader is disposed, even if nothing has been read. **Throws** `ArgumentNullException`: `messageFilePath` is `null`. |
| `EmlxReader(string messageFilePath, ImfOptions options)` | *(netstandard2.0+/.NET)* Creates a reader for a file. **`messageFilePath`:** The path of the file. The file is opened for asynchronous reading, and is closed when the reader is disposed, even if nothing has been read. **Throws** `ArgumentNullException`: `messageFilePath` or `options` is `null`. **Throws** `ArgumentException`: `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the number of bytes of the file read so far. **Throws** `ObjectDisposedException`: The reader has been disposed. |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems found so far that did not stop reading, as the options allow. The list is read-only, and grows as the reader finds more. **Throws** `ObjectDisposedException`: The reader has been disposed. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Closes the file the reader opened itself. A caller-supplied stream is left open. May be called more than once. If a read is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. |
| `ReadEntry(EmlxPartResolver? partResolver = null)` | `EmlxEntry` | Reads the file. **Returns:** The entry. **`partResolver`:** Supplies the content of a part that a partial file stores elsewhere, or `null` to read the message without it. **Throws** [`ImfFileException`](ImfFileException.md): The file breaks a rule of its layout, or the message a rule of its format, that the options don't accept, or something passes a limit. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: Another read is in progress. -or- The file has already been read, or an earlier read failed or was canceled. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `DisposeAsync()` | `ValueTask` | Closes the file the reader opened itself. A caller-supplied stream is left open. May be called more than once, and shares its state with `Dispose`. If a read is in progress, it completes with `ObjectDisposedException`, and the file is closed once it has finished with it. **Returns:** A task that has already completed. |
| `ReadEntryAsync(EmlxPartResolver? partResolver = null, CancellationToken cancellationToken = default)` | `Task<EmlxEntry>` | Reads the file asynchronously. **Returns:** A task whose result is the entry. **`partResolver`:** Supplies the content of a part that a partial file stores elsewhere, or `null` to read the message without it. It is called on the reading thread, and the streams it gives are read asynchronously. **`cancellationToken`:** The token to monitor for cancellation requests. The read is canceled only if this token is canceled, and the reader then cannot continue. **Throws** `ObjectDisposedException`: The reader has been disposed. Thrown by this call, or by the task if the reader is disposed while the read is in progress. **Throws** `InvalidOperationException`: Another read is in progress. -or- The file has already been read, or an earlier read failed or was canceled. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the file breaks a rule of its layout, or the message a rule of its format, that the options don't accept, or something passes a limit. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled before the call, or the read failed with an `OperationCanceledException`, whatever token it carries, while `cancellationToken` was canceled. The task is canceled. An `OperationCanceledException` from the stream, from `partResolver` or from a stream it gives, while `cancellationToken` isn't canceled faults the task, as any other exception from them does. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable` are available on `netstandard2.0`, `netstandard2.1`, and `net10.0` only; on `netstandard1.1`, construct it from a `Stream` and work synchronously.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read Apple Mail EMLX files](../how-to/read-emlx.md)
- [One message format, several file formats](../concepts/file-formats.md)
- [EmlxEntry](EmlxEntry.md)
- [EmlxPartResolver](EmlxPartResolver.md)
- [ImfDiagnosticKind](ImfDiagnosticKind.md)
- [ImfFileException](ImfFileException.md)
- [ImfOptions](ImfOptions.md)
- [ImfReader](ImfReader.md)
- [ImfWriter](ImfWriter.md)
- [File.InternetMessage API reference](index.md)
