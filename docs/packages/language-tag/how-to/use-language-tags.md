---
title: Use language tags
parent: LanguageTag
grand_parent: Packages
nav_order: 1
---

# Use language tags

The `Bcp47LanguageTag` type is an implementation of IETF language tags as defined
by BCP 47; it supports the composition and deconstruction of language tags from
strings or using a fluent-style builder.

## Create a language tag

You can create language tags using factory methods on the `Bcp47LanguageTag`
type. For example,

```csharp
Bcp47LanguageTag languageTag;
languageTag = Bcp47LanguageTag.Create("en");        // English
languageTag = Bcp47LanguageTag.Create("mas");       // Maasai
languageTag = Bcp47LanguageTag.Create("fr-CA");     // French as used in Canada
languageTag = Bcp47LanguageTag.Create("es-419");    // Spanish as used in Latin America
languageTag = Bcp47LanguageTag.Create("zh-Hans");   // Chinese written with Simplified script
```

Language tags created in this way have validation applied to the source tag using
rules based on codes implemented by other *Data Standardizer* packages. If an
attempt is made to create a language tag object with an invalid source tag then an
exception (`LanguageTagFormatException`) will be thrown.

The standard for language tags also specifies that the range of valid tags can be
defined or constrained by the *IANA Language Subtag Registry*. You can load an
instance of the registry from an external source and then use this to validate
what constitutes a valid language tag. For example,

```csharp
var subtagRegistry = SubtagRegistry.CreateFromFile("subtag_registry.txt");
var languageTag = Bcp47LanguageTag.Create("en", subtagRegistry);
```

If you would rather avoid exceptions for invalid input, the type also provides
`TryCreate` overloads that return `false` instead of throwing.

## Validate tags

You can validate a tag or subtags without having to try to create the tag. If you
want to check whether a string contains a valid, full language tag, you can call
the `IsWellFormedLanguageTagString()` method:

```csharp
var isValidTag = Bcp47LanguageTag.IsWellFormedLanguageTagString("Not_A_Valid_Tag"); // returns false
```

Subtags can also be checked. For example,

```csharp
bool isValid;
isValid = Bcp47LanguageTag.CheckPrimaryLanguageSubtag("en");    // English
isValid = Bcp47LanguageTag.CheckExtendedLanguageSubtag("xxx");
isValid = Bcp47LanguageTag.CheckScriptSubtag("Cyrl");           // Cyrillic
isValid = Bcp47LanguageTag.CheckRegionSubtag("419");            // Latin America and the Caribbean
isValid = Bcp47LanguageTag.CheckVariantSubtag("1606nict");      // Late Middle French
isValid = Bcp47LanguageTag.CheckExtensionSubtag("u-Latn");
isValid = Bcp47LanguageTag.CheckPrivateUseSubtag("x-private");
```

## Extract subtags

The subtags that comprise a language tag object can be extracted from the language
tag using properties for access to the raw subtag values, or methods for
conversion of the subtags to members of the relevant enums.

```csharp
var languageTag = Bcp47LanguageTag.Create("gsw-u-sd-chzh");
var primaryLanguageSubtag = languageTag.ToIso639Part3();
var extensionSubtags = languageTag.ExtensionSubtags;
```

For more on the parts that make up a tag, see
[Anatomy of a language tag](../concepts/language-tag-anatomy.md).

## Related

- [Create language tags using the builder](create-language-tags-using-builder.md)
- [API reference](../reference/index.md)
