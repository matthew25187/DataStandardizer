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

A registry is read-only once it is loaded. The first time a tag is validated
against it, the registry builds an index of its subtags and grandfathered tags,
and keeps it for every later validation against that registry. The index is
built once per `SubtagRegistry` instance, by whichever thread uses it first, and
is then shared by all threads. Loading a registry doesn't build the index, so a
registry that is never used for validation never pays for it. To validate many
tags quickly, load the registry once and reuse it; each newly loaded copy
builds its own index on first use.

A record whose `Subtag` is a range, such as `qaa..qtz`, `Qaaa..Qabx`, `QM..QZ`
or `XA..XZ` in the IANA registry, registers every subtag in that range
(RFC 5646 §3.1.1, §3.1.4). The index holds each subtag in the range, so `qaa`,
`en-Qabx` and `en-XA` are valid against the registry. Enumerating the registry
still yields the one record, with the range as its `Subtag`.

The `CreateFromStream` and `CreateFromStreamAsync` methods read from the
stream's current position and leave the stream open; the caller disposes it.
`CreateFromFileAsync` opens the file for asynchronous, sequential reading.

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
| `CreateFromContentAsync(string subtagRegistryContent, CancellationToken cancellationToken)` | `Task<SubtagRegistry>` | Static. Asynchronous form that can be cancelled. Throws `OperationCanceledException` if `cancellationToken` is cancelled. |
| `CreateFromFile(string subtagRegistryFilePath)` | `SubtagRegistry` | Static. Loads the registry from a file path. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CreateFromFileAsync(string subtagRegistryFilePath)` | `Task<SubtagRegistry>` | Static. Asynchronous form. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CreateFromFileAsync(string subtagRegistryFilePath, CancellationToken cancellationToken)` | `Task<SubtagRegistry>` | Static. Asynchronous form that can be cancelled. Throws `OperationCanceledException` if `cancellationToken` is cancelled. `netstandard2.0`, `net8.0` and `net10.0` only. |
| `CreateFromStream(Stream stream)` | `SubtagRegistry` | Static. Loads the registry from a stream. |
| `CreateFromStreamAsync(Stream stream)` | `Task<SubtagRegistry>` | Static. Asynchronous form. |
| `CreateFromStreamAsync(Stream stream, CancellationToken cancellationToken)` | `Task<SubtagRegistry>` | Static. Asynchronous form that can be cancelled. Throws `OperationCanceledException` if `cancellationToken` is cancelled. |
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
- [LanguageTag API reference](index.md)
