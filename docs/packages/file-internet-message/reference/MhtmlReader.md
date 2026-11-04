---
title: MhtmlReader Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# MhtmlReader Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Represents a reader of an MHTML file (`.mht`, `.mhtml`), or any other aggregate document (RFC 2557), into an [`MhtmlDocument`](MhtmlDocument.md).

**.NET Standard 1.1:**

```csharp
public sealed class MhtmlReader : IDisposable
```

**.NET and .NET Standard 2.0 and later:**

```csharp
public sealed class MhtmlReader : IDisposable, IAsyncDisposable
```

## Remarks

An MHTML file is a MIME message (RFC 2557 §3), so this reads it as an [`ImfReader`](ImfReader.md) does, under the same options, and gives the same tree of entities.

**Required fields.** Every check of [`ImfReader`](ImfReader.md) applies. Some programs write no Date or From field, which is recorded in `Diagnostics`, as [`ImfReader`](ImfReader.md) describes, and the `From: <Saved by Blink>` that Chromium writes needs [`ImfOptions.AllowInvalidValues`](ImfOptions.md).

**Structure.** Once it has been read, the message is checked as [`MhtmlDocument`](MhtmlDocument.md) describes. So a `multipart/related` entity with no `type` parameter, as Word writes, needs [`ImfOptions.AllowInvalidRelatedStructure`](ImfOptions.md).

**Character sets.** Word writes its HTML in `windows-1252`. On .NET Core and .NET 5 and later, [`ImfEntity.GetText`](ImfEntity.md) can't resolve that character set until the application registers the code pages or supplies an [`ImfOptions.CharsetResolver`](ImfOptions.md), as [`MimeCharsetResolver`](../../communication/reference/MimeCharsetResolver.md) describes. Reading the document doesn't need it.

The data holds one document, so it can be read only once, and any [`ImfFileException`](ImfFileException.md) ends reading. Later reads throw `InvalidOperationException`. A read started while another is in progress does too.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MhtmlReader(Stream messageStream)` | Creates a reader for the document at the current position of a stream. **Throws** `ArgumentNullException`: `messageStream` is `null`. **Throws** `ArgumentException`: `messageStream` does not support reading. |
| `MhtmlReader(Stream messageStream, ImfOptions options)` | Creates a reader for the document at the current position of a stream. **Throws** `ArgumentNullException`: `messageStream` or `options` is `null`. **Throws** `ArgumentException`: `messageStream` does not support reading. -or- `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `MhtmlReader(string messageFilePath)` | *(netstandard2.0+/.NET)* Creates a reader for the document in a file. **Throws** `ArgumentNullException`: `messageFilePath` is `null`. |
| `MhtmlReader(string messageFilePath, ImfOptions options)` | *(netstandard2.0+/.NET)* Creates a reader for the document in a file. **Throws** `ArgumentNullException`: `messageFilePath` or `options` is `null`. **Throws** `ArgumentException`: `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the number of bytes of the data read into the document so far. **Throws** `ObjectDisposedException`: The reader has been disposed. |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems found so far that did not stop reading. The list is read-only, and grows as the reader finds more. **Throws** `ObjectDisposedException`: The reader has been disposed. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Releases the reader's buffers, and the file it opened itself. A caller-supplied stream is left open. May be called more than once. If a read is in progress, it completes with `ObjectDisposedException`, and the buffers and file are released once it has finished with them. |
| `ReadDocument(Uri? baseUri = null)` | `MhtmlDocument` | Reads the document. **Returns:** The document. **Throws** `ArgumentException`: `baseUri` is not absolute. **Throws** [`ImfFileException`](ImfFileException.md): The document breaks a rule of its format that the options don't accept, or passes a limit. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: Another read is in progress. -or- The document has already been read, or an earlier read failed or was canceled. |

### Asynchronous *(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `DisposeAsync()` | `ValueTask` | Releases the reader's buffers, and the file it opened itself. A caller-supplied stream is left open. May be called more than once, and shares its state with `Dispose`. If a read is in progress, it completes with `ObjectDisposedException`, and the buffers and file are released once it has finished with them. **Returns:** A task that has already completed. |
| `ReadDocumentAsync(Uri? baseUri = null, CancellationToken cancellationToken = default)` | `Task<MhtmlDocument>` | Reads the document asynchronously. **Returns:** A task whose result is the document. **Throws** `ArgumentException`: `baseUri` is not absolute. **Throws** `ObjectDisposedException`: The reader has been disposed. Thrown by this call, or by the task if the reader is disposed while the read is in progress. **Throws** `InvalidOperationException`: Another read is in progress. -or- The document has already been read, or an earlier read failed or was canceled. **Throws** [`ImfFileException`](ImfFileException.md): By the task: the document breaks a rule of its format that the options don't accept, or passes a limit. **Throws** `OperationCanceledException`: By the task: `cancellationToken` was canceled. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable` are available on `netstandard2.0`, `netstandard2.1`, and `net10.0` only; on `netstandard1.1`, construct it from a `Stream` and work synchronously.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Open a saved web page](../how-to/open-a-saved-web-page.md)
- [One message format, several file formats](../concepts/file-formats.md)
- [ImfEntity](ImfEntity.md)
- [ImfFileException](ImfFileException.md)
- [ImfOptions](ImfOptions.md)
- [ImfReader](ImfReader.md)
- [MhtmlDocument](MhtmlDocument.md)
- [File.InternetMessage API reference](index.md)
