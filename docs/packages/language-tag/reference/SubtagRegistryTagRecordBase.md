---
title: SubtagRegistryTagRecordBase Class
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistryTagRecordBase Class

## Definition

Namespace: `DataStandardizer.LanguageTag.Registry`

The base for the tag and subtag records; exposes the common registry fields shared
by [SubtagRegistryTagRecord](SubtagRegistryTagRecord.md) and
[SubtagRegistrySubtagRecord](SubtagRegistrySubtagRecord.md).

```csharp
public abstract class SubtagRegistryTagRecordBase : SubtagRegistryRecordBase
```

## Remarks

Each property is parsed from the record's fields once, when the registry is
loaded, so reading one only returns the stored value.

`Description`, `Prefix` and `Comments` may appear more than once in a record
(RFC 5646 §3.1.2), so each returns every value, in registry order, as a list
that can't be changed. Each list is created the first time it is read, and the
same list is returned after that. A record without the field returns an empty
list. Where a field that may appear only once appears more than once, the
first is used.

`Added` and `Deprecated` are read from the `yyyy-MM-dd` full-date of RFC 3339
(RFC 5646 §3.1.1). A registry with a date in any other form fails to load with a
`FormatException`. `Deprecated` is `null` when the record has no `Deprecated`
field.

## Properties

| Property | Signature | Registry field |
| --- | --- | --- |
| `Added` | `DateTime Added { get; }` | `Added` |
| `Comments` | `IReadOnlyList<string> Comments { get; }` | `Comments` |
| `Deprecated` | `DateTime? Deprecated { get; }` | `Deprecated` |
| `Description` | `IReadOnlyList<string> Description { get; }` | `Description` |
| `Macrolanguage` | `string? Macrolanguage { get; }` | `Macrolanguage` |
| `PreferredValue` | `string? PreferredValue { get; }` | `Preferred-Value` |
| `Prefix` | `IReadOnlyList<string> Prefix { get; }` | `Prefix` |
| `Scope` | `string? Scope { get; }` | `Scope` |
| `SuppressScript` | `string? SuppressScript { get; }` | `Suppress-Script` |
| `Type` | `SubtagRegistryRecordType Type { get; }` | `Type`; see [SubtagRegistryRecordType](SubtagRegistryRecordType.md) |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, and `net10.0`. On the
.NET Standard targets nullability is expressed through `[MaybeNull]` attributes from
`System.Diagnostics.CodeAnalysis`. A project gets null warnings from them only on C# 8 or later with nullable enabled. The public surface is otherwise the same.

## See also

- [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md)
- [SubtagRegistryTagRecord](SubtagRegistryTagRecord.md)
- [SubtagRegistrySubtagRecord](SubtagRegistrySubtagRecord.md)
- [SubtagRegistryRecordType](SubtagRegistryRecordType.md)
- [LanguageTag API reference](index.md)
