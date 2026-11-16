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
| `IsNationalCurrency()` | `Iso4217CurrencyCurrent` | `bool` | `true` for a national currency code; `false` for a funds code. |
| `IsSupranationalCurrency()` | `Iso4217CurrencyCurrent` | `bool` | `true` for a supranational currency code. |

## Currency symbols

ISO 4217 defines no currency symbols, so they are sourced from the Unicode Common
Locale Data Repository rather than from the currency code standard itself. Most
currencies have no symbol distinct from their code, in which case
`GetCurrencySymbol` returns `null` and the currency code should be used.

Two forms are published. The standard form distinguishes currencies that share
a glyph, such as `NZ$` for the New Zealand Dollar, though it may be another
currency's narrow form, as `$` is. The narrow form is the shortest recognisable
form and may be shared, as `$` is by some thirty currencies. Where a currency has
no distinct narrow form, the standard form is returned. See
[CurrencySymbolKind](CurrencySymbolKind.md).

## Historic codes that share a numeric code

ISO 4217 reuses numeric codes across successive denominations, so several
`Iso4217CurrencyHistoric` members can share a value, such as `BGJ`, `BGK` and
`BGL` (100). The codes that have no numeric code (`XFO`, `XFU` and `XRE`) all
share -1. Members that share a value are the same value, so these methods can't
tell them apart. For a shared value, `GetCurrencyName` and `GetCurrencySymbol`
return the data of the member withdrawn most recently:

```csharp
Iso4217CurrencyHistoric.BGJ.GetCurrencyName();  // "Lev", the name of BGL
Iso4217CurrencyHistoric.HRD.GetCurrencySymbol(CurrencySymbolKind.Narrow);  // "kn", the symbol of HRK
```

Withdrawal dates are compared by their last year, then their last month. A date
that gives only a year, such as `1989 to 1990`, counts as December of its last
year. If two members were withdrawn at the same time, the one whose alphabetic
code sorts last is used. See [Iso4217CurrencyHistoric](Iso4217CurrencyHistoric.md#remarks).

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, and `net10.0`.

## See also

- [Access currency metadata](../how-to/access-currency-metadata.md)
- [Format money values](../how-to/format-money-values.md)
- [CurrencySymbolKind](CurrencySymbolKind.md)
- [Iso4217CurrencyCurrent](Iso4217CurrencyCurrent.md)
- [Iso4217CurrencyHistoric](Iso4217CurrencyHistoric.md)
- [Iso4217CurrencyCodeAttribute](Iso4217CurrencyCodeAttribute.md)
- [Money API reference](index.md)
