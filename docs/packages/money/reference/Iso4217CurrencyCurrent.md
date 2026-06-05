---
title: Iso4217CurrencyCurrent Enum
parent: Money
grand_parent: Packages
nav_exclude: true
---

# Iso4217CurrencyCurrent Enum

## Definition

Namespace: `DataStandardizer.Money`

The current ISO 4217 currency &amp; funds code list (Table A.1). Each member is
named after the standard's three-letter alphabetic code, and the member's
underlying value is the standard's numeric code.

```csharp
public enum Iso4217CurrencyCurrent : ushort
```

## Remarks

Per-member metadata — currency name, minor-unit digit count, and the funds-code
flag — is carried by `Iso4217CurrencyCodeAttribute` and read through
[Iso4217Extensions](Iso4217Extensions.md). The list reflects the official ISO 4217
data as at 2026-01-01.

## Fields

| Field | Numeric code | Currency name | Minor units | Funds code |
| --- | --- | --- | --- | --- |
| `AED` | 784 | UAE Dirham | 2 |  |
| `AFN` | 971 | Afghani | 2 |  |
| `ALL` | 8 | Lek | 2 |  |
| `AMD` | 51 | Armenian Dram | 2 |  |
| `AOA` | 973 | Kwanza | 2 |  |
| `ARS` | 32 | Argentine Peso | 2 |  |
| `AUD` | 36 | Australian Dollar | 2 |  |
| `AWG` | 533 | Aruban Florin | 2 |  |
| `AZN` | 944 | Azerbaijan Manat | 2 |  |
| `BAM` | 977 | Convertible Mark | 2 |  |
| `BBD` | 52 | Barbados Dollar | 2 |  |
| `BDT` | 50 | Taka | 2 |  |
| `BHD` | 48 | Bahraini Dinar | 3 |  |
| `BIF` | 108 | Burundi Franc | 0 |  |
| `BMD` | 60 | Bermudian Dollar | 2 |  |
| `BND` | 96 | Brunei Dollar | 2 |  |
| `BOB` | 68 | Boliviano | 2 |  |
| `BOV` | 984 | Mvdol | 2 | Yes |
| `BRL` | 986 | Brazilian Real | 2 |  |
| `BSD` | 44 | Bahamian Dollar | 2 |  |
| `BTN` | 64 | Ngultrum | 2 |  |
| `BWP` | 72 | Pula | 2 |  |
| `BYN` | 933 | Belarusian Ruble | 2 |  |
| `BZD` | 84 | Belize Dollar | 2 |  |
| `CAD` | 124 | Canadian Dollar | 2 |  |
| `CDF` | 976 | Congolese Franc | 2 |  |
| `CHE` | 947 | WIR Euro | 2 | Yes |
| `CHF` | 756 | Swiss Franc | 2 |  |
| `CHW` | 948 | WIR Franc | 2 | Yes |
| `CLF` | 990 | Unidad de Fomento | 4 | Yes |
| `CLP` | 152 | Chilean Peso | 0 |  |
| `CNY` | 156 | Yuan Renminbi | 2 |  |
| `COP` | 170 | Colombian Peso | 2 |  |
| `COU` | 970 | Unidad de Valor Real | 2 | Yes |
| `CRC` | 188 | Costa Rican Colon | 2 |  |
| `CUP` | 192 | Cuban Peso | 2 |  |
| `CVE` | 132 | Cabo Verde Escudo | 2 |  |
| `CZK` | 203 | Czech Koruna | 2 |  |
| `DJF` | 262 | Djibouti Franc | 0 |  |
| `DKK` | 208 | Danish Krone | 2 |  |
| `DOP` | 214 | Dominican Peso | 2 |  |
| `DZD` | 12 | Algerian Dinar | 2 |  |
| `EGP` | 818 | Egyptian Pound | 2 |  |
| `ERN` | 232 | Nakfa | 2 |  |
| `ETB` | 230 | Ethiopian Birr | 2 |  |
| `EUR` | 978 | Euro | 2 |  |
| `FJD` | 242 | Fiji Dollar | 2 |  |
| `FKP` | 238 | Falkland Islands Pound | 2 |  |
| `GBP` | 826 | Pound Sterling | 2 |  |
| `GEL` | 981 | Lari | 2 |  |
| `GHS` | 936 | Ghana Cedi | 2 |  |
| `GIP` | 292 | Gibraltar Pound | 2 |  |
| `GMD` | 270 | Dalasi | 2 |  |
| `GNF` | 324 | Guinean Franc | 0 |  |
| `GTQ` | 320 | Quetzal | 2 |  |
| `GYD` | 328 | Guyana Dollar | 2 |  |
| `HKD` | 344 | Hong Kong Dollar | 2 |  |
| `HNL` | 340 | Lempira | 2 |  |
| `HTG` | 332 | Gourde | 2 |  |
| `HUF` | 348 | Forint | 2 |  |
| `IDR` | 360 | Rupiah | 2 |  |
| `ILS` | 376 | New Israeli Sheqel | 2 |  |
| `INR` | 356 | Indian Rupee | 2 |  |
| `IQD` | 368 | Iraqi Dinar | 3 |  |
| `IRR` | 364 | Iranian Rial | 2 |  |
| `ISK` | 352 | Iceland Krona | 0 |  |
| `JMD` | 388 | Jamaican Dollar | 2 |  |
| `JOD` | 400 | Jordanian Dinar | 3 |  |
| `JPY` | 392 | Yen | 0 |  |
| `KES` | 404 | Kenyan Shilling | 2 |  |
| `KGS` | 417 | Som | 2 |  |
| `KHR` | 116 | Riel | 2 |  |
| `KMF` | 174 | Comorian Franc  | 0 |  |
| `KPW` | 408 | North Korean Won | 2 |  |
| `KRW` | 410 | Won | 0 |  |
| `KWD` | 414 | Kuwaiti Dinar | 3 |  |
| `KYD` | 136 | Cayman Islands Dollar | 2 |  |
| `KZT` | 398 | Tenge | 2 |  |
| `LAK` | 418 | Lao Kip | 2 |  |
| `LBP` | 422 | Lebanese Pound | 2 |  |
| `LKR` | 144 | Sri Lanka Rupee | 2 |  |
| `LRD` | 430 | Liberian Dollar | 2 |  |
| `LSL` | 426 | Loti | 2 |  |
| `LYD` | 434 | Libyan Dinar | 3 |  |
| `MAD` | 504 | Moroccan Dirham | 2 |  |
| `MDL` | 498 | Moldovan Leu | 2 |  |
| `MGA` | 969 | Malagasy Ariary | 2 |  |
| `MKD` | 807 | Denar | 2 |  |
| `MMK` | 104 | Kyat | 2 |  |
| `MNT` | 496 | Tugrik | 2 |  |
| `MOP` | 446 | Pataca | 2 |  |
| `MRU` | 929 | Ouguiya | 2 |  |
| `MUR` | 480 | Mauritius Rupee | 2 |  |
| `MVR` | 462 | Rufiyaa | 2 |  |
| `MWK` | 454 | Malawi Kwacha | 2 |  |
| `MXN` | 484 | Mexican Peso | 2 |  |
| `MXV` | 979 | Mexican Unidad de Inversion (UDI) | 2 | Yes |
| `MYR` | 458 | Malaysian Ringgit | 2 |  |
| `MZN` | 943 | Mozambique Metical | 2 |  |
| `NAD` | 516 | Namibia Dollar | 2 |  |
| `NGN` | 566 | Naira | 2 |  |
| `NIO` | 558 | Cordoba Oro | 2 |  |
| `NOK` | 578 | Norwegian Krone | 2 |  |
| `NPR` | 524 | Nepalese Rupee | 2 |  |
| `NZD` | 554 | New Zealand Dollar | 2 |  |
| `OMR` | 512 | Rial Omani | 3 |  |
| `PAB` | 590 | Balboa | 2 |  |
| `PEN` | 604 | Sol | 2 |  |
| `PGK` | 598 | Kina | 2 |  |
| `PHP` | 608 | Philippine Peso | 2 |  |
| `PKR` | 586 | Pakistan Rupee | 2 |  |
| `PLN` | 985 | Zloty | 2 |  |
| `PYG` | 600 | Guarani | 0 |  |
| `QAR` | 634 | Qatari Rial | 2 |  |
| `RON` | 946 | Romanian Leu | 2 |  |
| `RSD` | 941 | Serbian Dinar | 2 |  |
| `RUB` | 643 | Russian Ruble | 2 |  |
| `RWF` | 646 | Rwanda Franc | 0 |  |
| `SAR` | 682 | Saudi Riyal | 2 |  |
| `SBD` | 90 | Solomon Islands Dollar | 2 |  |
| `SCR` | 690 | Seychelles Rupee | 2 |  |
| `SDG` | 938 | Sudanese Pound | 2 |  |
| `SEK` | 752 | Swedish Krona | 2 |  |
| `SGD` | 702 | Singapore Dollar | 2 |  |
| `SHP` | 654 | Saint Helena Pound | 2 |  |
| `SLE` | 925 | Leone | 2 |  |
| `SOS` | 706 | Somali Shilling | 2 |  |
| `SRD` | 968 | Surinam Dollar | 2 |  |
| `SSP` | 728 | South Sudanese Pound | 2 |  |
| `STN` | 930 | Dobra | 2 |  |
| `SVC` | 222 | El Salvador Colon | 2 |  |
| `SYP` | 760 | Syrian Pound | 2 |  |
| `SZL` | 748 | Lilangeni | 2 |  |
| `THB` | 764 | Baht | 2 |  |
| `TJS` | 972 | Somoni | 2 |  |
| `TMT` | 934 | Turkmenistan New Manat | 2 |  |
| `TND` | 788 | Tunisian Dinar | 3 |  |
| `TOP` | 776 | Pa’anga | 2 |  |
| `TRY` | 949 | Turkish Lira | 2 |  |
| `TTD` | 780 | Trinidad and Tobago Dollar | 2 |  |
| `TWD` | 901 | New Taiwan Dollar | 2 |  |
| `TZS` | 834 | Tanzanian Shilling | 2 |  |
| `UAH` | 980 | Hryvnia | 2 |  |
| `UGX` | 800 | Uganda Shilling | 0 |  |
| `USD` | 840 | US Dollar | 2 |  |
| `USN` | 997 | US Dollar (Next day) | 2 | Yes |
| `UYI` | 940 | Uruguay Peso en Unidades Indexadas (UI) | 0 | Yes |
| `UYU` | 858 | Peso Uruguayo | 2 |  |
| `UYW` | 927 | Unidad Previsional | 4 |  |
| `UZS` | 860 | Uzbekistan Sum | 2 |  |
| `VED` | 926 | Bolívar Soberano | 2 |  |
| `VES` | 928 | Bolívar Soberano | 2 |  |
| `VND` | 704 | Dong | 0 |  |
| `VUV` | 548 | Vatu | 0 |  |
| `WST` | 882 | Tala | 2 |  |
| `YER` | 886 | Yemeni Rial | 2 |  |
| `ZAR` | 710 | Rand | 2 |  |
| `ZMW` | 967 | Zambian Kwacha | 2 |  |
| `ZWG` | 924 | Zimbabwe Gold | 2 |  |
| `XAD` | 396 | Arab Accounting Dinar | 2 |  |
| `XAF` | 950 | CFA Franc BEAC | 0 |  |
| `XAG` | 961 | Silver | — |  |
| `XAU` | 959 | Gold | — |  |
| `XBA` | 955 | Bond Markets Unit European Composite Unit (EURCO) | — |  |
| `XBB` | 956 | Bond Markets Unit European Monetary Unit (E.M.U.-6) | — |  |
| `XBC` | 957 | Bond Markets Unit European Unit of Account 9 (E.U.A.-9) | — |  |
| `XBD` | 958 | Bond Markets Unit European Unit of Account 17 (E.U.A.-17) | — |  |
| `XCD` | 951 | East Caribbean Dollar | 2 |  |
| `XCG` | 532 | Caribbean Guilder | 2 |  |
| `XDR` | 960 | SDR (Special Drawing Right) | — |  |
| `XOF` | 952 | CFA Franc BCEAO | 0 |  |
| `XPD` | 964 | Palladium | — |  |
| `XPF` | 953 | CFP Franc | 0 |  |
| `XPT` | 962 | Platinum | — |  |
| `XSU` | 994 | Sucre | — |  |
| `XTS` | 963 | Codes specifically reserved for testing purposes | — |  |
| `XUA` | 965 | ADB Unit of Account | — |  |
| `XXX` | 999 | The codes assigned for transactions where no currency is involved | — |  |

## See also

- [Money](Money.md)
- [Iso4217Extensions](Iso4217Extensions.md)
- [Use currency codes](../how-to/use-currency-codes.md)
- [Money API reference](index.md)
