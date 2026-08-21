---
title: Use language codes
parent: Language
grand_parent: Packages
nav_order: 1
---

# Use language codes

ISO 639 language codes are available across several types implementing Parts 1,
2, 3 and 5 of the standard.

Because the ISO 639 standard does not define numeric counterparts for its
language codes, these types are not C# enums. Instead each is a
`readonly struct` in which every language code is a static member whose string
value is the code from the standard. See
[Understanding the ISO 639 parts](../concepts/iso639-parts.md) for more on this
design.

To access an ISO 639 Part 1 language code, use the `Iso639Part1Language` type:

```csharp
var englishLanguage = Iso639Part1Language.en;
```

For historical reasons, ISO 639 Part 2 was defined in two forms, providing codes
for both bibliographic and terminological purposes. Both forms are available:

```csharp
var englishLanguage = Iso639Part2BLanguage.eng; // bibliographic code for English
```

or,

```csharp
var englishLanguage = Iso639Part2TLanguage.eng; // terminological code for English
```

Similarly, the language codes defined by ISO 639 Part 3 are available in their
own type:

```csharp
var phuThaiLanguage = Iso639Part3Language.pht;
```

Language families and groups have a type for ISO 639 Part 5:

```csharp
var caucasianLanguages = Iso639Part5LanguageFamily.cau;
```

## Next steps

- [Access language metadata](access-language-metadata.md)
