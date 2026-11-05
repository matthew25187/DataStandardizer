---
title: VcardKind Enum
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardKind Enum

## Definition

Namespace: `DataStandardizer.File.vCard`

The kind of object a card represents (RFC 6350 §6.1.4).

**Syntax**

```csharp
public enum VcardKind
```

## Fields

| Field | Value | `KIND` value |
| --- | --- | --- |
| `Individual` | 0 | `individual`, which is also what a card with no `KIND` is |
| `Group` | 1 | `group`, the only kind that may have `MEMBER` properties |
| `Organization` | 2 | `org` |
| `Location` | 3 | `location` |
| `Other` | 4 | Any other value, which `VcardCard.KindText` holds |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardCard](VcardCard.md)
