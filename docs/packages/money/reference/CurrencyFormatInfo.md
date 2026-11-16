---
title: CurrencyFormatInfo Class
parent: Money
grand_parent: Packages
nav_exclude: true
---

# CurrencyFormatInfo Class

## Definition

Namespace: `DataStandardizer.Money`

Defines how monetary values are formatted and parsed for a culture.

```csharp
public sealed class CurrencyFormatInfo : IFormatProvider
```

## Remarks

This type is the monetary counterpart of `NumberFormatInfo`, but it deliberately
omits a currency symbol. A [Money](Money.md) value carries its own currency, so
the symbol, the currency code and the default precision are all determined by the
value being formatted rather than by the culture formatting it. The culture
governs presentation only.

`CurrencyCode` is the exception, and exists for parsing rather than formatting: it
is what allows a currency symbol shared by several currencies, such as `$`, to be
resolved to a specific currency using the context of a culture.

The values for a culture come from the Unicode Common Locale Data Repository
(CLDR) data built into the package, and depend only on the culture's name, never
on the culture data of the operating system, so they are the same on every host
and target.

- **Presentation** (separators, group sizes, the two patterns and the negative
  sign) comes from the CLDR locale the name resolves to under CLDR inheritance.
  So de-AT has its own Austrian layout, and a culture such as pt-FR takes that of
  its CLDR parent, pt-PT. CLDR's Latin digits and currency separators are used.
  A culture that CLDR resolves to its root locale, or that it doesn't know, keeps
  the invariant presentation.
- **`CurrencyCode`** is the current currency CLDR gives the culture's region, as
  `EUR` for de-AT. A neutral culture such as `de` names no region, so it has no
  currency (`XXX`).
- **`CurrencyDecimalDigits`** is the number of ISO 4217 minor units of that
  currency, or 2 where there is none.

The data covers the locales CLDR rates at modern coverage. CLDR's minimum grouping
digits and currency spacing aren't represented, because the `NumberFormatInfo`
pattern indices can't express them.

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `CurrencyCode` | `string` | ISO 4217 code of the culture's own currency. Used when parsing a shared symbol. |
| `CurrencyDecimalDigits` | `int` | The culture's default precision. Lowest priority when formatting. |
| `CurrencyDecimalSeparator` | `string` | Separates the integral and fractional parts. |
| `CurrencyGroupSeparator` | `string` | Separates groups of digits. |
| `CurrencyGroupSizes` | `int[]` | Sizes of the digit groups; `{ 3, 2 }` for the Indian numbering system. |
| `CurrencyNegativePattern` | `int` | Placement pattern for negative values, using the `NumberFormatInfo` indices. |
| `CurrencyPositivePattern` | `int` | Placement pattern for positive values, using the `NumberFormatInfo` indices. |
| `NegativeSign` | `string` | The sign denoting a negative value; not always an ASCII hyphen. |
| `IsReadOnly` | `bool` | `true` when the instance may not be modified. |

Setting any property on a read-only instance raises an `InvalidOperationException`.

## Static properties

| Property | Type | Notes |
| --- | --- | --- |
| `CurrentInfo` | `CurrencyFormatInfo` | Read-only information for the current culture; tracks a change of culture. |
| `InvariantInfo` | `CurrencyFormatInfo` | Read-only culture-independent information. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetFormat(Type)` | `object?` | Returns the instance itself when `CurrencyFormatInfo` is requested. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, and `net10.0`.

`CurrentInfo` requires `netstandard2.0` or later, because the current culture is
unavailable on `netstandard1.1`.

## See also

- [Format money values](../how-to/format-money-values.md)
- [MoneyInfo](MoneyInfo.md)
- [Money](Money.md)
- [Money API reference](index.md)
