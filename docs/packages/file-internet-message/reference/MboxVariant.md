---
title: MboxVariant Enum
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# MboxVariant Enum

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The variants of the mbox format, which differ in how they keep a line of a message that starts with `From ` from being taken for a separator line.

**Syntax**

```csharp
public enum MboxVariant
```

## Remarks

RFC 4155 Appendix A prescribes no way of doing this, and the variants can't be told apart reliably from what a file holds, so a caller has to say which one a file uses.

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Mboxrd` | 0 | Any line matching `>*From ` gets one more `>`, and reading removes one, so a message reads back exactly. This is the default of [`MboxReader`](MboxReader.md) and [`MboxWriter`](MboxWriter.md). |
| `Mboxo` | 1 | A line starting `From ` is written as `>From `, and a line that already starts `>From ` is not changed. Reading can't tell these apart, so the quotes are kept. |
| `Mboxcl` | 2 | The quoting of `Mboxo`, with a Content-Length field that gives the length of the body as it is stored in the file, quotes included. The quotes are kept when reading. |
| `Mboxcl2` | 3 | A Content-Length field that gives the length of the body, with no quoting, so a message reads back exactly. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read, filter and rewrite an mbox file](../how-to/read-and-rewrite-mbox.md)
- [One message format, several file formats](../concepts/file-formats.md)
- [MboxReader](MboxReader.md)
- [MboxWriter](MboxWriter.md)
- [File.InternetMessage API reference](index.md)
