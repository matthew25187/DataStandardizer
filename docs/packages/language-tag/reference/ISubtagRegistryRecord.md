---
title: ISubtagRegistryRecord Interface
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# ISubtagRegistryRecord Interface

## Definition

Namespace: `DataStandardizer.LanguageTag.SubtagRegistry`

The base contract for all subtag-registry records: an ordered list of
`(field name, value)` pairs. It adds no members of its own beyond those of
`IList<Tuple<string, object>>`.

```csharp
public interface ISubtagRegistryRecord : IList<Tuple<string, object>>
```

## Remarks

Implemented by [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md) and its
derived record types. A [SubtagRegistry](SubtagRegistry.md) is a read-only
collection of these records.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [SubtagRegistry](SubtagRegistry.md)
- [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md)
- [LanguageTag API reference](index.md)
