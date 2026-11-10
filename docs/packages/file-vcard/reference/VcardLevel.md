---
title: VcardLevel Enum
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardLevel Enum

## Definition

Namespace: `DataStandardizer.File.vCard`

The level of an expertise, hobby or interest: the value of the `LEVEL` parameter (RFC 6715 §3.2).

**Syntax**

```csharp
public enum VcardLevel
```

## Fields

| Field | Value | `LEVEL` value | Goes with |
| --- | --- | --- | --- |
| `Beginner` | 0 | `beginner` | `EXPERTISE` |
| `Average` | 1 | `average` | `EXPERTISE` |
| `Expert` | 2 | `expert` | `EXPERTISE` |
| `High` | 3 | `high` | `HOBBY`, `INTEREST` |
| `Medium` | 4 | `medium` | `HOBBY`, `INTEREST` |
| `Low` | 5 | `low` | `HOBBY`, `INTEREST` |

A value on a property it doesn't go with is rejected by default, and `VcardProperty<TValue>.Level` gives `null` for it.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardProperty](VcardProperty.md)
- [VcardPropertyParameters](VcardPropertyParameters.md)
