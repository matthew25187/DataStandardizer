---
title: TzDataTimezone.Etc time zones
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimezone.Etc time zones

Predefined time zones under `TzDataTimezone.Etc`, members of [TzDataTimezone](TzDataTimezone.md). Access one by name, e.g. `TzDataTimezone.Etc.UTC`.

`TzDataTimezone.Etc` holds two time zones from the TZ Database's `etcetera` file: `Etc/UTC`, Coordinated Universal Time, and `Etc/GMT`, Greenwich Mean Time. Neither is listed in `zone1970.tab` or `zone.tab`, as they are used in no country. `default(TzDataTimezone)` is equal to `Etc/UTC`. See [The default instance](TzDataTimezone.md#the-default-instance). `Etc/GMT` is the time zone that `GMT`, `Greenwich` and the other names for it in `etcetera` and `backward` link to, so `Parse("GMT")` returns `TzDataTimezone.Etc.GMT`.

The other zones of `etcetera`, `Etc/GMT-14` to `Etc/GMT+12`, have fixed offsets from universal time and are used in no country. They aren't included: `Parse` and `TryParse` reject their identifiers. To work with a fixed offset, use a `TimeSpan` or `DateTimeOffset`.

## Fields

| Field | Identifier | Country codes | Latitude | Longitude |
| --- | --- | --- | --- | --- |
| `UTC` | Etc/UTC | | 0.0000 | 0.0000 |
| `GMT` | Etc/GMT | | 0.0000 | 0.0000 |

`Etc/UTC` and `Etc/GMT` each have a single zone line, with an offset of 0 and no daylight saving time. Their abbreviations are `UTC` and `GMT`. They have no location, so their latitudes and longitudes are 0.

## See also

- [TzDataTimezone](TzDataTimezone.md)
- [Calculate UTC offsets](../how-to/calculate-utc-offsets.md)
