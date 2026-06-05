---
title: UnM49AreaByAlpha2CountryCode Enum
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# UnM49AreaByAlpha2CountryCode Enum

## Definition

Namespace: `DataStandardizer.Geography`

UN M49 area codes keyed by ISO 3166-1 alpha-2 country code. Each member is named
after an ISO 3166-1 alpha-2 code, and the member's underlying value is the
numeric M49 area code.

```csharp
public enum UnM49AreaByAlpha2CountryCode : ushort
```

## Remarks

Per-member metadata — the related global, region, sub-region, and intermediate
region codes, plus the names of those codes and the area itself in several
languages — is carried by `UnM49AreaCodeAttribute` and read through
[UnM49Extensions](UnM49Extensions.md).

## Fields

| Field | M49 code | Country or area |
| --- | --- | --- |
| `DZ` | 12 | Algeria |
| `EG` | 818 | Egypt |
| `LY` | 434 | Libya |
| `MA` | 504 | Morocco |
| `SD` | 729 | Sudan |
| `TN` | 788 | Tunisia |
| `EH` | 732 | Western Sahara |
| `IO` | 86 | British Indian Ocean Territory |
| `BI` | 108 | Burundi |
| `KM` | 174 | Comoros |
| `DJ` | 262 | Djibouti |
| `ER` | 232 | Eritrea |
| `ET` | 231 | Ethiopia |
| `TF` | 260 | French Southern Territories |
| `KE` | 404 | Kenya |
| `MG` | 450 | Madagascar |
| `MW` | 454 | Malawi |
| `MU` | 480 | Mauritius |
| `YT` | 175 | Mayotte |
| `MZ` | 508 | Mozambique |
| `RE` | 638 | Réunion |
| `RW` | 646 | Rwanda |
| `SC` | 690 | Seychelles |
| `SO` | 706 | Somalia |
| `SS` | 728 | South Sudan |
| `UG` | 800 | Uganda |
| `TZ` | 834 | United Republic of Tanzania |
| `ZM` | 894 | Zambia |
| `ZW` | 716 | Zimbabwe |
| `AO` | 24 | Angola |
| `CM` | 120 | Cameroon |
| `CF` | 140 | Central African Republic |
| `TD` | 148 | Chad |
| `CG` | 178 | Congo |
| `CD` | 180 | Democratic Republic of the Congo |
| `GQ` | 226 | Equatorial Guinea |
| `GA` | 266 | Gabon |
| `ST` | 678 | Sao Tome and Principe |
| `BW` | 72 | Botswana |
| `SZ` | 748 | Eswatini |
| `LS` | 426 | Lesotho |
| `NA` | 516 | Namibia |
| `ZA` | 710 | South Africa |
| `BJ` | 204 | Benin |
| `BF` | 854 | Burkina Faso |
| `CV` | 132 | Cabo Verde |
| `CI` | 384 | Côte d’Ivoire |
| `GM` | 270 | Gambia |
| `GH` | 288 | Ghana |
| `GN` | 324 | Guinea |
| `GW` | 624 | Guinea-Bissau |
| `LR` | 430 | Liberia |
| `ML` | 466 | Mali |
| `MR` | 478 | Mauritania |
| `NE` | 562 | Niger |
| `NG` | 566 | Nigeria |
| `SH` | 654 | Saint Helena |
| `SN` | 686 | Senegal |
| `SL` | 694 | Sierra Leone |
| `TG` | 768 | Togo |
| `AI` | 660 | Anguilla |
| `AG` | 28 | Antigua and Barbuda |
| `AW` | 533 | Aruba |
| `BS` | 44 | Bahamas |
| `BB` | 52 | Barbados |
| `BQ` | 535 | Bonaire, Sint Eustatius and Saba |
| `VG` | 92 | British Virgin Islands |
| `KY` | 136 | Cayman Islands |
| `CU` | 192 | Cuba |
| `CW` | 531 | Curaçao |
| `DM` | 212 | Dominica |
| `DO` | 214 | Dominican Republic |
| `GD` | 308 | Grenada |
| `GP` | 312 | Guadeloupe |
| `HT` | 332 | Haiti |
| `JM` | 388 | Jamaica |
| `MQ` | 474 | Martinique |
| `MS` | 500 | Montserrat |
| `PR` | 630 | Puerto Rico |
| `BL` | 652 | Saint Barthélemy |
| `KN` | 659 | Saint Kitts and Nevis |
| `LC` | 662 | Saint Lucia |
| `MF` | 663 | Saint Martin (French Part) |
| `VC` | 670 | Saint Vincent and the Grenadines |
| `SX` | 534 | Sint Maarten (Dutch part) |
| `TT` | 780 | Trinidad and Tobago |
| `TC` | 796 | Turks and Caicos Islands |
| `VI` | 850 | United States Virgin Islands |
| `BZ` | 84 | Belize |
| `CR` | 188 | Costa Rica |
| `SV` | 222 | El Salvador |
| `GT` | 320 | Guatemala |
| `HN` | 340 | Honduras |
| `MX` | 484 | Mexico |
| `NI` | 558 | Nicaragua |
| `PA` | 591 | Panama |
| `AR` | 32 | Argentina |
| `BO` | 68 | Bolivia (Plurinational State of) |
| `BV` | 74 | Bouvet Island |
| `BR` | 76 | Brazil |
| `CL` | 152 | Chile |
| `CO` | 170 | Colombia |
| `EC` | 218 | Ecuador |
| `FK` | 238 | Falkland Islands (Malvinas) |
| `GF` | 254 | French Guiana |
| `GY` | 328 | Guyana |
| `PY` | 600 | Paraguay |
| `PE` | 604 | Peru |
| `GS` | 239 | South Georgia and the South Sandwich Islands |
| `SR` | 740 | Suriname |
| `UY` | 858 | Uruguay |
| `VE` | 862 | Venezuela (Bolivarian Republic of) |
| `BM` | 60 | Bermuda |
| `CA` | 124 | Canada |
| `GL` | 304 | Greenland |
| `PM` | 666 | Saint Pierre and Miquelon |
| `US` | 840 | United States of America |
| `AQ` | 10 | Antarctica |
| `KZ` | 398 | Kazakhstan |
| `KG` | 417 | Kyrgyzstan |
| `TJ` | 762 | Tajikistan |
| `TM` | 795 | Turkmenistan |
| `UZ` | 860 | Uzbekistan |
| `CN` | 156 | China |
| `HK` | 344 | China, Hong Kong Special Administrative Region |
| `MO` | 446 | China, Macao Special Administrative Region |
| `KP` | 408 | Democratic People's Republic of Korea |
| `JP` | 392 | Japan |
| `MN` | 496 | Mongolia |
| `KR` | 410 | Republic of Korea |
| `BN` | 96 | Brunei Darussalam |
| `KH` | 116 | Cambodia |
| `ID` | 360 | Indonesia |
| `LA` | 418 | Lao People's Democratic Republic |
| `MY` | 458 | Malaysia |
| `MM` | 104 | Myanmar |
| `PH` | 608 | Philippines |
| `SG` | 702 | Singapore |
| `TH` | 764 | Thailand |
| `TL` | 626 | Timor-Leste |
| `VN` | 704 | Viet Nam |
| `AF` | 4 | Afghanistan |
| `BD` | 50 | Bangladesh |
| `BT` | 64 | Bhutan |
| `IN` | 356 | India |
| `IR` | 364 | Iran (Islamic Republic of) |
| `MV` | 462 | Maldives |
| `NP` | 524 | Nepal |
| `PK` | 586 | Pakistan |
| `LK` | 144 | Sri Lanka |
| `AM` | 51 | Armenia |
| `AZ` | 31 | Azerbaijan |
| `BH` | 48 | Bahrain |
| `CY` | 196 | Cyprus |
| `GE` | 268 | Georgia |
| `IQ` | 368 | Iraq |
| `IL` | 376 | Israel |
| `JO` | 400 | Jordan |
| `KW` | 414 | Kuwait |
| `LB` | 422 | Lebanon |
| `OM` | 512 | Oman |
| `QA` | 634 | Qatar |
| `SA` | 682 | Saudi Arabia |
| `PS` | 275 | State of Palestine |
| `SY` | 760 | Syrian Arab Republic |
| `TR` | 792 | Türkiye |
| `AE` | 784 | United Arab Emirates |
| `YE` | 887 | Yemen |
| `BY` | 112 | Belarus |
| `BG` | 100 | Bulgaria |
| `CZ` | 203 | Czechia |
| `HU` | 348 | Hungary |
| `PL` | 616 | Poland |
| `MD` | 498 | Republic of Moldova |
| `RO` | 642 | Romania |
| `RU` | 643 | Russian Federation |
| `SK` | 703 | Slovakia |
| `UA` | 804 | Ukraine |
| `AX` | 248 | Åland Islands |
| `DK` | 208 | Denmark |
| `EE` | 233 | Estonia |
| `FO` | 234 | Faroe Islands |
| `FI` | 246 | Finland |
| `GG` | 831 | Guernsey |
| `IS` | 352 | Iceland |
| `IE` | 372 | Ireland |
| `IM` | 833 | Isle of Man |
| `JE` | 832 | Jersey |
| `LV` | 428 | Latvia |
| `LT` | 440 | Lithuania |
| `NO` | 578 | Norway |
| `SJ` | 744 | Svalbard and Jan Mayen Islands |
| `SE` | 752 | Sweden |
| `GB` | 826 | United Kingdom of Great Britain and Northern Ireland |
| `AL` | 8 | Albania |
| `AD` | 20 | Andorra |
| `BA` | 70 | Bosnia and Herzegovina |
| `HR` | 191 | Croatia |
| `GI` | 292 | Gibraltar |
| `GR` | 300 | Greece |
| `VA` | 336 | Holy See |
| `IT` | 380 | Italy |
| `MT` | 470 | Malta |
| `ME` | 499 | Montenegro |
| `MK` | 807 | North Macedonia |
| `PT` | 620 | Portugal |
| `SM` | 674 | San Marino |
| `RS` | 688 | Serbia |
| `SI` | 705 | Slovenia |
| `ES` | 724 | Spain |
| `AT` | 40 | Austria |
| `BE` | 56 | Belgium |
| `FR` | 250 | France |
| `DE` | 276 | Germany |
| `LI` | 438 | Liechtenstein |
| `LU` | 442 | Luxembourg |
| `MC` | 492 | Monaco |
| `NL` | 528 | Netherlands (Kingdom of the) |
| `CH` | 756 | Switzerland |
| `AU` | 36 | Australia |
| `CX` | 162 | Christmas Island |
| `CC` | 166 | Cocos (Keeling) Islands |
| `HM` | 334 | Heard Island and McDonald Islands |
| `NZ` | 554 | New Zealand |
| `NF` | 574 | Norfolk Island |
| `FJ` | 242 | Fiji |
| `NC` | 540 | New Caledonia |
| `PG` | 598 | Papua New Guinea |
| `SB` | 90 | Solomon Islands |
| `VU` | 548 | Vanuatu |
| `GU` | 316 | Guam |
| `KI` | 296 | Kiribati |
| `MH` | 584 | Marshall Islands |
| `FM` | 583 | Micronesia (Federated States of) |
| `NR` | 520 | Nauru |
| `MP` | 580 | Northern Mariana Islands |
| `PW` | 585 | Palau |
| `UM` | 581 | United States Minor Outlying Islands |
| `AS` | 16 | American Samoa |
| `CK` | 184 | Cook Islands |
| `PF` | 258 | French Polynesia |
| `NU` | 570 | Niue |
| `PN` | 612 | Pitcairn |
| `WS` | 882 | Samoa |
| `TK` | 772 | Tokelau |
| `TO` | 776 | Tonga |
| `TV` | 798 | Tuvalu |
| `WF` | 876 | Wallis and Futuna Islands |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Use area codes](../how-to/use-area-codes.md)
- [UnM49AreaByAlpha3CountryCode](UnM49AreaByAlpha3CountryCode.md)
- [UnM49Extensions](UnM49Extensions.md)
- [UnM49AreaCodeAttribute](UnM49AreaCodeAttribute.md)
- [Geography API reference](index.md)
</content>
