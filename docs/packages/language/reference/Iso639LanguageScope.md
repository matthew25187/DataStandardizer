---
title: Iso639LanguageScope Enum
parent: Language
grand_parent: Packages
nav_exclude: true
---

# Iso639LanguageScope Enum

## Definition

Namespace: `DataStandardizer.Language`

The scope of an ISO 639 language code, as defined by ISO 639 Part 3.

```csharp
public enum Iso639LanguageScope
```

## Remarks

Read through [Iso639Extensions.GetScope()](Iso639Extensions.md) for an
[Iso639Part3Language](Iso639Part3Language.md) code. The accessor returns `null`
when the scope is not recorded; an undefined value corresponds to `Unknown`.

## Fields

| Field | Value | Notes |
| --- | --- | --- |
| `Unknown` | 0 | |
| `Individual` | 1 | Individual language. |
| `Collective` | 2 | Collections of languages connected, for example genetically or by region. |
| `Macrolanguage` | 3 | Macrolanguages. |
| `Special` | 4 | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso639Extensions](Iso639Extensions.md)
- [Iso639Part3Language](Iso639Part3Language.md)
- [Understanding the ISO 639 parts](../concepts/iso639-parts.md)
- [Language API reference](index.md)
