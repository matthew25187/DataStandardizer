---
title: ImfHeaderFieldKind Enum
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfHeaderFieldKind Enum

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

How an Internet message writer may fold a header field (RFC 5322 §2.2).

**Syntax**

```csharp
public enum ImfHeaderFieldKind
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Unstructured` | 0 | An unstructured field, such as Subject, which may be folded at any space or tab (RFC 5322 §2.2.1). |
| `Structured` | 1 | A structured field, such as To or Content-Type, which may be folded only between its tokens (RFC 5322 §2.2.2), or between the words of a comment (RFC 5322 §3.2.2): at a space or tab outside a quoted string and a domain literal, and not in a quoted-pair. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [File.InternetMessage API reference](index.md)
