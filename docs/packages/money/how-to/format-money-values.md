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

Each of `C`, `H`, `I` and `N` may be followed by a precision, the number of
decimal places, as `C2` or `I3`. Where none is given, the number of minor units
of the currency is used, so a Japanese Yen amount is shown with no decimals and
a Kuwaiti Dinar amount with three.

`G` is `decimal`'s general format, and it and its precision mean what they mean
for `decimal`. The precision is a number of significant digits, the result may
be in scientific notation, and with no precision the minor units aren't applied:

```csharp
Money.Create(1234.56m, Iso4217CurrencyCurrent.USD).ToString("G2", CultureInfo.InvariantCulture);   // 1.2E+03
Money.Create(5m, Iso4217CurrencyCurrent.USD).ToString("G", CultureInfo.InvariantCulture);          // 5
```

```csharp
Money.Create(1234.5m, Iso4217CurrencyCurrent.JPY).ToString("C", enNz);   // ¥1,235
Money.Create(1234.5m, Iso4217CurrencyCurrent.KWD).ToString("C", enNz);   // KWD1,234.500
```

A format string consisting of an ISO 4217 currency code emits that code, and
asserts that it is the currency of the value. Formatting a New Zealand Dollar
value with `"USD"` raises a `FormatException` rather than producing output which
names the wrong currency.

## Rounding of the displayed amount

The amount shown is the rounded amount, as `(decimal)money` gives it, with the
value's `RoundingPrecision` and `RoundingMethod` applied. Where that amount has
more decimal places than the format displays, it's rounded again to the display
precision as `decimal` rounds it, with midpoints away from zero. The value's
`RoundingMethod` doesn't apply to that second rounding:

```csharp
var enUs = new CultureInfo("en-US");
var money = Money.Create(1.125m, Iso4217CurrencyCurrent.USD, 4, MidpointRounding.ToEven);

(decimal)money;                 // 1.125
money.ToString("C", enUs);      // $1.13, not $1.12
```

To display an amount rounded by the value's own method, give the value a
`RoundingPrecision` no greater than the precision displayed:

```csharp
Money.Create(1.125m, Iso4217CurrencyCurrent.USD, 2, MidpointRounding.ToEven).ToString("C", enUs);   // $1.12
```

## Standard and narrow symbols

ISO 4217 defines no currency symbols, so they are taken from the Unicode Common
Locale Data Repository, which publishes two forms.

The **standard** form, emitted by `C`, distinguishes currencies that share a
glyph, such as `NZ$` for the New Zealand Dollar and `CA$` for the Canadian
Dollar. In the current CLDR data no two currencies have the same standard
symbol. The **narrow** form, emitted by `H`, is the
shortest recognisable form and may be shared: `$` alone is the narrow symbol of
some thirty currencies. Use `H` only where the currency is already clear from the
context.

A standard symbol can also be the narrow symbol of other currencies. `$` is the
standard symbol of the US Dollar, `£` of the Pound Sterling, `¥` of the Japanese
Yen and `₩` of the South Korean Won, and each is the narrow symbol of other
currencies too. So `$` is unambiguous only where the value is known to be in the
`C` form, as it is for `ParseExact` with `C`. Parsing without a format still
treats it as shared. See
[Parse money values](parse-money-values.md#requiring-an-exact-format).

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

A value with no currency emits no currency token at all. Without a token, `C`,
`H`, `I` and `N` give it the separators and negative sign of the general format
`G`, which are those of the culture's number formatting, and parsing reads an
amount without a currency token with those. They differ from the currency
separators in some cultures.

```csharp
var rn = new CultureInfo("rn");   // numbers use "," for decimals, currencies "."
Money.Create(1234.5m).ToString("C", rn);                               // 1.234,50
Money.Create(1234.5m, Iso4217CurrencyCurrent.USD).ToString("I", rn);   // USD1,234.50
```

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
