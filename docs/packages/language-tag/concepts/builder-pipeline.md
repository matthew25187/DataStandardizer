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
  ├─ WithLanguageSubtagRegistry(...) or WithConformance(...)   (optional)
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
  This leads straight to `Build()`, and the string is checked when the tag is
  created.
- **`UsingPrimaryLanguageSubtag(...)`** — start a piece-by-piece composition,
  beginning with the (mandatory) primary language and adding refinements in order.

## Strongly-typed and string overloads

The subtag steps accept either raw strings or the strongly-typed enums from the
other *Data Standardizer* packages:

- Primary language — `Iso639Part1Language`, `Iso639Part2TLanguage`,
  `Iso639Part3Language`, `Iso639Part5LanguageFamily`, or `string`.
- Script — `Iso15924Script` or `string`.
- Region — `Iso3166Part1Alpha2Country` for a country, `UnM49Area` for an area
  made up of more than one country (such as `UnM49Area._419`), or `string`.

When you pass an enum, the builder checks that the value is defined (and, for
`UnM49Area`, that the member is an area BCP 47 allows); when you pass a string,
it checks the shape of the subtag (via the matching `Bcp47LanguageTag.Check…`
method). An undefined enum value or a string of the wrong shape throws
`ArgumentException` at the point of the call. Whether a subtag of the right shape
is acceptable is decided by `Build()`.

The variant and extension steps also refuse a repeated variant subtag or extension
singleton. A repeat is well-formed, but it could never be valid, so the builder
won't compose one.

## Optional configuration: what Build() checks

One of these may be applied before composition begins. If you call both on the same
builder, the last one applies. See
[Well-formed, valid and the ISO code lists](well-formed-and-valid.md).

- **`WithLanguageSubtagRegistry(subtagRegistry)`** — `Build()` checks that the tag is
  valid against a loaded copy of the *IANA Language Subtag Registry*.
- **`WithConformance(conformance)`** — `Build()` checks `WellFormed` or
  `IsoCodeLists`. `Valid` throws `ArgumentException`, because it needs a registry.

With neither, `Build()` checks that the tag is well-formed.

Version 2.0 removed the `WithTimeout` step, which had no effect: tags are checked
by a parser, which can't time out. See [Removed in v2.0](../../../resources/removed-in-v2.md#languagetag-match-timeouts).

## How Build() assembles the tag

`Build()` concatenates the supplied subtags, in order, into a single
hyphen-separated string and then constructs the `Bcp47LanguageTag` from it, using
the registry or conformance if one was configured. A few rules are enforced at
this stage — for example, no more than three extended language subtags are
permitted — and the resulting tag is checked as it is created, so a tag that fails
surfaces as a `LanguageTagFormatException`.

## Related

- [Create language tags using the builder](../how-to/create-language-tags-using-builder.md)
- [Anatomy of a language tag](language-tag-anatomy.md)
- [Well-formed, valid and the ISO code lists](well-formed-and-valid.md)
- [API reference](../reference/index.md)
