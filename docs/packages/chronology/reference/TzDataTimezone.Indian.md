---
title: TzDataTimezone.Indian time zones
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimezone.Indian time zones

Predefined time zones under `TzDataTimezone.Indian`, members of [TzDataTimezone](TzDataTimezone.md). Access one by name, e.g. `TzDataTimezone.Indian.Chagos`.

## Fields

| Field | Identifier | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- |
| `Chagos` | Indian/Chagos | IO | -7.3333 | 72.4167 |
| `Maldives` | Indian/Maldives | MV, TF, Kerguelen, St Paul I, Amsterdam I | 4.1667 | 73.5000 |
| `Mauritius` | Indian/Mauritius | MU | -20.1667 | 57.5000 |

## Links

These fields are links: time zones of their own countries, listed in `zone.tab`, that share the zone lines of a canonical time zone. `IsLink` returns `true` for them, and `Canonical` returns the time zone they link to. Their location metadata is their own, but their history, including that before 1970, is that of the canonical time zone. See [Links and canonical time zones](TzDataTimezone.md#links-and-canonical-time-zones).

| Field | Identifier | Canonical | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- | --- |
| `Antananarivo` | Indian/Antananarivo | `Africa.Nairobi` | MG | -18.9167 | 47.5167 |
| `Christmas` | Indian/Christmas | `Asia.Bangkok` | CX | -10.4167 | 105.7167 |
| `Cocos` | Indian/Cocos | `Asia.Yangon` | CC | -12.1667 | 96.9167 |
| `Comoro` | Indian/Comoro | `Africa.Nairobi` | KM | -11.6833 | 43.2667 |
| `Kerguelen` | Indian/Kerguelen | `Indian.Maldives` | TF | -49.3528 | 70.2175 |
| `Mahe` | Indian/Mahe | `Asia.Dubai` | SC | -4.6667 | 55.4667 |
| `Mayotte` | Indian/Mayotte | `Africa.Nairobi` | YT | -12.7833 | 45.2333 |
| `Reunion` | Indian/Reunion | `Asia.Dubai` | RE | -20.8667 | 55.4667 |

## See also

- [TzDataTimezone](TzDataTimezone.md)
- [Access time zone metadata](../how-to/access-timezone-metadata.md)
