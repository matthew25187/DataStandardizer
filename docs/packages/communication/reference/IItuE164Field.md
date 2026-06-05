---
title: IItuE164Field Interface
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# IItuE164Field Interface

## Definition

Namespace: `DataStandardizer.Communication.E164`

A field of an ITU-T E.164 number, exposing its digit count. Implemented by the
field structs `ItuE164NationalSignificantNumber`, `ItuE164SubscriberNumber`, and
`ItuE164GlobalSubscriberNumber`.

```csharp
public interface IItuE164Field
```

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `DigitCount` | `int DigitCount { get; }` | The number of digits in the field value. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The E.164 number model](../concepts/e164-number-model.md)
- [ItuE164NationalSignificantNumber](ItuE164NationalSignificantNumber.md)
- [ItuE164SubscriberNumber](ItuE164SubscriberNumber.md)
- [ItuE164GlobalSubscriberNumber](ItuE164GlobalSubscriberNumber.md)
- [Communication API reference](index.md)
</content>
