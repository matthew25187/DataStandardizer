---
title: LanguageTag
parent: Packages
nav_order: 6
has_children: true
---

# DataStandardizer.LanguageTag

Strongly-typed support for IETF language tags as defined by BCP 47 — compose,
deconstruct, and validate tags such as `en`, `fr-CA`, `zh-Hans`, and
`es-419`.

```shell
dotnet add package DataStandardizer.LanguageTag
```

## Standards

| Standard | What it provides |
| --- | --- |
| **BCP 47** | IETF language tags (RFC 5646), built from subtags for primary language, extended language, script, region, variant, extension, and private use. |

Validation draws on the codes implemented by the other *Data Standardizer*
packages (ISO 639 languages, ISO 15924 scripts, ISO 3166-1 / UN M49 regions),
and can optionally be constrained to a loaded copy of the *IANA Language Subtag
Registry*.

## Platform support

Targets .NET Standard 1.0 and 2.0 for use in legacy applications, as well as
in-support modern .NET runtimes (`net8.0`, `net10.0`).

## How-to guides

- [Use language tags](how-to/use-language-tags.md)
- [Create language tags using the builder](how-to/create-language-tags-using-builder.md)

## Concepts

- [Anatomy of a language tag](concepts/language-tag-anatomy.md)
- [The language-tag builder pipeline](concepts/builder-pipeline.md)

## Reference

- [API reference](reference/index.md)
