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
is uninitialised (default-constructed). On the `netstandard1.0` and
`netstandard2.0` targets the nullability of the operators, parameters, and return
values is expressed through JetBrains `[NotNull]` / `[CanBeNull]` attributes
rather than C# nullable reference types.

The `Create` and `TryCreate` overloads that take a `SubtagRegistry` constrain
validation to a loaded copy of the *IANA Language Subtag Registry*.

Language tags are checked by a parser, not by regular expressions, so there is
nothing that can time out. The overloads that take a `TimeSpan matchTimeout` are
**deprecated**: they are marked `[Obsolete]`, they accept the timeout and ignore
it, and they will be removed in version 2.0. They still check the timeout as
before, so a value that is zero, negative or more than `int.MaxValue` - 1
milliseconds, other than `Regex.InfiniteMatchTimeout`, throws
`ArgumentOutOfRangeException`. Use the overload without the `matchTimeout`
parameter.

A tag is parsed once, when it is created. The subtag properties return parts
kept from that parse, so reading them doesn't parse the tag again. The array
properties return a new copy of a non-empty array on each call, so a caller
can't change the tag's own subtags.

The overloads that take a `ReadOnlySpan<char>` validate the characters in place,
so a tag or subtag that is part of a larger buffer, such as an HTTP header or a
file being read, can be checked without first making a string of it.
`IsWellFormedLanguageTagString` and the `Check…Subtag` methods allocate nothing,
and `TryCreate` copies the tag to a string only when it is valid, so rejecting
invalid input allocates nothing either. (The first call in a process builds the
code lists that validation uses, once.) These overloads exist on
`netstandard2.0`, `net8.0` and `net10.0`, but not on `netstandard1.0`; the
`netstandard2.0` build gets `ReadOnlySpan<char>` from the `System.Memory`
package.

The 26 grandfathered tags listed in RFC 5646 §2.1, such as `i-klingon`,
`en-GB-oed` and `zh-min-nan`, are accepted in both modes and compared
case-insensitively. Each is interpreted as a whole (§2.2.8), so it has no
subtags: `PrimaryLanguageSubtag` is the empty string, and the other subtag
properties are `null` or empty. A tag that only starts with a grandfathered tag,
such as `i-klingon-x-a`, is validated as an ordinary language tag.

Without a subtag registry, the primary language subtag follows the RFC 5646
grammar (§2.1, §2.2.1): an ISO 639 code (including `qaa` to `qtz`), a four-letter
subtag reserved for future use, or a five- to eight-letter registered subtag.
Subtags of four or more characters are letters only, so `12345` is rejected.
Extended language subtags may follow only a two- or three-letter primary
language subtag (§2.2.2), so `zh-yue` is accepted and `abcde-yue` is not.

Without a subtag registry, the region subtag is an ISO 3166-1 alpha-2 code, a
private use code (§2.2.4¶3), or a UN M49 code for the world, a region, a
sub-region or an intermediate region. The UN M49 code `830` (Channel Islands) is rejected: §2.2.4
allows it to be registered, but it is not, and an unregistered UN M49 area code
must not be used. With a subtag registry, `830` is accepted only if the registry
has a record for it.

With a subtag registry, a record whose `Subtag` is a range, such as `qaa..qtz`,
`Qaaa..Qabx`, `QM..QZ` or `XA..XZ` in the IANA registry, registers every subtag
in that range (§3.1.1), so `qaa`, `en-Qabx` and `en-XA` are accepted.

## Properties

Each getter throws `InvalidOperationException` if the instance is uninitialised.

