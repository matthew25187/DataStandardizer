---
title: TzDataTimezone.Africa time zones
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimezone.Africa time zones

Predefined time zones under `TzDataTimezone.Africa`, members of [TzDataTimezone](TzDataTimezone.md). Access one by name, e.g. `TzDataTimezone.Africa.Abidjan`.

## Fields

| Field | Identifier | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- |
| `Abidjan` | Africa/Abidjan | CI, BF, GH, GM, GN, IS, ML, MR, SH, SL, SN, TG | 5.3167 | -4.0333 |
| `Algiers` | Africa/Algiers | DZ | 36.7833 | 3.0500 |
| `Bissau` | Africa/Bissau | GW | 11.8500 | -15.5833 |
| `Cairo` | Africa/Cairo | EG | 30.0500 | 31.2500 |
| `Casablanca` | Africa/Casablanca | MA | 33.6500 | -7.5833 |
| `Ceuta` | Africa/Ceuta | ES, Ceuta, Melilla | 35.8833 | -5.3167 |
| `El_Aaiun` | Africa/El_Aaiun | EH | 27.1500 | -13.2000 |
| `Johannesburg` | Africa/Johannesburg | ZA, LS, SZ | -26.2500 | 28.0000 |
| `Juba` | Africa/Juba | SS | 4.8500 | 31.6167 |
| `Khartoum` | Africa/Khartoum | SD | 15.6000 | 32.5333 |
| `Lagos` | Africa/Lagos | NG, AO, BJ, CD, CF, CG, CM, GA, GQ, NE, West Africa Time | 6.4500 | 3.4000 |
| `Maputo` | Africa/Maputo | MZ, BI, BW, CD, MW, RW, ZM, ZW, Central Africa Time | -25.9667 | 32.5833 |
| `Monrovia` | Africa/Monrovia | LR | 6.3000 | -10.7833 |
| `Nairobi` | Africa/Nairobi | KE, DJ, ER, ET, KM, MG, SO, TZ, UG, YT | -1.2833 | 36.8167 |
| `Ndjamena` | Africa/Ndjamena | TD | 12.1167 | 15.0500 |
| `Sao_Tome` | Africa/Sao_Tome | ST | 0.3333 | 6.7333 |
| `Tripoli` | Africa/Tripoli | LY | 32.9000 | 13.1833 |
| `Tunis` | Africa/Tunis | TN | 36.8000 | 10.1833 |
| `Windhoek` | Africa/Windhoek | NA | -22.5667 | 17.1000 |

## Links

These fields are links: time zones of their own countries, listed in `zone.tab`, that share the zone lines of a canonical time zone. `IsLink` returns `true` for them, and `Canonical` returns the time zone they link to. Their location metadata is their own, but their history, including that before 1970, is that of the canonical time zone. See [Links and canonical time zones](TzDataTimezone.md#links-and-canonical-time-zones).

| Field | Identifier | Canonical | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- | --- |
| `Accra` | Africa/Accra | `Africa.Abidjan` | GH | 5.5500 | -0.2167 |
| `Addis_Ababa` | Africa/Addis_Ababa | `Africa.Nairobi` | ET | 9.0333 | 38.7000 |
| `Asmara` | Africa/Asmara | `Africa.Nairobi` | ER | 15.3333 | 38.8833 |
| `Bamako` | Africa/Bamako | `Africa.Abidjan` | ML | 12.6500 | -8.0000 |
| `Bangui` | Africa/Bangui | `Africa.Lagos` | CF | 4.3667 | 18.5833 |
| `Banjul` | Africa/Banjul | `Africa.Abidjan` | GM | 13.4667 | -16.6500 |
| `Blantyre` | Africa/Blantyre | `Africa.Maputo` | MW | -15.7833 | 35.0000 |
| `Brazzaville` | Africa/Brazzaville | `Africa.Lagos` | CG | -4.2667 | 15.2833 |
| `Bujumbura` | Africa/Bujumbura | `Africa.Maputo` | BI | -3.3833 | 29.3667 |
| `Conakry` | Africa/Conakry | `Africa.Abidjan` | GN | 9.5167 | -13.7167 |
| `Dakar` | Africa/Dakar | `Africa.Abidjan` | SN | 14.6667 | -17.4333 |
| `Dar_es_Salaam` | Africa/Dar_es_Salaam | `Africa.Nairobi` | TZ | -6.8000 | 39.2833 |
| `Djibouti` | Africa/Djibouti | `Africa.Nairobi` | DJ | 11.6000 | 43.1500 |
| `Douala` | Africa/Douala | `Africa.Lagos` | CM | 4.0500 | 9.7000 |
| `Freetown` | Africa/Freetown | `Africa.Abidjan` | SL | 8.5000 | -13.2500 |
| `Gaborone` | Africa/Gaborone | `Africa.Maputo` | BW | -24.6500 | 25.9167 |
| `Harare` | Africa/Harare | `Africa.Maputo` | ZW | -17.8333 | 31.0500 |
| `Kampala` | Africa/Kampala | `Africa.Nairobi` | UG | 0.3167 | 32.4167 |
| `Kigali` | Africa/Kigali | `Africa.Maputo` | RW | -1.9500 | 30.0667 |
| `Kinshasa` | Africa/Kinshasa | `Africa.Lagos` | CD, Dem. Rep. of Congo (west) | -4.3000 | 15.3000 |
| `Libreville` | Africa/Libreville | `Africa.Lagos` | GA | 0.3833 | 9.4500 |
| `Lome` | Africa/Lome | `Africa.Abidjan` | TG | 6.1333 | 1.2167 |
| `Luanda` | Africa/Luanda | `Africa.Lagos` | AO | -8.8000 | 13.2333 |
| `Lubumbashi` | Africa/Lubumbashi | `Africa.Maputo` | CD, Dem. Rep. of Congo (east) | -11.6667 | 27.4667 |
| `Lusaka` | Africa/Lusaka | `Africa.Maputo` | ZM | -15.4167 | 28.2833 |
| `Malabo` | Africa/Malabo | `Africa.Lagos` | GQ | 3.7500 | 8.7833 |
| `Maseru` | Africa/Maseru | `Africa.Johannesburg` | LS | -29.4667 | 27.5000 |
| `Mbabane` | Africa/Mbabane | `Africa.Johannesburg` | SZ | -26.3000 | 31.1000 |
| `Mogadishu` | Africa/Mogadishu | `Africa.Nairobi` | SO | 2.0667 | 45.3667 |
| `Niamey` | Africa/Niamey | `Africa.Lagos` | NE | 13.5167 | 2.1167 |
| `Nouakchott` | Africa/Nouakchott | `Africa.Abidjan` | MR | 18.1000 | -15.9500 |
| `Ouagadougou` | Africa/Ouagadougou | `Africa.Abidjan` | BF | 12.3667 | -1.5167 |
| `Porto_Novo` | Africa/Porto-Novo | `Africa.Lagos` | BJ | 6.4833 | 2.6167 |

## See also

- [TzDataTimezone](TzDataTimezone.md)
- [Access time zone metadata](../how-to/access-timezone-metadata.md)
