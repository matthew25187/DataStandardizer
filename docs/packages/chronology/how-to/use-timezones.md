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

## Next steps

- [Access time zone metadata](access-timezone-metadata.md)
