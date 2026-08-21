---
title: The language-tag builder pipeline
parent: LanguageTag
grand_parent: Packages
nav_order: 11
---

# The language-tag builder pipeline

`Bcp47LanguageTagBuilder` composes a `Bcp47LanguageTag` one subtag at a time
through a fluent chain. Rather than exposing every method on a single flat
interface, the builder uses a series of *step interfaces* — each call returns the
interface that describes only the steps that may legally follow — so that the
subtags are added in their correct BCP 47 order and IntelliSense guides you
through a valid sequence.

## The guided flow

A build proceeds through optional configuration, a required primary language
subtag, then the remaining optional subtags in spec order, and finally `Build()`:

```text
new Bcp47LanguageTagBuilder()
  ├─ WithLanguageSubtagRegistry(...)   (optional)
  ├─ WithTimeout(...)                  (optional)
  │
  ├─ UsingLanguageTag("...")  ───────────────────────────► Build()
  │
  └─ UsingPrimaryLanguageSubtag(...)   (required)
        ├─ UsingExtendedLanguageSubtags(...)   (optional, up to 3)
        ├─ UsingScriptSubtag(...)              (optional)
        ├─ UsingRegionSubtag(...)              (optional)
        ├─ UsingVariantSubtags(...)            (optional)
        ├─ UsingExtensionSubtags(...)          (optional)
        ├─ UsingPrivateUseSubtag(...)          (optional)
        └─ Build()
```

Each `Using…` step returns a `…Next` interface whose base interfaces are exactly
the steps still permitted, so once you have added (say) a region subtag you can no
longer go back and add a script subtag. Every step from the primary language
onward also exposes `Build()`, so you can stop as soon as the tag is complete.

## Two ways in: full tag or subtag-by-subtag

There are two mutually exclusive entry points after the optional configuration
steps:

- **`UsingLanguageTag("yue-Hant-HK")`** — hand the builder a complete tag string.
  This leads straight to `Build()`, and the string is validated when the tag is
  created.
- **`UsingPrimaryLanguageSubtag(...)`** — start a piece-by-piece composition,
  beginning with the (mandatory) primary language and adding refinements in order.

## Strongly-typed and string overloads

The subtag steps accept either raw strings or the strongly-typed enums from the
other *Data Standardizer* packages:

- Primary language — `Iso639Part1Language`, `Iso639Part2TLanguage`,
  `Iso639Part3Language`, `Iso639Part5LanguageFamily`, or `string`.
- Script — `Iso15924Script` or `string`.
- Region — `Iso3166Part1Alpha2Country`, `UnM49AreaByAlpha2CountryCode`,
  `UnM49AreaByAlpha3CountryCode`, or `string`.

When you pass an enum, the builder checks that the value is defined; when you pass
a string, it checks the subtag against the BCP 47 rules (via the matching
`Bcp47LanguageTag.Check…` method). An undefined enum value or a malformed string
throws `ArgumentException` at the point of the call, so mistakes surface early
rather than at `Build()`.

## Optional configuration: registry and timeout

Two configuration steps may be applied before composition begins:

- **`WithLanguageSubtagRegistry(subtagRegistry)`** constrains validation to a
  loaded copy of the *IANA Language Subtag Registry*. When supplied, the final
  `Build()` constructs the tag through the registry-based
  `Bcp47LanguageTag.Create(..., subtagRegistry)` overload.
- **`WithTimeout(matchTimeout)`** sets a time limit (`TimeSpan`) for the regular
  expressions that validate the tag. Because validation is regex-driven, this lets
  you bound the worst-case matching time; when set, `Build()` forwards it to the
  `Bcp47LanguageTag.Create(..., matchTimeout)` overload.

## How Build() assembles the tag

`Build()` concatenates the supplied subtags, in order, into a single
hyphen-separated string and then constructs the `Bcp47LanguageTag` from it (using
the registry and/or timeout if they were configured). A few rules are enforced at
this stage — for example, no more than three extended language subtags are
permitted — and the resulting tag is validated as it is created, so an invalid
combination surfaces as a `LanguageTagFormatException`.

## Related

- [Create language tags using the builder](../how-to/create-language-tags-using-builder.md)
- [Anatomy of a language tag](language-tag-anatomy.md)
- [API reference](../reference/index.md)
