---
title: TzDataZoneLine Class
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataZoneLine Class

## Definition

Namespace: `DataStandardizer.Chronology`

A period in the offset history of a TZ Database time zone, corresponding to one
line of a `Zone` entry in the TZ Database source.

```csharp
public sealed class TzDataZoneLine
```

## Remarks

A zone line applies from the moment the preceding line stops applying, or from
the beginning of time for the first line, until `Until`. The last line has no
`Until` and applies indefinitely.

You don't construct zone lines; the constructor is internal. Read them through
[`TzDataTimezone.ZoneLines`](TzDataTimezone.md), or get the one in effect at an
instant with [`TzDataExtensions.GetZoneLine`](TzDataExtensions.md). Instances
are immutable, and are shared by every time zone that has them, including links.

The properties correspond to the columns of the source:

```text
# Zone NAME             STDOFF    RULES  FORMAT  [UNTIL]
Zone America/New_York   -4:56:02  -      LMT     1883 Nov 18 17:00u
                        -5:00     US     E%sT    1920
...
```

| Column | Property |
| --- | --- |
| `STDOFF` | `StandardOffset` |
| `RULES` | `RuleKind`, with `FixedSave` or `Rules` |
| `FORMAT` | `Format` |
| `UNTIL` | `Until` |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `FixedSave` | `TimeSpan? FixedSave { get; }` | The amount added to the standard offset throughout the line, or `null` unless `RuleKind` is `FixedSave`. |
| `Format` | `string Format { get; }` | The format of the zone abbreviation, verbatim from the source, for example `E%sT`, `GMT/BST` or `%z`. Format it with the abbreviation methods of [TzDataExtensions](TzDataExtensions.md). |
| `RuleKind` | `TzDataZoneRuleKind RuleKind { get; }` | What the `RULES` column contains: no rules, a fixed save, or a rule set. See [TzDataZoneRuleKind](TzDataZoneRuleKind.md). |
| `Rules` | `IReadOnlyList<TzDataRule> Rules { get; }` | The daylight saving rules of the rule set the line uses, as [TzDataRule](TzDataRule.md) instances. Empty unless `RuleKind` is `RuleSet`. |
| `StandardOffset` | `TimeSpan StandardOffset { get; }` | The offset of standard time from universal time. Historical offsets may include seconds, for example `-04:56:02`. |
| `Until` | `TzDataUntil? Until { get; }` | The moment at which the line stops applying, as a [TzDataUntil](TzDataUntil.md), or `null` for the last line of a zone. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Calculate UTC offsets](../how-to/calculate-utc-offsets.md#look-at-the-underlying-zone-lines)
- [TzDataTimezone](TzDataTimezone.md)
- [TzDataRule](TzDataRule.md)
- [TzDataUntil](TzDataUntil.md)
- [TzDataZoneRuleKind](TzDataZoneRuleKind.md)
- [Chronology API reference](index.md)
