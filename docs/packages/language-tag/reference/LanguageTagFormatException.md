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
by the builder's `Build()` step when the supplied or composed tag is not a valid
BCP 47 language tag. The offending tag is available on the `LanguageTag` property.

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

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Bcp47LanguageTag](Bcp47LanguageTag.md)
- [Bcp47LanguageTagBuilder](Bcp47LanguageTagBuilder.md)
- [LanguageTag API reference](index.md)
