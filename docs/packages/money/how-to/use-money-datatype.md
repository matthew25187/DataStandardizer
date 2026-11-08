---
title: Use the Money data type
parent: Money
grand_parent: Packages
nav_order: 1
---

# Use the Money data type

The `Money` data type is a practical implementation of ISO 4217 for managing
monetary values; it combines a currency code with an amount in a single value
type, supports rounding, and is compatible with the `decimal` data type for
arithmetic operations.

## Create a monetary value

Factory methods are provided to create monetary values. To create a value with
the default currency code:

```csharp
var monetaryValue = Money.Create(101.24m);
```

The default currency code of `XXX` has been reserved by ISO 4217 to mean "no
currency" applies.

A value can be created with a specific currency code by specifying it with the
amount:

```csharp
var monetaryValue = Money.Create(47.95m, Iso4217CurrencyCurrent.CAD);
```

You can apply rounding to the numeric output of a monetary value by specifying
the rounding precision when creating the value:

```csharp
var monetaryValue = Money.Create(86.37m, Iso4217CurrencyCurrent.GBP, 2);
```

A value created this way uses a rounding method that is compatible with IEEE
Standard 754, section 4.

A specific method of rounding can be specified with the rounding precision when
creating the value:

```csharp
var monetaryValue = Money.Create(37.49m, Iso4217CurrencyCurrent.AUD, 2, MidpointRounding.ToZero);
```

## Access value details

With a monetary value created, you can determine the number of digits specified
by the ISO 4217 standard for the currency's sub-units:

```csharp
var minorUnits = monetaryValue.CurrencyMinorUnits;
```

You can also access the currency code that was specified when the value was
created:

```csharp
var currencyCode = monetaryValue.IsoCurrencyCode;
```

Information about the rounding in effect on a monetary value is also available:

```csharp
var roundingPrecision = monetaryValue.RoundingPrecision;
var roundingMethod = monetaryValue.RoundingMethod;
```

## Convert strings to monetary values

A string that contains a serialized monetary value can be converted to an actual
monetary value. If you expect the string to contain a correctly formatted value,
you can convert the string directly:

```csharp
var monetaryValue = Money.Parse("USD63.50");
```

If it is possible that the string may not contain a valid value, you can attempt
the conversion without error if the conversion fails:

```csharp
if (Money.TryParse("EUR103.79", out var monetaryValue))
{
    // process money value
}
```

## Arithmetic

All standard arithmetic operators are implemented on the `Money` data type. This
allows for operations combining `Money` values and decimal values to produce
sensible results.

Standard cast operators allow for conversion of numbers to and from `Money`
values:

```csharp
var monetaryValue = (Money)55.67m;
var amount = (decimal)monetaryValue;
```

A cast to `double` or `float` also gives the amount. A cast to an integer type
gives the amount in minor units instead; see
[Amounts in minor units](#amounts-in-minor-units).

Arithmetic operators support the calculation of a result combining `Money` and
decimal values. For example:

```csharp
var monetaryValue = Money.Create(30m);
var result = monetaryValue - 10m;   // result = 20
```

Comparison operators are also supported. For example:

```csharp
var monetaryValue = Money.Create(25m);
if (monetaryValue > 10m)
{
    // process monetary values greater than 10
}
```

When performing arithmetic operations on two monetary values that have different
currency codes, an exception is thrown as this type of operation is not
supported. The result of an operation on two monetary values of different
currencies would be undefined, as these values are effectively of different
types and therefore incomparable. See [The Money type](../concepts/money-type.md)
for more on this design.

## Amounts in minor units

Payment systems and databases often store an amount as a whole number of the
currency's minor units, such as cents. A cast from `Money` to `long`, `int`,
`short` or `sbyte` gives the amount in the minor units ISO 4217 defines for its
currency:

```csharp
var dollars = Money.Create(12.34m, Iso4217CurrencyCurrent.USD);
var cents = (long)dollars;          // 1234

var yen = Money.Create(500m, Iso4217CurrencyCurrent.JPY);
var yenUnits = (long)yen;           // 500, the yen has no minor units

var dinars = Money.Create(1.234m, Iso4217CurrencyCurrent.BHD);
var fils = (long)dinars;            // 1234, the dinar has three minor-unit digits
```

The cast uses the rounded amount, so a value's rounding is applied first:

```csharp
var rounded = Money.Create(1.005m, Iso4217CurrencyCurrent.USD, 2, MidpointRounding.AwayFromZero);
var roundedCents = (long)rounded;   // 101
```

The cast never truncates or rounds the amount itself. It throws when there's no
exact answer:

- `InvalidCastException` when the currency has no minor units. That's `XXX`,
  including a value created without a currency and `default(Money)`, and `XTS`.
- `InvalidCastException` when the amount isn't a whole number of minor units,
  such as 1.005 USD without rounding. This is the exception whatever the size
  of the amount.
- `OverflowException` when the amount in minor units doesn't fit the target
  type, such as 21474836.48 USD cast to `int`.

To truncate the amount to an integer, which is what the integer casts used to
do, convert through `decimal`:

```csharp
var wholeDollars = (long)(decimal)dollars;   // 12
```

There's no cast from an integer to `Money`, because a cast has nowhere to carry
the currency. Create the value from a `decimal` amount instead.

### Migrating from earlier versions

{: .warning }
> In earlier versions an integer cast such as `(long)money` converted through
> `decimal` and truncated the amount, so 12.34 USD gave `12`. The same code
> recompiled against this version gives `1234`. Code compiled against an
> earlier version keeps the old behaviour until it's rebuilt.

Review each integer cast of a `Money` value:

- Where you want the amount in minor units, keep the cast, and handle the
  exceptions it can now throw.
- Where you want the truncated amount, change `(long)money` to
  `(long)(decimal)money`.

A cast to an unsigned integer type (`ulong`, `uint`, `ushort` or `byte`) no
longer compiles, and fails with error CS0619, so it can't quietly keep
truncating while the signed casts change meaning. Casts to `char` and `nuint`
fail the same way. Write `(uint)(decimal)money` to keep the old result.
`(nint)money` now gives the amount in minor units, through the `int` cast.

These conversions are unchanged: `decimal d = money`, `(decimal)money`,
`(double)money`, `(float)money`, the arithmetic and comparison operators, and
`Convert.ToInt64(money)` and its siblings. None of them scale the amount.
`Convert.ToInt64(money)` and the `IConvertible` integer members round a fraction
to the nearest integer, with ties to even, rather than truncating it as
`(long)(decimal)money` does.

The `IConvertible` members, which `Convert.ToDecimal((object)money)` and its
siblings call, don't scale the amount either, but they now convert it with the
rounding the value carries, as `(decimal)money` does. Before, they converted
the amount before rounding. `Convert.ToString(money)` and
`IConvertible.ToString` also apply the rounding now, as `money.ToString()` does.
No member returns the amount before rounding. If you need it, create the value
without a rounding precision, with `Money.Create(amount, currency)`, and round
where you need to.

`Money.Create` now throws `ArgumentOutOfRangeException` for a rounding precision
outside 0 to 28. Before, it accepted one, and converting the value to `decimal`
failed later.
