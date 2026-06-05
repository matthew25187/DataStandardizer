---
title: Iso4217CurrencyCodeAttribute Class
parent: Money
grand_parent: Packages
nav_exclude: true
---

# Iso4217CurrencyCodeAttribute Class

## Definition

Namespace: `DataStandardizer.Money`

Describes an ISO 4217 currency code member with its metadata: the currency name,
minor-unit digit count, and funds-code flag.

```csharp
public sealed class Iso4217CurrencyCodeAttribute : CodeAttributeBase
```

## Remarks

This attribute is applied to each member of `Iso4217CurrencyCurrent` and
`Iso4217CurrencyHistoric`. You normally read the metadata through the
[Iso4217Extensions](Iso4217Extensions.md) accessors rather than reading the
attribute directly.

## Constructors

| Constructor | Notes |
| --- | --- |
| `Iso4217CurrencyCodeAttribute(string currencyName)` | |
| `Iso4217CurrencyCodeAttribute(string currencyName, byte minorUnits)` | |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CurrencyName` | `string CurrencyName { get; }` | The currency name. |
| `IsFundsCode` | `bool IsFundsCode { get; set; }` | `true` when the code is a funds code. |
| `MinorUnits` | `byte? MinorUnits { get; }` | Minor-unit digit count, or `null`. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Iso4217Extensions](Iso4217Extensions.md)
- [Iso4217CurrencyCurrent](Iso4217CurrencyCurrent.md)
- [Iso4217CurrencyHistoric](Iso4217CurrencyHistoric.md)
- [Metadata and lookups](../../../concepts/metadata-and-lookups.md)
- [Money API reference](index.md)
