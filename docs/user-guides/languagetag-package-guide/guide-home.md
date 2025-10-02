# DataStandardizer.LanguageTag package

Support for the use of IETF language tags.

## BCP 47

### Features

- BCP 47 compliance

    An implementation of IETF language tags supporting all defined subtags.

- Ease of use

    Create a language tag object from a full language tag string or compose a language tag object from its subtags using a builder.

- Flexible

    Language tag objects can be created with validation based on either current implementations of the supporting standards or a copy of the subtag registry.

- IANA Language Subtag Registry support

    Ability to load a copy of the subtag registry from an external source for use with language tag validation.
    
- Wide runtime support

    Supports .Net Standard 1.x and 2.0 for use in legacy applications, as well as in-support modern .Net runtimes.

### How to...

- [Use language tags](bcp47-standard/use-language-tags.md)
- [Create language tags using the builder](bcp47-standard/create-language-tags-using-builder.md)