---
title: Iso639Part3Language Struct
parent: Language
grand_parent: Packages
nav_exclude: true
---

# Iso639Part3Language Struct

## Definition

Namespace: `DataStandardizer.Language`

ISO 639 Part 3 alpha-3 codes for the comprehensive coverage of languages (for
example `eng`). ISO 639 does not assign numeric values to its language codes, so
this type is not a C# enum: it is a `readonly struct` implementing
`DataStandardizer.Core.IStringEnum`, with each language code exposed as a
`public static readonly` member whose underlying value is the string code.

```csharp
public readonly partial struct Iso639Part3Language : DataStandardizer.Core.IStringEnum, IEquatable<Iso639Part3Language>
```

## Remarks

`IStringEnum` derives from `IComparable` on all targets and additionally from
`IConvertible` on the `netstandard2.0`, `net8.0`, and `net10.0` targets, but not on
`netstandard1.0`. The constructor is private; obtain a value from a static code
member or the explicit `string` conversion. Per-member metadata is read through the
[Iso639Extensions](Iso639Extensions.md) accessors. See
[Understanding the ISO 639 parts](../concepts/iso639-parts.md).

## Fields

There are roughly 7,900 codes, listed by initial letter:

- [A-F](Iso639Part3Language.A-F.md) — 1987 codes
- [G-L](Iso639Part3Language.G-L.md) — 1840 codes
- [M-R](Iso639Part3Language.M-R.md) — 1866 codes
- [S-Z](Iso639Part3Language.S-Z.md) — 2230 codes

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CompareTo(object obj)` | `int` | Throws `ArgumentNullException`/`ArgumentException` for a null or mismatched argument. |
| `Equals(Iso639Part3Language other)` | `bool` | |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |
| `GetTypeCode()` | `TypeCode` | *(netstandard2.0+/.NET)* `IConvertible` member. |
| `ToString()` | `string` | Override. Returns the member name, or the underlying code. |
| `ToString(IFormatProvider provider)` | `string` | *(netstandard2.0+/.NET)* Marked `[Obsolete]`; the provider is unused — use `ToString()`. |

### Explicit implementation

The struct implements `IConvertible` explicitly; each member delegates to the
underlying `string` and is callable only through an `IConvertible` reference.
*(netstandard2.0+/.NET)*

| Method | Returns | Notes |
| --- | --- | --- |
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
| `ToType(Type conversionType, IFormatProvider provider)` | `object` | |
| `ToUInt16(IFormatProvider provider)` | `ushort` | |
| `ToUInt32(IFormatProvider provider)` | `uint` | |
| `ToUInt64(IFormatProvider provider)` | `ulong` | |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Explicit | `explicit operator Iso639Part3Language(string)` | Wraps a code string. Throws `ArgumentNullException` for `null`. |
| Implicit | `implicit operator string(Iso639Part3Language)` | Unwraps to the underlying code string. |
| Equality | `operator ==`, `!=` `(Iso639Part3Language, Iso639Part3Language)` | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. `IConvertible`
is available on the `netstandard2.0`, `net8.0`, and `net10.0` builds, but **not** on
`netstandard1.0`.

## See also

- [Iso639Extensions](Iso639Extensions.md)
- [Iso639Part1Language](Iso639Part1Language.md)
- [Use language codes](../how-to/use-language-codes.md)
- [Understanding the ISO 639 parts](../concepts/iso639-parts.md)
- [Language API reference](index.md)
