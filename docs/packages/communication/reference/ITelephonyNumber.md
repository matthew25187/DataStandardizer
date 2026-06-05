---
title: ITelephonyNumber Interface
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# ITelephonyNumber Interface

## Definition

Namespace: `DataStandardizer.Communication`

The root abstraction for any telephony number, exposing the raw numeric value of
the whole number.

```csharp
public interface ITelephonyNumber
```

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Number` | `ulong Number { get; }` | The numeric value of the whole number. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The E.164 number model](../concepts/e164-number-model.md)
- [IItuE164InternationalNumber](IItuE164InternationalNumber.md)
- [Communication API reference](index.md)
</content>
