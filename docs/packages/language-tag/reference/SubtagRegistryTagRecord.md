---
title: SubtagRegistryTagRecord Class
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistryTagRecord Class

## Definition

Namespace: `DataStandardizer.LanguageTag.SubtagRegistry`

A *tag* entry from the IANA Subtag Registry. The constructor is internal; you
obtain instances by enumerating a [SubtagRegistry](SubtagRegistry.md).

```csharp
public class SubtagRegistryTagRecord : SubtagRegistryTagRecordBase
```

## Remarks

In addition to the common fields inherited from
[SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md), this record adds the
identifying `Tag` field.

## Properties

| Property | Signature | Registry field |
| --- | --- | --- |
| `Tag` | `string Tag { get; }` | `Tag` |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md)
- [SubtagRegistrySubtagRecord](SubtagRegistrySubtagRecord.md)
- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
