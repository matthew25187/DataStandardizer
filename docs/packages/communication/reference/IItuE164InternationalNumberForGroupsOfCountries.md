---
title: IItuE164InternationalNumberForGroupsOfCountries Interface
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# IItuE164InternationalNumberForGroupsOfCountries Interface

## Definition

Namespace: `DataStandardizer.Communication.E164`

The group-of-countries number role. Adds the group identification code and
subscriber number.

```csharp
public interface IItuE164InternationalNumberForGroupsOfCountries : IItuE164InternationalNumber
```

## Remarks

Extends [IItuE164InternationalNumber](IItuE164InternationalNumber.md) (which
contributes `Number` and `CountryCode`). On `ItuE164InternationalNumber` these
fields are explicit interface implementations; reading them on a number of a
different kind throws `NotSupportedException`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `GroupIdentificationCode` | `ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries GroupIdentificationCode { get; }` | The group identification code. |
| `SubscriberNumber` | `ItuE164SubscriberNumber SubscriberNumber { get; }` | The subscriber number within the group. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The E.164 number model](../concepts/e164-number-model.md)
- [IItuE164InternationalNumber](IItuE164InternationalNumber.md)
- [ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries](ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries.md)
- [ItuE164SubscriberNumber](ItuE164SubscriberNumber.md)
- [ItuE164InternationalNumber](ItuE164InternationalNumber.md)
- [Communication API reference](index.md)
</content>
