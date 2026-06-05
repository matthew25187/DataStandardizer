---
title: IItuE164InternationalNumber Interface
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# IItuE164InternationalNumber Interface

## Definition

Namespace: `DataStandardizer.Communication.E164`

The base abstraction for every kind of ITU-T E.164 international telephone number.
It adds the country code common to all kinds and is the root of the five role
interfaces.

```csharp
public interface IItuE164InternationalNumber : ITelephonyNumber
```

## Remarks

Extends [ITelephonyNumber](ITelephonyNumber.md) (which contributes `Number`). The
five `…For*` role interfaces all extend this interface. See
[The E.164 number model](../concepts/e164-number-model.md) for how they fit
together.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CountryCode` | `ushort CountryCode { get; }` | The E.164 country code. Throws `NotSupportedException` when not supported for the number's kind. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The E.164 number model](../concepts/e164-number-model.md)
- [ITelephonyNumber](ITelephonyNumber.md)
- [IItuE164InternationalNumberForGeographicAreas](IItuE164InternationalNumberForGeographicAreas.md)
- [IItuE164InternationalNumberForGlobalServices](IItuE164InternationalNumberForGlobalServices.md)
- [IItuE164InternationalNumberForNetworks](IItuE164InternationalNumberForNetworks.md)
- [IItuE164InternationalNumberForGroupsOfCountries](IItuE164InternationalNumberForGroupsOfCountries.md)
- [IItuE164InternationalNumberForTrials](IItuE164InternationalNumberForTrials.md)
- [Communication API reference](index.md)
</content>
