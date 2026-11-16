---
title: Iso4217CurrencyHistoric Enum
parent: Money
grand_parent: Packages
nav_exclude: true
---

# Iso4217CurrencyHistoric Enum

## Definition

Namespace: `DataStandardizer.Money`

Historic ISO 4217 denominations of currencies and funds (Table A.3). Each member
is named after the standard's three-letter alphabetic code, and the member's
underlying value is the standard's numeric code.

```csharp
public enum Iso4217CurrencyHistoric : short
```

## Remarks

Per-member metadata — currency name and minor-unit digit count — is carried by
`Iso4217CurrencyCodeAttribute` and read through
[Iso4217Extensions](Iso4217Extensions.md), except for members that share a value.

ISO 4217 reuses numeric codes across successive denominations, so several
members share a value, such as `BGJ`, `BGK` and `BGL` (100). The codes with no
numeric code (`XFO`, `XFU` and `XRE`) all share -1. Members that share a value
are the same value at run time and can't be told apart:

- They compare equal: `Iso4217CurrencyHistoric.BGJ == Iso4217CurrencyHistoric.BGL`
  is `true`.
- `ToString()` may return the name of any of them.
- The [Iso4217Extensions](Iso4217Extensions.md#historic-codes-that-share-a-numeric-code)
  methods return `null` for them, so `BGL.GetCurrencyName()` returns `null`.

The values are the numeric codes from the official ISO 4217 data, so they are
kept as they are rather than made unique.

The table below gives each member's name from ISO 4217. For a row marked
*shared*, `GetCurrencyName` returns `null`.

## Fields

| Field | Numeric code | Currency name | Minor units |
| --- | --- | --- | --- |
| `ADP` | 20 | Andorran Peseta | — |
| `AFA` | 4 | Afghani | — |
| `ALK` | 8 | Old Lek | — |
| `ANG` | 532 | Netherlands Antillean Guilder | — |
| `AOK` | 24 (shared) | Kwanza | — |
| `AON` | 24 (shared) | New Kwanza | — |
| `AOR` | 982 | Kwanza Reajustado | — |
| `ARA` | 32 (shared) | Austral | — |
| `ARP` | 32 (shared) | Peso Argentino | — |
| `ARY` | 32 (shared) | Peso | — |
| `ATS` | 40 | Schilling | — |
| `AYM` | 945 | Azerbaijan Manat | — |
| `AZM` | 31 | Azerbaijanian Manat | — |
| `BAD` | 70 | Dinar | — |
| `BEC` | 993 | Convertible Franc | — |
| `BEF` | 56 | Belgian Franc | — |
| `BEL` | 992 | Financial Franc | — |
| `BGJ` | 100 (shared) | Lev A/52 | — |
| `BGK` | 100 (shared) | Lev A/62 | — |
| `BGL` | 100 (shared) | Lev | — |
| `BGN` | 975 | Bulgarian Lev | — |
| `BOP` | 68 | Peso boliviano | — |
| `BRB` | 76 (shared) | Cruzeiro | — |
| `BRC` | 76 (shared) | Cruzado | — |
| `BRE` | 76 (shared) | Cruzeiro | — |
| `BRN` | 76 (shared) | New Cruzado | — |
| `BRR` | 987 | Cruzeiro Real | — |
| `BUK` | 104 | Kyat | — |
| `BYB` | 112 | Belarusian Ruble | — |
| `BYR` | 974 | Belarusian Ruble | — |
| `CHC` | 948 | WIR Franc (for electronic) | — |
| `CSD` | 891 (shared) | Serbian Dinar | — |
| `CSJ` | 203 | Krona A/53 | — |
| `CSK` | 200 | Koruna | — |
| `CUC` | 931 | Peso Convertible | — |
| `CYP` | 196 | Cyprus Pound | — |
| `DDM` | 278 | Mark der DDR | — |
| `DEM` | 276 | Deutsche Mark | — |
| `ECS` | 218 | Sucre | — |
| `ECV` | 983 | Unidad de Valor Constante (UVC) | — |
| `EEK` | 233 | Kroon | — |
| `ESA` | 996 | Spanish Peseta | — |
| `ESB` | 995 | \ | — |
| `ESP` | 724 | Spanish Peseta | — |
| `EUR` | 978 | Euro | — |
| `FIM` | 246 | Markka | — |
| `FRF` | 250 | French Franc | — |
| `GEK` | 268 | Georgian Coupon | — |
| `GHC` | 288 | Cedi | — |
| `GHP` | 939 | Ghana Cedi | — |
| `GNE` | 324 (shared) | Syli | — |
| `GNS` | 324 (shared) | Syli | — |
| `GQE` | 226 | Ekwele | — |
| `GRD` | 300 | Drachma | — |
| `GWE` | 624 (shared) | Guinea Escudo | — |
| `GWP` | 624 (shared) | Guinea-Bissau Peso | — |
| `HRD` | 191 (shared) | Croatian Dinar | — |
| `HRK` | 191 (shared) | Croatian Kuna | — |
| `IDR` | 360 | Rupiah | — |
| `IEP` | 372 | Irish Pound | — |
| `ILP` | 376 (shared) | Pound | — |
| `ILR` | 376 (shared) | Old Shekel | — |
| `ISJ` | 352 | Old Krona | — |
| `ITL` | 380 | Italian Lira | — |
| `LAJ` | 418 | Pathet Lao Kip | — |
| `LSM` | 426 | Loti | — |
| `LTL` | 440 (shared) | Lithuanian Litas | — |
| `LTT` | 440 (shared) | Talonas | — |
| `LUC` | 989 | Luxembourg Convertible Franc | — |
| `LUF` | 442 | Luxembourg Franc | — |
| `LUL` | 988 | Luxembourg Financial Franc | — |
| `LVL` | 428 (shared) | Latvian Lats | — |
| `LVR` | 428 (shared) | Latvian Ruble | — |
| `MGF` | 450 | Malagasy Franc | — |
| `MLF` | 466 | Mali Franc | — |
| `MRO` | 478 | Ouguiya | — |
| `MTL` | 470 (shared) | Maltese Lira | — |
| `MTP` | 470 (shared) | Maltese Pound | — |
| `MVQ` | 462 | Maldive Rupee | — |
| `MWK` | 454 | Kwacha | — |
| `MXP` | 484 | Mexican Peso | — |
| `MZE` | 508 (shared) | Mozambique Escudo | — |
| `MZM` | 508 (shared) | Mozambique Metical | — |
| `NIC` | 558 | Cordoba | — |
| `NLG` | 528 | Netherlands Guilder | — |
| `PEH` | 604 (shared) | Sol | — |
| `PEI` | 604 (shared) | Inti | — |
| `PEN` | 604 (shared) | Nuevo Sol  | — |
| `PES` | 604 (shared) | Sol | — |
| `PLZ` | 616 | Zloty | — |
| `PTE` | 620 | Portuguese Escudo | — |
| `RHD` | 716 (shared) | Rhodesian Dollar | — |
| `ROK` | 642 (shared) | Leu A/52 | — |
| `ROL` | 642 (shared) | Old Leu | — |
| `RON` | 946 | New Romanian Leu  | — |
| `RUR` | 810 (shared) | Russian Ruble | — |
| `SDD` | 736 (shared) | Sudanese Dinar | — |
| `SDG` | 938 | Sudanese Pound | — |
| `SDP` | 736 (shared) | Sudanese Pound | — |
| `SIT` | 705 | Tolar | — |
| `SKK` | 703 | Slovak Koruna | — |
| `SLL` | 694 | Leone | — |
| `SRG` | 740 | Surinam Guilder | — |
| `STD` | 678 | Dobra | — |
| `SUR` | 810 (shared) | Rouble | — |
| `SZL` | 748 | Lilangeni | — |
| `TJR` | 762 | Tajik Ruble | — |
| `TMM` | 795 | Turkmenistan Manat | — |
| `TPE` | 626 | Timor Escudo | — |
| `TRL` | 792 | Old Turkish Lira | — |
| `TRY` | 949 | New Turkish Lira | — |
| `UAK` | 804 | Karbovanet | — |
| `UGS` | 800 (shared) | Uganda Shilling | — |
| `UGW` | 800 (shared) | Old Shilling | — |
| `USS` | 998 | US Dollar (Same day) | — |
| `UYN` | 858 (shared) | Old Uruguay Peso | — |
| `UYP` | 858 (shared) | Uruguayan Peso | — |
| `VEB` | 862 | Bolivar | — |
| `VEF` | 937 | Bolivar Fuerte | — |
| `VNC` | 704 | Old Dong | — |
| `XEU` | 954 | European Currency Unit (E.C.U) | — |
| `YDD` | 720 | Yemeni Dinar | — |
| `YUD` | 890 (shared) | New Yugoslavian Dinar | — |
| `YUM` | 891 (shared) | New Dinar | — |
| `YUN` | 890 (shared) | Yugoslavian Dinar | — |
| `ZAL` | 991 | Financial Rand | — |
| `ZMK` | 894 | Zambian Kwacha | — |
| `ZRN` | 180 (shared) | New Zaire | — |
| `ZRZ` | 180 (shared) | Zaire | — |
| `ZWC` | 716 (shared) | Rhodesian Dollar | — |
| `ZWD` | 716 (shared) | Zimbabwe Dollar (old) | — |
| `ZWL` | 932 | Zimbabwe Dollar | — |
| `ZWN` | 942 | Zimbabwe Dollar (new) | — |
| `ZWR` | 935 | Zimbabwe Dollar | — |
| `XFO` | -1 (no numeric code; shared) | Gold-Franc | — |
| `XFU` | -1 (no numeric code; shared) | UIC-Franc | — |
| `XRE` | -1 (no numeric code; shared) | RINET Funds Code | — |

## See also

- [Money](Money.md)
- [Iso4217Extensions](Iso4217Extensions.md)
- [Use currency codes](../how-to/use-currency-codes.md)
- [Money API reference](index.md)
