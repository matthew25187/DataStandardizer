---
title: MoneyInfo Class
parent: Money
grand_parent: Packages
nav_exclude: true
---

# MoneyInfo Class

## Definition

Namespace: `DataStandardizer.Money`

Supplies the formatter and the currency formatting information used to format and
parse monetary values.

```csharp
public class MoneyInfo : IFormatProvider
```

## Remarks

`MoneyInfo` is the monetary counterpart of `CultureInfo`: it is the format
provider you pass where monetary formatting information is expected, and it
resolves both the custom formatter and the
[CurrencyFormatInfo](CurrencyFormatInfo.md) for a culture.

A `CultureInfo` may be passed wherever a `MoneyInfo` is expected. The monetary
information for that culture is resolved on your behalf, so formatting a
[Money](Money.md) value looks the same as formatting an intrinsic numeric type.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MoneyInfo()` | Culture-independent information. |
| `MoneyInfo(Iso639Part1Language)` | Information for a language. |
| `MoneyInfo(Iso639Part1Language, Iso3166Part1Alpha2Country)` | Information for a language and country. |
| `MoneyInfo(Iso639Part1Language, Iso3166Part1Alpha3Country)` | As above, with a three-letter country code. |
| `MoneyInfo(CultureInfo?)` | Information for a culture. |

A combination of a language and a country which is not a culture known to the host
falls back to the culture-independent information rather than failing.

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `CurrencyFormat` | `CurrencyFormatInfo` | The currency formatting information. |
| `IsReadOnly` | `bool` | `true` when the instance may not be modified. |

## Static members

| Member | Returns | Notes |
| --- | --- | --- |
| `CurrentMoney` | `MoneyInfo` | Read-only information for the current culture; tracks a change of culture. |
| `InvariantMoney` | `MoneyInfo` | Read-only culture-independent information. |
| `GetMoneyInfo(CultureInfo?)` | `MoneyInfo` | Read-only information for a culture. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetFormat(Type)` | `object?` | Returns a formatter for `ICustomFormatter`, or the currency format for `CurrencyFormatInfo`. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

`CurrentMoney` requires `netstandard2.0` or later, because the current culture is
unavailable on `netstandard1.0`.

## See also

- [Format money values](../how-to/format-money-values.md)
- [CurrencyFormatInfo](CurrencyFormatInfo.md)
- [Money](Money.md)
- [Money API reference](index.md)
