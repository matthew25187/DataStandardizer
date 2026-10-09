---
title: Iso4217Extensions Class
parent: Money
grand_parent: Packages
nav_exclude: true
---

# Iso4217Extensions Class

## Definition

Namespace: `DataStandardizer.Money`

Extension methods that read the metadata attached to the ISO 4217 currency code
enums.

```csharp
public static class Iso4217Extensions
```

## Remarks

Each accessor reads the `Iso4217CurrencyCodeAttribute` applied to the enum member
and returns the requested metadata, or `null` when it is unavailable.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `GetCurrencyName()` | `Iso4217CurrencyCurrent` | `string?` | Currency name, or `null` if unavailable. |
| `GetCurrencyName()` | `Iso4217CurrencyHistoric` | `string?` | Currency name for a historic code, or `null`. |
| `GetCurrencySymbol()` | `Iso4217CurrencyCurrent` | `string?` | Standard currency symbol, or `null` if none is defined. |
| `GetCurrencySymbol(CurrencySymbolKind)` | `Iso4217CurrencyCurrent` | `string?` | Currency symbol in the requested form, or `null`. |
| `GetCurrencySymbol()` | `Iso4217CurrencyHistoric` | `string?` | Standard currency symbol for a historic code, or `null`. |
| `GetCurrencySymbol(CurrencySymbolKind)` | `Iso4217CurrencyHistoric` | `string?` | Currency symbol for a historic code in the requested form, or `null`. |
| `GetMinorUnits()` | `Iso4217CurrencyCurrent` | `byte?` | Number of minor-unit digits, or `null`. |
| `IsFundCode()` | `Iso4217CurrencyCurrent` | `bool` | `true` if the code is a funds code. |
| `IsNationalCurrency()` | `Iso4217CurrencyCurrent` | `bool` | `true` for a national currency code. |
| `IsSupranationalCurrency()` | `Iso4217CurrencyCurrent` | `bool` | `true` for a supranational currency code. |

## Currency symbols

ISO 4217 defines no currency symbols, so they are sourced from the Unicode Common
Locale Data Repository rather than from the currency code standard itself. Most
currencies have no symbol distinct from their code, in which case
`GetCurrencySymbol` returns `null` and the currency code should be used.

Two forms are published. The standard form is unambiguous, as `NZ$` for the New
Zealand Dollar; the narrow form is the shortest recognisable form and may be
shared, as `$` is by some thirty currencies. Where a currency has no distinct
narrow form, the standard form is returned. See
[CurrencySymbolKind](CurrencySymbolKind.md).

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Access currency metadata](../how-to/access-currency-metadata.md)
- [Format money values](../how-to/format-money-values.md)
- [CurrencySymbolKind](CurrencySymbolKind.md)
- [Iso4217CurrencyCurrent](Iso4217CurrencyCurrent.md)
- [Iso4217CurrencyHistoric](Iso4217CurrencyHistoric.md)
- [Iso4217CurrencyCodeAttribute](Iso4217CurrencyCodeAttribute.md)
- [Money API reference](index.md)
