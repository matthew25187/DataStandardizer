---
title: Parse money values
parent: Money
grand_parent: Packages
nav_order: 5
---

# Parse money values

`Money.Parse` and `Money.TryParse` read a monetary value from a string,
identifying its currency from an ISO 4217 currency code or from a currency
symbol.

```csharp
Money.Parse("NZD1,234.50", new CultureInfo("en-NZ"));   // NZD 1234.50
Money.TryParse("₹1,234.50", null, out var rupees);      // INR 1234.50
```

## How the currency is identified

A currency **code** is never ambiguous, so it is always accepted.

A currency **symbol** is accepted where the currency it denotes can be
established with certainty. Most symbols denote exactly one currency and are
resolved without any culture at all: `₹` is the Indian Rupee, `zł` the Polish
Złoty, `NZ$` the New Zealand Dollar.

A few symbols are shared. `$` alone is used by some thirty currencies, and `kr`
by four. Such a symbol is resolved from the currency of the culture you supply,
and only when you ask for that with `MoneyStyles.AllowAmbiguousCurrencySymbol`.

```csharp
var enNz = MoneyInfo.GetMoneyInfo(new CultureInfo("en-NZ"));

Money.TryParse("$100", MoneyStyles.Any, enNz, out var nzd);        // true, NZD
Money.TryParse("$100", MoneyStyles.Currency, enNz, out _);         // false
Money.TryParse("$100", MoneyStyles.Any, null, out _);              // false
```

Where the currency cannot be determined, parsing **fails**. It never selects one
on the value's behalf, and it never resolves a shared symbol from the culture of
the current thread, which says nothing about where a value came from. A failure
is recoverable; a value silently attributed to the wrong currency is not.

## Restricting what is accepted

`MoneyStyles` determines which elements are permitted, as `NumberStyles` does for
the intrinsic numeric types. This matters when you are reading input of a known
shape and want anything else rejected rather than interpreted.

| Member | Permits |
| --- | --- |
| `AllowLeadingWhite`, `AllowTrailingWhite` | Surrounding white space |
| `AllowLeadingSign`, `AllowTrailingSign` | A negative sign on either side |
| `AllowParentheses` | A negative value in parentheses |
| `AllowThousands` | Group separators |
| `AllowDecimalPoint` | A decimal separator |
| `AllowCurrencyCode` | An ISO 4217 currency code |
| `AllowCurrencySymbol` | A symbol denoting exactly one currency |
| `AllowAmbiguousCurrencySymbol` | A symbol shared by several currencies |

`Number` permits the elements of a plain number, `Currency` adds parentheses and
an unambiguous currency, and `Any` adds shared symbols. `Currency` is the default
and deliberately excludes `AllowAmbiguousCurrencySymbol`.

To accept only currency codes:

```csharp
const MoneyStyles codeOnly = MoneyStyles.Number | MoneyStyles.AllowCurrencyCode;

Money.TryParse("NZD1,234.50", codeOnly, enNz, out var value);   // true
Money.TryParse("₹100", codeOnly, enNz, out _);                  // false
```

## Requiring an exact format

`ParseExact` and `TryParseExact` accept a value only where formatting the result
reproduces it under one of the formats you name. Use them when a value must be in
a particular shape rather than merely parseable.

```csharp
var codeText = money.ToString("I", enNz);    // NZD1,234.50
var symbolText = money.ToString("C", enNz);  // NZ$1,234.50

Money.TryParseExact(codeText, new[] { "I" }, enNz, MoneyStyles.Currency, out _);    // true
Money.TryParseExact(symbolText, new[] { "I" }, enNz, MoneyStyles.Currency, out _);  // false
```

This gives the round trip its strongest form. A feed which is expected to carry
currency codes fails loudly if it begins carrying symbols, rather than continuing
to parse by luck.

## Round-tripping

Formatting with `I` and parsing the result always returns an equivalent value.

```csharp
var original = Money.Create(1234.56m, Iso4217CurrencyCurrent.NZD);
var restored = Money.Parse(original.ToString("I", enNz), enNz);
```

Formatting with `C` also round-trips wherever the currency's symbol is
unambiguous. Where it is shared, as `$` is, parsing the result needs both the
culture and `MoneyStyles.AllowAmbiguousCurrencySymbol`.

Note that a value carrying more precision than its currency has minor units is
rounded when formatted, so `1234.567` in a two-minor-unit currency round-trips as
`1234.57`.

## See also

- [Format money values](format-money-values.md)
- [Use the Money datatype](use-money-datatype.md)
- [MoneyStyles](../reference/MoneyStyles.md)
- [Money](../reference/Money.md)
