---
title: Access time zone metadata
parent: Chronology
grand_parent: Packages
nav_order: 2
---

# Access time zone metadata

Each time zone in the TZ Database has associated metadata. This information is
retrieved through properties of `TzDataTimezone`.

## Country codes

Retrieve the ISO 3166 codes for the countries covered by a time zone:

```csharp
IReadOnlyList<string> timezoneCountryCodes = TzDataTimezone.Europe.Andorra.IsoCountryCodes;
```

## Location

Time zones defined by the TZ Database have a location for the principal city
within the zone — typically the city the zone is named after:

```csharp
var timezoneLatitude = TzDataTimezone.Australia.Brisbane.Latitude;
var timezoneLongitude = TzDataTimezone.Australia.Brisbane.Longitude;
```

## Comment

Some time zones carry a comment with additional information. The comment is
`null` for a time zone that has none:

```csharp
var timezoneComment = TzDataTimezone.Europe.Berlin.Comment;
```

## Time zones created from a string

Use `Parse` or `TryParse` to get a time zone from its identifier. They return
the predefined instance, so it has the same metadata:

```csharp
var timezone = TzDataTimezone.Parse("Europe/Berlin");
var timezoneComment = timezone.Comment;
```

A time zone created by explicit cast from its identifier also returns the same
metadata as the predefined instance, but the cast doesn't validate the
identifier. The properties throw `InvalidOperationException` for the `default`
value and for an identifier that is not that of a predefined instance, so prefer
`Parse` or `TryParse`.

## Migrating from the deprecated APIs

The `GetIsoCountryCodes`, `GetLatitude`, `GetLongitude` and `GetComment`
extension methods, and the `TzDataTimezoneAttribute` they read, are deprecated
and will be removed in version 2.0. Replace each call with the matching
property:

| Deprecated | Replacement |
| --- | --- |
| `timezone.GetIsoCountryCodes()` | `timezone.IsoCountryCodes` |
| `timezone.GetLatitude()` | `timezone.Latitude` |
| `timezone.GetLongitude()` | `timezone.Longitude` |
| `timezone.GetComment()` | `timezone.Comment` |
| `TzDataTimezoneAttribute`, read by reflection | The properties above |

The properties differ from the deprecated methods in two ways:

- `IsoCountryCodes` returns a read-only `IReadOnlyList<string>` rather than a
  `string[]`.
- The properties throw `InvalidOperationException` for an unknown time zone,
  where the deprecated methods return `null`, `0` or an empty array.
