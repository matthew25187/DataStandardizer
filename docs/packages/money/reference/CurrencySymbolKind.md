---
title: CurrencySymbolKind Enumeration
parent: Money
grand_parent: Packages
nav_exclude: true
---

# CurrencySymbolKind Enumeration

## Definition

Namespace: `DataStandardizer.Money`

Specifies which form of a currency symbol is required.

```csharp
public enum CurrencySymbolKind
```

## Remarks

ISO 4217 does not define currency symbols, so the symbols exposed by this library
are sourced from the Unicode Common Locale Data Repository (CLDR), which publishes
two forms for many currencies.

## Fields

| Member | Value | Notes |
| --- | --- | --- |
| `Standard` | `0` | The standard symbol; unambiguous in context. |
| `Narrow` | `1` | The shortest recognisable form, which may be shared by several currencies. |

Where a glyph is shared by several currencies, the standard form distinguishes
between them, as `NZ$` for the New Zealand Dollar and `CA$` for the Canadian
Dollar. The narrow form of both is `$`, which some thirty currencies use, so it
should only be used where the currency is already clear from the context.

Where a currency has no distinct narrow form, the standard form is returned.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Access currency metadata](../how-to/access-currency-metadata.md)
- [Format money values](../how-to/format-money-values.md)
- [Iso4217Extensions](Iso4217Extensions.md)
- [Money API reference](index.md)
