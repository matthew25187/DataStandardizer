---
title: API reference
parent: Money
grand_parent: Packages
nav_order: 20
---

# DataStandardizer.Money API reference

The public types of **DataStandardizer.Money**. All types are in the
`DataStandardizer.Money` namespace.

## Structures

| Type | Description |
| --- | --- |
| [Money](Money.md) | A monetary value combining a `decimal` amount with an ISO 4217 currency. |

## Enumerations

| Type | Description |
| --- | --- |
| [Iso4217CurrencyCurrent](Iso4217CurrencyCurrent.md) | The current ISO 4217 currency &amp; funds code list (Table A.1). |
| [Iso4217CurrencyHistoric](Iso4217CurrencyHistoric.md) | Historic ISO 4217 denominations (Table A.3). |
| [CurrencySymbolKind](CurrencySymbolKind.md) | Which form of a currency symbol is required. |
| [MoneyStyles](MoneyStyles.md) | Which elements are permitted when parsing a monetary value. |

## Classes

| Type | Description |
| --- | --- |
| [Iso4217Extensions](Iso4217Extensions.md) | Extension methods that read currency code metadata. |
| [Iso4217CurrencyCodeAttribute](Iso4217CurrencyCodeAttribute.md) | Describes a currency code member with its metadata. |
| [CurrencyFormatInfo](CurrencyFormatInfo.md) | How monetary values are formatted and parsed for a culture. |
| [MoneyInfo](MoneyInfo.md) | The format provider supplying monetary formatting information. |
| [MissingCultureResourceException](MissingCultureResourceException.md) | Raised when a required culture resource is absent. |
