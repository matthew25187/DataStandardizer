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
    IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistryNext,
    IBcp47LanguageTagBuilderStepWithTimeoutNext,
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
(`WithLanguageSubtagRegistry`), an optional timeout step (`WithTimeout`), then
either a full-tag step (`UsingLanguageTag`, which leads straight to `Build()`) or
the required primary-language step (`UsingPrimaryLanguageSubtag`) followed by the
optional extended-language, script, region, variant, extension, and private-use
steps, ending with `Build()`. See
[The language-tag builder pipeline](../concepts/builder-pipeline.md) for the flow.

The order is enforced by ~15 step interfaces in the `DataStandardizer.LanguageTag`
namespace (`IBcp47LanguageTagBuilderStep…` and the companion `…Next` interfaces).
Each `…Next` interface inherits exactly the stages that may follow its stage; you
normally never name these interfaces yourself, but instead let IntelliSense
surface the next valid step on the chain. Because of this design, the
registry, timeout, full-tag, and primary-language methods are declared as ordinary
`public` methods (see [Implicit implementation](#implicit-implementation)), while
the remaining subtag setters and `Build()` are **explicit interface
implementations** (see [Explicit implementation](#explicit-implementation)) reached
only through the chain — they cannot be called on a `Bcp47LanguageTagBuilder`-typed
variable directly.

Undefined enum values and malformed string subtags throw `ArgumentException` (or
`ArgumentNullException` for `null`) at the point of the call; `Build()` may throw
`LanguageTagFormatException` (the resulting tag is invalid) or
`InvalidOperationException` (e.g. more than three extended language subtags, or an
internal state error).

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
| `UsingPrimaryLanguageSubtag(string primaryLanguageSubtag)` | `IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext` | Registry/string form. Throws `ArgumentNullException` / `ArgumentException` for an invalid subtag. |
| `WithLanguageSubtagRegistry(SubtagRegistry subtagRegistry)` | `IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistryNext` | Optional registry step. Throws `ArgumentNullException` if `null`. |
| `WithTimeout(TimeSpan matchTimeout)` | `IBcp47LanguageTagBuilderStepWithTimeoutNext` | Optional timeout step; bounds the validation regex match time. |

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
| `UsingExtensionSubtags(string, params string[])` | `IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext` | Reached through `IBcp47LanguageTagBuilderStepUsingExtensionSubtags`. |
| `UsingPrivateUseSubtag(string)` | `IBcp47LanguageTagBuilderStepUsingPrivateUseSubtagNext` | Reached through `IBcp47LanguageTagBuilderStepUsingPrivateUseSubtag`. |
| `UsingRegionSubtag(Iso3166Part1Alpha2Country)` | `IBcp47LanguageTagBuilderStepUsingRegionSubtagNext` | Reached through `IBcp47LanguageTagBuilderStepUsingRegionSubtag`. |
| `UsingRegionSubtag(UnM49AreaByAlpha2CountryCode)` | `IBcp47LanguageTagBuilderStepUsingRegionSubtagNext` | |
| `UsingRegionSubtag(UnM49AreaByAlpha3CountryCode)` | `IBcp47LanguageTagBuilderStepUsingRegionSubtagNext` | |
| `UsingRegionSubtag(string)` | `IBcp47LanguageTagBuilderStepUsingRegionSubtagNext` | |
| `UsingScriptSubtag(Iso15924Script)` | `IBcp47LanguageTagBuilderStepUsingScriptSubtagNext` | Reached through `IBcp47LanguageTagBuilderStepUsingScriptSubtag`. |
| `UsingScriptSubtag(string)` | `IBcp47LanguageTagBuilderStepUsingScriptSubtagNext` | |
| `UsingVariantSubtags(string, params string[])` | `IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext` | Reached through `IBcp47LanguageTagBuilderStepUsingVariantSubtags`. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. On the
.NET Standard targets nullability is expressed through JetBrains
`[NotNull]` / `[CanBeNull]` attributes; the public surface is otherwise the same.

## See also

- [Create language tags using the builder](../how-to/create-language-tags-using-builder.md)
- [The language-tag builder pipeline](../concepts/builder-pipeline.md)
- [Bcp47LanguageTag](Bcp47LanguageTag.md)
- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
