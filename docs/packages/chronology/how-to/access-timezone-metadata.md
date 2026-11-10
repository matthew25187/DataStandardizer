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

## Links

A link, such as `TzDataTimezone.Europe.Oslo`, has its own country code,
location and comment, from `zone.tab`, although it shares the zone lines of its
canonical time zone, `TzDataTimezone.Europe.Berlin`:

```csharp
var osloCountryCodes = TzDataTimezone.Europe.Oslo.IsoCountryCodes;               // NO
var berlinCountryCodes = TzDataTimezone.Europe.Oslo.Canonical.IsoCountryCodes;   // DE, DK, NO, SE, SJ
```

## Time zones created from a string

Use `Parse` or `TryParse` to get a time zone from its identifier. They return
the predefined instance, so it has the same metadata:

```csharp
var timezone = TzDataTimezone.Parse("Europe/Berlin");
var timezoneComment = timezone.Comment;
```

The explicit cast from an identifier returns the predefined instance, so it
returns the same metadata. The cast throws `InvalidCastException` for an
identifier that is not that of a predefined instance. The `default` value is
`Etc/UTC`, which has no countries, no comment, and a latitude and longitude of 0.

## Upgrading from version 1.x

Version 2.0 removes the extension methods that version 1.x provided for this
metadata. See
[Removed in v2.0](../../../resources/removed-in-v2.md#chronology-time-zone-metadata)
for the property that replaces each one.
