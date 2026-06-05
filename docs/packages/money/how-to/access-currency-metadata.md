---
title: Access currency metadata
parent: Money
grand_parent: Packages
nav_order: 3
---

# Access currency metadata

Each member of the `Iso4217CurrencyCurrent` and `Iso4217CurrencyHistoric` enums
has associated metadata that you retrieve using extension methods on an
individual currency code.

The extension methods are in the `DataStandardizer.Money` namespace.

## Currency name

Every currency has a name as defined by the ISO 4217 standard. You can retrieve
this name using an extension method on a currency code:

```csharp
var currencyName = Iso4217CurrencyCurrent.ILS.GetCurrencyName();
```

Names can also be retrieved for historic currencies:

```csharp
var oldCurrencyName = Iso4217CurrencyHistoric.ZWD.GetCurrencyName();
```

## Minor units

Minor units is the number of digits for the value of the sub-units in a monetary
value for a specific currency. This is typically expressed as the fractional
component of a decimal number. The number of minor units typically ranges from 0
to 3 digits.

You can get the number of minor units for a currency using an extension method
on the currency code:

```csharp
var minorUnits = Iso4217CurrencyCurrent.IQD.GetMinorUnits();
```

## Funds code

The currency code enums include funds codes alongside regular currency codes.
You can tell whether a code in the currency enum is a funds code by using an
extension method:

```csharp
var isFundsCode = Iso4217CurrencyCurrent.BOV.IsFundCode();
```

## National & supranational currency codes

Currency codes in use by countries or groups of countries can be categorized as
national or supranational. To check if a currency code is a national currency
code, use the provided extension method:

```csharp
var isNationalCurrencyCode = Iso4217CurrencyCurrent.DJF.IsNationalCurrency();
```

If, however, a currency code is in use by a supranational collective, you can
check this status with a different extension method:

```csharp
var isSupranationalCurrencyCode = Iso4217CurrencyCurrent.XAF.IsSupranationalCurrency();
```
