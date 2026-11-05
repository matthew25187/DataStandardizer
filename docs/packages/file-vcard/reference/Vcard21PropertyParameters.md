---
title: Vcard21PropertyParameters Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# Vcard21PropertyParameters Class

## Definition

Namespace: `DataStandardizer.File.vCard`

The group and parameters of a property being added through an [IVcard21PropertyList](IVcard21PropertyList.md). Each one left at its default is left out. `ENCODING` and `CHARSET` aren't here, since a writer chooses them for the value, and neither is `VALUE`, which the value says.

**Syntax**

```csharp
public sealed class Vcard21PropertyParameters
```

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Group` | `string?` | The group, without its `.`. |
| `Types` | `IList<string>` | The `TYPE` values: each a `knowntype` of vCard 2.1 §2.9, such as `WORK`, in any case, or one that starts with `X-`. `PREF` isn't one of them, since `IsPreferred` says it. Any other makes `Add` throw `ArgumentException`. |
| `IsPreferred` | `bool` | Whether the property is preferred, which is `PREF` in vCard 2.1. |
| `Language` | `string?` | The `LANGUAGE`, a tag of RFC 1766, which vCard 2.1 §2.1.7 cites. Any other makes `Add` throw `ArgumentException`. |
| `AdditionalParameters` | `IList<VcardContentLineParameter>` | Parameters whose names start with `X-`, each with one value that is a `word` of §2.9, to go after the others. Any other makes `Add` throw `ArgumentException`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
