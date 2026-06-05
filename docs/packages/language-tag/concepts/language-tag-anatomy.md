---
title: Anatomy of a language tag
parent: LanguageTag
grand_parent: Packages
nav_order: 10
---

# Anatomy of a language tag

A BCP 47 language tag is a hyphen-separated sequence of *subtags*, each occupying
a fixed position and carrying a distinct meaning — from the broad ("this is
English") to the very specific ("written in Simplified Chinese, as used in Hong
Kong"). The `Bcp47LanguageTag` type models each of these positions as a separate
property so you can read them back independently.

## The subtag sequence

Subtags always appear in the same order. Only the primary language subtag is
required; the rest are optional refinements. Taking `zh-cmn-Hans-CN-x-private` as
an illustrative shape:

```text
zh    -cmn   -Hans  -CN     -x-private
│      │      │      │       │
│      │      │      │       └── private use
│      │      │      └────────── region
│      │      └───────────────── script
│      └──────────────────────── extended language
└─────────────────────────────── primary language
```

Variant and extension subtags (omitted above for brevity) sit between the region
and private-use positions.

## What each subtag means

| Subtag | `Bcp47LanguageTag` member | Purpose |
| --- | --- | --- |
| **Primary language** | `PrimaryLanguageSubtag` | The core language (e.g. `en`, `mas`, `gsw`). Always present. Backed by ISO 639-1/-2/-3/-5 codes. |
| **Extended language** | `ExtendedLanguageSubtags` | Up to three optional subtags that further specify the language. |
| **Script** | `ScriptSubtag` | The writing system (e.g. `Hans`, `Cyrl`). Backed by ISO 15924. |
| **Region** | `RegionSubtag` | A country or area (e.g. `CA`, `419`). Backed by ISO 3166-1 alpha-2 or UN M49. |
| **Variant** | `VariantSubtags` | Registered variations such as dialects or orthographies (e.g. `1606nict`). |
| **Extension** | `ExtensionSubtags` | Subtags introduced by a single-character *singleton* (e.g. `u-…`), each modelled as a `Bcp47KeyedSubtag`. |
| **Private use** | `PrivateUseSubtag` | An `x-…` sequence reserved for private agreement, modelled as a `Bcp47KeyedSubtag`. |

The primary language subtag is the only one guaranteed to exist; `ScriptSubtag`
and `RegionSubtag` are nullable, while the extended-language, variant, and
extension members return arrays (empty when the corresponding subtags are
absent).

## Raw values and strongly-typed conversions

Each property above returns the *raw* string as it appeared in the tag. For the
positions that correspond to one of the other *Data Standardizer* standards, the
type also offers conversion methods that turn the raw subtag into a strongly-typed
enum value:

- `ToIso639Part1()`, `ToIso639Part2T()`, `ToIso639Part3()`, `ToIso639Part5()`
  for the primary language subtag.
- `ToIso15924()` for the script subtag.
- `ToIso3166Part1Alpha2()` and `ToUnM49()` for the region subtag.

Each conversion returns a nullable result, yielding `null` when the subtag is
absent or is not a recognised code in that standard. This keeps the raw,
spec-faithful representation separate from the convenience of strongly-typed
lookups.

## Keyed subtags: extension and private use

Extension and private-use subtags share a structure: a single-character key (the
*singleton*) followed by one or more further subtags. The `Bcp47KeyedSubtag`
struct captures both halves:

- `Singleton` — the leading single character (e.g. `'u'` or `'x'`), or `null` if
  the subtag is empty.
- `Subtags` — the remaining hyphen-separated parts as a string array.

So an extension subtag `u-sd-chzh` exposes a `Singleton` of `'u'` and `Subtags`
of `["sd", "chzh"]`.

## Related

- [Use language tags](../how-to/use-language-tags.md)
- [The language-tag builder pipeline](builder-pipeline.md)
- [API reference](../reference/index.md)
