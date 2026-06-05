---
title: Access time zone metadata
parent: Chronology
grand_parent: Packages
nav_order: 2
---

# Access time zone metadata

Each time zone in the TZ Database has associated metadata. This information is
retrieved using extension methods on an instance of `TzDataTimezone`.

## Country codes

Retrieve a collection of the ISO 3166 codes for the countries covered by a time
zone:

```csharp
var timezoneCountryCodes = TzDataTimezone.Europe.Andorra.GetIsoCountryCodes();
```

## Location

Time zones defined by the TZ Database have a location for the principal city
within the zone — typically the city the zone is named after:

```csharp
var timezoneLatitude = TzDataTimezone.Australia.Brisbane.GetLatitude();
var timezoneLongitude = TzDataTimezone.Australia.Brisbane.GetLongitude();
```

## Comment

Some time zones carry a comment with additional information:

```csharp
var timezoneComment = TzDataTimezone.Europe.Berlin.GetComment();
```
