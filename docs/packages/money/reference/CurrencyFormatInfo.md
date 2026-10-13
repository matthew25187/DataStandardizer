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

Values are loaded from the package's culture-specific resources, falling back to
the neutral resources where a culture defines none.

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

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

`CurrentInfo` requires `netstandard2.0` or later, because the current culture is
unavailable on `netstandard1.0`.

## See also

- [Format money values](../how-to/format-money-values.md)
- [MoneyInfo](MoneyInfo.md)
- [Money](Money.md)
- [Money API reference](index.md)
