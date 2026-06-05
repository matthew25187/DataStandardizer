---
title: Iso639Part2TLanguage Struct
parent: Language
grand_parent: Packages
nav_exclude: true
---

# Iso639Part2TLanguage Struct

## Definition

Namespace: `DataStandardizer.Language`

ISO 639 Part 2 alpha-3 terminological (T) language codes. ISO 639 does not assign
numeric values to its language codes, so this type is not a C# enum: it is a
`readonly struct` implementing `DataStandardizer.Core.IStringEnum`, with each
language code exposed as a `public static readonly` member whose underlying value
is the string code from the standard. It behaves much like an enum — you access
codes as static members and compare them with `==` — but it wraps a `string`.

```csharp
public readonly struct Iso639Part2TLanguage : DataStandardizer.Core.IStringEnum, IEquatable<Iso639Part2TLanguage>
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
| `aar` | Afar |
| `abk` | Abkhazian |
| `ace` | Achinese |
| `ach` | Acoli |
| `ada` | Adangme |
| `ady` | Adyghe; Adygei |
| `afa` | Afro-Asiatic languages |
| `afh` | Afrihili |
| `afr` | Afrikaans |
| `ain` | Ainu |
| `aka` | Akan |
| `akk` | Akkadian |
| `sqi` | Albanian |
| `ale` | Aleut |
| `alg` | Algonquian languages |
| `alt` | Southern Altai |
| `amh` | Amharic |
| `ang` | English, Old (ca.450-1100) |
| `anp` | Angika |
| `apa` | Apache languages |
| `ara` | Arabic |
| `arc` | Official Aramaic (700-300 BCE); Imperial Aramaic (700-300 BCE) |
| `arg` | Aragonese |
| `hye` | Armenian |
| `arn` | Mapudungun; Mapuche |
| `arp` | Arapaho |
| `art` | Artificial languages |
| `arw` | Arawak |
| `asm` | Assamese |
| `ast` | Asturian; Bable; Leonese; Asturleonese |
| `ath` | Athapascan languages |
| `aus` | Australian languages |
| `ava` | Avaric |
| `ave` | Avestan |
| `awa` | Awadhi |
| `aym` | Aymara |
| `aze` | Azerbaijani |
| `bad` | Banda languages |
| `bai` | Bamileke languages |
| `bak` | Bashkir |
| `bal` | Baluchi |
| `bam` | Bambara |
| `ban` | Balinese |
| `eus` | Basque |
| `bas` | Basa |
| `bat` | Baltic languages |
| `bej` | Beja; Bedawiyet |
| `bel` | Belarusian |
| `bem` | Bemba |
| `ben` | Bengali |
| `ber` | Berber languages |
| `bho` | Bhojpuri |
| `bih` | Bihari languages |
| `bik` | Bikol |
| `bin` | Bini; Edo |
| `bis` | Bislama |
| `bla` | Siksika |
| `bnt` | Bantu languages |
| `bos` | Bosnian |
| `bra` | Braj |
| `bre` | Breton |
| `btk` | Batak languages |
| `bua` | Buriat |
| `bug` | Buginese |
| `bul` | Bulgarian |
| `mya` | Burmese |
| `byn` | Blin; Bilin |
| `cad` | Caddo |
| `cai` | Central American Indian languages |
| `car` | Galibi Carib |
| `cat` | Catalan; Valencian |
| `cau` | Caucasian languages |
| `ceb` | Cebuano |
| `cel` | Celtic languages |
| `cha` | Chamorro |
| `chb` | Chibcha |
| `che` | Chechen |
| `chg` | Chagatai |
| `zho` | Chinese |
| `chk` | Chuukese |
| `chm` | Mari |
| `chn` | Chinook jargon |
| `cho` | Choctaw |
| `chp` | Chipewyan; Dene Suline |
| `chr` | Cherokee |
| `chu` | Church Slavic; Old Slavonic; Church Slavonic; Old Bulgarian; Old Church Slavonic |
| `chv` | Chuvash |
| `chy` | Cheyenne |
| `cmc` | Chamic languages |
| `cnr` | Montenegrin |
| `cop` | Coptic |
| `cor` | Cornish |
| `cos` | Corsican |
| `cpe` | Creoles and pidgins, English based |
| `cpf` | Creoles and pidgins, French-based |
| `cpp` | Creoles and pidgins, Portuguese-based |
| `cre` | Cree |
| `crh` | Crimean Tatar; Crimean Turkish |
| `crp` | Creoles and pidgins |
| `csb` | Kashubian |
| `cus` | Cushitic languages |
| `ces` | Czech |
| `dak` | Dakota |
| `dan` | Danish |
| `dar` | Dargwa |
| `day` | Land Dayak languages |
| `del` | Delaware |
| `den` | Slave (Athapascan) |
| `dgr` | Tlicho; Dogrib |
| `din` | Dinka |
| `div` | Divehi; Dhivehi; Maldivian |
| `doi` | Dogri |
| `dra` | Dravidian languages |
| `dsb` | Lower Sorbian |
| `dua` | Duala |
| `dum` | Dutch, Middle (ca.1050-1350) |
| `nld` | Dutch; Flemish |
| `dyu` | Dyula |
| `dzo` | Dzongkha |
| `efi` | Efik |
| `egy` | Egyptian (Ancient) |
| `eka` | Ekajuk |
| `elx` | Elamite |
| `eng` | English |
| `enm` | English, Middle (1100-1500) |
| `epo` | Esperanto |
| `est` | Estonian |
| `ewe` | Ewe |
| `ewo` | Ewondo |
| `fan` | Fang |
| `fao` | Faroese |
| `fat` | Fanti |
| `fij` | Fijian |
| `fil` | Filipino; Pilipino |
| `fin` | Finnish |
| `fiu` | Finno-Ugrian languages |
| `fon` | Fon |
| `fra` | French |
| `frm` | French, Middle (ca.1400-1600) |
| `fro` | French, Old (842-ca.1400) |
| `frr` | Northern Frisian |
| `frs` | Eastern Frisian |
| `fry` | Western Frisian |
| `ful` | Fulah |
| `fur` | Friulian |
| `gaa` | Ga |
| `gay` | Gayo |
| `gba` | Gbaya |
| `gem` | Germanic languages |
| `kat` | Georgian |
| `deu` | German |
| `gez` | Geez |
| `gil` | Gilbertese |
| `gla` | Gaelic; Scottish Gaelic |
| `gle` | Irish |
| `glg` | Galician |
| `glv` | Manx |
| `gmh` | German, Middle High (ca.1050-1500) |
| `goh` | German, Old High (ca.750-1050) |
| `gon` | Gondi |
| `gor` | Gorontalo |
| `got` | Gothic |
| `grb` | Grebo |
| `grc` | Greek, Ancient (to 1453) |
| `ell` | Greek, Modern (1453-) |
| `grn` | Guarani |
| `gsw` | Swiss German; Alemannic; Alsatian |
| `guj` | Gujarati |
| `gwi` | Gwich\'in |
| `hai` | Haida |
| `hat` | Haitian; Haitian Creole |
| `hau` | Hausa |
| `haw` | Hawaiian |
| `heb` | Hebrew |
| `her` | Herero |
| `hil` | Hiligaynon |
| `him` | Himachali languages; Western Pahari languages |
| `hin` | Hindi |
| `hit` | Hittite |
| `hmn` | Hmong; Mong |
| `hmo` | Hiri Motu |
| `hrv` | Croatian |
| `hsb` | Upper Sorbian |
| `hun` | Hungarian |
| `hup` | Hupa |
| `iba` | Iban |
| `ibo` | Igbo |
| `isl` | Icelandic |
| `ido` | Ido |
| `iii` | Sichuan Yi; Nuosu |
| `ijo` | Ijo languages |
| `iku` | Inuktitut |
| `ile` | Interlingue; Occidental |
| `ilo` | Iloko |
| `ina` | Interlingua (International Auxiliary Language Association) |
| `inc` | Indic languages |
| `ind` | Indonesian |
| `ine` | Indo-European languages |
| `inh` | Ingush |
| `ipk` | Inupiaq |
| `ira` | Iranian languages |
| `iro` | Iroquoian languages |
| `ita` | Italian |
| `jav` | Javanese |
| `jbo` | Lojban |
| `jpn` | Japanese |
| `jpr` | Judeo-Persian |
| `jrb` | Judeo-Arabic |
| `kaa` | Kara-Kalpak |
| `kab` | Kabyle |
| `kac` | Kachin; Jingpho |
| `kal` | Kalaallisut; Greenlandic |
| `kam` | Kamba |
| `kan` | Kannada |
| `kar` | Karen languages |
| `kas` | Kashmiri |
| `kau` | Kanuri |
| `kaw` | Kawi |
| `kaz` | Kazakh |
| `kbd` | Kabardian |
| `kha` | Khasi |
| `khi` | Khoisan languages |
| `khm` | Central Khmer |
| `kho` | Khotanese; Sakan |
| `kik` | Kikuyu; Gikuyu |
| `kin` | Kinyarwanda |
| `kir` | Kirghiz; Kyrgyz |
| `kmb` | Kimbundu |
| `kok` | Konkani |
| `kom` | Komi |
| `kon` | Kongo |
| `kor` | Korean |
| `kos` | Kosraean |
| `kpe` | Kpelle |
| `krc` | Karachay-Balkar |
| `krl` | Karelian |
| `kro` | Kru languages |
| `kru` | Kurukh |
| `kua` | Kuanyama; Kwanyama |
| `kum` | Kumyk |
| `kur` | Kurdish |
| `kut` | Kutenai |
| `lad` | Ladino |
| `lah` | Lahnda |
| `lam` | Lamba |
| `lao` | Lao |
| `lat` | Latin |
| `lav` | Latvian |
| `lez` | Lezghian |
| `lim` | Limburgan; Limburger; Limburgish |
| `lin` | Lingala |
| `lit` | Lithuanian |
| `lol` | Mongo |
| `loz` | Lozi |
| `ltz` | Luxembourgish; Letzeburgesch |
| `lua` | Luba-Lulua |
| `lub` | Luba-Katanga |
| `lug` | Ganda |
| `lui` | Luiseno |
| `lun` | Lunda |
| `luo` | Luo (Kenya and Tanzania) |
| `lus` | Lushai |
| `mkd` | Macedonian |
| `mad` | Madurese |
| `mag` | Magahi |
| `mah` | Marshallese |
| `mai` | Maithili |
| `mak` | Makasar |
| `mal` | Malayalam |
| `man` | Mandingo |
| `mri` | Maori |
| `map` | Austronesian languages |
| `mar` | Marathi |
| `mas` | Masai |
| `msa` | Malay |
| `mdf` | Moksha |
| `mdr` | Mandar |
| `men` | Mende |
| `mga` | Irish, Middle (900-1200) |
| `mic` | Mi\'kmaq; Micmac |
| `min` | Minangkabau |
| `mis` | Uncoded languages |
| `mkh` | Mon-Khmer languages |
| `mlg` | Malagasy |
| `mlt` | Maltese |
| `mnc` | Manchu |
| `mni` | Manipuri |
| `mno` | Manobo languages |
| `moh` | Mohawk |
| `mon` | Mongolian |
| `mos` | Mossi |
| `mul` | Multiple languages |
| `mun` | Munda languages |
| `mus` | Creek |
| `mwl` | Mirandese |
| `mwr` | Marwari |
| `myn` | Mayan languages |
| `myv` | Erzya |
| `nah` | Nahuatl languages |
| `nai` | North American Indian languages |
| `nap` | Neapolitan |
| `nau` | Nauru |
| `nav` | Navajo; Navaho |
| `nbl` | Ndebele, South; South Ndebele |
| `nde` | Ndebele, North; North Ndebele |
| `ndo` | Ndonga |
| `nds` | Low German; Low Saxon; German, Low; Saxon, Low |
| `nep` | Nepali |
| `new` | Nepal Bhasa; Newari |
| `nia` | Nias |
| `nic` | Niger-Kordofanian languages |
| `niu` | Niuean |
| `nno` | Norwegian Nynorsk; Nynorsk, Norwegian |
| `nob` | Bokmål, Norwegian; Norwegian Bokmål |
| `nog` | Nogai |
| `non` | Norse, Old |
| `nor` | Norwegian |
| `nqo` | N\'Ko |
| `nso` | Pedi; Sepedi; Northern Sotho |
| `nub` | Nubian languages |
| `nwc` | Classical Newari; Old Newari; Classical Nepal Bhasa |
| `nya` | Chichewa; Chewa; Nyanja |
| `nym` | Nyamwezi |
| `nyn` | Nyankole |
| `nyo` | Nyoro |
| `nzi` | Nzima |
| `oci` | Occitan (post 1500) |
| `oji` | Ojibwa |
| `ori` | Oriya |
| `orm` | Oromo |
| `osa` | Osage |
| `oss` | Ossetian; Ossetic |
| `ota` | Turkish, Ottoman (1500-1928) |
| `oto` | Otomian languages |
| `paa` | Papuan languages |
| `pag` | Pangasinan |
| `pal` | Pahlavi |
| `pam` | Pampanga; Kapampangan |
| `pan` | Panjabi; Punjabi |
| `pap` | Papiamento |
| `pau` | Palauan |
| `peo` | Persian, Old (ca.600-400 B.C.) |
| `fas` | Persian |
| `phi` | Philippine languages |
| `phn` | Phoenician |
| `pli` | Pali |
| `pol` | Polish |
| `pon` | Pohnpeian |
| `por` | Portuguese |
| `pra` | Prakrit languages |
| `pro` | Provençal, Old (to 1500); Occitan, Old (to 1500) |
| `pus` | Pushto; Pashto |
| `que` | Quechua |
| `raj` | Rajasthani |
| `rap` | Rapanui |
| `rar` | Rarotongan; Cook Islands Maori |
| `roa` | Romance languages |
| `roh` | Romansh |
| `rom` | Romany |
| `ron` | Romanian; Moldavian; Moldovan |
| `run` | Rundi |
| `rup` | Aromanian; Arumanian; Macedo-Romanian |
| `rus` | Russian |
| `sad` | Sandawe |
| `sag` | Sango |
| `sah` | Yakut |
| `sai` | South American Indian languages |
| `sal` | Salishan languages |
| `sam` | Samaritan Aramaic |
| `san` | Sanskrit |
| `sas` | Sasak |
| `sat` | Santali |
| `scn` | Sicilian |
| `sco` | Scots |
| `sel` | Selkup |
| `sem` | Semitic languages |
| `sga` | Irish, Old (to 900) |
| `sgn` | Sign Languages |
| `shn` | Shan |
| `sid` | Sidamo |
| `sin` | Sinhala; Sinhalese |
| `sio` | Siouan languages |
| `sit` | Sino-Tibetan languages |
| `sla` | Slavic languages |
| `slk` | Slovak |
| `slv` | Slovenian |
| `sma` | Southern Sami |
| `sme` | Northern Sami |
| `smi` | Sami languages |
| `smj` | Lule Sami |
| `smn` | Inari Sami |
| `smo` | Samoan |
| `sms` | Skolt Sami |
| `sna` | Shona |
| `snd` | Sindhi |
| `snk` | Soninke |
| `sog` | Sogdian |
| `som` | Somali |
| `son` | Songhai languages |
| `sot` | Sotho, Southern |
| `spa` | Spanish; Castilian |
| `srd` | Sardinian |
| `srn` | Sranan Tongo |
| `srp` | Serbian |
| `srr` | Serer |
| `ssa` | Nilo-Saharan languages |
| `ssw` | Swati |
| `suk` | Sukuma |
| `sun` | Sundanese |
| `sus` | Susu |
| `sux` | Sumerian |
| `swa` | Swahili |
| `swe` | Swedish |
| `syc` | Classical Syriac |
| `syr` | Syriac |
| `tah` | Tahitian |
| `tai` | Tai languages |
| `tam` | Tamil |
| `tat` | Tatar |
| `tel` | Telugu |
| `tem` | Timne |
| `ter` | Tereno |
| `tet` | Tetum |
| `tgk` | Tajik |
| `tgl` | Tagalog |
| `tha` | Thai |
| `bod` | Tibetan |
| `tig` | Tigre |
| `tir` | Tigrinya |
| `tiv` | Tiv |
| `tkl` | Tokelau |
| `tlh` | Klingon; tlhIngan-Hol |
| `tli` | Tlingit |
| `tmh` | Tamashek |
| `tog` | Tonga (Nyasa) |
| `ton` | Tonga (Tonga Islands) |
| `tpi` | Tok Pisin |
| `tsi` | Tsimshian |
| `tsn` | Tswana |
| `tso` | Tsonga |
| `tuk` | Turkmen |
| `tum` | Tumbuka |
| `tup` | Tupi languages |
| `tur` | Turkish |
| `tut` | Altaic languages |
| `tvl` | Tuvalu |
| `twi` | Twi |
| `tyv` | Tuvinian |
| `udm` | Udmurt |
| `uga` | Ugaritic |
| `uig` | Uighur; Uyghur |
| `ukr` | Ukrainian |
| `umb` | Umbundu |
| `und` | Undetermined |
| `urd` | Urdu |
| `uzb` | Uzbek |
| `vai` | Vai |
| `ven` | Venda |
| `vie` | Vietnamese |
| `vol` | Volapük |
| `vot` | Votic |
| `wak` | Wakashan languages |
| `wal` | Wolaitta; Wolaytta |
| `war` | Waray |
| `was` | Washo |
| `cym` | Welsh |
| `wen` | Sorbian languages |
| `wln` | Walloon |
| `wol` | Wolof |
| `xal` | Kalmyk; Oirat |
| `xho` | Xhosa |
| `yao` | Yao |
| `yap` | Yapese |
| `yid` | Yiddish |
| `yor` | Yoruba |
| `ypk` | Yupik languages |
| `zap` | Zapotec |
| `zbl` | Blissymbols; Blissymbolics; Bliss |
| `zen` | Zenaga |
| `zgh` | Standard Moroccan Tamazight |
| `zha` | Zhuang; Chuang |
| `znd` | Zande languages |
| `zul` | Zulu |
| `zun` | Zuni |
| `zxx` | No linguistic content; Not applicable |
| `zza` | Zaza; Dimili; Dimli; Kirdki; Kirmanjki; Zazaki |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CompareTo(object obj)` | `int` | Throws `ArgumentNullException`/`ArgumentException` for a null or mismatched argument. |
| `Equals(Iso639Part2TLanguage other)` | `bool` | |
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
| Explicit | `explicit operator Iso639Part2TLanguage(string)` | Wraps a code string. Throws `ArgumentNullException` for `null`. |
| Implicit | `implicit operator string(Iso639Part2TLanguage)` | Unwraps to the underlying code string. |
| Equality | `operator ==`, `!=` `(Iso639Part2TLanguage, Iso639Part2TLanguage)` | |

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
