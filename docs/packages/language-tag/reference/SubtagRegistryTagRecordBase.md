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
list. Every other field appears at most once in a record; a registry where
one appears twice fails to load.

`Added` and `Deprecated` are read from the `yyyy-MM-dd` full-date of RFC 3339
(RFC 5646 §3.1.1). A registry with a date in any other form fails to load with a
`FormatException`. `Deprecated` is `null` when the record has no `Deprecated`
field.

A loaded registry has `Prefix` only on `extlang` and `variant` records, with
exactly one on each `extlang` record, and `SuppressScript`, `Macrolanguage` and
`Scope` only on `language` and `extlang` records. `Scope` is one of
`macrolanguage`, `collection`, `special` and `private-use` (RFC 5646 §3.1.2).
Each `Prefix` is a well-formed language tag that repeats no variant subtag or
extension singleton (§2.2.9, §3.1.8), `SuppressScript` is a script subtag of
four letters (§3.1.9), and `Macrolanguage` is a primary language subtag of two to
eight letters (§3.1.10). `PreferredValue` is a primary language subtag on a
`language` record, a subtag of the record's type on a `script`, `region` or
`variant` record, the record's own `Subtag` on an `extlang` record, which always
has one (§2.2.2), and an extended language range (RFC 4647 §2.2) on a
`grandfathered` or `redundant` record (§3.1.2, §3.1.7). Apart from the
`extlang` `PreferredValue`, these fields may be in any case, as §3.1.4 sets the
case of `Subtag` and `Tag` alone. Whether the subtags they name have records
in the registry is not checked.

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
