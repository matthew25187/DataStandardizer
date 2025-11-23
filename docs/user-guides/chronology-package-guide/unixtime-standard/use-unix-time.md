# Unix time

Unix time is the means by which time is represented in the Unix operating system.  It is a measure of the number of seconds elapsed since the Unix epoch, being 1 January 1970.  Since then it has become widely used in other contexts and with the *DataStandardizer.Chronology* package you can make use of it in your own .Net applications.

A Unix time value is represented by the `UnixTime` type.  Internally, the time is stored as a signed 64-bit integer.  To create a Unix time value, use the constructor to instantiate the type:

    // Unix time for 1 January 2000 15:00
    var unixTime = new UnixTime(946738800L);

As the type also implements conversion operators, you can also cast integers to the `UnixTime` type.

    // Unix time for 1 January 2000 15:00 by casting
    var unixTime = (UnixTime)946738800L;

With a `UnixTime` value, you can convert to standard .Net date & time types.  For example,

    var unixTime = new UnixTime(946738800L);

    // Convert to a DateTime.
    var unixTimeAsDateTime = unixTime.ToDateTime();

    // Convert to a DateOnly.
    var unixTimeAsDateOnly = unixTime.ToDateOnly();

    // Convert to a TimeOnly.
    var unixTimeAsTimeOnly = unixTime.ToTimeOnly();

Conversions can also go the other way.  Starting with a standard .Net date or time type, you can convert these to a Unix time.

    // Convert DateTime to UnixTime.
    var dateTime = new DateTime(2000, 1, 1, 15, 0, 0);
    var unixTime = dateTime.ToUnixTime();

By itself, a `UnixTime` instance does not carry any date and time semantics.  You can add date & time capabilities to a `UnixTime` by wrapping it in a `SystemTimeWithGregorianCalendar` decorator.

    // Unix time with date & time
    var unixTimeWithDateTime = new SystemTimeWithGregorianCalendar(unixTime);
    var year = unixTimeWithDateTime.Year;
    var month = unixTimeWithDateTime.Month;
    var day = unixTimeWithDateTime.Day;
    var hour = unixTimeWithDateTime.Hour;
    var minute = unixTimeWithDateTime.Minute;
    var second = unixTimeWithDateTime.Second;

As both `UnixTime` and `SystemTimeWithGregorianCalendar` implement the `ISystemTime` interface, you can use an instance of either interchangeably by typing your variable as `ISystemTime`.

If you have a `SystemTimeWithGregorianCalendar`, you can get back the original Unix time like so:

    // Get back Unix time from decorated value.
    var unixTime = unixTimeWithDateTime.AsUnixTime();