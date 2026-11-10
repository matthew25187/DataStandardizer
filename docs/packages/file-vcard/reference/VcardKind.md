---
title: VcardKind Enum
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardKind Enum

## Definition

Namespace: `DataStandardizer.File.vCard`

The kind of object a card represents (RFC 6350 §6.1.4, RFC 6473 and RFC 6869).

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
| `Application` | 4 | `application`, a software application such as a server, an online service or a bot (RFC 6473 §2) |
| `Device` | 5 | `device`, a computing device such as an appliance, a computer or a network element (RFC 6869 §2) |
| `Other` | 6 | Any other value, which `VcardCard.KindText` holds |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardCard](VcardCard.md)
