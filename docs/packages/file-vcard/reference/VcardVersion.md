---
title: VcardVersion Enum
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardVersion Enum

## Definition

Namespace: `DataStandardizer.File.vCard`

A version of the vCard format.

**Syntax**

```csharp
public enum VcardVersion
```

## Remarks

The members are `V4_0` (RFC 6350), `V3_0` (RFC 2425 and RFC 2426) and `V2_1` (the 1996 IMC specification). `V4_0` is the zero value, and the default wherever a method has no version.

The versions are not additive, so a value valid in one can be invalid in another. Each parses by its own grammar: `1985-04-12` is a date in vCard 3.0 and not in vCard 4.0, and `--0412` the other way round.

See the [API reference](index.md).
