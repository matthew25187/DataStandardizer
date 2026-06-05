---
title: Iso639Part1Language Struct
parent: Language
grand_parent: Packages
nav_exclude: true
---

# Iso639Part1Language Struct

## Definition

Namespace: `DataStandardizer.Language`

ISO 639 Part 1 alpha-2 language codes (for example `en`). ISO 639 does not assign
numeric values to its language codes, so this type is not a C# enum: it is a
`readonly struct` implementing `DataStandardizer.Core.IStringEnum`, with each
language code exposed as a `public static readonly` member whose underlying value
is the string code from the standard. It behaves much like an enum — you access
codes as static members and compare them with `==` — but it wraps a `string`.

```csharp
public readonly struct Iso639Part1Language : DataStandardizer.Core.IStringEnum, IEquatable<Iso639Part1Language>
```

## Remarks

`IStringEnum` derives from `IComparable` on all targets and additionally from
`IConvertible` on the `netstandard2.0`, `net8.0`, and `net10.0` targets, but not on
`netstandard1.0`. The constructor is private; obtain a value from a static code
member or the explicit `string` conversion. Per-member metadata is read through the
[Iso639Extensions](Iso639Extensions.md) accessors. See
[Understanding the ISO 639 parts](../concepts/iso639-parts.md).

## Fields

