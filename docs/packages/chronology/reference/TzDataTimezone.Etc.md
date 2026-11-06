---
title: TzDataTimezone.Etc time zones
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimezone.Etc time zones

Predefined time zones under `TzDataTimezone.Etc`, members of [TzDataTimezone](TzDataTimezone.md). Access one by name, e.g. `TzDataTimezone.Etc.UTC`.

`TzDataTimezone.Etc` holds `Etc/UTC`, Coordinated Universal Time, from the TZ Database's `etcetera` file. It isn't listed in `zone1970.tab` or `zone.tab`, as it is used in no country. `default(TzDataTimezone)` is equal to it. See [The default instance](TzDataTimezone.md#the-default-instance).

## Time zones

| Field | Identifier | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- |
| `UTC` | Etc/UTC | | 0.0000 | 0.0000 |

`Etc/UTC` has a single zone line, with an offset of 0, the abbreviation `UTC` and no daylight saving time. It has no location, so its latitude and longitude are 0.

## See also

- [TzDataTimezone](TzDataTimezone.md)
- [Calculate UTC offsets](../how-to/calculate-utc-offsets.md)
