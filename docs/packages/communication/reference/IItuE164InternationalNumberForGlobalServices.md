---
title: IItuE164InternationalNumberForGlobalServices Interface
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# IItuE164InternationalNumberForGlobalServices Interface

## Definition

Namespace: `DataStandardizer.Communication.E164`

The global-service number role. Adds the global subscriber number.

```csharp
public interface IItuE164InternationalNumberForGlobalServices : IItuE164InternationalNumber
```

## Remarks

Extends [IItuE164InternationalNumber](IItuE164InternationalNumber.md) (which
contributes `Number` and `CountryCode`). On `ItuE164InternationalNumber` this field
is an explicit interface implementation; reading it on a number of a different kind
throws `NotSupportedException`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `GlobalSubscriberNumber` | `ItuE164GlobalSubscriberNumber GlobalSubscriberNumber { get; }` | The global subscriber number. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The E.164 number model](../concepts/e164-number-model.md)
- [IItuE164InternationalNumber](IItuE164InternationalNumber.md)
- [ItuE164GlobalSubscriberNumber](ItuE164GlobalSubscriberNumber.md)
- [ItuE164InternationalNumber](ItuE164InternationalNumber.md)
- [Communication API reference](index.md)
</content>
