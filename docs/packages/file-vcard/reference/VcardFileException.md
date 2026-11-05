---
title: VcardFileException Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardFileException Class

## Definition

Namespace: `DataStandardizer.File.vCard`

The exception for a problem in vCard data, read or to be written. Details of where the problem was found are in
`Exception.Data`, under the keys in `VcardFileException.DataItemName`.

**Syntax**

```csharp
public sealed class VcardFileException : Exception
```

## Data items

| Key | Type | Notes |
| --- | --- | --- |
| `BytePosition` | `long` | The 0-based offset in bytes of the JSON token at fault, for a problem `JcardReader` finds in the structure of the data. For JSON that isn't valid, the end of the last token read. |
| `CardIndex` | `int` | The 0-based index of the card, counting each `BEGIN:VCARD` line. Present only when the line is in a card, up to and including its `END:VCARD` line. |
| `DiagnosticKind` | `VcardDiagnosticKind` | The kind of conformance problem. `VcardReader` and `VcardWriter` set it. The content line reader and writer find no conformance problems, so they never do. |
| `Group` | `string` | The group of the line, without its `.`. Present only when the line has one and it has been read. |
| `JsonPath` | `string` | The JSONPath of the value at fault, such as `$[1][3][0]`, for a problem `JcardReader` finds in the structure of the data once the value has been read. |
| `LineNumber` | `long` | The 1-based number of the logical line; for a line that can't be written, the number it would have had in the output. Not present when the data could not be decoded. For a value that `VcardContentLine.GetTextValue` can't read, it is 0 for a line not read from data. For JSON that `JcardReader` finds isn't valid, the line of the JSON document. |
| `LinePosition` | `long` | The 1-based position in bytes on the line, for JSON that `JcardReader` finds isn't valid. |
| `PhysicalLineNumber` | `long` | The 1-based number of the physical line. Not present for a line that can't be written, or when the data could not be decoded. For a value that `VcardContentLine.GetTextValue` can't read, it is 0 for a line not read from data. For a value that `GetDecodedValue` or `GetDecodedText` can't decode, it is the line on which the content line starts. |
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
- [VcardContentLineWriter](VcardContentLineWriter.md)
- [VcardReader](VcardReader.md)
- [VcardWriter](VcardWriter.md)
- [JcardReader](JcardReader.md)
- [JcardWriter](JcardWriter.md)
