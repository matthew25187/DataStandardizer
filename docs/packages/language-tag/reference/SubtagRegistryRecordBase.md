---
title: SubtagRegistryRecordBase Class
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistryRecordBase Class

## Definition

Namespace: `DataStandardizer.LanguageTag.SubtagRegistry`

The shared base for every subtag-registry record. It holds the record's fields
and renders them back to registry text.

```csharp
public abstract class SubtagRegistryRecordBase : ISubtagRegistryRecord
```

## Remarks

Records are created only when a [SubtagRegistry](SubtagRegistry.md) is loaded.
The constructor is internal, so the class can't be derived from outside the
package. A record never changes once it is loaded.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Fields` | `IReadOnlyList<KeyValuePair<string, string>> Fields { get; }` | The record's fields in registry order. See [ISubtagRegistryRecord](ISubtagRegistryRecord.md). |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | Override. Renders the record's fields as `field: value` lines. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [ISubtagRegistryRecord](ISubtagRegistryRecord.md)
- [SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md)
- [SubtagRegistryFileDateRecord](SubtagRegistryFileDateRecord.md)
- [LanguageTag API reference](index.md)
