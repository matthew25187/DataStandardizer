---
title: The Money type
parent: Money
grand_parent: Packages
nav_order: 10
---

# The Money type

A monetary value is meaningless without a currency: "47.95" is not money until
you know whether it is dollars, yen, or dinars. The `Money` type captures both
pieces together, following the *Money* pattern described in Martin Fowler's
*Patterns of Enterprise Application Architecture*.

## Amount and currency together

`Money` is a `readonly struct` that stores a `decimal` amount alongside an
optional ISO 4217 currency (`Iso4217CurrencyCurrent`). When no currency is
supplied, it reports the ISO 4217 reserved code `XXX` ("no currency") through its
`IsoCurrencyCode` property. Because the amount and currency travel as one value,
you cannot accidentally lose track of which currency an amount is denominated in
as it passes through your code.

You never construct a `Money` directly; its constructors are private. Instead you
use the `Money.Create` factory overloads, which validate the supplied currency.
A currency must be a defined enum member **and** a national or supranational
currency (or the `XTS` testing code) — otherwise `Create` throws an
`ArgumentException`. This keeps placeholder and funds-only codes out of real
monetary values.

## Currency-safe arithmetic

`Money` defines arithmetic operators (`+`, `-`, `*`, `/`) against `decimal`, so
you can compute with amounts naturally while the currency and rounding settings
carry through to the result. It also defines implicit conversions to and from
`decimal`, so a `Money` behaves like a number in expressions.

The comparison and equality operators (`==`, `!=`, `<`, `>`, `<=`, `>=`) enforce
a key invariant: comparing two `Money` values whose currencies differ throws an
`InvalidOperationException`. Two amounts in different currencies are effectively
different types, and silently comparing them would produce undefined results, so
the type refuses the operation rather than returning a misleading answer.

## Rounding as part of the value

Currencies differ in how many minor units (decimal places) they use, and
financial calculations need predictable rounding. A `Money` value can carry a
`RoundingPrecision` and an optional `RoundingMethod` (`MidpointRounding`). When
the value is converted back to a `decimal`, that rounding is applied
automatically — with a precision but no explicit method, rounding follows the
IEEE 754 default. The currency's own minor-unit count is available separately
through `CurrencyMinorUnits`. Keeping the rounding policy attached to the value
means the same rule applies everywhere the amount is used, rather than relying on
each call site to remember it.

## Why represent money this way

- **No orphaned amounts.** The currency is part of the value, so an amount can
  never drift away from the currency it belongs to.
- **Mistakes become errors, not silent bugs.** Mixing currencies in a comparison
  throws instead of returning a wrong-but-plausible result.
- **One rounding policy.** Precision and method are stored on the value and
  applied consistently on conversion to `decimal`.
- **Familiar ergonomics.** Implicit `decimal` conversions and arithmetic
  operators let `Money` slot into existing numeric code with minimal friction.

## Related

- [Use the Money data type](../how-to/use-money-datatype.md)
- [Use currency codes](../how-to/use-currency-codes.md)
- [API reference](../reference/index.md)
