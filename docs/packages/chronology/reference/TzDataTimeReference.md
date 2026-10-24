---
title: TzDataTimeReference Enum
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimeReference Enum

## Definition

Namespace: `DataStandardizer.Chronology`

Identifies the clock against which a TZ Database time of day is measured.

```csharp
public enum TzDataTimeReference
```

## Remarks

Used by the `AtTimeReference` property of [TzDataRule](TzDataRule.md) and the
`TimeReference` property of [TzDataUntil](TzDataUntil.md). In the source, it is
given by a suffix on the time, as in `2:00s`.

## Fields

| Field | Suffix | Meaning |
| --- | --- | --- |
| `Wall` | None, or `w` | Local wall clock time, including any daylight saving in effect. |
| `Standard` | `s` | Local standard time, excluding any daylight saving. |
| `Universal` | `u`, `g` or `z` | Universal time. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [TzDataRule](TzDataRule.md)
- [TzDataUntil](TzDataUntil.md)
- [Chronology API reference](index.md)
