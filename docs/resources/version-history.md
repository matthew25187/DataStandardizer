---
title: Version history
parent: Resources
nav_order: 5
---

# Version history

Where to find the released versions of each *Data Standardizer* package.

There is no changelog or release-notes file in this repository. For the version
history of released packages, see each package's listing on NuGet:

[Data Standardizer packages on NuGet](https://www.nuget.org/packages?q=DataStandardizer){: .btn }

Each package page on NuGet lists its published versions and release dates.

## Removed in v2.0

Version 2.0 removes the legacy `DataStandardizer.BCP47`, `DataStandardizer.File`,
`DataStandardizer.ISO15924`, `DataStandardizer.ISO3166`,
`DataStandardizer.ISO4217`, `DataStandardizer.ISO639` and
`DataStandardizer.UNM49` packages. See [Removed in v2.0](removed-in-v2.md) for the
package that replaces each one and the types that were renamed. It also raises
the lowest target of `DataStandardizer.Communication`,
`DataStandardizer.LanguageTag` and `DataStandardizer.Money` from `netstandard1.0`
to `netstandard1.1`. It changes the `ExtendedLanguageSubtags`, `VariantSubtags`
and `ExtensionSubtags` properties of `Bcp47LanguageTag`, and the `Subtags`
property of `Bcp47KeyedSubtag`, from arrays to `IReadOnlyList<T>`, and removes
the `DataStandardizer.LanguageTag` members that took a match timeout, and the
builder's `UsingRegionSubtag` overloads that took a UN M49 country code enum.

`Bcp47LanguageTag.PrimaryLanguageSubtag` is now a `string?`. On a private use tag
such as `x-private`, or a grandfathered tag such as `i-klingon`, it is `null`
instead of the empty string, so code that tests for an empty string should test
for `null` instead.

Creating a `Bcp47LanguageTag` without a subtag registry now checks that the tag is
well-formed and nothing else, so it accepts tags such as `zz` and `de-1901-1901`
that version 1.x rejected. `IsWellFormedLanguageTagString` is replaced by
`IsWellFormed` and `IsValid`, and the ISO code lists are an explicit opt-in, which now
rejects three-letter codes such as `eng`. `Check*Subtag` and the builder's string steps
check only the shape of a subtag. See
[what creating a language tag checks](removed-in-v2.md#what-creating-a-language-tag-checks).

In `DataStandardizer.File.CSV`, `ICsvFileLine` is no longer an
`IOrderedDictionary`. It is an ordered collection of fields with `string` names,
enumerated as `KeyValuePair<string, object?>` items, with typed `Keys` and `Values`
lists. See [the File.CSV line model](removed-in-v2.md#filecsv-line-model).

A File.CSV mapper registered with `RegisterMapper` now applies only to the reader
or writer it is registered on, not to every reader and writer of the record type,
and the `protected` members of `CsvFileIoBase<TRecordLine>` and
`CsvFileCacheRepositoryBase` are removed. `CsvMapperCache.Clear()` clears the
cached mappers built from attributes. See
[File.CSV mapper registration](removed-in-v2.md#filecsv-mapper-registration).

A File.CSV reader now parses a mapped numeric, date, time, `bool`, `Guid` or enum
property that has no converter, instead of leaving it at its default value, and a
field that fails to convert, with or without a converter, goes to the bad-value
handler or raises a `CsvFileException`. See
[File.CSV conversion of mapped properties](removed-in-v2.md#filecsv-conversion-of-mapped-properties).

`SubtagRegistry` moves to the `DataStandardizer.LanguageTag` namespace, and the
types of its records move to `DataStandardizer.LanguageTag.Registry`. See
[where the registry types live](removed-in-v2.md#where-the-registry-types-live).

In `DataStandardizer.LanguageTag`, Subtag Registry records are now immutable,
with their fields parsed once when the registry is loaded:

- `ISubtagRegistryRecord` is no longer an `IList<Tuple<string, object>>`. Read a
  record's raw fields from its `Fields` property, a read-only list of
  `KeyValuePair<string, string>` (`Key` and `Value` in place of `Item1` and
  `Item2`).
- `SubtagRegistryTagRecordBase.Type` is a `SubtagRegistryRecordType` instead of a
  string: compare with `SubtagRegistryRecordType.Language` rather than
  `"language"`.
- `Description` and `Prefix` are `IReadOnlyList<string>` instead of arrays, and
  `Comments` is an `IReadOnlyList<string>` with every `Comments` field instead of a
  string with only the first.
- A date that isn't in the `yyyy-MM-dd` form reads as the default `DateTime` (or
  `null` for `Deprecated`) instead of throwing `FormatException`.
- `SubtagRegistryFieldAttribute`, the protected `GetPropertyValue<T>()` and
  `GetPropertyValues<T>()` helpers, and the public `SubtagRegistryFileDateRecord`
  constructor are removed. The record classes are sealed and their constructors
  internal, so records come only from a loaded `SubtagRegistry`.

`SubtagRegistry` gains `TryGetSubtag`, `IsGrandfathered`, `FileDate` and a list of
the records of each type (`Languages`, `ExtendedLanguages`, `Scripts`, `Regions`,
`Variants`, `GrandfatheredTags` and `RedundantTags`), so code that filtered the
records by type can use those instead.

## How the docs track versions

This documentation reflects the **current state of the packages** rather than any
single released version. Where a feature is tied to a particular target framework
or release, the relevant page carries a short **"Applies to"** note instead of the
docs being split into versioned sets. The
[Platform support](../overview/platform-support.md) page is a good example of
this convention in action.
