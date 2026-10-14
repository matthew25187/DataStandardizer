namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The names of the components that RFC 5545 defines.
    /// </summary>
    /// <remarks>
    /// Component names are case-insensitive; these are in the upper case the RFC uses.  Compare them with
    /// <see cref="IcalComponent.Name"/>, or use them to create an <see cref="IcalComponent"/>.
    /// </remarks>
    public static class IcalComponentNames
    {
        /// <summary>
        /// <c>VCALENDAR</c> (RFC 5545 §3.4): an iCalendar object.
        /// </summary>
        public const string Calendar = "VCALENDAR";

        /// <summary>
        /// <c>VEVENT</c> (RFC 5545 §3.6.1): an event.
        /// </summary>
        public const string Event = "VEVENT";

        /// <summary>
        /// <c>VTODO</c> (RFC 5545 §3.6.2): a to-do.
        /// </summary>
        public const string Todo = "VTODO";

        /// <summary>
        /// <c>VJOURNAL</c> (RFC 5545 §3.6.3): a journal entry.
        /// </summary>
        public const string Journal = "VJOURNAL";

        /// <summary>
        /// <c>VFREEBUSY</c> (RFC 5545 §3.6.4): free or busy time information.
        /// </summary>
        public const string FreeBusy = "VFREEBUSY";

        /// <summary>
        /// <c>VTIMEZONE</c> (RFC 5545 §3.6.5): a time zone definition.
        /// </summary>
        public const string TimeZone = "VTIMEZONE";

        /// <summary>
        /// <c>STANDARD</c> (RFC 5545 §3.6.5): the standard time observance of a time zone.
        /// </summary>
        public const string Standard = "STANDARD";

        /// <summary>
        /// <c>DAYLIGHT</c> (RFC 5545 §3.6.5): the daylight saving time observance of a time zone.
        /// </summary>
        public const string Daylight = "DAYLIGHT";

        /// <summary>
        /// <c>VALARM</c> (RFC 5545 §3.6.6): an alarm.
        /// </summary>
        public const string Alarm = "VALARM";
    }
}
