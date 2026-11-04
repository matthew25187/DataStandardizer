---
title: ImfFileException Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfFileException Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Exception to normal processing of an Internet message.

**Syntax**

```csharp
public sealed class ImfFileException : Exception
```

## Remarks

Details of where the problem was found are held in `Exception.Data`, under the keys in `DataItemName`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfFileException()` | Creates an exception with a default message. |
| `ImfFileException(string message)` | Creates an exception with a message. |
| `ImfFileException(string message, Exception innerException)` | Creates an exception with a message and the exception that caused it. |

## ImfFileException.DataItemName Class

Keys of the items in `Exception.Data`.

| Field | Value | Meaning |
| --- | --- | --- |
| `BytePosition` | `BytePosition` | The 0-based offset, in bytes from the start of the data, at which the problem was found, as a `Int64`. Present only for a problem found by a reader. |
| `DiagnosticKind` | `DiagnosticKind` | The kind of problem, as an [`ImfDiagnosticKind`](ImfDiagnosticKind.md). Present only for a problem that an option can turn into an [`ImfDiagnostic`](ImfDiagnostic.md), found by an [`ImfReader`](ImfReader.md), [`ImfWriter`](ImfWriter.md), [`ImfEntityReader`](ImfEntityReader.md), [`MboxReader`](MboxReader.md), [`MboxWriter`](MboxWriter.md), [`EmlxReader`](EmlxReader.md) or [`EmlxWriter`](EmlxWriter.md). |
| `EntityPath` | `EntityPath` | The path of the entity in which the problem was found, as a `String`, in the form that [`ImfEntityEvent.EntityPath`](ImfEntityEvent.md) describes. Present only for a problem found by an [`ImfEntityReader`](ImfEntityReader.md) or [`ImfEntityWriter`](ImfEntityWriter.md), or by the conformance checks of an [`ImfReader`](ImfReader.md) or [`ImfWriter`](ImfWriter.md). |
| `FieldName` | `FieldName` | The name of the header field, as a `String`, which for a required field that is missing is the name it should have. Otherwise present only once the name has been read. |
| `LineNumber` | `LineNumber` | The 1-based number of the physical line on which the problem was found, as a `Int64`. Present only for a problem found by a reader. |
| `MessageIndex` | `MessageIndex` | The 0-based index of the message in an mbox file, as a `Int64`. Present only for a problem found by an [`MboxReader`](MboxReader.md) or [`MboxWriter`](MboxWriter.md), or by an [`ImfEntityReader`](ImfEntityReader.md) that an [`MboxReader`](MboxReader.md) gave. In a problem with the format of the file, `DataItemName.BytePosition` and `DataItemName.LineNumber` count from the start of the file. In a problem in a message, they count from the start of the message as it reads after any quotes have been removed. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Compliant defaults and the opt-ins](../concepts/compliant-defaults.md)
- [Limits for hostile input](../concepts/hostile-input-limits.md)
- [EmlxReader](EmlxReader.md)
- [EmlxWriter](EmlxWriter.md)
- [ImfDiagnostic](ImfDiagnostic.md)
- [ImfDiagnosticKind](ImfDiagnosticKind.md)
- [ImfEntityEvent](ImfEntityEvent.md)
- [ImfEntityReader](ImfEntityReader.md)
- [ImfEntityWriter](ImfEntityWriter.md)
- [ImfReader](ImfReader.md)
- [File.InternetMessage API reference](index.md)
