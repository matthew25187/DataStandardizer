---
title: SubtagRegistry Class
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistry Class

## Definition

Namespace: `DataStandardizer.LanguageTag.SubtagRegistry`

A read-only collection of the records that comprise a loaded copy of the *IANA
Language Subtag Registry*, used to constrain language-tag validation. The
constructor is private; load a registry with one of the static `CreateFrom…`
factory methods.

```csharp
public class SubtagRegistry : IReadOnlyCollection<ISubtagRegistryRecord>
```

## Remarks

Enumerating the registry yields [ISubtagRegistryRecord](ISubtagRegistryRecord.md)
instances ([SubtagRegistryTagRecord](SubtagRegistryTagRecord.md),
[SubtagRegistrySubtagRecord](SubtagRegistrySubtagRecord.md), and
[SubtagRegistryFileDateRecord](SubtagRegistryFileDateRecord.md)). The factory
methods throw `ArgumentNullException` for a `null` argument. Pass a loaded registry
to the [Bcp47LanguageTag](Bcp47LanguageTag.md) `Create` / `TryCreate` overloads, or
to the builder's `WithLanguageSubtagRegistry` step, to validate against it.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Count` | `int Count { get; }` | Number of records in the registry. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CreateFromContent(string subtagRegistryContent)` | `SubtagRegistry` | Static. Loads the registry from an in-memory string. |
| `CreateFromContentAsync(string subtagRegistryContent)` | `Task<SubtagRegistry>` | Static. Asynchronous form. |
| `CreateFromFile(string subtagRegistryFilePath)` | `SubtagRegistry` | *netstandard1.3+/.NET.* Static. Loads the registry from a file path. |
| `CreateFromFileAsync(string subtagRegistryFilePath)` | `Task<SubtagRegistry>` | *netstandard1.3+/.NET.* Static. Asynchronous form. |
| `CreateFromStream(Stream stream)` | `SubtagRegistry` | Static. Loads the registry from a stream. |
| `CreateFromStreamAsync(Stream stream)` | `Task<SubtagRegistry>` | Static. Asynchronous form. |
| `ToString()` | `string` | Override. Renders the registry back to its text representation. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. The
`CreateFromFile` / `CreateFromFileAsync` methods are available on the
`netstandard1.3`+ and .NET builds only — they are not present on `netstandard1.0`.

## See also

- [Bcp47LanguageTag](Bcp47LanguageTag.md)
- [Bcp47LanguageTagBuilder](Bcp47LanguageTagBuilder.md)
- [ISubtagRegistryRecord](ISubtagRegistryRecord.md)
- [SubtagRegistryTagRecord](SubtagRegistryTagRecord.md)
- [SubtagRegistrySubtagRecord](SubtagRegistrySubtagRecord.md)
- [LanguageTag API reference](index.md)
