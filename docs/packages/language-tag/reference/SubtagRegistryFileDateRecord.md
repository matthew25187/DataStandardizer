---
title: SubtagRegistryFileDateRecord Class
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistryFileDateRecord Class

## Definition

Namespace: `DataStandardizer.LanguageTag.SubtagRegistry`

The registry's `File-Date` header record — carries the date the loaded copy of the
IANA Subtag Registry was published.

```csharp
public class SubtagRegistryFileDateRecord : SubtagRegistryRecordBase
```

## Constructors

| Constructor | Notes |
| --- | --- |
| `SubtagRegistryFileDateRecord(Tuple<string, object> field)` | Wraps the single `File-Date` field pair. |

## Properties

| Property | Signature | Registry field |
| --- | --- | --- |
| `FileDate` | `DateTime FileDate { get; }` | `File-Date` |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md)
- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
