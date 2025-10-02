# Use timezones

Individual timezones are represented by instances of the `TzDataTimezone` type.  You don't create instances of `TzDataTimezone` yourself, but rather make use of predefined instances for each of the timezones from tz database.

The naming convention for tz database indicates the area covered by the timezone as a hierarchical identifier.  This implementation of tz database replicates this hierarchical convention by using nested classes in the `TzDataTimezone` type.

To refer to a specific timezone use dot notation to access nested members in the `TzDataTimezone` type.  For a continent-based timezone that will typically be a two-part nested reference, e.g.

    // Africa/Casablanca time zone
    var timezone = TzDataTimezone.Africa.Casablanca;

Some timezones have a multi-part identifier with more than 2 components.  They are accessed in a similar manner.

    // America/Argentina/Buenos_Aires time zone
    var timezone = TzDataTimezone.America.Argentina.Buenos_Aires;