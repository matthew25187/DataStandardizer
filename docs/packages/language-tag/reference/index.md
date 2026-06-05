---
title: API reference
parent: LanguageTag
grand_parent: Packages
nav_order: 20
---

# DataStandardizer.LanguageTag API reference

The public types of **DataStandardizer.LanguageTag**. The core types are in the
`DataStandardizer.LanguageTag` namespace; the registry types are in the nested
`DataStandardizer.LanguageTag.SubtagRegistry` namespace.

## Structures

| Type | Description |
| --- | --- |
| [Bcp47LanguageTag](Bcp47LanguageTag.md) | An IETF BCP 47 language tag. |
| [Bcp47KeyedSubtag](Bcp47KeyedSubtag.md) | A subtag introduced by a single-character key (extension and private-use subtags). |

## Classes

| Type | Description |
| --- | --- |
| [Bcp47LanguageTagBuilder](Bcp47LanguageTagBuilder.md) | A fluent builder that composes a `Bcp47LanguageTag` from its subtags. |
| [SubtagRegistry](SubtagRegistry.md) | A loaded copy of the *IANA Language Subtag Registry*. |
| [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md) | The shared base class for every registry record. |
| [SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md) | The base class for the tag and subtag records. |
| [SubtagRegistryTagRecord](SubtagRegistryTagRecord.md) | A registry *tag* entry. |
| [SubtagRegistrySubtagRecord](SubtagRegistrySubtagRecord.md) | A registry *subtag* entry. |
| [SubtagRegistryFileDateRecord](SubtagRegistryFileDateRecord.md) | The registry's `File-Date` header record. |
| [SubtagRegistryFieldAttribute](SubtagRegistryFieldAttribute.md) | Maps a record property to its underlying registry field name. |

## Interfaces

| Type | Description |
| --- | --- |
| [ISubtagRegistryRecord](ISubtagRegistryRecord.md) | The base contract for all registry records. |

## Exceptions

| Type | Description |
| --- | --- |
| [LanguageTagFormatException](LanguageTagFormatException.md) | Thrown when a language tag is not correctly formatted. |

## Related

- [Anatomy of a language tag](../concepts/language-tag-anatomy.md)
- [The language-tag builder pipeline](../concepts/builder-pipeline.md)
- [Use language tags](../how-to/use-language-tags.md)
</content>
</invoke>
