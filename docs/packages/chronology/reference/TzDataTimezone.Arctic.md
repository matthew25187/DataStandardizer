---
title: TzDataTimezone.Arctic time zones
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimezone.Arctic time zones

Predefined time zones under `TzDataTimezone.Arctic`, members of [TzDataTimezone](TzDataTimezone.md). Access one by name, e.g. `TzDataTimezone.Arctic.Longyearbyen`.

`TzDataTimezone.Arctic` has no canonical time zones; every field is a link.

## Links

These fields are links: time zones of their own countries, listed in `zone.tab`, that share the zone lines of a canonical time zone. `IsLink` returns `true` for them, and `Canonical` returns the time zone they link to. Their location metadata is their own, but their history, including that before 1970, is that of the canonical time zone. See [Links and canonical time zones](TzDataTimezone.md#links-and-canonical-time-zones).

| Field | Identifier | Canonical | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- | --- |
| `Longyearbyen` | Arctic/Longyearbyen | `Europe.Berlin` | SJ | 78.0000 | 16.0000 |

## See also

- [TzDataTimezone](TzDataTimezone.md)
- [Access time zone metadata](../how-to/access-timezone-metadata.md)
