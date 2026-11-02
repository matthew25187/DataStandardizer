---
title: Well-formed, valid and the ISO code lists
parent: LanguageTag
grand_parent: Packages
nav_order: 12
---

# Well-formed, valid and the ISO code lists

A language tag can be right in two different ways: it can have the right form, and
its subtags can mean something. RFC 5646 §2.2.9 defines a conformance class for each,
and `Bcp47LanguageTag` implements both. It also offers a third check, against the ISO
code lists, for callers that can't load a registry. The
[`LanguageTagConformance`](../reference/LanguageTagConformance.md) enum names all
three.

| Conformance | What is checked | How to ask for it |
| --- | --- | --- |
| `WellFormed` | The grammar of RFC 5646 §2.1, and nothing else | `Create(string)`, `TryCreate`, `Parse`, `IsWellFormed` |
| `Valid` | RFC 5646 §2.2.9, against a loaded copy of the registry | The overloads that take a `SubtagRegistry` |
| `IsoCodeLists` | The grammar, plus the code lists in the *Data Standardizer* packages | `Create(string, LanguageTagConformance.IsoCodeLists)` and the other overloads that take a conformance |

## Well-formed

RFC 5646 §2.2.9 says:

> A tag is considered "well-formed" if it conforms to the ABNF (Section 2.1). Language
> tags may be well-formed in terms of syntax but not valid in terms of content.

A well-formed tag has subtags of the right length and characters in the right order.
Nothing has been looked up, so `zz` is well-formed even though no language has that
code. The 26 grandfathered tags, such as `i-klingon`, are well-formed because the
grammar lists them.

This is what you get by default, because it's the only check that needs no data.

```csharp
var languageTag = Bcp47LanguageTag.Create("zz");
var conformance = languageTag.Conformance;           // LanguageTagConformance.WellFormed

var isWellFormed = Bcp47LanguageTag.IsWellFormed("en--US");  // false: an empty subtag
```

## Valid

RFC 5646 §2.2.9 says a tag is valid if it satisfies these conditions:

> - The tag is well-formed.
> - Either the tag is in the list of grandfathered tags or all of its primary
>   language, extended language, script, region, and variant subtags appear in the IANA
>   Language Subtag Registry as of the particular registry date.
> - There are no duplicate variant subtags.
> - There are no duplicate singleton (extension) subtags.

Validity needs a registry, so you pass one in:

```csharp
var subtagRegistry = SubtagRegistry.CreateFromFile("language-subtag-registry.txt");

var languageTag = Bcp47LanguageTag.Create("en-US", subtagRegistry);
var conformance = languageTag.Conformance;           // LanguageTagConformance.Valid
```

The `Prefix` and `Deprecated` fields of the registry's records aren't checked, because
§2.2.9 doesn't list them. A deprecated subtag is still valid, and an extended language
subtag is valid after any primary language.

### Duplicates

A repeated variant subtag or extension singleton, as in `de-1901-1901` or
`en-a-bb-a-cc`, matches the grammar, so it's well-formed. §2.2.9 lists duplicates under
validity alone, so it isn't valid. The comparison ignores case.

### The registry date

The second condition says "as of the particular registry date", so validity depends on
the copy of the registry you load. A subtag added after that copy's `FileDate` isn't
valid against it. To keep up with new subtags, load a newer copy.

## The ISO code lists

`LanguageTagConformance.IsoCodeLists` is for a caller that wants more than the grammar
and can't load a registry. It checks that a tag is well-formed and that:

- The primary language is an ISO 639-1 code, an ISO 639-2/T or ISO 639-3 code of a
  language with no ISO 639-1 code, an ISO 639-5 code, or in `qaa` to `qtz`. A primary
  language of four to eight letters is checked by shape only.
- The script is an ISO 15924 code, or in `Qaaa` to `Qabx`.
- The region is an ISO 3166-1 code, a code that RFC 5646 §2.2.4 reserves, or a UN M.49
  area other than 830.
- No variant subtag or extension singleton is repeated.

This approximates validity, and **it isn't RFC 5646 validity**. It has these gaps:

- It doesn't check variant subtags, so `de-abcde` passes.
- It doesn't check primary languages of four to eight letters, so `abcde` passes.
- It doesn't check extended language subtags or their prefixes, so `en-abc` passes.
- Its lists are only as current as the `DataStandardizer.Language` and
  `DataStandardizer.Geography` packages you have installed.
- It rejects `sh`, which the registry has but ISO 639-1 doesn't. It also rejects `hbs`,
  which ISO 639-3 has but the registry doesn't, because the registry defines `sh` for
  that language.

A three-letter code of a language that has an ISO 639-1 code, such as `eng`, is
rejected, because RFC 5646 §2.2.1 says only the two-letter code is defined.

```csharp
var languageTag = Bcp47LanguageTag.Create("en-NZ", LanguageTagConformance.IsoCodeLists);
var isListed = Bcp47LanguageTag.IsValid("eng", LanguageTagConformance.IsoCodeLists);  // false
```

## The three side by side

| Tag | Well-formed | ISO code lists | Valid against the IANA registry |
| --- | --- | --- | --- |
| `en-US` | Yes | Yes | Yes |
| `i-klingon` | Yes | Yes | Yes |
| `x-a` | Yes | Yes | Yes |
| `en-Zzzz` | Yes | Yes | Yes |
| `zz` | Yes | No | No |
| `en-Abcd` | Yes | No | No |
| `en-830` | Yes | No | No |
| `eng` | Yes | No | No |
| `de-1901-1901` | Yes | No | No |
| `en-a-bb-a-cc` | Yes | No | No |
| `de-abcde` | Yes | Yes | No |
| `sh` | Yes | No | Yes |
| `en--US` | No | No | No |
| `en-a-x-a-y` | No | No | No |

## Choosing

- **You read tags from elsewhere and pass them on.** Create them well-formed. A tag
  that uses a subtag newer than your registry or your packages shouldn't stop you
  reading the file it came in. Check validity later, where it matters, with
  `IsValid`.
- **You accept tags from people and want to catch mistakes.** Load a registry and use
  validity. It's the only check that covers every subtag.
- **You want to catch mistakes but can't ship or load a registry.** Use the ISO code
  lists, and accept the gaps above.

The `Conformance` of a tag records what was checked when it was created, and never
changes, because the struct is read-only. `IsValid` on an existing tag reports a result
without changing it. To carry `Valid`, create the tag with the registry.

```csharp
var languageTag = Bcp47LanguageTag.Create("en-US");

var isValid = languageTag.IsValid(subtagRegistry);   // true
var conformance = languageTag.Conformance;           // still LanguageTagConformance.WellFormed
```

## Related

- [Use language tags](../how-to/use-language-tags.md)
- [Anatomy of a language tag](language-tag-anatomy.md)
- [LanguageTagConformance](../reference/LanguageTagConformance.md)
- [Bcp47LanguageTag](../reference/Bcp47LanguageTag.md)
