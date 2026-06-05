---
title: Iso639Extensions Class
parent: Language
grand_parent: Packages
nav_exclude: true
---

# Iso639Extensions Class

## Definition

Namespace: `DataStandardizer.Language`

Extension methods that read the metadata attached to the ISO 639 language code
types. Which methods apply to which type depends on the metadata each part of the
standard defines.

```csharp
public static class Iso639Extensions
```

## Remarks

Each accessor reads the [Iso639LanguageCodeAttribute](Iso639LanguageCodeAttribute.md)
applied to the code member and returns the requested metadata, or `null` (or an
empty array, for the `*Names` methods) when it is unavailable.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `GetEnglishName()` | `Iso639Part1Language` | `string?` | English name, or `null` when absent. |
| `GetEnglishName()` | `Iso639Part2BLanguage` | `string?` | English name, or `null` when absent. |
| `GetEnglishName()` | `Iso639Part2TLanguage` | `string?` | English name, or `null` when absent. |
| `GetEnglishName()` | `Iso639Part3Language` | `string?` | English name, or `null` when absent. |
| `GetEnglishName()` | `Iso639Part5LanguageFamily` | `string?` | English name, or `null` when absent. |
| `GetEnglishNames()` | `Iso639Part1Language` | `string[]` | Empty array when absent. |
| `GetEnglishNames()` | `Iso639Part2BLanguage` | `string[]` | Empty array when absent. |
| `GetEnglishNames()` | `Iso639Part2TLanguage` | `string[]` | Empty array when absent. |
| `GetEnglishNames()` | `Iso639Part5LanguageFamily` | `string[]` | Empty array when absent. |
| `GetFrenchName()` | `Iso639Part1Language` | `string?` | French name, or `null` when absent. |
| `GetFrenchName()` | `Iso639Part2BLanguage` | `string?` | French name, or `null` when absent. |
| `GetFrenchName()` | `Iso639Part2TLanguage` | `string?` | French name, or `null` when absent. |
| `GetFrenchName()` | `Iso639Part5LanguageFamily` | `string?` | French name, or `null` when absent. |
| `GetFrenchNames()` | `Iso639Part1Language` | `string[]` | Empty array when absent. |
| `GetFrenchNames()` | `Iso639Part2BLanguage` | `string[]` | Empty array when absent. |
| `GetFrenchNames()` | `Iso639Part2TLanguage` | `string[]` | Empty array when absent. |
| `GetFrenchNames()` | `Iso639Part5LanguageFamily` | `string[]` | Empty array when absent. |
| `GetInvertedName()` | `Iso639Part3Language` | `string?` | Inverted name, or `null` when absent. |
| `GetLanguageType()` | `Iso639Part3Language` | `Iso639LanguageType?` | Language type, or `null` when absent. |
| `GetMacrolanguageCode()` | `Iso639Part3Language` | `string?` | Related macrolanguage code, or `null` when absent. |
| `GetPart1Code()` | `Iso639Part2BLanguage` | `string?` | Related ISO 639 Part 1 code, or `null` when absent. |
| `GetPart1Code()` | `Iso639Part2TLanguage` | `string?` | Related ISO 639 Part 1 code, or `null` when absent. |
| `GetPart1Code()` | `Iso639Part3Language` | `string?` | Related ISO 639 Part 1 code, or `null` when absent. |
| `GetPart2BCode()` | `Iso639Part1Language` | `string?` | Related ISO 639 Part 2B code, or `null` when absent. |
| `GetPart2BCode()` | `Iso639Part3Language` | `string?` | Related ISO 639 Part 2B code, or `null` when absent. |
| `GetPart2TCode()` | `Iso639Part1Language` | `string?` | Related ISO 639 Part 2T code, or `null` when absent. |
| `GetPart2TCode()` | `Iso639Part3Language` | `string?` | Related ISO 639 Part 2T code, or `null` when absent. |
| `GetPrintName()` | `Iso639Part3Language` | `string?` | Print name, or `null` when absent. |
| `GetScope()` | `Iso639Part3Language` | `Iso639LanguageScope?` | Scope, or `null` when absent. |

On the `netstandard1.0` and `netstandard2.0` targets the `string?` return types are
plain `string` annotated with JetBrains `[CanBeNull]`.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso639LanguageCodeAttribute](Iso639LanguageCodeAttribute.md)
- [Iso639LanguageScope](Iso639LanguageScope.md)
- [Iso639LanguageType](Iso639LanguageType.md)
- [Access language metadata](../how-to/access-language-metadata.md)
- [Language API reference](index.md)
