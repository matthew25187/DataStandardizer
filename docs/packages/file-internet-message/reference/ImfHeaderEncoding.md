---
title: ImfHeaderEncoding Enum
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfHeaderEncoding Enum

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

How an Internet message writer writes header text that is not US-ASCII.

**Syntax**

```csharp
public enum ImfHeaderEncoding
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `EncodedWords` | 0 | As RFC 2047 encoded words, in UTF-8. A field body given to [`ImfHeaderWriter.WriteField(string, string, ImfHeaderFieldKind)`](ImfHeaderWriter.md) or [`ImfEntitySource.AddField(string, string, ImfHeaderFieldKind)`](ImfEntitySource.md) is written as it is, so one that is not US-ASCII is refused. Text given to [`ImfHeaderWriter.WriteTextField`](ImfHeaderWriter.md) or [`ImfEntitySource.AddTextField`](ImfEntitySource.md) is encoded. |
| `Utf8` | 1 | As UTF-8 (RFC 6532), which a transport can carry only if it supports internationalized email (RFC 6531). |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfEntitySource](ImfEntitySource.md)
- [ImfHeaderWriter](ImfHeaderWriter.md)
- [File.InternetMessage API reference](index.md)
