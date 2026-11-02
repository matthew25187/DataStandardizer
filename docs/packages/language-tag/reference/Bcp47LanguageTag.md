---
title: Bcp47LanguageTag Struct
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# Bcp47LanguageTag Struct

## Definition

Namespace: `DataStandardizer.LanguageTag`

An IETF BCP 47 language tag. Instances are created through the static `Create` /
`TryCreate` factory methods, the explicit `string` conversion, or
[Bcp47LanguageTagBuilder](Bcp47LanguageTagBuilder.md); the constructors are
private. The subtag accessors decompose the tag, and the `To…` methods convert
individual subtags to the strongly-typed enums of the other *Data Standardizer*
packages.

```csharp
public readonly struct Bcp47LanguageTag : IEquatable<Bcp47LanguageTag>,
    ISpanFormattable, IParsable<Bcp47LanguageTag>, ISpanParsable<Bcp47LanguageTag>
```

The struct implements `ISpanFormattable`, `IParsable<Bcp47LanguageTag>` and
`ISpanParsable<Bcp47LanguageTag>` on `net8.0` and `net10.0` only. On the .NET
Standard targets it implements `IEquatable<Bcp47LanguageTag>` alone.

## Remarks

Each subtag property getter throws `InvalidOperationException` when the instance
is uninitialised (default-constructed). On the `netstandard1.1` and
`netstandard2.0` targets the nullability of the operators, parameters, and return
values is expressed through JetBrains `[NotNull]` / `[CanBeNull]` attributes
rather than C# nullable reference types.

`Conformance` and the instance `IsValid` overloads throw
`InvalidOperationException` when the instance is uninitialised too.

What a tag is checked for depends on how it's created. See
[Well-formed, valid and the ISO code lists](../concepts/well-formed-and-valid.md).

- **Well-formed.** `Create(string)`, `TryCreate` without a registry, `Parse`, `TryParse`
  and the explicit conversion check the ABNF of RFC 5646 §2.1, and nothing else. So `zz`,
  `en-830` and `de-1901-1901` are accepted, and `en--US` isn't.
- **Valid.** The overloads that take a `SubtagRegistry` check RFC 5646 §2.2.9 against a
  loaded copy of the *IANA Language Subtag Registry*: every primary language, extended
  language, script, region and variant subtag has a record, and no variant subtag or
  extension singleton repeats. `Prefix` and `Deprecated` aren't checked, because §2.2.9
  doesn't list them.
- **The ISO code lists.** The overloads that take `LanguageTagConformance.IsoCodeLists`
  check the grammar, the code lists of the `DataStandardizer.Language` and
  `DataStandardizer.Geography` packages, and that nothing repeats. This approximates
  validity, and isn't RFC 5646 validity.

The overloads that take a `LanguageTagConformance` throw `ArgumentException` for
`Valid`, which needs a registry, and `ArgumentOutOfRangeException` for an undefined
value. `TryCreate` throws these too, because a wrong argument isn't a wrong tag.

