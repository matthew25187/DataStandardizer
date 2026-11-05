---
title: VcardLegacySyntax Enum
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardLegacySyntax Enum

## Definition

Namespace: `DataStandardizer.File.vCard`

The lexical features of vCard 2.1 that a content line uses.

```csharp
[Flags]
public enum VcardLegacySyntax
```

## Remarks

The flags say what the line has, not whether it is allowed where it is. In a vCard 2.1
card all of them are. A `CHARSET` or `ENCODING=QUOTED-PRINTABLE` parameter in a later
version is an unknown parameter, which the reader reads as any other, so the flag is how
to find out that it is there.

## Fields

| Field | Value | Notes |
| --- | --- | --- |
| `None` | `0` | The line uses none of them. |
| `ValuelessParameter` | `1` | A parameter with no name, only a value, as in `TEL;HOME;VOICE:`. |
| `QuotedPrintable` | `2` | The `QUOTED-PRINTABLE` encoding (vCard 2.1 §2.1.5). |
| `Base64` | `4` | The `BASE64` encoding (vCard 2.1 §2.1.5), not the `b` of RFC 2426 §4. |
| `SevenBit` | `8` | The `7BIT` encoding (vCard 2.1 §2.1.5), named. |
| `EightBit` | `16` | The `8BIT` encoding (vCard 2.1 §2.1.5). |
| `Charset` | `32` | The `CHARSET` parameter (vCard 2.1 §2.1.6). |

## Applies to

`netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net10.0`

## See also

- [VcardContentLine](VcardContentLine.md)
- [Handle legacy character sets](../how-to/handle-legacy-character-sets.md)
