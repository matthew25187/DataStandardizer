---
title: IItuE164InternationalNumberForGeographicAreas Interface
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# IItuE164InternationalNumberForGeographicAreas Interface

## Definition

Namespace: `DataStandardizer.Communication.E164`

The geographic-area number role. Adds the national significant number (NSN) that
follows the country code.

```csharp
public interface IItuE164InternationalNumberForGeographicAreas : IItuE164InternationalNumber
```

## Remarks

Extends [IItuE164InternationalNumber](IItuE164InternationalNumber.md) (which
contributes `Number` and `CountryCode`). On `ItuE164InternationalNumber` this field
is an explicit interface implementation; reading it on a number of a different kind
throws `NotSupportedException`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `NationalSignificantNumber` | `ItuE164NationalSignificantNumber NationalSignificantNumber { get; }` | The national significant number following the country code. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The E.164 number model](../concepts/e164-number-model.md)
- [IItuE164InternationalNumber](IItuE164InternationalNumber.md)
- [ItuE164NationalSignificantNumber](ItuE164NationalSignificantNumber.md)
- [ItuE164InternationalNumber](ItuE164InternationalNumber.md)
- [Communication API reference](index.md)
</content>
