---
title: EmlxMessageFlags Enum
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# EmlxMessageFlags Enum

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The single-bit flags in the `flags` key of the property list of an EMLX file.

**Syntax**

```csharp
[Flags]
public enum EmlxMessageFlags : long
```

## Remarks

Apple doesn't publish these. They come from community documentation, such as the EMLX import code of Thunderbird, so [`EmlxMetadata.Flags`](EmlxMetadata.md) always gives the whole integer too. The bits that hold numbers are given by [`EmlxMetadata.AttachmentCount`](EmlxMetadata.md) and [`EmlxMetadata.PriorityLevel`](EmlxMetadata.md).

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `None` | 0 | None of the flags. |
| `Read` | 1 | The message has been read. |
| `Deleted` | 2 | The message has been deleted. |
| `Answered` | 4 | The message has been answered. |
| `Encrypted` | 8 | The message is encrypted. |
| `Flagged` | 16 | The message has been flagged. |
| `Recent` | 32 | The message is recent. |
| `Draft` | 64 | The message is a draft. |
| `Forwarded` | 256 | The message has been forwarded. |
| `Redirected` | 512 | The message has been redirected. |
| `Signed` | 8388608 | The message is signed. |
| `Junk` | 16777216 | The message is junk. |
| `NotJunk` | 33554432 | The message is not junk. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read Apple Mail EMLX files](../how-to/read-emlx.md)
- [EmlxMetadata](EmlxMetadata.md)
- [File.InternetMessage API reference](index.md)
