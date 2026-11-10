---
title: ImfEntityEventKind Enum
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfEntityEventKind Enum

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The kinds of event that an [`ImfEntityReader`](ImfEntityReader.md) gives.

**Syntax**

```csharp
public enum ImfEntityEventKind
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `EntityStart` | 0 | An entity starts. Its header fields come next. |
| `HeaderField` | 1 | A field of the entity's header section. |
| `HeaderEnd` | 2 | The entity's header section has ended, so its media type and transfer encoding are known. |
| `Preamble` | 3 | Raw bytes of the preamble of a multipart entity (RFC 2046 §5.1.1). A preamble may take several events. |
| `BodyChunk` | 4 | Bytes of the body of an entity that is neither a multipart entity nor an embedded message. A body may take several events. |
| `Epilogue` | 5 | Raw bytes of the epilogue of a multipart entity (RFC 2046 §5.1.1). An epilogue may take several events. |
| `EntityEnd` | 6 | An entity ends. The event gives the whole range of raw bytes of the entity. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Stream a large message](../how-to/stream-a-large-message.md)
- [ImfEntityReader](ImfEntityReader.md)
- [File.InternetMessage API reference](index.md)
