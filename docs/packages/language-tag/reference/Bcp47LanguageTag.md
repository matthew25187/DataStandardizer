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
public readonly struct Bcp47LanguageTag : IEquatable<Bcp47LanguageTag>
```

## Remarks

Each subtag property getter throws `InvalidOperationException` when the instance
is uninitialised (default-constructed). On the `netstandard1.0` and
`netstandard2.0` targets the nullability of the operators, parameters, and return
values is expressed through JetBrains `[NotNull]` / `[CanBeNull]` attributes
rather than C# nullable reference types.

The `Create` and `TryCreate` overloads that take a `SubtagRegistry` constrain
validation to a loaded copy of the *IANA Language Subtag Registry*; the overloads
that take a `TimeSpan` bound the time spent matching the validation regular
expression.

## Properties

Each getter throws `InvalidOperationException` if the instance is uninitialised.

| Property | Signature | Notes |
| --- | --- | --- |
| `ExtendedLanguageSubtags` | `string[] ExtendedLanguageSubtags { get; }` | Empty if absent. |
| `ExtensionSubtags` | `Bcp47KeyedSubtag[] ExtensionSubtags { get; }` | Empty if absent. |
| `PrimaryLanguageSubtag` | `string PrimaryLanguageSubtag { get; }` | Always present on a valid tag. |
| `PrivateUseSubtag` | `Bcp47KeyedSubtag? PrivateUseSubtag { get; }` | `null` if absent. |
| `RegionSubtag` | `string? RegionSubtag { get; }` | `null` if absent. |
| `ScriptSubtag` | `string? ScriptSubtag { get; }` | `null` if absent. |
| `VariantSubtags` | `string[] VariantSubtags { get; }` | Empty if absent. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CheckExtendedLanguageSubtag(string extendedLanguageSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. |
| `CheckExtensionSubtag(string extensionSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. |
| `CheckPrimaryLanguageSubtag(string primaryLanguageSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. |
| `CheckPrivateUseSubtag(string privateUseSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. |
| `CheckRegionSubtag(string regionSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. |
| `CheckScriptSubtag(string scriptSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. |
| `CheckVariantSubtag(string variantSubtag)` | `bool` | Static. Validates using the default BCP 47 rules. |
| `Create(string languageTag)` | `Bcp47LanguageTag` | Static. Throws `LanguageTagFormatException` if the tag is invalid; `ArgumentNullException` if `null`. |
| `Create(string languageTag, TimeSpan matchTimeout)` | `Bcp47LanguageTag` | Static. As above, with a regex match timeout. |
| `Create(string languageTag, SubtagRegistry subtagRegistry)` | `Bcp47LanguageTag` | Static. Validates against a loaded subtag registry. |
| `Create(string languageTag, SubtagRegistry subtagRegistry, TimeSpan matchTimeout)` | `Bcp47LanguageTag` | Static. Registry validation with a match timeout. |
| `Equals(Bcp47LanguageTag other)` | `bool` | Case-insensitive comparison of the tag string. |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |
| `IsWellFormedLanguageTagString(string languageTagString)` | `bool` | Static. Validates using the default BCP 47 rules. |
| `ToIso15924()` | `Iso15924Script?` | From the script subtag; `null` if absent or unrecognised. |
| `ToIso3166Part1Alpha2()` | `Iso3166Part1Alpha2Country?` | From the region subtag; `null` if absent or unrecognised. |
| `ToIso639Part1()` | `Iso639Part1Language?` | From the primary language subtag; `null` if unrecognised. |
| `ToIso639Part2T()` | `Iso639Part2TLanguage?` | From the primary language subtag; `null` if unrecognised. |
| `ToIso639Part3()` | `Iso639Part3Language?` | From the primary language subtag; `null` if unrecognised. |
| `ToIso639Part5()` | `Iso639Part5LanguageFamily?` | From the primary language subtag; `null` if unrecognised. |
| `ToString()` | `string` | Override. Returns the underlying tag string. |
| `ToUnM49()` | `ushort?` | From the region subtag; `null` if absent or not a UN M49 code. |
| `TryCreate(string? languageTag, out Bcp47LanguageTag result)` | `bool` | Static. Returns `false` instead of throwing. |
| `TryCreate(string? languageTag, TimeSpan matchTimeout, out Bcp47LanguageTag result)` | `bool` | Static. With a regex match timeout. |
| `TryCreate(string? languageTag, SubtagRegistry subtagRegistry, out Bcp47LanguageTag result)` | `bool` | Static. Validates against a loaded subtag registry. |
| `TryCreate(string? languageTag, SubtagRegistry subtagRegistry, TimeSpan matchTimeout, out Bcp47LanguageTag result)` | `bool` | Static. Registry validation with a match timeout. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Explicit | `explicit operator Bcp47LanguageTag(string)` | Wraps a string *without* validation. |
| Implicit | `implicit operator string?(Bcp47LanguageTag)` | Unwraps to the underlying tag string. |
| Equality | `operator ==`, `!=` `(Bcp47LanguageTag, Bcp47LanguageTag)` | Case-insensitive comparison of the tag string. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. On the
.NET Standard targets nullability is expressed through JetBrains
`[NotNull]` / `[CanBeNull]` attributes; the public surface is otherwise the same.

## See also

- [Use language tags](../how-to/use-language-tags.md)
- [Anatomy of a language tag](../concepts/language-tag-anatomy.md)
- [Bcp47KeyedSubtag](Bcp47KeyedSubtag.md)
- [Bcp47LanguageTagBuilder](Bcp47LanguageTagBuilder.md)
- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
