---
title: ImfWriteProgress Struct
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfWriteProgress Struct

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The progress of a writer. [`ImfHeaderWriter`](ImfHeaderWriter.md) reports it after each field of a bulk write, [`ImfEntityWriter`](ImfEntityWriter.md) after each block of output, and [`ImfWriter`](ImfWriter.md) after each field and each block of a body, and [`MboxWriter`](MboxWriter.md) after each message of a bulk write.

**Syntax**

```csharp
public readonly struct ImfWriteProgress : IEquatable<ImfWriteProgress>
```

## Remarks

Counts are cumulative for the writer, not for the call that reports them.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfWriteProgress(long fieldsWritten, long entitiesWritten, long bytesWritten)` | Creates a progress value. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytesWritten` | `long BytesWritten { get; }` | Gets the number of bytes the writer has handed to the underlying stream so far. |
| `EntitiesWritten` | `long EntitiesWritten { get; }` | Gets the number of entities the writer has written so far, at any depth, including each message; always 0 for [`ImfHeaderWriter`](ImfHeaderWriter.md). [`MboxWriter`](MboxWriter.md) counts each message as one. An entity written from its raw bytes counts as one, whatever it holds. |
| `FieldsWritten` | `long FieldsWritten { get; }` | Gets the number of header fields the writer has written so far. The fields inside an entity written from its raw bytes are not counted. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(ImfWriteProgress other)` | `bool` | Indicates whether this progress value equals another. **Returns:** `true` if all the counts are equal; otherwise `false`. |
| `ToString()` | `string` | The value in the form it is written in a header. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `operator !=(ImfWriteProgress left, ImfWriteProgress right)` | `bool` | Indicates whether two progress values differ. |
| `operator ==(ImfWriteProgress left, ImfWriteProgress right)` | `bool` | Indicates whether two progress values are equal. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfEntityWriter](ImfEntityWriter.md)
- [ImfHeaderWriter](ImfHeaderWriter.md)
- [ImfWriter](ImfWriter.md)
- [MboxWriter](MboxWriter.md)
- [File.InternetMessage API reference](index.md)
