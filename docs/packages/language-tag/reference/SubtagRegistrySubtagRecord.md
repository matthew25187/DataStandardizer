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
get instances from a [SubtagRegistry](SubtagRegistry.md): by enumerating it, from
its `Languages`, `ExtendedLanguages`, `Scripts`, `Regions` and `Variants` lists,
or with its `TryGetSubtag` method.

```csharp
public sealed class SubtagRegistrySubtagRecord : SubtagRegistryTagRecordBase
```

## Remarks

In addition to the common fields inherited from
[SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md), this record adds the
identifying `Subtag` field. A `Subtag` may denote a range, such as `qaa..qtz`
(RFC 5646 §3.1.4).

## Properties

| Property | Signature | Registry field |
| --- | --- | --- |
| `Subtag` | `string Subtag { get; }` | `Subtag` |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md)
- [SubtagRegistryTagRecord](SubtagRegistryTagRecord.md)
- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
