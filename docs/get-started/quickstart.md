---
title: Quickstart
parent: Get started
nav_order: 2
---

# Quickstart

Install a package, reference a strongly-typed value, and put it to work — end to
end in a few minutes.

This walkthrough uses **DataStandardizer.Money** to build a monetary value from
an ISO 4217 currency code.

## 1. Install the package

```shell
dotnet add package DataStandardizer.Money
```

## 2. Reference a strongly-typed currency

ISO 4217 currency codes are available as members of the
`Iso4217CurrencyCurrent` enum (in the `DataStandardizer.Money` namespace) — so
an invalid code is a compile error, not a runtime surprise.

```csharp
using DataStandardizer.Money;

// A currency from ISO 4217 — not a magic string.
Iso4217CurrencyCurrent currency = Iso4217CurrencyCurrent.NZD;   // New Zealand Dollar
```

## 3. Create a Money value and use it

The `Money` struct combines a `decimal` amount with a currency. Create one with
the static `Create` factory, then read its metadata or treat it as a `decimal`.

```csharp
using DataStandardizer.Money;

// Amount + currency, validated together.
Money price = Money.Create(19.95m, Iso4217CurrencyCurrent.NZD);

// Read the currency back, and its ISO 4217 minor-unit digit count.
Iso4217CurrencyCurrent code = price.IsoCurrencyCode;   // NZD
byte? minorUnits = price.CurrencyMinorUnits;           // 2

// Look up the currency's name via the metadata extension.
string? name = price.IsoCurrencyCode.GetCurrencyName(); // "New Zealand Dollar"

// Arithmetic carries the currency through to the result.
Money withTax = price * 1.15m;

// Money converts implicitly to decimal when you need the raw amount.
decimal raw = withTax;
```

`Create` validates the currency for you — passing an undefined or unsupported
code throws an `ArgumentException`, so you can't accidentally build a value with
a meaningless currency.

## Next steps

- Dig into the package: [Money](../packages/money/index.md).
- Browse the full API: [Money API reference](../packages/money/reference/index.md).
- Understand the design behind the types: [Concepts](../concepts/index.md).
