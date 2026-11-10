---
title: Format money values
parent: Money
grand_parent: Packages
nav_order: 4
---

# Format money values

A `Money` value is formatted the same way an intrinsic numeric type is, through
.NET's formatting infrastructure. You can pass a format string, a format
provider, or both, and use `Money` in `string.Format` and interpolated strings.

```csharp
var money = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD);

money.ToString("C2", new CultureInfo("en-NZ"));   // NZ$1,234.50
string.Format(new CultureInfo("de-DE"), "{0:C}", money);   // 1.234,50 NZ$
```

## Format specifiers

| Specifier | Emits | Example (NZD, en-NZ) |
| --- | --- | --- |
| `C` | Currency symbol | `NZ$1,234.50` |
| `H` | Narrow currency symbol | `$1,234.50` |
| `I` | ISO 4217 currency code | `NZD1,234.50` |
| `N` | Currency name | `New Zealand Dollar1,234.50` |
| `G`, or no format | The amount alone | `1234.5` |
| *currency code* | The code, asserting the currency | `NZD1,234.50` |

Case is not significant: `C` and `c` are equivalent, as they are for `decimal`.

Each specifier may be followed by a precision, as `C2` or `I3`. Where none is
given, the number of minor units of the currency is used, so a Japanese Yen
amount is shown with no decimals and a Kuwaiti Dinar amount with three.

```csharp
Money.Create(1234.5m, Iso4217CurrencyCurrent.JPY).ToString("C", enNz);   // ¥1,235
Money.Create(1234.5m, Iso4217CurrencyCurrent.KWD).ToString("C", enNz);   // KWD1,234.500
```

A format string consisting of an ISO 4217 currency code emits that code, and
asserts that it is the currency of the value. Formatting a New Zealand Dollar
value with `"USD"` raises a `FormatException` rather than producing output which
names the wrong currency.

## Standard and narrow symbols

ISO 4217 defines no currency symbols, so they are taken from the Unicode Common
Locale Data Repository, which publishes two forms.

The **standard** form, emitted by `C`, is unambiguous: `NZ$` for the New Zealand
Dollar and `CA$` for the Canadian Dollar. The **narrow** form, emitted by `H`, is
the shortest recognisable form and may be shared: `$` alone is the narrow symbol
of some thirty currencies. Use `H` only where the currency is already clear from
the context.

Most currencies have no symbol distinct from their code, so `C` emits the code
for them. That is the correct convention rather than a gap in the data.

```csharp
Money.Create(1234.5m, Iso4217CurrencyCurrent.CHF).ToString("C", enNz);   // CHF1,234.50
```

## The culture governs presentation, not currency

A `Money` value carries its own currency, so the culture you supply determines
only how the value is presented: the separators, the group sizes, where the
currency token is placed, and the negative sign. Which currency is denoted, and
the default precision, come from the value.

```csharp
var money = Money.Create(1234567.5m, Iso4217CurrencyCurrent.NZD);
money.ToString("C", new CultureInfo("en-IN"));   // NZ$12,34,567.50
```

The amount is grouped in the Indian style because that culture was asked for, but
the value is still denoted in New Zealand Dollars. Substituting the culture's own
symbol would assert that 1,234,567.50 New Zealand Dollars *is* the same quantity
of Indian Rupees, which is false and which neither a reader nor a subsequent
parse could detect.

A value with no currency emits no currency token at all.

To express an amount *in* another currency you need a conversion, which requires
an exchange rate and is outside the scope of this package.

## Changed behaviour of `C`

{: .warning }
> In earlier versions `C` emitted the ISO 4217 currency code. It now emits the
> currency symbol, matching the meaning the specifier has for the intrinsic
> numeric types. Use `I` where you previously used `C`.

The mapping is deliberate: a caller who writes `money.ToString("C2", culture)`
should get what `decimal.ToString("C2", culture)` would give them.

## See also

- [Parse money values](parse-money-values.md)
- [Use the Money datatype](use-money-datatype.md)
- [CurrencyFormatInfo](../reference/CurrencyFormatInfo.md)
- [MoneyInfo](../reference/MoneyInfo.md)
- [Money](../reference/Money.md)
