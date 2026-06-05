---
title: Iso639Part5LanguageFamily Struct
parent: Language
grand_parent: Packages
nav_exclude: true
---

# Iso639Part5LanguageFamily Struct

## Definition

Namespace: `DataStandardizer.Language`

ISO 639 Part 5 alpha-3 codes for language families and groups (for example `cau`).
ISO 639 does not assign numeric values to its codes, so this type is not a C# enum:
it is a `readonly struct` implementing `DataStandardizer.Core.IStringEnum`, with
each code exposed as a `public static readonly` member whose underlying value is the
string code from the standard. It behaves much like an enum — you access codes as
static members and compare them with `==` — but it wraps a `string`.

```csharp
public readonly struct Iso639Part5LanguageFamily : DataStandardizer.Core.IStringEnum, IEquatable<Iso639Part5LanguageFamily>
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
| `aav` | Austro-Asiatic languages |
| `afa` | Afro-Asiatic languages |
| `alg` | Algonquian languages |
| `alv` | Atlantic-Congo languages |
| `apa` | Apache languages |
| `aqa` | Alacalufan languages |
| `aql` | Algic languages |
| `art` | Artificial languages |
| `ath` | Athapascan languages |
| `auf` | Arauan languages |
| `aus` | Australian languages |
| `awd` | Arawakan languages |
| `azc` | Uto-Aztecan languages |
| `bad` | Banda languages |
| `bai` | Bamileke languages |
| `bat` | Baltic languages |
| `ber` | Berber languages |
| `bih` | Bihari languages |
| `bnt` | Bantu languages |
| `btk` | Batak languages |
| `cai` | Central American Indian languages |
| `cau` | Caucasian languages |
| `cba` | Chibchan languages |
| `ccn` | North Caucasian languages |
| `ccs` | South Caucasian languages |
| `cdc` | Chadic languages |
| `cdd` | Caddoan languages |
| `cel` |  |
| `cmc` | Chamic languages |
| `cpe` | Creoles and pidgins, English‑based |
| `cpf` | Creoles and pidgins, French‑based |
| `cpp` | Creoles and pidgins, Portuguese-based |
| `crp` | Creoles and pidgins |
| `csu` | Central Sudanic languages |
| `cus` | Cushitic languages |
| `day` | Land Dayak languages |
| `dmn` | Mande languages |
| `dra` | Dravidian languages |
| `egx` | Egyptian languages |
| `esx` | Eskimo-Aleut languages |
| `euq` | Basque (family) |
| `fiu` | Finno-Ugrian languages |
| `fox` | Formosan languages |
| `gem` | Germanic languages |
| `gme` | East Germanic languages |
| `gmq` | North Germanic languages |
| `gmw` | West Germanic languages |
| `grk` | Greek languages |
| `hmx` | Hmong-Mien languages |
| `hok` | Hokan languages |
| `hyx` | Armenian (family) |
| `iir` | Indo-Iranian languages |
| `ijo` | Ijo languages |
| `inc` | Indic languages |
| `ine` | Indo-European languages |
| `ira` | Iranian languages |
| `iro` | Iroquoian languages |
| `itc` | Italic languages |
| `jpx` | Japanese (family) |
| `kar` | Karen languages |
| `kdo` | Kordofanian languages |
| `khi` | Khoisan languages |
| `kro` | Kru languages |
| `map` | Austronesian languages |
| `mkh` | Mon-Khmer languages |
| `mno` | Manobo languages |
| `mun` | Munda languages |
| `myn` | Mayan languages |
| `nah` | Nahuatl languages |
| `nai` | North American Indian languages |
| `ngf` | Trans-New Guinea languages |
| `nic` | Niger-Kordofanian languages |
| `nub` | Nubian languages |
| `omq` | Oto-Manguean languages |
| `omv` | Omotic languages |
| `oto` | Otomian languages |
| `paa` | Papuan languages |
| `phi` | Philippine languages |
| `plf` | Central Malayo-Polynesian languages |
| `poz` | Malayo-Polynesian languages |
| `pqe` | Eastern Malayo-Polynesian languages |
| `pqw` | Western Malayo-Polynesian languages |
| `pra` | Prakrit languages |
| `qwe` | Quechuan (family) |
| `roa` | Romance languages |
| `sai` | South American Indian languages |
| `sal` | Salishan languages |
| `sdv` | Eastern Sudanic languages |
| `sem` | Semitic languages |
| `sgn` | sign languages |
| `sio` | Siouan languages |
| `sit` | Sino-Tibetan languages |
| `sla` | Slavic languages |
| `smi` | Sami languages |
| `son` | Songhai languages |
| `sqj` | Albanian languages |
| `ssa` | Nilo-Saharan languages |
| `syd` | Samoyedic languages |
| `tai` | Tai languages |
| `tbq` | Tibeto-Burman languages |
| `trk` | Turkic languages |
| `tup` | Tupi languages |
| `tut` | Altaic languages |
| `tuw` | Tungus languages |
| `urj` | Uralic languages |
| `wak` | Wakashan languages |
| `wen` | Sorbian languages |
| `xgn` | Mongolian languages |
| `xnd` | Na-Dene languages |
| `ypk` | Yupik languages |
| `zhx` | Chinese (family) |
| `zle` | East Slavic languages |
| `zls` | South Slavic languages |
| `zlw` | West Slavic languages |
| `znd` | Zande languages |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CompareTo(object obj)` | `int` | Throws `ArgumentNullException`/`ArgumentException` for a null or mismatched argument. |
| `Equals(Iso639Part5LanguageFamily other)` | `bool` | |
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
| Explicit | `explicit operator Iso639Part5LanguageFamily(string)` | Wraps a code string. Throws `ArgumentNullException` for `null`. |
| Implicit | `implicit operator string(Iso639Part5LanguageFamily)` | Unwraps to the underlying code string. |
| Equality | `operator ==`, `!=` `(Iso639Part5LanguageFamily, Iso639Part5LanguageFamily)` | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. `IConvertible`
(and its members, including `GetTypeCode()` and `ToString(IFormatProvider)`) is
available on the `netstandard2.0`, `net8.0`, and `net10.0` builds, but **not** on
`netstandard1.0`.

## See also

- [Iso639Extensions](Iso639Extensions.md)
- [Iso639Part1Language](Iso639Part1Language.md)
- [Iso639Part2BLanguage](Iso639Part2BLanguage.md)
- [Iso639Part3Language](Iso639Part3Language.md)
- [Use language codes](../how-to/use-language-codes.md)
- [Understanding the ISO 639 parts](../concepts/iso639-parts.md)
- [Language API reference](index.md)
