---
title: SubtagRegistryFileDateRecord Class
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistryFileDateRecord Class

## Definition

Namespace: `DataStandardizer.LanguageTag.Registry`

The registry's `File-Date` header record — carries the date the loaded copy of the
IANA Subtag Registry was published.

```csharp
public sealed class SubtagRegistryFileDateRecord : SubtagRegistryRecordBase
```

## Remarks

The constructor is internal; the record is created when a
[SubtagRegistry](SubtagRegistry.md) is loaded, and the registry's `FileDate`
property returns its date. A registry whose `File-Date` isn't an RFC 3339
full-date (`yyyy-MM-dd`) fails to load with a `FormatException`.

## Properties

| Property | Signature | Registry field |
| --- | --- | --- |
| `FileDate` | `DateTime FileDate { get; }` | `File-Date` |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, and `net10.0`.

## See also

- [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md)
- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
