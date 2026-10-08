---
title: TzDataExtensions Class
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataExtensions Class

## Definition

Namespace: `DataStandardizer.Chronology`

Extension methods on [TzDataTimezone](TzDataTimezone.md) that calculate the
offset from universal time, daylight saving time, transitions and abbreviations
of a time zone at an instant, from its zone lines. Also holds the deprecated
metadata accessors, which read the
[TzDataTimezoneAttribute](TzDataTimezoneAttribute.md) attached to its static
fields.

```csharp
public static class TzDataExtensions
```

## Remarks

### Offsets, transitions and abbreviations

Each calculation method has an overload that takes a `DateTime` and one that
takes a `DateTimeOffset`.

- A `DateTime` argument is an instant in universal time. A `DateTime` of kind
  `Unspecified` is treated as universal time, and one of kind `Local` throws
  `ArgumentException`.
- A `DateTimeOffset` argument is converted to universal time through its
  `UtcDateTime`.
- Every method throws `InvalidOperationException` for `default(TzDataTimezone)`
  and for an instance whose identifier is not that of a known time zone.

The calculations follow the TZ Database's reference compiler, `zic`. Rules that
continue indefinitely are applied to any future year. Leap seconds are ignored.
Data before 1970 is guaranteed only for the zone's principal location. See
[Calculate UTC offsets](../how-to/calculate-utc-offsets.md#limitations).

The TZ Database decides which time is daylight saving time and which is
standard time. Europe/Dublin saves a negative hour in winter, so its winter time
(`GMT`) is daylight saving time, and its summer time (`IST`) is standard time.

Abbreviations are the TZ Database's own, formatted from the `FORMAT` of the zone
line in effect as `zic` formats them. Many are numeric, such as `+04`. Long and
localized names such as "Eastern Standard Time" come from the Unicode CLDR and
are not available.

### Deprecated metadata accessors

`GetComment`, `GetIsoCountryCodes`, `GetLatitude` and `GetLongitude` are
deprecated: each is marked `[Obsolete]` and will be removed in version 2.0. Use
the `Comment`, `IsoCountryCodes`, `Latitude` and `Longitude` properties of
[TzDataTimezone](TzDataTimezone.md) instead. See
[Access time zone metadata](../how-to/access-timezone-metadata.md#migrating-from-the-deprecated-apis).

Each deprecated accessor locates the time zone's declaring field, reads its
`TzDataTimezoneAttribute`, and returns the requested metadata, or a default
(`null`, `0` or an empty array) for an unknown time zone. On `net8.0`/`net10.0`
`GetComment` returns `string?`; on the .NET Standard targets it returns `string`
annotated `[CanBeNull]`.

## Methods

### Extension

Every calculation method below also has an overload that takes a
`DateTimeOffset` in place of each `DateTime`, for example
`GetUtcOffset(DateTimeOffset instant)`.

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `GetAbbreviation(DateTime utc)` | `TzDataTimezone` | `string` | The abbreviation in effect at the instant, for example `EST`, `BST` or `+0545`. The same as `TzDataOffsetInfo.Abbreviation`. |
| `GetDaylightAbbreviation(DateTime utc)` | `TzDataTimezone` | `string?` | The daylight saving time abbreviation for the zone line and year in effect at the instant, for example `EDT` or `BST`, or `null` if no daylight saving time is observed in the year. Returns `string` (`[CanBeNull]`) on the .NET Standard targets. |
| `GetDaylightSavings(DateTime utc)` | `TzDataTimezone` | `TimeSpan` | The amount added to the standard offset at the instant. May be negative, as for Europe/Dublin in winter. |
| `GetOffsetInfo(DateTime utc)` | `TzDataTimezone` | [`TzDataOffsetInfo`](TzDataOffsetInfo.md) | The offset, its parts and abbreviation at the instant, with the period over which they are in effect. |
| `GetStandardAbbreviation(DateTime utc)` | `TzDataTimezone` | `string` | The standard time abbreviation for the zone line and year in effect at the instant, for example `EST` or `GMT`. |
| `GetStandardOffset(DateTime utc)` | `TzDataTimezone` | `TimeSpan` | The offset of standard time from universal time at the instant, excluding daylight saving. |
| `GetTransitions(DateTime fromUtc, DateTime toUtc)` | `TzDataTimezone` | `IEnumerable<`[`TzDataTransition`](TzDataTransition.md)`>` | The changes of offset from `fromUtc`, inclusive, to `toUtc`, exclusive, in chronological order. A transition is reported where the offset, the standard offset, daylight saving time or the abbreviation changes. Throws `ArgumentOutOfRangeException` if `toUtc` precedes `fromUtc`. The arguments are checked when the method is called, not when the result is enumerated. |
| `GetUtcOffset(DateTime utc)` | `TzDataTimezone` | `TimeSpan` | The total offset from universal time at the instant, including daylight saving. |
| `GetZoneLine(DateTime utc)` | `TzDataTimezone` | [`TzDataZoneLine`](TzDataZoneLine.md) | The zone line in effect at the instant. |
| `IsDaylightSavingTime(DateTime utc)` | `TzDataTimezone` | `bool` | `true` if daylight saving time is in effect at the instant; otherwise, `false`. |
| `GetComment()` | `TzDataTimezone` | `string?` | **Deprecated** — use `TzDataTimezone.Comment`. Timezone comment, or `null` if unavailable. Returns `string` (`[CanBeNull]`) on the .NET Standard targets. |
| `GetIsoCountryCodes()` | `TzDataTimezone` | `string[]` | **Deprecated** — use `TzDataTimezone.IsoCountryCodes`. ISO 3166-1 Alpha-2 codes for the countries the zone covers (empty if none). |
| `GetLatitude()` | `TzDataTimezone` | `double` | **Deprecated** — use `TzDataTimezone.Latitude`. Latitude of the zone's principal location (`0` if unavailable). |
| `GetLongitude()` | `TzDataTimezone` | `double` | **Deprecated** — use `TzDataTimezone.Longitude`. Longitude of the zone's principal location (`0` if unavailable). |

### Standard and daylight abbreviations

`GetStandardAbbreviation` and `GetDaylightAbbreviation` format the `FORMAT` of
the zone line in effect at the instant:

- `%s` is replaced by the `LETTER` of the latest standard time rule, or the
  latest daylight saving time rule, in effect in the year on the local clock.
- A `FORMAT` of the form `A/B`, such as `GMT/BST`, gives `A` for standard time
  and `B` for daylight saving time.
- `%z` gives the standard offset, or the offset with daylight saving, as `+hh`,
  `+hhmm` or `+hhmmss`, using the shortest form that gives the offset exactly.
- A zone line that saves a fixed amount observes daylight saving time where the
  amount is not zero.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Calculate UTC offsets](../how-to/calculate-utc-offsets.md)
- [Access time zone metadata](../how-to/access-timezone-metadata.md)
- [Use time zones](../how-to/use-timezones.md)
- [TzDataTimezone](TzDataTimezone.md)
- [TzDataOffsetInfo](TzDataOffsetInfo.md)
- [TzDataTransition](TzDataTransition.md)
- [TzDataZoneLine](TzDataZoneLine.md)
- [TzDataTimezoneAttribute](TzDataTimezoneAttribute.md)
- [Chronology API reference](index.md)
