---
title: EmlxPlistValueKind Enum
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# EmlxPlistValueKind Enum

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The kinds of value in an XML property list.

**Syntax**

```csharp
public enum EmlxPlistValueKind
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Dictionary` | 0 | A `dict`: keys and values, in order. |
| `Array` | 1 | An `array`: values, in order. |
| `String` | 2 | A `string`. |
| `Integer` | 3 | An `integer`. |
| `Real` | 4 | A `real`. |
| `Date` | 5 | A `date`. |
| `Data` | 6 | A `data`: bytes in base64. |
| `True` | 7 | A `true`. |
| `False` | 8 | A `false`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read Apple Mail EMLX files](../how-to/read-emlx.md)
- [File.InternetMessage API reference](index.md)