Language tags are checked by a parser, not by regular expressions, so there is
nothing that can time out. Version 2.0 removed the overloads that took a
`TimeSpan matchTimeout`; see
[Removed in v2.0](../../../resources/removed-in-v2.md#languagetag-match-timeouts).

A tag is parsed once, when it is created. The subtag properties return parts
kept from that parse, so reading them doesn't parse the tag again.
`ExtendedLanguageSubtags`, `VariantSubtags` and `ExtensionSubtags` return
read-only lists. Each read returns the same list, without copying it, and the
list can't be changed, even by casting it to `IList<T>` or an array, so a
caller can't change the tag's own subtags.

The overloads that take a `ReadOnlySpan<char>` check the characters in place,
so a tag or subtag that is part of a larger buffer, such as an HTTP header or a
file being read, can be checked without first making a string of it.
`IsWellFormed`, `IsValid` and the `Check…Subtag` methods allocate nothing,
and `TryCreate` copies the tag to a string only when it accepts it, so rejecting
input allocates nothing either. (The first use of the ISO code lists in a process
builds them, once.) These overloads exist on every target;
the `netstandard1.1` and `netstandard2.0` builds get `ReadOnlySpan<char>` from
the `System.Memory` package.

The 26 grandfathered tags listed in RFC 5646 §2.1, such as `i-klingon`,
`en-GB-oed` and `zh-min-nan`, are well-formed and are accepted under the ISO code
lists, compared case-insensitively. With a subtag registry, the grandfathered tags are
the registry's `grandfathered` records. Each is interpreted as a whole (§2.2.8), so it
has no subtags: `PrimaryLanguageSubtag` and the other subtag properties are `null` or
empty. A tag that only starts with a grandfathered tag,
such as `i-klingon-x-a`, is checked as an ordinary language tag.

In every mode, a primary language subtag has letters only, so `12345` is rejected, and
extended language subtags may follow only a two- or three-letter primary language
subtag (§2.2.2), so `zh-yue` is accepted and `abcde-yue` is not.

Under the ISO code lists, the primary language subtag is an ISO 639-1 code, an
ISO 639-2/T or ISO 639-3 code of a language with no ISO 639-1 code, an ISO 639-5 code
or in `qaa` to `qtz`, or has four to eight letters. So `eng` is rejected (§2.2.1). `hbs`
is rejected too, because the registry defines `sh` for that language. The script subtag
is an ISO 15924 code or one of the 50 codes reserved for private use (§2.2.3¶3): `Qaaa` to
`Qaaz`, then `Qaba` to `Qabx`. So `en-Qaaz` and `en-Qabx` are accepted, and `en-Qaby` is
not. The region subtag is an ISO 3166-1 alpha-2 code, a private use code (§2.2.4¶3), or a
UN M49 code for the world, a region, a sub-region or an intermediate region. The UN M49
code `830` (Channel Islands) is rejected: §2.2.4 allows it to be registered, but it is
not.

With a subtag registry, `830` is accepted only if the registry has a record for it, and a record whose `Subtag` is a range, such as `qaa..qtz`,
`Qaaa..Qabx`, `QM..QZ` or `XA..XZ` in the IANA registry, registers every subtag
in that range (§3.1.1), so `qaa`, `en-Qabx` and `en-XA` are accepted.

## Properties

Each getter throws `InvalidOperationException` if the instance is uninitialised.

| Property | Signature | Notes |
| --- | --- | --- |
| `Conformance` | `LanguageTagConformance Conformance { get; }` | What was checked when the tag was created. It never changes, even after `IsValid`. |
| `ExtendedLanguageSubtags` | `IReadOnlyList<string> ExtendedLanguageSubtags { get; }` | Empty if absent. |
| `ExtensionSubtags` | `IReadOnlyList<Bcp47KeyedSubtag> ExtensionSubtags { get; }` | Empty if absent. |
| `PrimaryLanguageSubtag` | `string? PrimaryLanguageSubtag { get; }` | `null` on a grandfathered or private use tag; otherwise always present. |
| `PrivateUseSubtag` | `Bcp47KeyedSubtag? PrivateUseSubtag { get; }` | `null` if absent. |
| `RegionSubtag` | `string? RegionSubtag { get; }` | `null` if absent. |
| `ScriptSubtag` | `string? ScriptSubtag { get; }` | `null` if absent. |
| `VariantSubtags` | `IReadOnlyList<string> VariantSubtags { get; }` | Empty if absent. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `AsSpan()` | `ReadOnlySpan<char>` | The tag string as a span, without copying it. An empty span if the instance is uninitialised. |
| `CheckExtendedLanguageSubtag(string extendedLanguageSubtag)` | `bool` | Static. `true` if the subtag has the shape of an extended language: three letters. Looks nothing up. Throws `ArgumentNullException` if `null`. |
| `CheckExtendedLanguageSubtag(ReadOnlySpan<char> extendedLanguageSubtag)` | `bool` | Static. As for the `string` overload. |
| `CheckExtensionSubtag(string extensionSubtag)` | `bool` | Static. `true` if the string has the shape of one extension: a singleton other than `x`, then one or more subtags of two to eight letters or digits. Looks nothing up. Throws `ArgumentNullException` if `null`. |
| `CheckExtensionSubtag(ReadOnlySpan<char> extensionSubtag)` | `bool` | Static. As for the `string` overload. |
| `CheckPrimaryLanguageSubtag(string primaryLanguageSubtag)` | `bool` | Static. `true` if the subtag has the shape of a primary language: two to eight letters. Looks nothing up, so it is `true` for `zz` and `eng`. Throws `ArgumentNullException` if `null`. |
| `CheckPrimaryLanguageSubtag(ReadOnlySpan<char> primaryLanguageSubtag)` | `bool` | Static. As for the `string` overload. |
| `CheckPrivateUseSubtag(string privateUseSubtag)` | `bool` | Static. `true` if the string has the shape of a private use sequence: `x`, then one or more subtags of one to eight letters or digits. Throws `ArgumentNullException` if `null`. |
| `CheckPrivateUseSubtag(ReadOnlySpan<char> privateUseSubtag)` | `bool` | Static. As for the `string` overload. |
| `CheckRegionSubtag(string regionSubtag)` | `bool` | Static. `true` if the subtag has the shape of a region: two letters or three digits. Looks nothing up, so it is `true` for `830`. Throws `ArgumentNullException` if `null`. |
| `CheckRegionSubtag(ReadOnlySpan<char> regionSubtag)` | `bool` | Static. As for the `string` overload. |
| `CheckScriptSubtag(string scriptSubtag)` | `bool` | Static. `true` if the subtag has the shape of a script: four letters. Looks nothing up. Throws `ArgumentNullException` if `null`. |
| `CheckScriptSubtag(ReadOnlySpan<char> scriptSubtag)` | `bool` | Static. As for the `string` overload. |
| `CheckVariantSubtag(string variantSubtag)` | `bool` | Static. `true` if the subtag has the shape of one variant: five to eight letters or digits, or a digit followed by three letters or digits. Looks nothing up. Throws `ArgumentNullException` if `null`. |
| `CheckVariantSubtag(ReadOnlySpan<char> variantSubtag)` | `bool` | Static. As for the `string` overload. |
| `Create(string languageTag)` | `Bcp47LanguageTag` | Static. Checks that the tag is well-formed. Throws `LanguageTagFormatException` if it isn't; `ArgumentNullException` if `null`. |
| `Create(string languageTag, LanguageTagConformance conformance)` | `Bcp47LanguageTag` | Static. Checks `WellFormed` or `IsoCodeLists`. Throws `ArgumentException` for `Valid`. |
| `Create(string languageTag, SubtagRegistry subtagRegistry)` | `Bcp47LanguageTag` | Static. Checks that the tag is valid against a loaded subtag registry. |
| `Equals(Bcp47LanguageTag other)` | `bool` | Case-insensitive comparison of the tag string. |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |
| `IsValid(LanguageTagConformance conformance)` | `bool` | Whether the tag satisfies `WellFormed` or `IsoCodeLists`, whatever was checked when it was created. Throws `ArgumentException` for `Valid`, and `InvalidOperationException` if the instance is uninitialised. |
| `IsValid(SubtagRegistry subtagRegistry)` | `bool` | Whether the tag is valid against a loaded subtag registry, whatever was checked when it was created. Throws `ArgumentNullException` if `null`, and `InvalidOperationException` if the instance is uninitialised. |
| `IsValid(string languageTag, LanguageTagConformance conformance)` | `bool` | Static. Whether the string satisfies `WellFormed` or `IsoCodeLists`. Throws `ArgumentException` for `Valid`, and `ArgumentNullException` if the string is `null`. |
| `IsValid(ReadOnlySpan<char> languageTag, LanguageTagConformance conformance)` | `bool` | Static. As for the `string` overload. |
| `IsValid(string languageTag, SubtagRegistry subtagRegistry)` | `bool` | Static. Whether the string is valid against a loaded subtag registry (RFC 5646 §2.2.9). Throws `ArgumentNullException` if either is `null`. |
| `IsValid(ReadOnlySpan<char> languageTag, SubtagRegistry subtagRegistry)` | `bool` | Static. As for the `string` overload. |
| `IsWellFormed(string languageTag)` | `bool` | Static. Whether the string matches the ABNF of RFC 5646 §2.1. `true` for the grandfathered tags, and for a tag that repeats a variant subtag or extension singleton (§2.2.9). Throws `ArgumentNullException` if `null`. |
| `IsWellFormed(ReadOnlySpan<char> languageTag)` | `bool` | Static. As for the `string` overload. |
| `Parse(string s, IFormatProvider? provider)` | `Bcp47LanguageTag` | Static; implements `IParsable<Bcp47LanguageTag>`. As for `Create(string)`; `provider` is ignored. `net8.0` and `net10.0` only. |
| `Parse(ReadOnlySpan<char> s, IFormatProvider? provider)` | `Bcp47LanguageTag` | Static; implements `ISpanParsable<Bcp47LanguageTag>`. Throws `LanguageTagFormatException` if the tag isn't well-formed. `provider` is ignored. `net8.0` and `net10.0` only. |
| `ToIso15924()` | `Iso15924Script?` | From the script subtag; `null` if absent or unrecognised. |
| `ToIso3166Part1Alpha2()` | `Iso3166Part1Alpha2Country?` | From the region subtag; `null` if absent or unrecognised. |
| `ToIso639Part1()` | `Iso639Part1Language?` | From the primary language subtag; `null` if absent or unrecognised. |
| `ToIso639Part2T()` | `Iso639Part2TLanguage?` | From the primary language subtag; `null` if absent or unrecognised. |
| `ToIso639Part3()` | `Iso639Part3Language?` | From the primary language subtag; `null` if absent or unrecognised. |
| `ToIso639Part5()` | `Iso639Part5LanguageFamily?` | From the primary language subtag; `null` if absent or unrecognised. |
| `ToString()` | `string` | Override. Returns the underlying tag string. |
| `ToUnM49()` | `ushort?` | From the region subtag; `null` if absent or not a UN M49 code. |
| `TryCreate(string? languageTag, out Bcp47LanguageTag result)` | `bool` | Static. Checks that the tag is well-formed. Returns `false` instead of throwing. |
| `TryCreate(string? languageTag, LanguageTagConformance conformance, out Bcp47LanguageTag result)` | `bool` | Static. Checks `WellFormed` or `IsoCodeLists`. Throws `ArgumentException` for `Valid`. |
| `TryCreate(string? languageTag, SubtagRegistry subtagRegistry, out Bcp47LanguageTag result)` | `bool` | Static. Checks that the tag is valid against a loaded subtag registry. |
| `TryCreate(ReadOnlySpan<char> languageTag, out Bcp47LanguageTag result)` | `bool` | Static. Checks the span in place and copies it to a string only if it is well-formed, so rejecting input allocates nothing. |
| `TryCreate(ReadOnlySpan<char> languageTag, LanguageTagConformance conformance, out Bcp47LanguageTag result)` | `bool` | Static. As above, checking `WellFormed` or `IsoCodeLists`. Throws `ArgumentException` for `Valid`. |
| `TryCreate(ReadOnlySpan<char> languageTag, SubtagRegistry subtagRegistry, out Bcp47LanguageTag result)` | `bool` | Static. As above, checking validity against a loaded subtag registry. Returns `false` if `subtagRegistry` is `null`. |
| `TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)` | `bool` | Implements `ISpanFormattable`. Copies the tag string, as `ToString()` returns it, into `destination`; `false`, with `charsWritten` 0, if it is too short. `format` and `provider` are ignored. `net8.0` and `net10.0` only. |
| `TryParse(string? s, IFormatProvider? provider, out Bcp47LanguageTag result)` | `bool` | Static; implements `IParsable<Bcp47LanguageTag>`. As for `TryCreate(string?, out Bcp47LanguageTag)`; `provider` is ignored. `net8.0` and `net10.0` only. |
| `TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Bcp47LanguageTag result)` | `bool` | Static; implements `ISpanParsable<Bcp47LanguageTag>`. As for `TryCreate(ReadOnlySpan<char>, out Bcp47LanguageTag)`; `provider` is ignored. `net8.0` and `net10.0` only. |

### Explicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `IFormattable.ToString(string? format, IFormatProvider? formatProvider)` | `string` | Returns the same as `ToString()`; `format` and `formatProvider` are ignored. `net8.0` and `net10.0` only. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Explicit | `explicit operator Bcp47LanguageTag(string)` | Checks that the tag is well-formed, like `Create(string)`. Throws `InvalidCastException` for `null` or a tag that is not well-formed. |
| Implicit | `implicit operator string?(Bcp47LanguageTag)` | Unwraps to the underlying tag string. |
| Equality | `operator ==`, `!=` `(Bcp47LanguageTag, Bcp47LanguageTag)` | Case-insensitive comparison of the tag string. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `net8.0`, and `net10.0`. On the
.NET Standard targets nullability is expressed through JetBrains
`[NotNull]` / `[CanBeNull]` attributes. The public surface otherwise differs by
target as follows:

| Members | `netstandard1.1` | `netstandard2.0` | `net8.0` | `net10.0` |
| --- | --- | --- | --- | --- |
| `AsSpan()` and the `ReadOnlySpan<char>` overloads of `Check…Subtag`, `IsWellFormed`, `IsValid` and `TryCreate` | Yes | Yes | Yes | Yes |
| `ISpanFormattable` (`TryFormat`, `IFormattable.ToString`) | No | No | Yes | Yes |
| `IParsable<Bcp47LanguageTag>` and `ISpanParsable<Bcp47LanguageTag>` (`Parse`, `TryParse`) | No | No | Yes | Yes |

On `net10.0`, the ISO code lists and registry checks look subtags up in hash sets; the
other targets search sorted arrays. Neither lookup allocates, and the results are the
same.

## See also

- [Use language tags](../how-to/use-language-tags.md)
- [Anatomy of a language tag](../concepts/language-tag-anatomy.md)
- [Well-formed, valid and the ISO code lists](../concepts/well-formed-and-valid.md)
- [LanguageTagConformance](LanguageTagConformance.md)
- [Bcp47KeyedSubtag](Bcp47KeyedSubtag.md)
- [Bcp47LanguageTagBuilder](Bcp47LanguageTagBuilder.md)
- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
