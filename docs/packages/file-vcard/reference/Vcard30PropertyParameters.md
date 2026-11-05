---
title: Vcard30PropertyParameters Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# Vcard30PropertyParameters Class

## Definition

Namespace: `DataStandardizer.File.vCard`

The group and parameters of a property being added through an [IVcard30PropertyList](IVcard30PropertyList.md). Each one left at its default is left out.

**Syntax**

```csharp
public sealed class Vcard30PropertyParameters
```

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Group` | `string?` | The group, without its `.`. |
| `Types` | `IList<string>` | The `TYPE` values, other than `pref`. |
| `IsPreferred` | `bool` | Whether the property is preferred, which is `TYPE=pref` in vCard 3.0. |
| `Language` | `string?` | The `LANGUAGE`, a tag of RFC 1766, which RFC 2426 §4 cites. |
| `AdditionalParameters` | `IList<VcardContentLineParameter>` | Parameters no version defines, to go after the others. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