| Property | Signature | Notes |
| --- | --- | --- |
| `ExtendedLanguageSubtags` | `string[] ExtendedLanguageSubtags { get; }` | Empty if absent. |
| `ExtensionSubtags` | `Bcp47KeyedSubtag[] ExtensionSubtags { get; }` | Empty if absent. |
| `PrimaryLanguageSubtag` | `string PrimaryLanguageSubtag { get; }` | Empty on a grandfathered or private use tag; otherwise always present. |
| `PrivateUseSubtag` | `Bcp47KeyedSubtag? PrivateUseSubtag { get; }` | `null` if absent. |
| `RegionSubtag` | `string? RegionSubtag { get; }` | `null` if absent. |
| `ScriptSubtag` | `string? ScriptSubtag { get; }` | `null` if absent. |
| `VariantSubtags` | `string[] VariantSubtags { get; }` | Empty if absent. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `AsSpan()` | `ReadOnlySpan<char>` | The tag string as a span, without copying it. An empty span if the instance is uninitialised. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CheckExtendedLanguageSubtag(string extendedLanguageSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. Throws `ArgumentNullException` if `null`. |
| `CheckExtendedLanguageSubtag(ReadOnlySpan<char> extendedLanguageSubtag)` | `bool` | Static. As for the `string` overload. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CheckExtensionSubtag(string extensionSubtag)` | `bool` | Static. Validates one extension (a singleton and its subtags) using the default BCP 47 rules. Throws `ArgumentNullException` if `null`. |
| `CheckExtensionSubtag(ReadOnlySpan<char> extensionSubtag)` | `bool` | Static. As for the `string` overload. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CheckPrimaryLanguageSubtag(string primaryLanguageSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. `true` for an ISO 639 code or four to eight letters (RFC 5646 §2.2.1). Throws `ArgumentNullException` if `null`. |
| `CheckPrimaryLanguageSubtag(ReadOnlySpan<char> primaryLanguageSubtag)` | `bool` | Static. As for the `string` overload. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CheckPrivateUseSubtag(string privateUseSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. Throws `ArgumentNullException` if `null`. |
| `CheckPrivateUseSubtag(ReadOnlySpan<char> privateUseSubtag)` | `bool` | Static. As for the `string` overload. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CheckRegionSubtag(string regionSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. Throws `ArgumentNullException` if `null`. |
| `CheckRegionSubtag(ReadOnlySpan<char> regionSubtag)` | `bool` | Static. As for the `string` overload. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CheckScriptSubtag(string scriptSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. Throws `ArgumentNullException` if `null`. |
| `CheckScriptSubtag(ReadOnlySpan<char> scriptSubtag)` | `bool` | Static. As for the `string` overload. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CheckVariantSubtag(string variantSubtag)` | `bool` | Static. Validates one variant subtag using the default BCP 47 rules. Throws `ArgumentNullException` if `null`. |
| `CheckVariantSubtag(ReadOnlySpan<char> variantSubtag)` | `bool` | Static. As for the `string` overload. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `Create(string languageTag)` | `Bcp47LanguageTag` | Static. Throws `LanguageTagFormatException` if the tag is invalid, including when it repeats a variant subtag or extension singleton; `ArgumentNullException` if `null`. |
| `Create(string languageTag, TimeSpan matchTimeout)` | `Bcp47LanguageTag` | **Deprecated.** Static. As for `Create(string)`; the timeout has no effect. See [Remarks](#remarks). |
| `Create(string languageTag, SubtagRegistry subtagRegistry)` | `Bcp47LanguageTag` | Static. Validates against a loaded subtag registry. |
| `Create(string languageTag, SubtagRegistry subtagRegistry, TimeSpan matchTimeout)` | `Bcp47LanguageTag` | **Deprecated.** Static. As for `Create(string, SubtagRegistry)`; the timeout has no effect. |
| `Equals(Bcp47LanguageTag other)` | `bool` | Case-insensitive comparison of the tag string. |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |
| `IsWellFormedLanguageTagString(string languageTagString)` | `bool` | Static. Validates using the default BCP 47 rules. `false` if a variant subtag or extension singleton is repeated (RFC 5646 §2.2.5, §2.2.6). `true` for the grandfathered tags (§2.1). Throws `ArgumentNullException` if `null`. |
| `IsWellFormedLanguageTagString(ReadOnlySpan<char> languageTagString)` | `bool` | Static. As for the `string` overload. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `Parse(string s, IFormatProvider? provider)` | `Bcp47LanguageTag` | Static; implements `IParsable<Bcp47LanguageTag>`. As for `Create(string)`; `provider` is ignored. `net8.0` and `net10.0` only. |
| `Parse(ReadOnlySpan<char> s, IFormatProvider? provider)` | `Bcp47LanguageTag` | Static; implements `ISpanParsable<Bcp47LanguageTag>`. Throws `LanguageTagFormatException` if the tag is invalid. `provider` is ignored. `net8.0` and `net10.0` only. |
| `ToIso15924()` | `Iso15924Script?` | From the script subtag; `null` if absent or unrecognised. |
| `ToIso3166Part1Alpha2()` | `Iso3166Part1Alpha2Country?` | From the region subtag; `null` if absent or unrecognised. |
| `ToIso639Part1()` | `Iso639Part1Language?` | From the primary language subtag; `null` if unrecognised. |
| `ToIso639Part2T()` | `Iso639Part2TLanguage?` | From the primary language subtag; `null` if unrecognised. |
| `ToIso639Part3()` | `Iso639Part3Language?` | From the primary language subtag; `null` if unrecognised. |
| `ToIso639Part5()` | `Iso639Part5LanguageFamily?` | From the primary language subtag; `null` if unrecognised. |
| `ToString()` | `string` | Override. Returns the underlying tag string. |
| `ToUnM49()` | `ushort?` | From the region subtag; `null` if absent or not a UN M49 code. |
| `TryCreate(string? languageTag, out Bcp47LanguageTag result)` | `bool` | Static. Returns `false` instead of throwing. |
| `TryCreate(string? languageTag, TimeSpan matchTimeout, out Bcp47LanguageTag result)` | `bool` | **Deprecated.** Static. As for `TryCreate(string?, out Bcp47LanguageTag)`; the timeout has no effect. |
| `TryCreate(string? languageTag, SubtagRegistry subtagRegistry, out Bcp47LanguageTag result)` | `bool` | Static. Validates against a loaded subtag registry. |
| `TryCreate(string? languageTag, SubtagRegistry subtagRegistry, TimeSpan matchTimeout, out Bcp47LanguageTag result)` | `bool` | **Deprecated.** Static. As for `TryCreate(string?, SubtagRegistry, out Bcp47LanguageTag)`; the timeout has no effect. |
| `TryCreate(ReadOnlySpan<char> languageTag, out Bcp47LanguageTag result)` | `bool` | Static. Validates the span in place and copies it to a string only if it is valid, so invalid input allocates nothing. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `TryCreate(ReadOnlySpan<char> languageTag, SubtagRegistry subtagRegistry, out Bcp47LanguageTag result)` | `bool` | Static. As above, validating against a loaded subtag registry. Returns `false` if `subtagRegistry` is `null`. `netstandard2.0`, `net8.0` and `net10.0` only. |
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
| Explicit | `explicit operator Bcp47LanguageTag(string)` | Validates with the default BCP 47 rules, like `Create(string)`. Throws `InvalidCastException` for `null` or a tag that is not well-formed. |
| Implicit | `implicit operator string?(Bcp47LanguageTag)` | Unwraps to the underlying tag string. |
| Equality | `operator ==`, `!=` `(Bcp47LanguageTag, Bcp47LanguageTag)` | Case-insensitive comparison of the tag string. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. On the
.NET Standard targets nullability is expressed through JetBrains
`[NotNull]` / `[CanBeNull]` attributes. The public surface otherwise differs by
target as follows:

| Members | `netstandard1.0` | `netstandard2.0` | `net8.0` | `net10.0` |
| --- | --- | --- | --- | --- |
| `AsSpan()` and the `ReadOnlySpan<char>` overloads of `Check…Subtag`, `IsWellFormedLanguageTagString` and `TryCreate` | No | Yes | Yes | Yes |
| `ISpanFormattable` (`TryFormat`, `IFormattable.ToString`) | No | No | Yes | Yes |
| `IParsable<Bcp47LanguageTag>` and `ISpanParsable<Bcp47LanguageTag>` (`Parse`, `TryParse`) | No | No | Yes | Yes |

On `net10.0`, validation looks subtags up in hash sets; the other targets search
sorted arrays. Neither lookup allocates, and the results are the same.

## See also

- [Use language tags](../how-to/use-language-tags.md)
- [Anatomy of a language tag](../concepts/language-tag-anatomy.md)
- [Bcp47KeyedSubtag](Bcp47KeyedSubtag.md)
- [Bcp47LanguageTagBuilder](Bcp47LanguageTagBuilder.md)
- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
