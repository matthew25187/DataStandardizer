---
title: TzDataTimezone.Antarctica time zones
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimezone.Antarctica time zones

Predefined time zones under `TzDataTimezone.Antarctica`, members of [TzDataTimezone](TzDataTimezone.md). Access one by name, e.g. `TzDataTimezone.Antarctica.Casey`.

## Fields

| Field | Identifier | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- |
| `Casey` | Antarctica/Casey | AQ, Casey | -66.2833 | 110.5167 |
| `Davis` | Antarctica/Davis | AQ, Davis | -68.5833 | 77.9667 |
| `Macquarie` | Antarctica/Macquarie | AU, Macquarie Island | -54.5000 | 158.9500 |
| `Mawson` | Antarctica/Mawson | AQ, Mawson | -67.6000 | 62.8833 |
| `Palmer` | Antarctica/Palmer | AQ, Palmer | -64.8000 | -64.1000 |
| `Rothera` | Antarctica/Rothera | AQ, Rothera | -67.5667 | -68.1333 |
| `Troll` | Antarctica/Troll | AQ, Troll | -72.0114 | 2.5350 |
| `Vostok` | Antarctica/Vostok | AQ, Vostok | -78.4000 | 106.9000 |

## Links

These fields are links: time zones of their own countries, listed in `zone.tab`, that share the zone lines of a canonical time zone. `IsLink` returns `true` for them, and `Canonical` returns the time zone they link to. Their location metadata is their own, but their history, including that before 1970, is that of the canonical time zone. See [Links and canonical time zones](TzDataTimezone.md#links-and-canonical-time-zones).

| Field | Identifier | Canonical | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- | --- |
| `DumontDUrville` | Antarctica/DumontDUrville | `Pacific.Port_Moresby` | AQ, Dumont-d\'Urville | -66.6667 | 140.0167 |
| `McMurdo` | Antarctica/McMurdo | `Pacific.Auckland` | AQ, New Zealand time - McMurdo, South Pole | -77.8333 | 166.6000 |
| `Syowa` | Antarctica/Syowa | `Asia.Riyadh` | AQ, Syowa | -69.0061 | 39.5900 |

## See also

- [TzDataTimezone](TzDataTimezone.md)
- [Access time zone metadata](../how-to/access-timezone-metadata.md)
