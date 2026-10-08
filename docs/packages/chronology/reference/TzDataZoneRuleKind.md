---
title: TzDataZoneRuleKind Enum
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataZoneRuleKind Enum

## Definition

Namespace: `DataStandardizer.Chronology`

Identifies what the `RULES` column of a TZ Database zone line contains.

```csharp
public enum TzDataZoneRuleKind
```

## Remarks

Used by the `RuleKind` property of [TzDataZoneLine](TzDataZoneLine.md). The kind
determines which of its `FixedSave` and `Rules` properties is set.

## Fields

| Field | Source form | Meaning |
| --- | --- | --- |
| `None` | `-` | No rules apply, and standard time is always in effect. `FixedSave` is `null` and `Rules` is empty. |
| `FixedSave` | An amount, such as `1:00` | A fixed amount is always added to the standard offset. `FixedSave` holds the amount and `Rules` is empty. |
| `RuleSet` | A rule name, such as `US` | The named set of daylight saving rules applies. `Rules` holds the rules and `FixedSave` is `null`. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [TzDataZoneLine](TzDataZoneLine.md)
- [TzDataRule](TzDataRule.md)
- [Chronology API reference](index.md)
