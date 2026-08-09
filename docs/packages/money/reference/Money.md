---
title: Money Struct
parent: Money
grand_parent: Packages
nav_exclude: true
---

# Money Struct

## Definition

Namespace: `DataStandardizer.Money`

A monetary value that combines a `decimal` amount with an optional ISO 4217
currency and optional rounding. Instances are created through the static `Create`
factory methods.

**.NET 8.0 and later:**

```csharp
public readonly struct Money : IComparable, IComparable<Money>, IEquatable<Money>, IFormattable, IConvertible,
                               ISpanFormattable, IParsable<Money>, ISpanParsable<Money>
```

**.NET Standard 2.0:**

```csharp
public readonly struct Money : IComparable, IComparable<Money>, IEquatable<Money>, IFormattable, IConvertible
```

**.NET Standard 1.0:**

```csharp
public readonly struct Money : IComparable, IComparable<Money>, IEquatable<Money>, IFormattable
```

## Remarks

The constructors are private; build a value with one of the `Create` overloads. A
value created without a currency uses `XXX` ("no currency"). Arithmetic carries
the currency and any rounding into the result, and the equality and comparison
operators throw `InvalidOperationException` when the two operands have different
currencies.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CurrencyMinorUnits` | `byte? CurrencyMinorUnits { get; }` | ISO 4217 minor-unit digit count for the currency, or `null`. |
| `IsoCurrencyCode` | `Iso4217CurrencyCurrent IsoCurrencyCode { get; }` | The currency, or `XXX` when none was supplied. |
| `RoundingMethod` | `MidpointRounding? RoundingMethod { get; }` | Rounding method in effect, or `null`. |
| `RoundingPrecision` | `int? RoundingPrecision { get; }` | Rounding precision in effect, or `null`. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CompareTo(Money other)` | `int` | |
| `CompareTo(object obj)` | `int` | |
| `Create(decimal amount)` | `Money` | Uses the default currency `XXX` ("no currency"). |
| `Create(decimal amount, Iso4217CurrencyCurrent currency)` | `Money` | Throws `ArgumentException` if `currency` is undefined or is not a national/supranational/`XTS` code. |
| `Create(decimal amount, Iso4217CurrencyCurrent currency, int roundingPrecision)` | `Money` | Adds rounding precision (IEEE 754 default method). |
| `Create(decimal amount, Iso4217CurrencyCurrent currency, int roundingPrecision, MidpointRounding roundingMethod)` | `Money` | Adds rounding precision and an explicit rounding method. |
| `Equals(Money other)` | `bool` | |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |
| `Parse(string s)` | `Money` | Throws `FormatException` if `s` is not a valid value. |
| `Parse(string s, IFormatProvider? provider)` | `Money` | Culture-specific parse. |
| `Parse(string s, MoneyStyles styles, IFormatProvider? provider)` | `Money` | Permits only the specified elements. |
| `ParseExact(string s, string format, IFormatProvider? provider)` | `Money` | Requires `s` to be in the given format. |
| `ParseExact(string s, string[] formats, IFormatProvider? provider, MoneyStyles styles)` | `Money` | Requires `s` to be in one of the given formats. |
| `ToString()` | `string` | The amount alone. |
| `ToString(IFormatProvider? provider)` | `string` | |
| `ToString(string? format)` | `string` | |
| `ToString(string? format, IFormatProvider? formatProvider)` | `string` | `IFormattable` implementation (a normal public method). |
| `TryParse(string? s, out Money result)` | `bool` | Returns `false` instead of throwing on failure. |
| `TryParse(string? s, IFormatProvider? provider, out Money result)` | `bool` | Culture-specific try-parse. |
| `TryParse(string? s, MoneyStyles styles, IFormatProvider? provider, out Money result)` | `bool` | Permits only the specified elements. |
| `TryParseExact(string? s, string[]? formats, IFormatProvider? provider, MoneyStyles styles, out Money result)` | `bool` | Requires `s` to be in one of the given formats. |

On .NET 8.0 and later, `TryFormat`, `Parse(ReadOnlySpan<char>, IFormatProvider?)`
and `TryParse(ReadOnlySpan<char>, IFormatProvider?, out Money)` are also
available, satisfying `ISpanFormattable` and `ISpanParsable<Money>`.

### Formatting and parsing

`ToString` supports the specifiers `C` (currency symbol), `H` (narrow currency
symbol), `I` (ISO 4217 currency code), `N` (currency name) and `G` (the amount
alone), each optionally followed by a precision. Where no precision is given the
number of minor units of the currency is used.

The culture supplied governs presentation only. The currency denoted, and the
default precision, come from the value, so formatting a value of one currency
under the culture of another applies that culture's separators and grouping but
never substitutes its currency.

{: .warning }
> In earlier versions `C` emitted the ISO 4217 currency code. It now emits the
> currency symbol, matching the meaning the specifier has for the intrinsic
> numeric types. Use `I` where you previously used `C`.

See [Format money values](../how-to/format-money-values.md) and
[Parse money values](../how-to/parse-money-values.md).

### Explicit implementation

`Money` implements `IConvertible` explicitly; each member delegates to the
underlying `decimal` and is callable only through an `IConvertible` reference.
*(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
| `GetTypeCode()` | `TypeCode` | |
| `ToBoolean(IFormatProvider provider)` | `bool` | |
| `ToByte(IFormatProvider provider)` | `byte` | |
| `ToChar(IFormatProvider provider)` | `char` | |
| `ToDateTime(IFormatProvider provider)` | `DateTime` | |
| `ToDecimal(IFormatProvider provider)` | `decimal` | |
| `ToDouble(IFormatProvider provider)` | `double` | |
| `ToInt16(IFormatProvider provider)` | `short` | |
| `ToInt32(IFormatProvider provider)` | `int` | |
| `ToInt64(IFormatProvider provider)` | `long` | |
| `ToSByte(IFormatProvider provider)` | `sbyte` | |
| `ToSingle(IFormatProvider provider)` | `float` | |
| `ToString(IFormatProvider provider)` | `string` | |
| `ToType(Type conversionType, IFormatProvider provider)` | `object` | |
| `ToUInt16(IFormatProvider provider)` | `ushort` | |
| `ToUInt32(IFormatProvider provider)` | `uint` | |
| `ToUInt64(IFormatProvider provider)` | `ulong` | |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Implicit | `implicit operator decimal(Money)` | Applies any configured rounding. |
| Implicit | `implicit operator Money(decimal)` | Wraps a `decimal` (no currency). |
| Arithmetic | `operator +`, `-`, `*`, `/` `(Money, decimal)` | Carry currency and rounding into the result. |
| Equality | `operator ==`, `!=` `(Money, Money)` | Throw `InvalidOperationException` if currencies differ. |
| Comparison | `operator <`, `>`, `<=`, `>=` `(Money, Money)` | Throw `InvalidOperationException` if currencies differ. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. `IConvertible`
is available on the `netstandard2.0`, `net8.0`, and `net10.0` builds, but **not**
on `netstandard1.0`.

## See also

- [Use the Money data type](../how-to/use-money-datatype.md)
- [The Money type](../concepts/money-type.md)
- [Iso4217CurrencyCurrent](Iso4217CurrencyCurrent.md)
- [Iso4217Extensions](Iso4217Extensions.md)
- [Money API reference](index.md)
