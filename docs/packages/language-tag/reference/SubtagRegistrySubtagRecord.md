---
title: SubtagRegistrySubtagRecord Class
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistrySubtagRecord Class

## Definition

Namespace: `DataStandardizer.LanguageTag.SubtagRegistry`

A *subtag* entry from the IANA Subtag Registry. The constructor is internal; you
obtain instances by enumerating a [SubtagRegistry](SubtagRegistry.md).

```csharp
public class SubtagRegistrySubtagRecord : SubtagRegistryTagRecordBase
```

## Remarks

In addition to the common fields inherited from
[SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md), this record adds the
identifying `Subtag` field.

## Properties

| Property | Signature | Registry field |
| --- | --- | --- |
| `Subtag` | `string Subtag { get; }` | `Subtag` |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md)
- [SubtagRegistryTagRecord](SubtagRegistryTagRecord.md)
- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
