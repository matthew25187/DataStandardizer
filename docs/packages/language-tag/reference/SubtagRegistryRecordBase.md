---
title: SubtagRegistryRecordBase Class
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistryRecordBase Class

## Definition

Namespace: `DataStandardizer.LanguageTag.SubtagRegistry`

The shared base for every subtag-registry record. It stores the raw field pairs
(implementing `IList<Tuple<string, object>>` explicitly) and provides the protected
helpers that derived records use to expose typed, named fields.

```csharp
public abstract class SubtagRegistryRecordBase : ISubtagRegistryRecord
```

## Remarks

The protected `GetPropertyValue<T>()` / `GetPropertyValues<T>()` helpers resolve a
calling property to its underlying registry field — by the field name supplied via
[SubtagRegistryFieldAttribute](SubtagRegistryFieldAttribute.md), or the property
name when no attribute is present — and convert the raw value to the requested
type. The `IList<Tuple<string, object>>` members are implemented explicitly, so the
record presents typed properties rather than the raw field list by default.

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | Override. Renders the record's fields as `field: value` lines. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [ISubtagRegistryRecord](ISubtagRegistryRecord.md)
- [SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md)
- [SubtagRegistryFileDateRecord](SubtagRegistryFileDateRecord.md)
- [SubtagRegistryFieldAttribute](SubtagRegistryFieldAttribute.md)
- [LanguageTag API reference](index.md)
