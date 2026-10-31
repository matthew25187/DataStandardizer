---
title: ISubtagRegistryRecord Interface
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# ISubtagRegistryRecord Interface

## Definition

Namespace: `DataStandardizer.LanguageTag.SubtagRegistry`

The base contract for all subtag-registry records: the record's fields, as read
from the registry.

```csharp
public interface ISubtagRegistryRecord
```

## Remarks

Implemented by [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md) and its
derived record types. A [SubtagRegistry](SubtagRegistry.md) is a read-only
collection of these records.

Records are immutable. The typed properties of each record type are parsed from
its fields once, when the registry is loaded. `Fields` keeps every field, so a
field that has no typed property, such as one added to the registry format after
this version, can still be read.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Fields` | `IReadOnlyList<KeyValuePair<string, string>> Fields { get; }` | The record's fields in registry order, as field name and field body. A folded body is unfolded with one space. The list can't be changed. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [SubtagRegistry](SubtagRegistry.md)
- [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md)
- [LanguageTag API reference](index.md)
