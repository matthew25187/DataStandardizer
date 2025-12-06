# DataStandardizer.Money package

Support for money-related data standards.

## ISO 4217

### Features

- Supports **ISO 4217, *Codes for the representation of currencies and funds – Current currency & funds code list***
- Supports **ISO 4217, *Codes for the representation of currencies and funds – List of codes for historic denominations of currencies & funds***

### How to...

- [Use currency codes](iso4217-standard/use-currency-codes.md)
- [Access metadata](iso4217-standard/access-currency-metadata.md)

## Money data type

### Features

- Utility

    Combines the essential elements of a monetary value: the amount and a currency code.

- Ease of use

    Works like a `decimal` value so you can use an instance of `Money` in arithmetic or expressions with non-`Money` values.

- Rounding

    Built-in support for rounding so when a `Money` object is converted back to a `decimal`, the rounding is automatically applied using the chosen rounding method.

### How to...

- [Use money data type](use-money-datatype.md)