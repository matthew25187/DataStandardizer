---
title: VcardPropertyInfo Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardPropertyInfo Class

## Definition

Namespace: `DataStandardizer.File.vCard`

What the vCard specifications say about a property: which versions define it, and how many instances a vCard 4.0 card may have. The versions are not additive.

**Syntax**

```csharp
public sealed class VcardPropertyInfo
```

## Members

| Member | Notes |
| --- | --- |
| `All` | Static. Every property any version defines. |
| `Find(string name)` | Static. What is known about a property, in any case, or `null` if no version defines it. |
| `Name` | The name, in upper case. |
| `Cardinality` | How many instances a vCard 4.0 card may have. A property vCard 4.0 doesn't define is `Any`. |
| `IsDefinedIn(VcardVersion version)` | Whether a version defines the property. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardCardinality](VcardCardinality.md)
- [VcardVersion](VcardVersion.md)
