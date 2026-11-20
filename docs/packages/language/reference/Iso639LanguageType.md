---
title: Iso639LanguageType Enum
parent: Language
grand_parent: Packages
nav_exclude: true
---

# Iso639LanguageType Enum

## Definition

Namespace: `DataStandardizer.Language`

The type of language an ISO 639 language code represents, as defined by ISO 639
Part 3.

```csharp
public enum Iso639LanguageType
```

## Remarks

Read through [Iso639Extensions.GetLanguageType()](Iso639Extensions.md) for an
[Iso639Part3Language](Iso639Part3Language.md) code. The accessor returns `null`
when the type is not recorded; an undefined value corresponds to `Unknown`.

## Fields

| Field | Value | Notes |
| --- | --- | --- |
| `Unknown` | 0 | The language type isn't known. |
| `Living` | 1 | Living languages. |
| `Ancient` | 2 | Obsolete. Ancient languages (extinct since ancient times). ISO 639:2023 merged the ancient type into `Historical`, and no ISO 639-3 code has it: languages such as Latin (`lat`) and Sumerian (`sux`) are `Historical`. |
| `Historical` | 3 | Historical languages (distinct from their modern form). |
| `Extinct` | 4 | Extinct languages in recent times. |
| `Constructed` | 5 | Constructed languages. |
| `Special` | 6 | Special codes, which don't identify a language (for example `und`, `mul` and `zxx`). |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, and `net10.0`.

## See also

- [Iso639Extensions](Iso639Extensions.md)
- [Iso639Part3Language](Iso639Part3Language.md)
- [Understanding the ISO 639 parts](../concepts/iso639-parts.md)
- [Language API reference](index.md)
