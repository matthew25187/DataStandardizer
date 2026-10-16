---
title: IcalDateTimeKind Enum
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalDateTimeKind Enum

## Definition

Namespace: `DataStandardizer.File.iCal`

The forms of a DATE-TIME value (RFC 5545 §3.3.5). Read it from
[IcalDateTime.Kind](IcalDateTime.md).

**Syntax**

```csharp
public enum IcalDateTimeKind
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Floating` | 0 | A local time not bound to any time zone, such as `19980118T230000`. It means the same wall-clock time wherever it is read. |
| `Utc` | 1 | A time in UTC, such as `19980119T070000Z`. |
| `Zoned` | 2 | A local time in the time zone named by a `TZID` parameter, such as `TZID=America/New_York:19980119T020000`. |

## Remarks

The kind is not stored separately. An `IcalDateTime` is `Zoned` when it has a time
zone identifier, `Utc` when its time is in UTC, and `Floating` otherwise. The default
`IcalDateTime` is `Floating`.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [Time zone resolution](../concepts/time-zone-resolution.md)
- [IcalDateTime](IcalDateTime.md)
- [IcalTime](IcalTime.md)
- [File.iCal API reference](index.md)
