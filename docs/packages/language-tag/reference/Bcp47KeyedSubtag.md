---
title: Bcp47KeyedSubtag Struct
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# Bcp47KeyedSubtag Struct

## Definition

Namespace: `DataStandardizer.LanguageTag`

A subtag introduced by a single-character key — used for extension subtags
(singleton `u`, `t`, …) and the private-use subtag (singleton `x`). The
constructor is internal; you obtain instances from a
[Bcp47LanguageTag](Bcp47LanguageTag.md)'s `ExtensionSubtags` and
`PrivateUseSubtag` members.

```csharp
public readonly struct Bcp47KeyedSubtag : IEquatable<Bcp47KeyedSubtag>
```

## Remarks

The `Singleton` and `Subtags` getters throw `InvalidOperationException` when the
instance is uninitialised (default-constructed). On the `netstandard1.0` and
`netstandard2.0` targets nullability is expressed through JetBrains
`[NotNull]` / `[CanBeNull]` attributes rather than C# nullable reference types.

## Properties

Each getter throws `InvalidOperationException` if the instance is uninitialised.

| Property | Signature | Notes |
| --- | --- | --- |
| `Singleton` | `char? Singleton { get; }` | The leading single character, or `null` if empty. |
| `Subtags` | `string[] Subtags { get; }` | The remaining hyphen-separated parts. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(Bcp47KeyedSubtag other)` | `bool` | Value equality on the subtag. |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |
| `ToString()` | `string` | Override. Returns the raw subtag string. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Explicit | `explicit operator Bcp47KeyedSubtag(string)` | |
| Implicit | `implicit operator string?(Bcp47KeyedSubtag)` | Unwraps to the raw subtag string. |
| Equality | `operator ==`, `!=` `(Bcp47KeyedSubtag, Bcp47KeyedSubtag)` | Value equality on the subtag. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. On the
.NET Standard targets nullability is expressed through JetBrains
`[NotNull]` / `[CanBeNull]` attributes; the public surface is otherwise the same.

## See also

- [Bcp47LanguageTag](Bcp47LanguageTag.md)
- [Anatomy of a language tag](../concepts/language-tag-anatomy.md)
- [LanguageTag API reference](index.md)
