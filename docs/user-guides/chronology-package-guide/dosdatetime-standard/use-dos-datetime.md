# DOS date/time

Date & time values for the MS-DOS operating system are a pair of packed 16-bit integers representing the date and time of an event, typically a change to an object in the file system.  Valid values range from 1 January 1980 to 31 December 2107.  You can read more about the format in [Microsoft's documentation](https://learn.microsoft.com/en-us/windows/win32/sysinfo/ms-dos-date-and-time).

DOS date/time values are represented by the `DosDateTime` type.  Internally, date/time values are stored as a packed 32-bit integer, which you can retrieve by casting a `DosDateTime` value to an unsigned 32-bit integer:

    // Retrieve the packed integer form of a DOS date/time.
    var packedDateTime = (DosDateTime)dosDateTime;

To create a `DosDateTime` you use the constructor in either of two ways.  First, if you already have a DOS date/time as a packed 32-bit integer, you can use this integer when creating the `DosDateTime`:

    // Create DosDateTime from packed integer.
    var dosDateTime = new DosDateTime(0x28217DA5);

Alternately, you can create a `DosDateTime` using the components of the date and time:

    // Create DosDateTime from the date and time components.
    var dosDateTime = new DosDateTime(1984, 2, 29, 12, 30, 0);

The second form can also be used to create a `DosDateTime` with just the date components if you don't have a time.

With a `DosDateTime` value, you can convert it to standard .Net date and time types.

    var dosDateTime = new DosDateTime(0x85D63C0);

    // Convert DosDateTime to date object.
    var dosDateTimeDateOnly = dosDateTime.ToDateOnly();

    // Convert DosDateTime to date & time object.
    var dosDateTimeDateTime = dosDateTime.ToDateTime();

    // Convert DosDateTime to time object.
    var dosDateTimeTimeOnly = dosDateTime.ToTimeOnly();

Conversions can also go the other way.  Starting with a .Net date or time object, you can create the `DosDateTime` equivalent:

    var myDate = new DateOnly(1999, 12, 31);

    // Convert to DosDateTime.
    var dosDateTime = myDate.ToDosDateTime();

As `DosDateTime` implements the `ISystemTime` interface, you can wrap it in the `SystemTimeWithGregorianCalendar` decorator to add date & time conversion information.  This wrapper adds properties for `Year`, `Month`, `Day`, `Hour`, `Minute`, and `Second`.  A wrapped `DosDateTime` can then be used interchangeably with the original value as both have the `ISystemTime` interface.  If you have a decorated `DosDateTime` value, you can still get back the original value like so:

    // Getting back original date/time.
    var dosDateTime = decoratedDosDateTime.AsDosDateTime();