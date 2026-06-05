---
title: SubtagRegistryTagRecordBase Class
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistryTagRecordBase Class

## Definition

Namespace: `DataStandardizer.LanguageTag.SubtagRegistry`

The base for the tag and subtag records; exposes the common registry fields shared
by [SubtagRegistryTagRecord](SubtagRegistryTagRecord.md) and
[SubtagRegistrySubtagRecord](SubtagRegistrySubtagRecord.md).

```csharp
public abstract class SubtagRegistryTagRecordBase : SubtagRegistryRecordBase
```

## Remarks

Each property is backed by the inherited `GetPropertyValue<T>()` /
`GetPropertyValues<T>()` helpers and mapped to its registry field by a
[SubtagRegistryFieldAttribute](SubtagRegistryFieldAttribute.md).

## Properties

| Property | Signature | Registry field |
| --- | --- | --- |
| `Added` | `DateTime Added { get; }` | `Added` |
| `Comments` | `string? Comments { get; }` | `Comments` |
| `Deprecated` | `DateTime? Deprecated { get; }` | `Deprecated` |
| `Description` | `string[] Description { get; }` | `Description` |
| `Macrolanguage` | `string? Macrolanguage { get; }` | `Macrolanguage` |
| `PreferredValue` | `string? PreferredValue { get; }` | `Preferred-Value` |
| `Prefix` | `string[] Prefix { get; }` | `Prefix` |
| `Scope` | `string? Scope { get; }` | `Scope` |
| `SuppressScript` | `string? SuppressScript { get; }` | `Suppress-Script` |
| `Type` | `string Type { get; }` | `Type` |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. On the
.NET Standard targets nullability is expressed through JetBrains
`[CanBeNull]` attributes; the public surface is otherwise the same.

## See also

- [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md)
- [SubtagRegistryTagRecord](SubtagRegistryTagRecord.md)
- [SubtagRegistrySubtagRecord](SubtagRegistrySubtagRecord.md)
- [SubtagRegistryFieldAttribute](SubtagRegistryFieldAttribute.md)
- [LanguageTag API reference](index.md)
