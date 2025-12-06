# Use Money data type

The `Money` data type is a practical implementation of ISO 4217 to manage monetary values.  It combines a currency code with an amount in a single value type object with support for rounding and is compatible with the `decimal` data type for arithmetic operations.

## Create a monetary value

Factory methods have been provided to create monetary values.  To create a value with the default currency code:

    var monetaryValue = Money.Create(101.24m);

The default currency code of `XXX` has been reserved by ISO 4217 to mean "no currency" applies.

A value can be created with a specific currency code by specifying it with the amount:

    var monetaryValue = Money.Create(47.95m, Iso4217CurrencyCurrent.CAD);

You can apply rounding to the numeric output of a monetary value by specifying the rounding precision when creating the value:

    var monetaryValue = Money.Create(86.37m, Iso4217CurrencyCurrent.GBP, 2);

A value created this way will use a rounding method that is compatible with IEEE Standard 754, section 4.

A specific method of rounding can be specified with the rounding precision when creating the value:

    var monetaryValue = Money.Create(37.49m, Iso4217CurrencyCurrent.AUD, 2, MidpointRounding.ToZero);

## Access value details

With a monetary value created, you can determine the number of digits specified by the ISO 4217 standard for the currency's sub units.

    var minorUnits = monetaryValue.CurrencyMinorUnits;

You can also access the currency code that was specified when the value was created.

    var currencyCode = monetaryValue.IsoCurrencyCode;

Information about the rounding in effect on a monetary value is also available.

    var roundingPrecision = monetaryValue.RoundingPrecision;
    var roundingMethod = monetaryValue.RoundingMethod;

## Convert strings to monetary values

A string that contains a serialized monetary value can be converted to an actual monetary value.  If you expect the string to contain a correctly formatted value, you can convert the string directly:

    var monetaryValue = Money.Parse("USD63.50");

If it is possible that the string may not contain a valid value, you can attempt the conversion without error if the conversion fails:
```
if (Money.TryParse("EUR103.79", out var monetaryValue))
{
    // process money value
}
```

## Arithmetic

All standard arithmetic operators are implemented on the `Money` data type.  This allows for operations combining `Money` values and decimal values to produce sensible results.

Standard cast operators allow for conversion of numbers to and from `Money` values.

    var monetaryValue = (Money)55.67m;
    var amount = (decimal)monetaryValue;

Arithmetic operators support the calculation of a result combining `Money` and decimal values.  For example,

    var monetaryValue = Money.Create(30m);
    var result = monetaryValue - 10m;   // result = 20

Comparison operators are also supported.  For example,
```
var monetaryValue = Money.Create(25m);
if (monetaryValue > 10m)
{
    // process monetary values greater than 10
}
```

When performing arithmetic operations on two monetary values that have different currency codes an exception will be thrown as this type of operation is not supported.  The result of an operation on two monetary values of different currencies would be undefined as these values are effectively of different types and therefore incomparable.