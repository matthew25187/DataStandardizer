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
public sealed class SubtagRegistryFileDateRecord : SubtagRegistryRecordBase
```

## Remarks

The constructor is internal; the record is created when a
[SubtagRegistry](SubtagRegistry.md) is loaded, and the registry's `FileDate`
property returns its date. A `File-Date` that isn't in the `yyyy-MM-dd` form
reads as the default `DateTime`.

## Properties

| Property | Signature | Registry field |
| --- | --- | --- |
| `FileDate` | `DateTime FileDate { get; }` | `File-Date` |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md)
- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
