---
title: Use time zones
parent: Chronology
grand_parent: Packages
nav_order: 1
---

# Use time zones

Individual time zones are represented by instances of the `TzDataTimezone` type.
You don't create instances of `TzDataTimezone` yourself; instead you use the
predefined instances provided for each of the time zones in the TZ Database.

The naming convention for the TZ Database indicates the area covered by the time
zone as a hierarchical identifier. This implementation replicates that
hierarchical convention using nested classes within the `TzDataTimezone` type.

To refer to a specific time zone, use dot notation to access the nested members.
For a continent-based time zone that is typically a two-part reference:

```csharp
// Africa/Casablanca time zone
var timezone = TzDataTimezone.Africa.Casablanca;
```

Some time zones have an identifier with more than two components. They are
accessed in the same way:

```csharp
// America/Argentina/Buenos_Aires time zone
var timezone = TzDataTimezone.America.Argentina.Buenos_Aires;
```

## Get a time zone from its identifier

When the identifier is only known at run time, for example from user input or a
stored setting, use `TryParse` or `Parse`. They return the predefined instance
with that identifier, with all its metadata:

```csharp
if (TzDataTimezone.TryParse("Pacific/Auckland", out var timezone))
{
    // timezone == TzDataTimezone.Pacific.Auckland
}

// Throws FormatException for an unknown identifier
var berlin = TzDataTimezone.Parse("Europe/Berlin");
```

Identifiers are case-sensitive, as they are in the TZ Database, so
`"europe/berlin"` is not recognised.

On .NET 8 and later, `TzDataTimezone` implements `IParsable<TzDataTimezone>`, so
it can be used wherever a parsable type is expected.

Prefer `Parse` and `TryParse` to the explicit cast from `string`. The cast
accepts any string, including identifiers that don't exist, and the resulting
instance throws `InvalidOperationException` when its metadata is read.

## Work with links and deprecated names

Some time zones, such as `Europe/Oslo`, are links: they have their own field and
location, but share the history of a canonical time zone, here `Europe/Berlin`.
Use `IsLink` and `Canonical` to find out:

```csharp
var oslo = TzDataTimezone.Parse("Europe/Oslo");   // TzDataTimezone.Europe.Oslo
bool isLink = oslo.IsLink;                        // true
var canonical = oslo.Canonical;                   // TzDataTimezone.Europe.Berlin

// Equality is by identifier; compare Canonical to ask whether histories are shared
bool sameZone = oslo == TzDataTimezone.Europe.Berlin;                        // false
bool sameRules = oslo.Canonical == TzDataTimezone.Europe.Berlin.Canonical;   // true
```

A link's history before 1970 is its canonical time zone's, so
`TzDataTimezone.Europe.Oslo.ZoneLines` shows Berlin's history.

Deprecated names that older systems still emit, such as `Asia/Calcutta` or
`US/Eastern`, have no fields. `Parse` and `TryParse` return the canonical time
zone for them:

```csharp
var kolkata = TzDataTimezone.Parse("Asia/Calcutta");   // TzDataTimezone.Asia.Kolkata
```

## Next steps

- [Access time zone metadata](access-timezone-metadata.md)
- [Calculate UTC offsets](calculate-utc-offsets.md)
