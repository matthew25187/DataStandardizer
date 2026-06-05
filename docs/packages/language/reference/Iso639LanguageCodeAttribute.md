---
title: Iso639LanguageCodeAttribute Class
parent: Language
grand_parent: Packages
nav_exclude: true
---

# Iso639LanguageCodeAttribute Class

## Definition

Namespace: `DataStandardizer.Language`

Describes an ISO 639 language code member with its metadata: related codes across
the ISO 639 parts, print and inverted names, macrolanguage code, scope, and
language type. English and French names are inherited from `CodeAttributeBase`.

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class Iso639LanguageCodeAttribute : CodeAttributeBase
```

## Remarks

This attribute is applied to each language code member of the ISO 639 code
structs ([Iso639Part1Language](Iso639Part1Language.md),
[Iso639Part2BLanguage](Iso639Part2BLanguage.md),
[Iso639Part2TLanguage](Iso639Part2TLanguage.md),
[Iso639Part3Language](Iso639Part3Language.md) and
[Iso639Part5LanguageFamily](Iso639Part5LanguageFamily.md)). You normally read the
metadata through the [Iso639Extensions](Iso639Extensions.md) accessors rather than
reading the attribute directly.

## Constructors

| Constructor | Notes |
| --- | --- |
| `Iso639LanguageCodeAttribute(string englishName)` | |
| `Iso639LanguageCodeAttribute(string englishName, string frenchName)` | |
| `Iso639LanguageCodeAttribute(string englishName, Iso639LanguageScope scope, Iso639LanguageType languageType)` | |
| `Iso639LanguageCodeAttribute(string[] englishNames)` | |
| `Iso639LanguageCodeAttribute(string[] englishNames, string[] frenchNames)` | |
| `Iso639LanguageCodeAttribute(string[] englishNames, Iso639LanguageScope scope, Iso639LanguageType languageType)` | |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `InvertedName` | `string? InvertedName { get; set; }` | The inverted name for the language code. |
| `LanguageType` | `Iso639LanguageType? LanguageType { get; }` | The language type. |
| `MacrolanguageCode` | `string? MacrolanguageCode { get; set; }` | The related macrolanguage code. |
| `Part1Code` | `string? Part1Code { get; set; }` | The related ISO 639 Part 1 code. |
| `Part2BCode` | `string? Part2BCode { get; set; }` | The related ISO 639 Part 2B code. |
| `Part2TCode` | `string? Part2TCode { get; set; }` | The related ISO 639 Part 2T code. |
| `PrintName` | `string? PrintName { get; set; }` | The print name for the language code. |
| `Scope` | `Iso639LanguageScope? Scope { get; }` | The scope of the language code. |

Inherited from `CodeAttributeBase`: `EnglishName`, `EnglishNames`, `FrenchName`,
`FrenchNames`.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. On the
`netstandard1.0` and `netstandard2.0` targets the reference-type properties are
annotated with JetBrains `[CanBeNull]` rather than C# nullable reference types.

## See also

- [Iso639Extensions](Iso639Extensions.md)
- [Iso639LanguageScope](Iso639LanguageScope.md)
- [Iso639LanguageType](Iso639LanguageType.md)
- [Access language metadata](../how-to/access-language-metadata.md)
- [Language API reference](index.md)
