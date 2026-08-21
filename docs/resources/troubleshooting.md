---
title: Troubleshooting
parent: Resources
nav_order: 2
---

# Troubleshooting

Fixes for a few issues you might hit when adopting the *Data Standardizer*
packages, grounded in the documented behaviour of the packages.

## A type or member isn't available on my target framework

A few APIs are available only on some target frameworks. For example, the `Money`
struct in **DataStandardizer.Money** implements `IConvertible` on .NET Standard
1.3+ and modern .NET but not on the `netstandard1.0` build, and
`Iso3166Part2Subdivision` in **DataStandardizer.Geography** behaves the same way.

If a member you expect is missing, check the **"Applies to"** note on the relevant
per-package reference page, which spells out exactly which targets the member
covers, and confirm your project is building against a supported framework. The
full target matrix is on [Platform support](../overview/platform-support.md).

## I can't find a currency code I expect

ISO 4217 is implemented as two separate enums: `Iso4217CurrencyCurrent` for codes
that are currently in use, and `Iso4217CurrencyHistoric` for historic
denominations. A code you can't find on one may live on the other.

Choose the enum that matches the code set you need — current codes for live
currencies, historic codes for superseded ones. See
[Use currency codes](../packages/money/how-to/use-currency-codes.md) for
examples of both.

## A package won't resolve behind a proxy

Depending on the .NET platform you target, the packages also depend on various
system and third-party packages. These are included as static dependencies where
required and should resolve automatically, but if you are using a proxy for your
package server you may need to make sure those other packages are available
through it as well.

This is noted under software dependencies in the project README and on
[Build from source](build-from-source.md).
