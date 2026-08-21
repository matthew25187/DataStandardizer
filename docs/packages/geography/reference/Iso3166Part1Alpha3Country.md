---
title: Iso3166Part1Alpha3Country Enum
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# Iso3166Part1Alpha3Country Enum

## Definition

Namespace: `DataStandardizer.Geography`

ISO 3166-1 alpha-3 country codes. Each member is named after the standard's
three-letter alphabetic code (e.g. `GRC`), and the member's underlying value is
the associated ISO/UN numeric country code.

```csharp
public enum Iso3166Part1Alpha3Country : ushort
```

## Remarks

Per-member metadata — English and native country names, territories, and the
independence flag — is carried by `Iso3166CountryCodeAttribute`,
`Iso3166CountryNameAttribute`, and `Iso3166CountryTerritoryAttribute`, and read
through [Iso3166Extensions](Iso3166Extensions.md).

## Fields

| Field | Numeric code | Country or area |
| --- | --- | --- |
| `AND` | 20 | Andorra |
| `ARE` | 784 | United Arab Emirates (the) |
| `AFG` | 4 | Afghanistan |
| `ATG` | 28 | Antigua and Barbuda |
| `AIA` | 660 | Anguilla |
| `ALB` | 8 | Albania |
| `ARM` | 51 | Armenia |
| `AGO` | 24 | Angola |
| `ATA` | 10 | Antarctica |
| `ARG` | 32 | Argentina |
| `ASM` | 16 | American Samoa |
| `AUT` | 40 | Austria |
| `AUS` | 36 | Australia |
| `ABW` | 533 | Aruba |
| `ALA` | 248 | Åland Islands |
| `AZE` | 31 | Azerbaijan |
| `BIH` | 70 | Bosnia and Herzegovina |
| `BRB` | 52 | Barbados |
| `BGD` | 50 | Bangladesh |
| `BEL` | 56 | Belgium |
| `BFA` | 854 | Burkina Faso |
| `BGR` | 100 | Bulgaria |
| `BHR` | 48 | Bahrain |
| `BDI` | 108 | Burundi |
| `BEN` | 204 | Benin |
| `BLM` | 652 | Saint Barthélemy |
| `BMU` | 60 | Bermuda |
| `BRN` | 96 | Brunei Darussalam |
| `BOL` | 68 | Bolivia (Plurinational State of) |
| `BES` | 535 | Bonaire, Sint Eustatius and Saba |
| `BRA` | 76 | Brazil |
| `BHS` | 44 | Bahamas (the) |
| `BTN` | 64 | Bhutan |
| `BVT` | 74 | Bouvet Island |
| `BWA` | 72 | Botswana |
| `BLR` | 112 | Belarus |
| `BLZ` | 84 | Belize |
| `CAN` | 124 | Canada |
| `CCK` | 166 | Cocos (Keeling) Islands (the) |
| `COD` | 180 | Congo (the Democratic Republic of the) |
| `CAF` | 140 | Central African Republic (the) |
| `COG` | 178 | Congo (the) |
| `CHE` | 756 | Switzerland |
| `CIV` | 384 | Côte d'Ivoire |
| `COK` | 184 | Cook Islands (the) |
| `CHL` | 152 | Chile |
| `CMR` | 120 | Cameroon |
| `CHN` | 156 | China |
| `COL` | 170 | Colombia |
| `CRI` | 188 | Costa Rica |
| `CUB` | 192 | Cuba |
| `CPV` | 132 | Cabo Verde |
| `CUW` | 531 | Curaçao |
| `CXR` | 162 | Christmas Island |
| `CYP` | 196 | Cyprus |
| `CZE` | 203 | Czechia |
| `DEU` | 276 | Germany |
| `DJI` | 262 | Djibouti |
| `DNK` | 208 | Denmark |
| `DMA` | 212 | Dominica |
| `DOM` | 214 | Dominican Republic (the) |
| `DZA` | 12 | Algeria |
| `ECU` | 218 | Ecuador |
| `EST` | 233 | Estonia |
| `EGY` | 818 | Egypt |
| `ESH` | 732 | Western Sahara* |
| `ERI` | 232 | Eritrea |
| `ESP` | 724 | Spain |
| `ETH` | 231 | Ethiopia |
| `FIN` | 246 | Finland |
| `FJI` | 242 | Fiji |
| `FLK` | 238 | Falkland Islands (the) [Malvinas] |
| `FSM` | 583 | Micronesia (Federated States of) |
| `FRO` | 234 | Faroe Islands (the) |
| `FRA` | 250 | France |
| `GAB` | 266 | Gabon |
| `GBR` | 826 | United Kingdom of Great Britain and Northern Ireland (the) |
| `GRD` | 308 | Grenada |
| `GEO` | 268 | Georgia |
| `GUF` | 254 | French Guiana |
| `GGY` | 831 | Guernsey |
| `GHA` | 288 | Ghana |
| `GIB` | 292 | Gibraltar |
| `GRL` | 304 | Greenland |
| `GMB` | 270 | Gambia (the) |
| `GIN` | 324 | Guinea |
| `GLP` | 312 | Guadeloupe |
| `GNQ` | 226 | Equatorial Guinea |
| `GRC` | 300 | Greece |
| `SGS` | 239 | South Georgia and the South Sandwich Islands |
| `GTM` | 320 | Guatemala |
| `GUM` | 316 | Guam |
| `GNB` | 624 | Guinea-Bissau |
| `GUY` | 328 | Guyana |
| `HKG` | 344 | Hong Kong |
| `HMD` | 334 | Heard Island and McDonald Islands |
| `HND` | 340 | Honduras |
| `HRV` | 191 | Croatia |
| `HTI` | 332 | Haiti |
| `HUN` | 348 | Hungary |
| `IDN` | 360 | Indonesia |
| `IRL` | 372 | Ireland |
| `ISR` | 376 | Israel |
| `IMN` | 833 | Isle of Man |
| `IND` | 356 | India |
| `IOT` | 86 | British Indian Ocean Territory (the) |
| `IRQ` | 368 | Iraq |
| `IRN` | 364 | Iran (Islamic Republic of) |
| `ISL` | 352 | Iceland |
| `ITA` | 380 | Italy |
| `JEY` | 832 | Jersey |
| `JAM` | 388 | Jamaica |
| `JOR` | 400 | Jordan |
| `JPN` | 392 | Japan |
| `KEN` | 404 | Kenya |
| `KGZ` | 417 | Kyrgyzstan |
| `KHM` | 116 | Cambodia |
| `KIR` | 296 | Kiribati |
| `COM` | 174 | Comoros (the) |
| `KNA` | 659 | Saint Kitts and Nevis |
| `PRK` | 408 | Korea (the Democratic People's Republic of) |
| `KOR` | 410 | Korea (the Republic of) |
| `KWT` | 414 | Kuwait |
| `CYM` | 136 | Cayman Islands (the) |
| `KAZ` | 398 | Kazakhstan |
| `LAO` | 418 | Lao People's Democratic Republic (the) |
| `LBN` | 422 | Lebanon |
| `LCA` | 662 | Saint Lucia |
| `LIE` | 438 | Liechtenstein |
| `LKA` | 144 | Sri Lanka |
| `LBR` | 430 | Liberia |
| `LSO` | 426 | Lesotho |
| `LTU` | 440 | Lithuania |
| `LUX` | 442 | Luxembourg |
| `LVA` | 428 | Latvia |
| `LBY` | 434 | Libya |
| `MAR` | 504 | Morocco |
| `MCO` | 492 | Monaco |
| `MDA` | 498 | Moldova (the Republic of) |
| `MNE` | 499 | Montenegro |
| `MAF` | 663 | Saint Martin (French part) |
| `MDG` | 450 | Madagascar |
| `MHL` | 584 | Marshall Islands (the) |
| `MKD` | 807 | North Macedonia |
| `MLI` | 466 | Mali |
| `MMR` | 104 | Myanmar |
| `MNG` | 496 | Mongolia |
| `MAC` | 446 | Macao |
| `MNP` | 580 | Northern Mariana Islands (the) |
| `MTQ` | 474 | Martinique |
| `MRT` | 478 | Mauritania |
| `MSR` | 500 | Montserrat |
| `MLT` | 470 | Malta |
| `MUS` | 480 | Mauritius |
| `MDV` | 462 | Maldives |
| `MWI` | 454 | Malawi |
| `MEX` | 484 | Mexico |
| `MYS` | 458 | Malaysia |
| `MOZ` | 508 | Mozambique |
| `NAM` | 516 | Namibia |
| `NCL` | 540 | New Caledonia |
| `NER` | 562 | Niger (the) |
| `NFK` | 574 | Norfolk Island |
| `NGA` | 566 | Nigeria |
| `NIC` | 558 | Nicaragua |
| `NLD` | 528 | Netherlands (Kingdom of the) |
| `NOR` | 578 | Norway |
| `NPL` | 524 | Nepal |
| `NRU` | 520 | Nauru |
| `NIU` | 570 | Niue |
| `NZL` | 554 | New Zealand |
| `OMN` | 512 | Oman |
| `PAN` | 591 | Panama |
| `PER` | 604 | Peru |
| `PYF` | 258 | French Polynesia |
| `PNG` | 598 | Papua New Guinea |
| `PHL` | 608 | Philippines (the) |
| `PAK` | 586 | Pakistan |
| `POL` | 616 | Poland |
| `SPM` | 666 | Saint Pierre and Miquelon |
| `PCN` | 612 | Pitcairn |
| `PRI` | 630 | Puerto Rico |
| `PSE` | 275 | Palestine, State of |
| `PRT` | 620 | Portugal |
| `PLW` | 585 | Palau |
| `PRY` | 600 | Paraguay |
| `QAT` | 634 | Qatar |
| `REU` | 638 | Réunion |
| `ROU` | 642 | Romania |
| `SRB` | 688 | Serbia |
| `RUS` | 643 | Russian Federation (the) |
| `RWA` | 646 | Rwanda |
| `SAU` | 682 | Saudi Arabia |
| `SLB` | 90 | Solomon Islands |
| `SYC` | 690 | Seychelles |
| `SDN` | 729 | Sudan (the) |
| `SWE` | 752 | Sweden |
| `SGP` | 702 | Singapore |
| `SHN` | 654 | Saint Helena, Ascension and Tristan da Cunha |
| `SVN` | 705 | Slovenia |
| `SJM` | 744 | Svalbard and Jan Mayen |
| `SVK` | 703 | Slovakia |
| `SLE` | 694 | Sierra Leone |
| `SMR` | 674 | San Marino |
| `SEN` | 686 | Senegal |
| `SOM` | 706 | Somalia |
| `SUR` | 740 | Suriname |
| `SSD` | 728 | South Sudan |
| `STP` | 678 | Sao Tome and Principe |
| `SLV` | 222 | El Salvador |
| `SXM` | 534 | Sint Maarten (Dutch part) |
| `SYR` | 760 | Syrian Arab Republic (the) |
| `SWZ` | 748 | Eswatini |
| `TCA` | 796 | Turks and Caicos Islands (the) |
| `TCD` | 148 | Chad |
| `ATF` | 260 | French Southern Territories (the) |
| `TGO` | 768 | Togo |
| `THA` | 764 | Thailand |
| `TJK` | 762 | Tajikistan |
| `TKL` | 772 | Tokelau |
| `TLS` | 626 | Timor-Leste |
| `TKM` | 795 | Turkmenistan |
| `TUN` | 788 | Tunisia |
| `TON` | 776 | Tonga |
| `TUR` | 792 | Türkiye |
| `TTO` | 780 | Trinidad and Tobago |
| `TUV` | 798 | Tuvalu |
| `TWN` | 158 | Taiwan (Province of China) |
| `TZA` | 834 | Tanzania, the United Republic of |
| `UKR` | 804 | Ukraine |
| `UGA` | 800 | Uganda |
| `UMI` | 581 | United States Minor Outlying Islands (the) |
| `USA` | 840 | United States of America (the) |
| `URY` | 858 | Uruguay |
| `UZB` | 860 | Uzbekistan |
| `VAT` | 336 | Holy See (the) |
| `VCT` | 670 | Saint Vincent and the Grenadines |
| `VEN` | 862 | Venezuela (Bolivarian Republic of) |
| `VGB` | 92 | Virgin Islands (British) |
| `VIR` | 850 | Virgin Islands (U.S.) |
| `VNM` | 704 | Viet Nam |
| `VUT` | 548 | Vanuatu |
| `WLF` | 876 | Wallis and Futuna |
| `WSM` | 882 | Samoa |
| `YEM` | 887 | Yemen |
| `MYT` | 175 | Mayotte |
| `ZAF` | 710 | South Africa |
| `ZMB` | 894 | Zambia |
| `ZWE` | 716 | Zimbabwe |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Use country codes](../how-to/use-country-codes.md)
- [Iso3166Part1Alpha2Country](Iso3166Part1Alpha2Country.md)
- [Iso3166Extensions](Iso3166Extensions.md)
- [Iso3166CountryCodeAttribute](Iso3166CountryCodeAttribute.md)
- [Geography API reference](index.md)
</content>
