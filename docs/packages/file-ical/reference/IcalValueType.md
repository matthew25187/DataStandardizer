---
title: IcalValueType Enum
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalValueType Enum

## Definition

Namespace: `DataStandardizer.File.iCal`

The value types of RFC 5545 §3.3, named by the `VALUE` parameter.

**Syntax**

```csharp
public enum IcalValueType
```

## Remarks

[IcalValueTypeRegistry](IcalValueTypeRegistry.md) gives the value type of a property
and converts between these members and the names a `VALUE` parameter uses, such as
`DATE-TIME`. `Unknown` has no such name.

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Unknown` | 0 | A value whose type isn't known, such as that of an unknown, experimental (`X-`) or IANA-registered property. It is kept as raw text, so that it round-trips without loss. |
| `Binary` | 1 | `BINARY` (RFC 5545 §3.3.1): base64-encoded bytes. See `IcalValue.TryParseBinary`. |
| `Boolean` | 2 | `BOOLEAN` (RFC 5545 §3.3.2). See `IcalValue.TryParseBoolean`. |
| `CalAddress` | 3 | `CAL-ADDRESS` (RFC 5545 §3.3.3): a calendar user address. See `IcalValue.TryParseCalAddress`. |
| `Date` | 4 | `DATE` (RFC 5545 §3.3.4). See [IcalDate](IcalDate.md). |
| `DateTime` | 5 | `DATE-TIME` (RFC 5545 §3.3.5). See [IcalDateTime](IcalDateTime.md). |
| `Duration` | 6 | `DURATION` (RFC 5545 §3.3.6). See [IcalDuration](IcalDuration.md). |
| `Float` | 7 | `FLOAT` (RFC 5545 §3.3.7). See `IcalValue.TryParseFloat`. |
| `Integer` | 8 | `INTEGER` (RFC 5545 §3.3.8). See `IcalValue.TryParseInteger`. |
| `Period` | 9 | `PERIOD` (RFC 5545 §3.3.9). See [IcalPeriod](IcalPeriod.md). |
| `Recur` | 10 | `RECUR` (RFC 5545 §3.3.10). See [IcalRecurrenceRule](IcalRecurrenceRule.md). |
| `Text` | 11 | `TEXT` (RFC 5545 §3.3.11). See `IcalValue.TryParseText`. |
| `Time` | 12 | `TIME` (RFC 5545 §3.3.12). See [IcalTime](IcalTime.md). |
| `Uri` | 13 | `URI` (RFC 5545 §3.3.13). See `IcalValue.TryParseUri`. |
| `UtcOffset` | 14 | `UTC-OFFSET` (RFC 5545 §3.3.14). See [IcalUtcOffset](IcalUtcOffset.md). |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [IcalValueTypeRegistry](IcalValueTypeRegistry.md)
- [IcalValue](IcalValue.md)
- [IcalValueFormat](IcalValueFormat.md)
- [File.iCal API reference](index.md)
