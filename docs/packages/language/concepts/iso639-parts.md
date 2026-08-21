---
title: Understanding the ISO 639 parts
parent: Language
grand_parent: Packages
nav_order: 10
---

# Understanding the ISO 639 parts

ISO 639 is a multi-part standard for representing the names of languages. Each
part covers a different scope and code form, and the Language package exposes a
distinct type for each. Knowing which part you need is the key to choosing the
right type.

## The four parts

| Part | Code form | Scope | Type |
| --- | --- | --- | --- |
| Part 1 | Alpha-2 | The most widely used languages | `Iso639Part1Language` |
| Part 2 | Alpha-3 | Languages and a number of collections | `Iso639Part2TLanguage`, `Iso639Part2BLanguage` |
| Part 3 | Alpha-3 | Comprehensive coverage of individual languages | `Iso639Part3Language` |
| Part 5 | Alpha-3 | Language families and groups | `Iso639Part5LanguageFamily` |

### Part 1 — Alpha-2 codes

Part 1 assigns a two-letter code (for example `en` for English) to the most
commonly encountered languages. It is the smallest of the code sets and is what
most people picture when they think of a "language code".

### Part 2 — Alpha-3 codes, terminological and bibliographic

Part 2 uses three-letter codes. For historical reasons it was published in two
forms, and the package provides a separate type for each:

- **Terminological (T)** — `Iso639Part2TLanguage`. The code derived from the
  language's own name (for example `deu` for German).
- **Bibliographic (B)** — `Iso639Part2BLanguage`. The code historically used in
  bibliographic contexts, often derived from an English or French name (for
  example `ger` for German).

For most languages the T and B codes are identical; they differ only for the
relatively small set of languages where the two naming traditions diverged.

### Part 3 — comprehensive Alpha-3 codes

Part 3 extends the Alpha-3 approach of Part 2 to cover individual languages
comprehensively, including many that have no Part 1 or Part 2 code at all (for
example `pht` for Phu Thai). It carries the richest metadata of any part —
scope, language type, print and inverted names, and macrolanguage relationships.

### Part 5 — language families and groups

Part 5 provides Alpha-3 codes for language *families* and *groups* rather than
individual languages (for example `cau` for the Caucasian languages). Its type
is therefore named `Iso639Part5LanguageFamily`.

## Why these are `IStringEnum` structs, not C# enums

A C# `enum` is backed by an integer. ISO 639 does not define a numeric value for
its language codes — the code *is* the string. To model this faithfully, the
Part 1, 2 (T and B), 3 and 5 types are not enums but `readonly struct` types
implementing the package's `IStringEnum` interface. Each language code is a
static member whose underlying value is the string code from the standard, and
the struct defines an implicit conversion to `string` so a code can be used
directly where a string is expected.

This is why, in the how-to guides, codes are accessed as static members
(`Iso639Part1Language.en`) just like enum members, even though the types are not
enums. By contrast, ISO 15924 *does* define numeric codes for scripts, so
[`Iso15924Script`](../reference/Iso15924Script.md) is a conventional C#
`enum`.

## Related

- [Use language codes](../how-to/use-language-codes.md)
- [Access language metadata](../how-to/access-language-metadata.md)
- [API reference](../reference/index.md)
