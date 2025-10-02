# Access timezone metadata

There is metadata associated with each of the timezones in tz database.  This information can be retrieved using extension methods on an instance of `TzDataTimezone`.

## Country codes

You can retrieve a collection of the ISO 3166 codes for the countries covered by a timezone.

    var timezoneCountryCodes = TzDataTimezone.Europe.Andorra.GetIsoCountryCodes()

## Location

Timezones defined by the tz database have a location for the principal city within the timezone after which the timezone is typically named.

    var timezoneLatitude = TzDataTimezone.Australia.Brisbane.GetLatitude()
    var timezoneLongitude = TzDataTimezone.Australia.Brisbane.GetLongitude()

## Comment

Some timezones may have a comment with additional information about the timezone.

    var timezoneComment = TzDataTimezone.Europe.Berlin.GetComment()