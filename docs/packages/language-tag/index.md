---
title: LanguageTag
parent: Packages
nav_order: 8
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
| **RFC 5646** | *Tags for Identifying Languages* (part of IETF BCP 47): language tags built from subtags for primary language, extended language, script, region, variant, extension, and private use, together with the format of the *IANA Language Subtag Registry* (section 3.1). The package implements both conformance classes of section 2.2.9: well-formed and valid. |

BCP 47 also includes RFC 4647, *Matching of Language Tags*, which defines
filtering and lookup of language ranges against tags. This package does not
implement RFC 4647 matching.

Creating a language tag checks that it's well-formed: it matches the grammar, and
nothing is looked up. To check that it's valid, pass a loaded copy of the *IANA
Language Subtag Registry*. If you can't load a registry, you can opt in to checking
against the codes implemented by the other *Data Standardizer* packages (ISO 639
languages, ISO 15924 scripts, ISO 3166-1 / UN M49 regions). See
[Well-formed, valid and the ISO code lists](concepts/well-formed-and-valid.md).

## Platform support

Targets .NET Standard 1.1 and 2.0 for use in legacy applications, as well as
in-support modern .NET runtimes (`net10.0`).

The `netstandard1.1` and `netstandard2.0` builds depend on the `System.Memory`
package, which provides the span types they use. NuGet installs it
automatically. The `net10.0` build has the span types built in,
so it has no extra dependency.

> **Applies to:** the `ReadOnlySpan<char>` overloads of `TryCreate`,
> `IsWellFormed`, `IsValid` and the `Check…Subtag` methods, and
> `Bcp47LanguageTag.AsSpan()`, exist on every target. `Bcp47LanguageTag` implements `ISpanFormattable`, `IParsable<T>` and
> `ISpanParsable<T>` on `net10.0` only. The builder's
> `params ReadOnlySpan<string>` overloads exist on `net10.0` only. Loading a
> subtag registry from a file path (`SubtagRegistry.CreateFromFile` and
> `CreateFromFileAsync`) needs `netstandard2.0` or later; on `netstandard1.1`,
> load it from a `Stream` or a string.

See [Platform support](../../overview/platform-support.md) for the full matrix,
and [Use language tags](how-to/use-language-tags.md#performance) for performance
notes.

## How-to guides

- [Use language tags](how-to/use-language-tags.md)
- [Create language tags using the builder](how-to/create-language-tags-using-builder.md)

## Concepts

- [Anatomy of a language tag](concepts/language-tag-anatomy.md)
- [The language-tag builder pipeline](concepts/builder-pipeline.md)
- [Well-formed, valid and the ISO code lists](concepts/well-formed-and-valid.md)

## Reference

- [API reference](reference/index.md)
