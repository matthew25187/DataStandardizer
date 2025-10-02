# Use language tag builder

Instead of creating language tag objects using the provided factory methods, you can instead use the language tag builder.  The builder provides a fluent-style interface for composing a language tag from its constituent subtags.

## Create language tags from raw subtags

Language tags can be composed using the subtags as raw string values.
```
// Cantonese using traditional Han characters
var languageTag = new Bcp47LanguageTagBuilder()
    .UsingPrimaryLanguageSubtag("yue")
    .UsingScriptSubtag("Hant")
    .UsingRegionSubtag("HK")
    .Build();
```

## Create language tags using enums

You can also compose a language tag using the strongly-typed enums representing the collection of values on which the subtag is based.  For example, to write the preceding example using the relevant enums, it would look like this:
```
// Cantonese using traditional Han characters
var languageTag = new Bcp47LanguageTagBuilder()
    .UsingPrimaryLanguageSubtag(Iso639Part3Language.yue)
    .UsingScriptSubtag(Iso15924Script.Hant)
    .UsingRegionSubtag(Iso3166Part1Alpha2Country.HK)
    .Build();
```

## Create language tags with subtag registry

A copy of the *IANA Language Subtag Registry* can be used to validate the subtags for a language tag.  For example,
```
var subtagRegistry = SubtagRegistry.CreateFromFile("registry.txt");
var languageTag = new Bcp47LanguageTagBuilder()
    .WithLanguageSubtagRegistry(subtagRegistry)
    .UsingPrimaryLanguageSubtag("gsw")
    .UsingExtensionSubtags("u-sd-chzh")
    .Build();
```