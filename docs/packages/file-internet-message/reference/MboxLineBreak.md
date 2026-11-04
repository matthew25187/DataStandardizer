---
title: MboxLineBreak Enum
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# MboxLineBreak Enum

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The line breaks that an [`MboxWriter`](MboxWriter.md) writes.

**Syntax**

```csharp
public enum MboxLineBreak
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `LineFeed` | 0 | A line feed, which RFC 4155 Appendix A requires of a canonical mbox database. This is the default. |
| `CarriageReturnLineFeed` | 1 | A carriage return and a line feed, which RFC 4155 Appendix A doesn't allow, for a mailbox that a program expects in this form. An [`MboxReader`](MboxReader.md) reads it only with [`ImfOptions.AllowMboxCarriageReturnLineFeed`](ImfOptions.md). |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read, filter and rewrite an mbox file](../how-to/read-and-rewrite-mbox.md)
- [ImfOptions](ImfOptions.md)
- [MboxReader](MboxReader.md)
- [MboxWriter](MboxWriter.md)
- [File.InternetMessage API reference](index.md)
