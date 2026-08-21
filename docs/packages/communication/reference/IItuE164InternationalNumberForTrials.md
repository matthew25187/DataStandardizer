---
title: IItuE164InternationalNumberForTrials Interface
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# IItuE164InternationalNumberForTrials Interface

## Definition

Namespace: `DataStandardizer.Communication.E164`

The trial number role. Adds the trial identification code and an optional
subscriber number.

```csharp
public interface IItuE164InternationalNumberForTrials : IItuE164InternationalNumber
```

## Remarks

Extends [IItuE164InternationalNumber](IItuE164InternationalNumber.md) (which
contributes `Number` and `CountryCode`). On `ItuE164InternationalNumber` these
fields are explicit interface implementations; reading them on a number of a
different kind throws `NotSupportedException`. The subscriber number is nullable —
not all trial numbers include one.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `SubscriberNumber` | `ItuE164SubscriberNumber? SubscriberNumber { get; }` | The optional subscriber number. |
| `TrialIdentificationCode` | `ItuE164AssignedTrialIdentificationCodesForTrials TrialIdentificationCode { get; }` | The trial identification code. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The E.164 number model](../concepts/e164-number-model.md)
- [IItuE164InternationalNumber](IItuE164InternationalNumber.md)
- [ItuE164AssignedTrialIdentificationCodesForTrials](ItuE164AssignedTrialIdentificationCodesForTrials.md)
- [ItuE164SubscriberNumber](ItuE164SubscriberNumber.md)
- [ItuE164InternationalNumber](ItuE164InternationalNumber.md)
- [Communication API reference](index.md)
</content>
