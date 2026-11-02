---
title: Bcp47LanguageTagBuilder Class
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# Bcp47LanguageTagBuilder Class

## Definition

Namespace: `DataStandardizer.LanguageTag`

A fluent builder that composes a [Bcp47LanguageTag](Bcp47LanguageTag.md) from its
subtags. The call order is enforced by a chain of step interfaces — you start on a
`Bcp47LanguageTagBuilder` instance, chain the `With…` and `Using…` calls, and end
with `Build()`.

```csharp
public class Bcp47LanguageTagBuilder :
    IBcp47LanguageTagBuilderStepWithConformance,
    IBcp47LanguageTagBuilderStepWithConformanceNext,
    IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistryNext,
    IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext,
    IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext,
    IBcp47LanguageTagBuilderStepUsingScriptSubtagNext,
    IBcp47LanguageTagBuilderStepUsingRegionSubtagNext,
    IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext,
    IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext,
    IBcp47LanguageTagBuilderStepUsingPrivateUseSubtagNext
```

## Remarks

The builder's call sequence is, in order: an optional registry step
(`WithLanguageSubtagRegistry`) or conformance step (`WithConformance`), then either a full-tag step (`UsingLanguageTag`, which leads straight to `Build()`) or
the required primary-language step (`UsingPrimaryLanguageSubtag`) followed by the
optional extended-language, script, region, variant, extension, and private-use
steps, ending with `Build()`. See
[The language-tag builder pipeline](../concepts/builder-pipeline.md) for the flow.

