---
title: LanguageTagFormatException Exception
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# LanguageTagFormatException Exception

## Definition

Namespace: `DataStandardizer.LanguageTag`

The exception that is thrown when a language tag is not correctly formatted.

```csharp
public class LanguageTagFormatException : FormatException
```

## Remarks

Thrown by the [Bcp47LanguageTag](Bcp47LanguageTag.md) `Create` factory methods and
by the builder's `Build()` step when the supplied or composed tag fails what was
checked. The offending tag is available on the `LanguageTag` property.

The message names the class that failed:

| What was checked | Message |
| --- | --- |
| Well-formedness | `The language tag is not well-formed.` |
| Validity against a registry | `The language tag is not valid against the subtag registry.` |
| The ISO code lists | `The language tag is not well-formed, or is not in the ISO code lists.` |

## Constructors

| Constructor | Notes |
| --- | --- |
| `LanguageTagFormatException(string message, string languageTag)` | |
| `LanguageTagFormatException(string message, string languageTag, Exception innerException)` | |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `LanguageTag` | `string LanguageTag { get; }` | The incorrectly formatted language tag. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, and `net10.0`.

## See also

- [Bcp47LanguageTag](Bcp47LanguageTag.md)
- [Bcp47LanguageTagBuilder](Bcp47LanguageTagBuilder.md)
- [LanguageTag API reference](index.md)
