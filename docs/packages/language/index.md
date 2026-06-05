---
title: Language
parent: Packages
nav_order: 5
has_children: true
---

# DataStandardizer.Language

Strongly-typed support for language-related data standards: ISO 639 language
codes (Parts 1, 2, 3 and 5) and ISO 15924 script codes.

```shell
dotnet add package DataStandardizer.Language
```

## Standards

| Standard | What it provides |
| --- | --- |
| **ISO 639** | Codes for the representation of names of languages, across four parts: |
| &nbsp;&nbsp;*Part 1* | Alpha-2 language codes (e.g. `en`). |
| &nbsp;&nbsp;*Part 2* | Alpha-3 language codes, in both terminological (T) and bibliographic (B) forms (e.g. `eng`). |
| &nbsp;&nbsp;*Part 3* | Alpha-3 codes for comprehensive coverage of languages (e.g. `pht`). |
| &nbsp;&nbsp;*Part 5* | Alpha-3 codes for language families and groups (e.g. `cau`). |
| **ISO 15924** | Codes for the representation of names of scripts (e.g. `Cyrl`). |

## Platform support

Targets .NET Standard 1.0 and 2.0 for use in legacy applications, as well as
in-support modern .NET runtimes (.NET 8 and .NET 10).

## How-to guides

- [Use language codes](how-to/use-language-codes.md)
- [Access language metadata](how-to/access-language-metadata.md)
- [Use script codes](how-to/use-script-codes.md)
- [Access script metadata](how-to/access-script-metadata.md)

## Concepts

- [Understanding the ISO 639 parts](concepts/iso639-parts.md)

## Reference

- [API reference](reference/index.md)