The order is enforced by 20 step interfaces in the `DataStandardizer.LanguageTag`
namespace (`IBcp47LanguageTagBuilderStep…` and the companion `…Next` interfaces).
Each `…Next` interface inherits exactly the stages that may follow its stage; you
normally never name these interfaces yourself, but instead let IntelliSense
surface the next valid step on the chain. Because of this design, the
registry, full-tag, and primary-language methods are declared as ordinary
`public` methods (see [Implicit implementation](#implicit-implementation)), while
the remaining subtag setters and `Build()` are **explicit interface
implementations** (see [Explicit implementation](#explicit-implementation)) reached
only through the chain — they cannot be called on a `Bcp47LanguageTagBuilder`-typed
variable directly.

The steps that take a string check only the shape of the subtag. `Build()` then
applies what you chose: validity against the registry from
`WithLanguageSubtagRegistry`, the conformance from `WithConformance`, or otherwise
well-formedness. If you call both on the same builder, the last one applies. The
steps that take a code are ISO by construction.

Undefined enum values, string subtags of the wrong shape and repeated variant
subtags or extension singletons throw `ArgumentException` (or
`ArgumentNullException` for `null`) at the point of the call. A repeated variant or
singleton is well-formed (RFC 5646 §2.2.9), but the builder refuses to compose a
tag that could never be valid; use `Bcp47LanguageTag.Create` to accept one.
`Build()` may throw `LanguageTagFormatException` (the tag doesn't satisfy what was
chosen) or `InvalidOperationException` (for example, more than three extended
language subtags, or an internal state error).

## Methods

### Implicit implementation

These methods are declared as ordinary `public ReturnType Method(…)` members, so
they are callable directly on a `Bcp47LanguageTagBuilder` instance. Each returns a
`…Next` step interface that surfaces the next valid calls in the chain.

| Method | Returns | Notes |
| --- | --- | --- |
| `UsingLanguageTag(string languageTag)` | `IBcp47LanguageTagBuilderStepBuild` | Full-tag step; leads straight to `Build()`. Throws `ArgumentNullException` if `null`. |
| `UsingPrimaryLanguageSubtag(Iso639Part1Language primaryLanguageSubtag)` | `IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext` | Required primary-language step. Throws `ArgumentException` if the code is undefined. |
| `UsingPrimaryLanguageSubtag(Iso639Part2TLanguage primaryLanguageSubtag)` | `IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext` | Throws `ArgumentException` if the code is undefined. |
| `UsingPrimaryLanguageSubtag(Iso639Part3Language primaryLanguageSubtag)` | `IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext` | Throws `ArgumentException` if the code is undefined. |
| `UsingPrimaryLanguageSubtag(Iso639Part5LanguageFamily primaryLanguageSubtag)` | `IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext` | Throws `ArgumentException` if the code is undefined. |
| `UsingPrimaryLanguageSubtag(string primaryLanguageSubtag)` | `IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext` | Checks shape only: two to eight letters. Throws `ArgumentNullException` for `null`, and `ArgumentException` for the wrong shape. |
| `WithConformance(LanguageTagConformance conformance)` | `IBcp47LanguageTagBuilderStepWithConformanceNext` | Optional. `Build()` checks `WellFormed` or `IsoCodeLists`. Throws `ArgumentException` for `Valid`, which needs `WithLanguageSubtagRegistry`, and `ArgumentOutOfRangeException` for an undefined value. |
| `WithLanguageSubtagRegistry(SubtagRegistry subtagRegistry)` | `IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistryNext` | Optional. `Build()` checks validity against the registry. Throws `ArgumentNullException` if `null`. |

### Explicit implementation

These members are declared on `Bcp47LanguageTagBuilder` as
`ReturnType IBcp47…Step.Method(…)`, so they are callable only through the fluent
step chain (the relevant `IBcp47LanguageTagBuilderStep…` interface), not on a
`Bcp47LanguageTagBuilder`-typed variable.

| Method | Returns | Notes |
| --- | --- | --- |
| `Build()` | `Bcp47LanguageTag` | Reached through `IBcp47LanguageTagBuilderStepBuild`. May throw `LanguageTagFormatException` or `InvalidOperationException`. |
| `UsingExtendedLanguageSubtags(string)` | `IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext` | Reached through `IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtags`. One subtag. |
| `UsingExtendedLanguageSubtags(string, string)` | `IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext` | Two subtags. |
| `UsingExtendedLanguageSubtags(string, string, string)` | `IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext` | Three subtags (the maximum). |
| `UsingExtensionSubtags(string, params string[])` | `IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext` | Reached through `IBcp47LanguageTagBuilderStepUsingExtensionSubtags`. Throws `ArgumentException` if two extensions share a singleton (compared case-insensitively). A `null` or invalid subtag in the array throws with `ParamName` `extensionSubtags`. |
| `UsingExtensionSubtags(string, params ReadOnlySpan<string>)` | `IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext` | `net10.0` only. As for the array overload, but the builder reads the subtags from the span without copying them to an array first. The compiler picks this overload for a list of arguments. |
| `UsingPrivateUseSubtag(string)` | `IBcp47LanguageTagBuilderStepUsingPrivateUseSubtagNext` | Reached through `IBcp47LanguageTagBuilderStepUsingPrivateUseSubtag`. |
| `UsingRegionSubtag(Iso3166Part1Alpha2Country)` | `IBcp47LanguageTagBuilderStepUsingRegionSubtagNext` | Reached through `IBcp47LanguageTagBuilderStepUsingRegionSubtag`. |
| `UsingRegionSubtag(UnM49Area)` | `IBcp47LanguageTagBuilderStepUsingRegionSubtagNext` | Accepts a UN M49 area made up of more than one country, such as `UnM49Area._419`, and writes it as a three-digit subtag (`es-419`). Throws `ArgumentException` for a country or area member such as `UnM49Area._840`, which BCP 47 doesn't allow; use the `Iso3166Part1Alpha2Country` overload for a country. |
| `UsingRegionSubtag(string)` | `IBcp47LanguageTagBuilderStepUsingRegionSubtagNext` | Checks shape only: two letters or three digits. |
| `UsingScriptSubtag(Iso15924Script)` | `IBcp47LanguageTagBuilderStepUsingScriptSubtagNext` | Reached through `IBcp47LanguageTagBuilderStepUsingScriptSubtag`. |
| `UsingScriptSubtag(string)` | `IBcp47LanguageTagBuilderStepUsingScriptSubtagNext` | Checks shape only: four letters. |
| `UsingVariantSubtags(string, params string[])` | `IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext` | Reached through `IBcp47LanguageTagBuilderStepUsingVariantSubtags`. Throws `ArgumentException` if a subtag is repeated (compared case-insensitively). A `null` or invalid subtag in the array throws with `ParamName` `variantSubtags`. |
| `UsingVariantSubtags(string, params ReadOnlySpan<string>)` | `IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext` | `net10.0` only. As for the array overload, but the builder reads the subtags from the span without copying them to an array first. The compiler picks this overload for a list of arguments. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `net8.0`, and `net10.0`. On the
.NET Standard targets nullability is expressed through JetBrains
`[NotNull]` / `[CanBeNull]` attributes. The `params ReadOnlySpan<string>`
overloads of `UsingVariantSubtags` and `UsingExtensionSubtags` exist only on
`net10.0`; their interfaces give them a default implementation that copies the
span to an array and calls the array overload. The public surface is otherwise
the same.

## See also

- [Create language tags using the builder](../how-to/create-language-tags-using-builder.md)
- [The language-tag builder pipeline](../concepts/builder-pipeline.md)
- [Bcp47LanguageTag](Bcp47LanguageTag.md)
- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
