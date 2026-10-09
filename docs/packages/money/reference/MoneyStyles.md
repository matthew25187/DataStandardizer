---
title: MoneyStyles Enumeration
parent: Money
grand_parent: Packages
nav_exclude: true
---

# MoneyStyles Enumeration

## Definition

Namespace: `DataStandardizer.Money`

Determines which elements are permitted in a string being parsed as a monetary
value.

```csharp
[Flags]
public enum MoneyStyles : uint
```

## Remarks

These styles serve the same purpose for [Money](Money.md) as `NumberStyles` does
for the intrinsic numeric types: they let a caller accept only the elements they
expect, so that input which happens to be well formed but is not of the expected
shape is rejected rather than parsed.

## Fields

| Member | Value | Permits |
| --- | --- | --- |
| `None` | `0` | Decimal digits only. |
| `AllowLeadingWhite` | `1` | Leading white space. |
| `AllowTrailingWhite` | `2` | Trailing white space. |
| `AllowLeadingSign` | `4` | A leading negative sign. |
| `AllowTrailingSign` | `8` | A trailing negative sign. |
| `AllowParentheses` | `16` | A negative value enclosed in parentheses. |
| `AllowThousands` | `32` | Group separators. |
| `AllowDecimalPoint` | `64` | A decimal separator. |
| `AllowCurrencyCode` | `128` | An ISO 4217 currency code. |
| `AllowCurrencySymbol` | `256` | A currency symbol denoting exactly one currency. |
| `AllowAmbiguousCurrencySymbol` | `512` | A currency symbol shared by several currencies. |

## Composite fields

| Member | Comprises |
| --- | --- |
| `Number` | White space, signs, group separators and a decimal separator. |
| `Currency` | `Number`, parentheses, a currency code and an unambiguous currency symbol. |
| `Any` | `Currency` and a shared currency symbol. |

`Currency` is the default used by the parse methods which take no styles, and
deliberately excludes `AllowAmbiguousCurrencySymbol`.

## Remarks on ambiguous symbols

A handful of currency symbols are shared: `$` alone is used by some thirty
currencies. Resolving one requires the context of a culture, and a caller must opt
in to it, because the currency of an ambient culture is rarely a safe assumption
to make about the origin of a monetary value. Where the culture supplied does not
resolve the symbol, parsing fails rather than choosing a currency.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Parse money values](../how-to/parse-money-values.md)
- [Money](Money.md)
- [Money API reference](index.md)