| Field | English name |
| --- | --- |
| `aa` | Afar |
| `ab` | Abkhazian |
| `af` | Afrikaans |
| `ak` | Akan |
| `sq` | Albanian |
| `am` | Amharic |
| `ar` | Arabic |
| `an` | Aragonese |
| `hy` | Armenian |
| `as` | Assamese |
| `av` | Avaric |
| `ae` | Avestan |
| `ay` | Aymara |
| `az` | Azerbaijani |
| `ba` | Bashkir |
| `bm` | Bambara |
| `eu` | Basque |
| `be` | Belarusian |
| `bn` | Bengali |
| `bi` | Bislama |
| `bs` | Bosnian |
| `br` | Breton |
| `bg` | Bulgarian |
| `my` | Burmese |
| `ca` |  |
| `ch` | Chamorro |
| `ce` | Chechen |
| `zh` | Chinese |
| `cu` |  |
| `cv` | Chuvash |
| `kw` | Cornish |
| `co` | Corsican |
| `cr` | Cree |
| `cs` | Czech |
| `da` | Danish |
| `dv` |  |
| `nl` |  |
| `dz` | Dzongkha |
| `en` | English |
| `eo` | Esperanto |
| `et` | Estonian |
| `ee` | Ewe |
| `fo` | Faroese |
| `fj` | Fijian |
| `fi` | Finnish |
| `fr` | French |
| `fy` | Western Frisian |
| `ff` | Fulah |
| `ka` | Georgian |
| `de` | German |
| `gd` |  |
| `ga` | Irish |
| `gl` | Galician |
| `gv` |  |
| `el` | Greek, Modern (1453-) |
| `gn` | Guarani |
| `gu` | Gujarati |
| `ht` |  |
| `ha` | Hausa |
| `he` | Hebrew |
| `hz` | Herero |
| `hi` | Hindi |
| `ho` | Hiri Motu |
| `hr` | Croatian |
| `hu` | Hungarian |
| `ig` | Igbo |
| `is` | Icelandic |
| `io` | Ido |
| `ii` |  |
| `iu` | Inuktitut |
| `ie` |  |
| `ia` | Interlingua (International Auxiliary Language Association) |
| `id` | Indonesian |
| `ik` | Inupiaq |
| `it` | Italian |
| `jv` | Javanese |
| `ja` | Japanese |
| `kl` |  |
| `kn` | Kannada |
| `ks` | Kashmiri |
| `kr` | Kanuri |
| `kk` | Kazakh |
| `km` | Central Khmer |
| `ki` |  |
| `rw` | Kinyarwanda |
| `ky` |  |
| `kv` | Komi |
| `kg` | Kongo |
| `ko` | Korean |
| `kj` |  |
| `ku` | Kurdish |
| `lo` | Lao |
| `la` | Latin |
| `lv` | Latvian |
| `li` |  |
| `ln` | Lingala |
| `lt` | Lithuanian |
| `lb` |  |
| `lu` | Luba-Katanga |
| `lg` | Ganda |
| `mk` | Macedonian |
| `mh` | Marshallese |
| `ml` | Malayalam |
| `mi` | Maori |
| `mr` | Marathi |
| `ms` | Malay |
| `mg` | Malagasy |
| `mt` | Maltese |
| `mn` | Mongolian |
| `na` | Nauru |
| `nv` |  |
| `nr` |  |
| `nd` |  |
| `ng` | Ndonga |
| `ne` | Nepali |
| `nn` |  |
| `nb` |  |
| `no` | Norwegian |
| `ny` |  |
| `oc` | Occitan (post 1500) |
| `oj` | Ojibwa |
| `or` | Oriya |
| `om` | Oromo |
| `os` |  |
| `pa` |  |
| `fa` | Persian |
| `pi` | Pali |
| `pl` | Polish |
| `pt` | Portuguese |
| `ps` |  |
| `qu` | Quechua |
| `rm` | Romansh |
| `ro` |  |
| `rn` | Rundi |
| `ru` | Russian |
| `sg` | Sango |
| `sa` | Sanskrit |
| `si` |  |
| `sk` | Slovak |
| `sl` | Slovenian |
| `se` | Northern Sami |
| `sm` | Samoan |
| `sn` | Shona |
| `sd` | Sindhi |
| `so` | Somali |
| `st` | Sotho, Southern |
| `es` |  |
| `sc` | Sardinian |
| `sr` | Serbian |
| `ss` | Swati |
| `su` | Sundanese |
| `sw` | Swahili |
| `sv` | Swedish |
| `ty` | Tahitian |
| `ta` | Tamil |
| `tt` | Tatar |
| `te` | Telugu |
| `tg` | Tajik |
| `tl` | Tagalog |
| `th` | Thai |
| `bo` | Tibetan |
| `ti` | Tigrinya |
| `to` | Tonga (Tonga Islands) |
| `tn` | Tswana |
| `ts` | Tsonga |
| `tk` | Turkmen |
| `tr` | Turkish |
| `tw` | Twi |
| `ug` |  |
| `uk` | Ukrainian |
| `ur` | Urdu |
| `uz` | Uzbek |
| `ve` | Venda |
| `vi` | Vietnamese |
| `vo` | Volapük |
| `cy` | Welsh |
| `wa` | Walloon |
| `wo` | Wolof |
| `xh` | Xhosa |
| `yi` | Yiddish |
| `yo` | Yoruba |
| `za` |  |
| `zu` | Zulu |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CompareTo(object obj)` | `int` | Throws `ArgumentNullException`/`ArgumentException` for a null or mismatched argument. |
| `Equals(Iso639Part1Language other)` | `bool` | |
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
| Explicit | `explicit operator Iso639Part1Language(string)` | Wraps a code string. Throws `ArgumentNullException` for `null`. |
| Implicit | `implicit operator string(Iso639Part1Language)` | Unwraps to the underlying code string. |
| Equality | `operator ==`, `!=` `(Iso639Part1Language, Iso639Part1Language)` | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. `IConvertible`
(and its members, including `GetTypeCode()` and `ToString(IFormatProvider)`) is
available on the `netstandard2.0`, `net8.0`, and `net10.0` builds, but **not** on
`netstandard1.0`.

## See also

- [Iso639Extensions](Iso639Extensions.md)
- [Iso639Part2BLanguage](Iso639Part2BLanguage.md)
- [Iso639Part2TLanguage](Iso639Part2TLanguage.md)
- [Iso639Part3Language](Iso639Part3Language.md)
- [Use language codes](../how-to/use-language-codes.md)
- [Understanding the ISO 639 parts](../concepts/iso639-parts.md)
- [Language API reference](index.md)
