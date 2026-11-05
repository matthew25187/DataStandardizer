---
title: VcardFileException Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardFileException Class

## Definition

Namespace: `DataStandardizer.File.vCard`

The exception for a problem in vCard data. Details of where the problem was found are in
`Exception.Data`, under the keys in `VcardFileException.DataItemName`.

**Syntax**

```csharp
public sealed class VcardFileException : Exception
```

## Data items

| Key | Type | Notes |
| --- | --- | --- |
| `CardIndex` | `int` | The 0-based index of the card, counting each `BEGIN:VCARD` line. Present only when the line is in a card, up to and including its `END:VCARD` line. |
| `DiagnosticKind` | | The kind of conformance problem. The content line reader finds no conformance problems, so it never sets this item. |
| `Group` | `string` | The group of the line, without its `.`. Present only when the line has one and it has been read. |
| `LineNumber` | `long` | The 1-based number of the logical line. Not present when the data could not be decoded. For a value that `VcardContentLine.GetTextValue` can't read, it is 0 for a line not read from data. |
| `PhysicalLineNumber` | `long` | The 1-based number of the physical line. Not present when the data could not be decoded. For a value that `VcardContentLine.GetTextValue` can't read, it is 0 for a line not read from data. |
| `PropertyName` | `string` | The name of the property. Present only once the name has been read. |

When the data could not be decoded, `InnerException` is the decoder's
`DecoderFallbackException`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardFileException()` | A default message. |
| `VcardFileException(string message)` | The message that describes the problem. |
| `VcardFileException(string message, Exception innerException)` | With the exception that caused it. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardContentLineReader](VcardContentLineReader.md)
