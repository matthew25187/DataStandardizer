---
title: Use currency codes
parent: Money
grand_parent: Packages
nav_order: 2
---

# Use currency codes

ISO 4217 is implemented as two separate enums for current and historic currency
codes, so you can select a strongly-typed currency code wherever your code needs
one.

Each member of the enum includes the currency code from the standard as the name
of the enum member and the numeric code from the standard as the value of the
member.

To access an individual currency code from the current collection, you can use
it like any other enum:

```csharp
var currencyCode = Iso4217CurrencyCurrent.INR;  // Indian Rupee
```

Similarly, historic currency codes can be accessed from the relevant enum:

```csharp
var oldCurrencyCode = Iso4217CurrencyHistoric.ZWD;  // Zimbabwe Dollar
```

Both enums live in the `DataStandardizer.Money` namespace. To read the name,
minor units, and other metadata for a code, see
[Access currency metadata](access-currency-metadata.md).
