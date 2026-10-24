---
title: TzDataTimezone.Pacific time zones
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimezone.Pacific time zones

Predefined time zones under `TzDataTimezone.Pacific`, members of [TzDataTimezone](TzDataTimezone.md). Access one by name, e.g. `TzDataTimezone.Pacific.Apia`.

## Fields

| Field | Identifier | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- |
| `Apia` | Pacific/Apia | WS | -13.8333 | -171.7333 |
| `Auckland` | Pacific/Auckland | NZ, AQ, New Zealand time | -36.8667 | 174.7667 |
| `Bougainville` | Pacific/Bougainville | PG, Bougainville | -6.2167 | 155.5667 |
| `Chatham` | Pacific/Chatham | NZ, Chatham Islands | -43.9500 | -176.5500 |
| `Easter` | Pacific/Easter | CL, Easter Island | -27.1500 | -109.4333 |
| `Efate` | Pacific/Efate | VU | -17.6667 | 168.4167 |
| `Fakaofo` | Pacific/Fakaofo | TK | -9.3667 | -171.2333 |
| `Fiji` | Pacific/Fiji | FJ | -18.1333 | 178.4167 |
| `Galapagos` | Pacific/Galapagos | EC, Galápagos Islands | -0.9000 | -89.6000 |
| `Gambier` | Pacific/Gambier | PF, Gambier Islands | -23.1333 | -134.9500 |
| `Guadalcanal` | Pacific/Guadalcanal | SB, FM, Pohnpei | -9.5333 | 160.2000 |
| `Guam` | Pacific/Guam | GU, MP | 13.4667 | 144.7500 |
| `Honolulu` | Pacific/Honolulu | US, Hawaii | 21.3069 | -157.8583 |
| `Kanton` | Pacific/Kanton | KI, Phoenix Islands | -2.7833 | -171.7167 |
| `Kiritimati` | Pacific/Kiritimati | KI, Line Islands | 1.8667 | -157.3333 |
| `Kosrae` | Pacific/Kosrae | FM, Kosrae | 5.3167 | 162.9833 |
| `Kwajalein` | Pacific/Kwajalein | MH, Kwajalein | 9.0833 | 167.3333 |
| `Marquesas` | Pacific/Marquesas | PF, Marquesas Islands | -9.0000 | -139.5000 |
| `Nauru` | Pacific/Nauru | NR | -0.5167 | 166.9167 |
| `Niue` | Pacific/Niue | NU | -19.0167 | -169.9167 |
| `Norfolk` | Pacific/Norfolk | NF | -29.0500 | 167.9667 |
| `Noumea` | Pacific/Noumea | NC | -22.2667 | 166.4500 |
| `Pago_Pago` | Pacific/Pago_Pago | AS, UM, Midway | -14.2667 | -170.7000 |
| `Palau` | Pacific/Palau | PW | 7.3333 | 134.4833 |
| `Pitcairn` | Pacific/Pitcairn | PN | -25.0667 | -130.0833 |
| `Port_Moresby` | Pacific/Port_Moresby | PG, AQ, FM | -9.5000 | 147.1667 |
| `Rarotonga` | Pacific/Rarotonga | CK | -21.2333 | -159.7667 |
| `Tahiti` | Pacific/Tahiti | PF, Society Islands | -17.5333 | -149.5667 |
| `Tarawa` | Pacific/Tarawa | KI, MH, TV, UM, WF, Gilberts, Marshalls, Wake | 1.4167 | 173.0000 |
| `Tongatapu` | Pacific/Tongatapu | TO | -21.1333 | -175.2000 |

## Links

These fields are links: time zones of their own countries, listed in `zone.tab`, that share the zone lines of a canonical time zone. `IsLink` returns `true` for them, and `Canonical` returns the time zone they link to. Their location metadata is their own, but their history, including that before 1970, is that of the canonical time zone. See [Links and canonical time zones](TzDataTimezone.md#links-and-canonical-time-zones).

| Field | Identifier | Canonical | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- | --- |
| `Chuuk` | Pacific/Chuuk | `Pacific.Port_Moresby` | FM, Chuuk/Truk, Yap | 7.4167 | 151.7833 |
| `Funafuti` | Pacific/Funafuti | `Pacific.Tarawa` | TV | -8.5167 | 179.2167 |
| `Majuro` | Pacific/Majuro | `Pacific.Tarawa` | MH, most of Marshall Islands | 7.1500 | 171.2000 |
| `Midway` | Pacific/Midway | `Pacific.Pago_Pago` | UM, Midway Islands | 28.2167 | -177.3667 |
| `Pohnpei` | Pacific/Pohnpei | `Pacific.Guadalcanal` | FM, Pohnpei/Ponape | 6.9667 | 158.2167 |
| `Saipan` | Pacific/Saipan | `Pacific.Guam` | MP | 15.2000 | 145.7500 |
| `Wake` | Pacific/Wake | `Pacific.Tarawa` | UM, Wake Island | 19.2833 | 166.6167 |
| `Wallis` | Pacific/Wallis | `Pacific.Tarawa` | WF | -13.3000 | -176.1667 |

## See also

- [TzDataTimezone](TzDataTimezone.md)
- [Access time zone metadata](../how-to/access-timezone-metadata.md)
