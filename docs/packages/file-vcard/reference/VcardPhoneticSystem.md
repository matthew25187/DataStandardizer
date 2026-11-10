---
title: VcardPhoneticSystem Enum
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardPhoneticSystem Enum

## Definition

Namespace: `DataStandardizer.File.vCard`

The phonetic system of a value: the value of the `PHONETIC` parameter (RFC 9554 §4.6).

**Syntax**

```csharp
public enum VcardPhoneticSystem
```

## Fields

| Field | Value | `PHONETIC` value | Notes |
| --- | --- | --- | --- |
| `Ipa` | 0 | `ipa` | The International Phonetic Alphabet. |
| `Jyutping` | 1 | `jyut` | Jyutping, a romanization of Cantonese. |
| `Pinyin` | 2 | `piny` | Hanyu Pinyin, a romanization of Standard Mandarin. |
| `Script` | 3 | `script` | A system that isn't known, for which the `SCRIPT` parameter has to give the script. |

RFC 9554 §4.6 says the value "MUST be one of the values enumerated in the IANA" registry, which holds these four, so a reader rejects any other unless `AllowInvalidValues` is set.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardProperty](VcardProperty.md)
- [VcardPropertyParameters](VcardPropertyParameters.md)
