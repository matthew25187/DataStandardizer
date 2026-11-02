---
title: LanguageTagConformance Enum
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# LanguageTagConformance Enum

## Definition

Namespace: `DataStandardizer.LanguageTag`

What was checked when a language tag was created.

```csharp
public enum LanguageTagConformance
```

## Remarks

RFC 5646 §2.2.9 defines two classes of conformance, which are `WellFormed` and `Valid`.
`IsoCodeLists` isn't defined by the RFC: it's an approximation of validity for a
caller that can't load a Subtag Registry. See
[Well-formed, valid and the ISO code lists](../concepts/well-formed-and-valid.md).

The overloads that take a `LanguageTagConformance` throw
`ArgumentOutOfRangeException` for a value the enum doesn't define, and
`ArgumentException` for `Valid`, because validity needs a registry. To check validity,
use the overloads that take a [SubtagRegistry](SubtagRegistry.md) instead.

## Fields

| Field | Description |
| --- | --- |
| `WellFormed` | The tag matches the ABNF of RFC 5646 §2.1, and nothing else was checked. The default when a tag is created without a registry. |
| `IsoCodeLists` | The tag is well-formed, its primary language, script and region subtags are in the ISO 639, ISO 15924, ISO 3166-1 and UN M.49 lists in the `DataStandardizer.Language` and `DataStandardizer.Geography` packages, or are codes RFC 5646 reserves for local or private use, and it repeats no variant subtag or extension singleton. It doesn't check variants, primary languages of four to eight letters or extended language prefixes, and it isn't RFC 5646 validity. |
| `Valid` | The tag satisfies RFC 5646 §2.2.9 against a Subtag Registry. The registry's `Prefix` and `Deprecated` fields aren't checked, because §2.2.9 doesn't list them. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, and `net10.0`.

## See also

- [Well-formed, valid and the ISO code lists](../concepts/well-formed-and-valid.md)
- [Bcp47LanguageTag](Bcp47LanguageTag.md)
- [Bcp47LanguageTagBuilder](Bcp47LanguageTagBuilder.md)
- [LanguageTag API reference](index.md)
