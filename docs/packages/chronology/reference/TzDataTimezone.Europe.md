---
title: TzDataTimezone.Europe time zones
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimezone.Europe time zones

Predefined time zones under `TzDataTimezone.Europe`, members of [TzDataTimezone](TzDataTimezone.md). Access one by name, e.g. `TzDataTimezone.Europe.Andorra`.

## Fields

| Field | Identifier | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- |
| `Andorra` | Europe/Andorra | AD | 42.5000 | 1.5167 |
| `Astrakhan` | Europe/Astrakhan | RU, MSK+01 - Astrakhan | 46.3500 | 48.0500 |
| `Athens` | Europe/Athens | GR | 37.9667 | 23.7167 |
| `Belgrade` | Europe/Belgrade | RS, BA, HR, ME, MK, SI | 44.8333 | 20.5000 |
| `Berlin` | Europe/Berlin | DE, DK, NO, SE, SJ, most of Germany | 52.5000 | 13.3667 |
| `Brussels` | Europe/Brussels | BE, LU, NL | 50.8333 | 4.3333 |
| `Bucharest` | Europe/Bucharest | RO | 44.4333 | 26.1000 |
| `Budapest` | Europe/Budapest | HU | 47.5000 | 19.0833 |
| `Chisinau` | Europe/Chisinau | MD | 47.0000 | 28.8333 |
| `Dublin` | Europe/Dublin | IE | 53.3333 | -6.2500 |
| `Gibraltar` | Europe/Gibraltar | GI | 36.1333 | -5.3500 |
| `Helsinki` | Europe/Helsinki | FI, AX | 60.1667 | 24.9667 |
| `Istanbul` | Europe/Istanbul | TR | 41.0167 | 28.9667 |
| `Kaliningrad` | Europe/Kaliningrad | RU, MSK-01 - Kaliningrad | 54.7167 | 20.5000 |
| `Kirov` | Europe/Kirov | RU, MSK+00 - Kirov | 58.6000 | 49.6500 |
| `Kyiv` | Europe/Kyiv | UA, most of Ukraine | 50.4333 | 30.5167 |
| `Lisbon` | Europe/Lisbon | PT | 38.7167 | -9.1333 |
| `London` | Europe/London | GB, GG, IM, JE | 51.5083 | -0.1253 |
| `Madrid` | Europe/Madrid | ES | 40.4000 | -3.6833 |
| `Malta` | Europe/Malta | MT | 35.9000 | 14.5167 |
| `Minsk` | Europe/Minsk | BY | 53.9000 | 27.5667 |
| `Moscow` | Europe/Moscow | RU, MSK+00 - Moscow area | 55.7558 | 37.6178 |
| `Paris` | Europe/Paris | FR, MC | 48.8667 | 2.3333 |
| `Prague` | Europe/Prague | CZ, SK | 50.0833 | 14.4333 |
| `Riga` | Europe/Riga | LV | 56.9500 | 24.1000 |
| `Rome` | Europe/Rome | IT, SM, VA | 41.9000 | 12.4833 |
| `Samara` | Europe/Samara | RU, MSK+01 - Samara, Udmurtia | 53.2000 | 50.1500 |
| `Saratov` | Europe/Saratov | RU, MSK+01 - Saratov | 51.5667 | 46.0333 |
| `Simferopol` | Europe/Simferopol | RU, UA, Crimea | 44.9500 | 34.1000 |
| `Sofia` | Europe/Sofia | BG | 42.6833 | 23.3167 |
| `Tallinn` | Europe/Tallinn | EE | 59.4167 | 24.7500 |
| `Tirane` | Europe/Tirane | AL | 41.3333 | 19.8333 |
| `Ulyanovsk` | Europe/Ulyanovsk | RU, MSK+01 - Ulyanovsk | 54.3333 | 48.4000 |
| `Vienna` | Europe/Vienna | AT | 48.2167 | 16.3333 |
| `Vilnius` | Europe/Vilnius | LT | 54.6833 | 25.3167 |
| `Volgograd` | Europe/Volgograd | RU, MSK+00 - Volgograd | 48.7333 | 44.4167 |
| `Warsaw` | Europe/Warsaw | PL | 52.2500 | 21.0000 |
| `Zurich` | Europe/Zurich | CH, DE, LI, Büsingen | 47.3833 | 8.5333 |

## Links

These fields are links: time zones of their own countries, listed in `zone.tab`, that share the zone lines of a canonical time zone. `IsLink` returns `true` for them, and `Canonical` returns the time zone they link to. Their location metadata is their own, but their history, including that before 1970, is that of the canonical time zone. See [Links and canonical time zones](TzDataTimezone.md#links-and-canonical-time-zones).

| Field | Identifier | Canonical | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- | --- |
| `Amsterdam` | Europe/Amsterdam | `Europe.Brussels` | NL | 52.3667 | 4.9000 |
| `Bratislava` | Europe/Bratislava | `Europe.Prague` | SK | 48.1500 | 17.1167 |
| `Busingen` | Europe/Busingen | `Europe.Zurich` | DE, Busingen | 47.7000 | 8.6833 |
| `Copenhagen` | Europe/Copenhagen | `Europe.Berlin` | DK | 55.6667 | 12.5833 |
| `Guernsey` | Europe/Guernsey | `Europe.London` | GG | 49.4547 | -2.5361 |
| `Isle_of_Man` | Europe/Isle_of_Man | `Europe.London` | IM | 54.1500 | -4.4667 |
| `Jersey` | Europe/Jersey | `Europe.London` | JE | 49.1836 | -2.1067 |
| `Ljubljana` | Europe/Ljubljana | `Europe.Belgrade` | SI | 46.0500 | 14.5167 |
| `Luxembourg` | Europe/Luxembourg | `Europe.Brussels` | LU | 49.6000 | 6.1500 |
| `Mariehamn` | Europe/Mariehamn | `Europe.Helsinki` | AX | 60.1000 | 19.9500 |
| `Monaco` | Europe/Monaco | `Europe.Paris` | MC | 43.7000 | 7.3833 |
| `Oslo` | Europe/Oslo | `Europe.Berlin` | NO | 59.9167 | 10.7500 |
| `Podgorica` | Europe/Podgorica | `Europe.Belgrade` | ME | 42.4333 | 19.2667 |
| `San_Marino` | Europe/San_Marino | `Europe.Rome` | SM | 43.9167 | 12.4667 |
| `Sarajevo` | Europe/Sarajevo | `Europe.Belgrade` | BA | 43.8667 | 18.4167 |
| `Skopje` | Europe/Skopje | `Europe.Belgrade` | MK | 41.9833 | 21.4333 |
| `Stockholm` | Europe/Stockholm | `Europe.Berlin` | SE | 59.3333 | 18.0500 |
| `Vaduz` | Europe/Vaduz | `Europe.Zurich` | LI | 47.1500 | 9.5167 |
| `Vatican` | Europe/Vatican | `Europe.Rome` | VA | 41.9022 | 12.4531 |
| `Zagreb` | Europe/Zagreb | `Europe.Belgrade` | HR | 45.8000 | 15.9667 |

## See also

- [TzDataTimezone](TzDataTimezone.md)
- [Access time zone metadata](../how-to/access-timezone-metadata.md)
