---
title: VcardCardinality Enum
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardCardinality Enum

## Definition

Namespace: `DataStandardizer.File.vCard`

How many instances of a property a card may have (RFC 6350 §6). Instances with the same `ALTID` count as one (§5.4).

**Syntax**

```csharp
public enum VcardCardinality
```

## Fields

| Field | Value | Written in the RFC |
| --- | --- | --- |
| `Any` | 0 | `*` |
| `ExactlyOne` | 1 | `1` |
| `AtMostOne` | 2 | `*1` |
| `AtLeastOne` | 3 | `1*` |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardPropertyInfo](VcardPropertyInfo.md)
