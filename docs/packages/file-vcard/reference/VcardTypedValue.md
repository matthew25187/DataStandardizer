---
title: VcardTypedValue Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardTypedValue Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

The value of a property that can have more than one value type: `TEL`, `UID`, `KEY`, `RELATED`, `BIRTHPLACE` and `DEATHPLACE` (text or URI), `BDAY`, `ANNIVERSARY` and `DEATHDATE` (date-and-or-time or text), and `TZ` (text, URI or UTC offset).

**Syntax**

```csharp
public readonly struct VcardTypedValue : IEquatable<VcardTypedValue>
```

## Members

| Member | Notes |
| --- | --- |
| `VcardTypedValue(string)`, `(Uri)`, `(VcardDateAndOrTime)`, `(VcardUtcOffset)` | The text is without any escaping. A URI must be absolute. |
| `ValueType` | `Text`, `Uri`, `DateAndOrTime` or `UtcOffset`. |
| `Text`, `Uri`, `DateAndOrTime`, `UtcOffset` | The value. Asking for another type than it holds throws `InvalidOperationException`. |
| `ToString()` | The value as it appears in a vCard 4.0 content line, with any escaping. |
| `Equals`, `==`, `!=`, `GetHashCode` | The same type and the same value. A URI is compared as it was given. |

A default instance is not initialised: every member but the equality members and `ToString` throws `InvalidOperationException`, and `ToString` gives the type name.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardCard](VcardCard.md)
