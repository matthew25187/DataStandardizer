---
title: Access language metadata
parent: Language
grand_parent: Packages
nav_order: 2
---

# Access language metadata

Each ISO 639 language code carries associated metadata that you retrieve through
extension methods on the language code types.

## English names

A language code may have one or more English names. To get a single English
name:

```csharp
var englishName = Iso639Part1Language.tl.GetEnglishName();
```

Some language codes have multiple English names:

```csharp
var englishNames = Iso639Part1Language.nl.GetEnglishNames();
```

These methods are available on the `Iso639Part1Language`, `Iso639Part2BLanguage`,
`Iso639Part2TLanguage`, `Iso639Part3Language` and `Iso639Part5LanguageFamily`
types.

## French names

A language code may have one or more French names. To get a single French name:

```csharp
var frenchName = Iso639Part1Language.de.GetFrenchName();
```

Some language codes have multiple French names:

```csharp
var frenchNames = Iso639Part1Language.nn.GetFrenchNames();
```

These methods are available on the `Iso639Part1Language`, `Iso639Part2BLanguage`,
`Iso639Part2TLanguage` and `Iso639Part5LanguageFamily` types.

## Inverted name

ISO 639 Part 3 defines inverted names for its language codes. To get an inverted
name:

```csharp
var invertedName = Iso639Part3Language.est.GetInvertedName();
```

## Language type

Language types are included in the ISO 639 Part 3 standard. To get a language
type from a language code:

```csharp
var languageType = Iso639Part3Language.slk.GetLanguageType();
```

Language types indicate whether the language is living, ancient, historical,
extinct, constructed or special.

## Macrolanguage

Macrolanguages are defined as part of the ISO 639 Part 3 standard. To get a
macrolanguage code from a language code:

```csharp
var macrolanguage = Iso639Part3Language.pst.GetMacrolanguageCode();
```

## Part 1 code

The ISO 639 standard associates Part 1 codes with other language codes. To get
the Part 1 code for a language code:

```csharp
var part1Code = Iso639Part3Language.ita.GetPart1Code();
```

Part 1 codes are associated, where available, with language codes from the
`Iso639Part2BLanguage`, `Iso639Part2TLanguage` and `Iso639Part3Language` types.

## Part 2 code

The ISO 639 standard associates Part 2 codes with other language codes.
Associated Part 2 codes are available in both bibliographic and terminological
forms.

To get the bibliographic Part 2 code associated with a language code:

```csharp
var part2Code = Iso639Part1Language.fj.GetPart2BCode();
```

Similarly, the terminological Part 2 code associated with a language code can be
retrieved like so:

```csharp
var part2Code = Iso639Part1Language.fj.GetPart2TCode();
```

Part 2 codes are associated, where available, with language codes from the
`Iso639Part1Language` and `Iso639Part3Language` types.

## Print name

Print names are defined by the ISO 639 Part 3 standard and can be retrieved like
so:

```csharp
var printName = Iso639Part3Language.tat.GetPrintName();
```

## Scope

ISO 639 Part 3 defines a scope for its language codes. You can retrieve the
scope of a language from an `Iso639Part3Language` language code:

```csharp
var scope = Iso639Part3Language.heb.GetScope();
```
