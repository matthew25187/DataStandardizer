---
title: IItuE164InternationalNumberForNetworks Interface
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# IItuE164InternationalNumberForNetworks Interface

## Definition

Namespace: `DataStandardizer.Communication.E164`

The network number role. Adds the network identification code and subscriber
number.

```csharp
public interface IItuE164InternationalNumberForNetworks : IItuE164InternationalNumber
```

## Remarks

Extends [IItuE164InternationalNumber](IItuE164InternationalNumber.md) (which
contributes `Number` and `CountryCode`). On `ItuE164InternationalNumber` these
fields are explicit interface implementations; reading them on a number of a
different kind throws `NotSupportedException`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `IdentificationCode` | `ItuE164AssignedIdentificationCodesForNetworks IdentificationCode { get; }` | The network identification code. |
| `SubscriberNumber` | `ItuE164SubscriberNumber SubscriberNumber { get; }` | The subscriber number within the identification code. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The E.164 number model](../concepts/e164-number-model.md)
- [IItuE164InternationalNumber](IItuE164InternationalNumber.md)
- [ItuE164AssignedIdentificationCodesForNetworks](ItuE164AssignedIdentificationCodesForNetworks.md)
- [ItuE164SubscriberNumber](ItuE164SubscriberNumber.md)
- [ItuE164InternationalNumber](ItuE164InternationalNumber.md)
- [Communication API reference](index.md)
</content>
