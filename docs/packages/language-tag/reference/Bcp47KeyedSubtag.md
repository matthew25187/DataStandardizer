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
`PrivateUseSubtag` members, or by casting a string.

Every instance holds either one extension (RFC 5646 section 2.2.6): a singleton
other than `x` followed by one or more subtags of two to eight letters or digits,
such as `u-ca-gregory`; or one private use sequence (section 2.2.7): `x` followed
by one or more subtags of one to eight letters or digits, such as `x-abc`.

```csharp
public readonly struct Bcp47KeyedSubtag : IEquatable<Bcp47KeyedSubtag>
```

## Remarks

The `Singleton` and `Subtags` getters throw `InvalidOperationException` when the
instance is uninitialised (default-constructed). On the `netstandard1.1` and
`netstandard2.0` targets nullability is expressed through
`System.Diagnostics.CodeAnalysis` attributes such as `[MaybeNull]` and `[DisallowNull]` rather than C# nullable reference types.
A project gets null warnings from them only on C# 8 or later with nullable enabled.

Equality and hashing ignore case, because RFC 5646 section 2.1.1 makes subtags
case-insensitive and [Bcp47LanguageTag](Bcp47LanguageTag.md) equality does the
same, so `u-co-phonebk` equals `U-CO-PHONEBK`. `ToString`, `Singleton` and
`Subtags` return the subtag as written.

## Properties

Each getter throws `InvalidOperationException` if the instance is uninitialised.

| Property | Signature | Notes |
| --- | --- | --- |
| `Singleton` | `char Singleton { get; }` | The singleton, in the case it was written in: the extension's leading character, or `x` or `X` for a private use sequence. |
| `Subtags` | `IReadOnlyList<string> Subtags { get; }` | The remaining hyphen-separated parts. Each read splits the subtag again and returns a new list. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(Bcp47KeyedSubtag other)` | `bool` | Case-insensitive comparison of the subtag string. |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. Case-insensitive, consistent with `Equals`. |
| `ToString()` | `string` | Override. Returns the raw subtag string. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Explicit | `explicit operator Bcp47KeyedSubtag(string)` | Accepts one extension or one private use sequence, in either case, and keeps the case as written. Throws `InvalidCastException` if `null` or anything else, such as `" u-ca"`, `"ab-cd"`, `"u"` or `"x"`. Checks the shape only: the singleton need not be one that has been assigned. |
| Implicit | `implicit operator string?(Bcp47KeyedSubtag)` | Unwraps to the raw subtag string. |
| Equality | `operator ==`, `!=` `(Bcp47KeyedSubtag, Bcp47KeyedSubtag)` | Case-insensitive comparison of the subtag string. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, and `net10.0`. On the
.NET Standard targets nullability is expressed through `System.Diagnostics.CodeAnalysis` attributes such as `[MaybeNull]` and `[DisallowNull]`. A project gets null warnings from them only on C# 8 or later with nullable enabled. The public surface is otherwise the same.

## See also

- [Bcp47LanguageTag](Bcp47LanguageTag.md)
- [Anatomy of a language tag](../concepts/language-tag-anatomy.md)
- [LanguageTag API reference](index.md)
