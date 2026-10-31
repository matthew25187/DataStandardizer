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

A registry and its records are immutable once loaded, so one instance may be
shared by every thread. Each record's fields are parsed into its typed
properties when the registry is loaded, and the records of each `Type` are
gathered into the typed lists (`Languages`, `Scripts`, `GrandfatheredTags` and
so on), in registry order.

The first time a tag is validated against the registry, or `IsGrandfathered`
is called, the registry builds an index of its subtags and grandfathered tags,
and keeps it for every later validation against that registry. The first call
to `TryGetSubtag` builds a second index, from each subtag to its record, and
keeps it for every later lookup. Each index is built once per `SubtagRegistry`
instance, by whichever thread uses it first, and is then shared by all threads.
Loading a registry builds neither, so a registry that is never used for
validation or lookup never pays for them. To validate many tags quickly, load
the registry once and reuse it; each newly loaded copy builds its own indexes on
first use. Lookups compare subtags and tags case-insensitively
(RFC 5646 §2.1.1), and allocate nothing.

A record whose `Subtag` is a range, such as `qaa..qtz`, `Qaaa..Qabx`, `QM..QZ`
or `XA..XZ` in the IANA registry, registers every subtag in that range
(RFC 5646 §3.1.1, §3.1.4). The index holds each subtag in the range, so `qaa`,
`en-Qabx` and `en-XA` are valid against the registry, and `TryGetSubtag` finds
the range's record for `qab`. Enumerating the registry still yields the one
record, with the range as its `Subtag`.

```csharp
var registry = SubtagRegistry.CreateFromFile("language-subtag-registry.txt");

if (registry.TryGetSubtag(SubtagRegistryRecordType.Region, "BU", out var region))
{
    Console.WriteLine($"{region.Description[0]}, deprecated {region.Deprecated:yyyy-MM-dd}, use {region.PreferredValue}");
}

var isGrandfathered = registry.IsGrandfathered("i-klingon"); // true
var scriptCount = registry.Scripts.Count;
```

The `CreateFromStream` and `CreateFromStreamAsync` methods read from the
stream's current position and leave the stream open; the caller disposes it.
`CreateFromFileAsync` opens the file for asynchronous, sequential reading.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Count` | `int Count { get; }` | Number of records in the registry. |
| `FileDate` | `DateTime? FileDate { get; }` | Date of the registry's `File-Date` record, or `null` when it has none. |
| `Languages` | `IReadOnlyList<SubtagRegistrySubtagRecord> Languages { get; }` | Records of `Type` `language`. |
| `ExtendedLanguages` | `IReadOnlyList<SubtagRegistrySubtagRecord> ExtendedLanguages { get; }` | Records of `Type` `extlang`. |
| `Scripts` | `IReadOnlyList<SubtagRegistrySubtagRecord> Scripts { get; }` | Records of `Type` `script`. |
| `Regions` | `IReadOnlyList<SubtagRegistrySubtagRecord> Regions { get; }` | Records of `Type` `region`. |
| `Variants` | `IReadOnlyList<SubtagRegistrySubtagRecord> Variants { get; }` | Records of `Type` `variant`. |
| `GrandfatheredTags` | `IReadOnlyList<SubtagRegistryTagRecord> GrandfatheredTags { get; }` | Records of `Type` `grandfathered`. |
| `RedundantTags` | `IReadOnlyList<SubtagRegistryTagRecord> RedundantTags { get; }` | Records of `Type` `redundant`. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CreateFromContent(string subtagRegistryContent)` | `SubtagRegistry` | Static. Loads the registry from an in-memory string. |
| `CreateFromContentAsync(string subtagRegistryContent)` | `Task<SubtagRegistry>` | Static. Asynchronous form. |
| `CreateFromContentAsync(string subtagRegistryContent, CancellationToken cancellationToken)` | `Task<SubtagRegistry>` | Static. Asynchronous form that can be cancelled. Throws `OperationCanceledException` if `cancellationToken` is cancelled. |
| `CreateFromFile(string subtagRegistryFilePath)` | `SubtagRegistry` | Static. Loads the registry from a file path. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CreateFromFileAsync(string subtagRegistryFilePath)` | `Task<SubtagRegistry>` | Static. Asynchronous form. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CreateFromFileAsync(string subtagRegistryFilePath, CancellationToken cancellationToken)` | `Task<SubtagRegistry>` | Static. Asynchronous form that can be cancelled. Throws `OperationCanceledException` if `cancellationToken` is cancelled. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CreateFromStream(Stream stream)` | `SubtagRegistry` | Static. Loads the registry from a stream. |
| `CreateFromStreamAsync(Stream stream)` | `Task<SubtagRegistry>` | Static. Asynchronous form. |
| `CreateFromStreamAsync(Stream stream, CancellationToken cancellationToken)` | `Task<SubtagRegistry>` | Static. Asynchronous form that can be cancelled. Throws `OperationCanceledException` if `cancellationToken` is cancelled. |
| `TryGetSubtag(SubtagRegistryRecordType type, string? subtag, out SubtagRegistrySubtagRecord? record)` | `bool` | Gets the record of a subtag of type `Language`, `ExtendedLanguage`, `Script`, `Region` or `Variant`. Returns `false` for an unregistered subtag, a `null` subtag or any other type. |
| `TryGetSubtag(SubtagRegistryRecordType type, ReadOnlySpan<char> subtag, out SubtagRegistrySubtagRecord? record)` | `bool` | Span form. |
| `IsGrandfathered(string? languageTag)` | `bool` | Whether the registry has a `grandfathered` record for the tag. Returns `false` for `null`. |
| `IsGrandfathered(ReadOnlySpan<char> languageTag)` | `bool` | Span form. |
| `ToString()` | `string` | Override. Renders the registry back to its text representation. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `net8.0`, and `net10.0`. The
`CreateFromFile` and `CreateFromFileAsync` methods, including the
`CancellationToken` overload, exist on `netstandard2.0`, `net8.0` and `net10.0`
only; on `netstandard1.1`, load the registry from a `Stream` or a string.
The public surface is otherwise the same.

## See also

- [Bcp47LanguageTag](Bcp47LanguageTag.md)
- [Bcp47LanguageTagBuilder](Bcp47LanguageTagBuilder.md)
- [ISubtagRegistryRecord](ISubtagRegistryRecord.md)
- [SubtagRegistryTagRecord](SubtagRegistryTagRecord.md)
- [SubtagRegistrySubtagRecord](SubtagRegistrySubtagRecord.md)
- [SubtagRegistryRecordType](SubtagRegistryRecordType.md)
- [LanguageTag API reference](index.md)
