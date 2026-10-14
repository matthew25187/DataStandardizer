namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The names of the properties that RFC 5545 and RFC 7986 define.
    /// </summary>
    /// <remarks>
    /// Property names are case-insensitive; these are in the upper case the RFCs use.  Use them with
    /// <see cref="IcalComponent.GetProperty"/>, <see cref="IcalComponent.GetProperties"/> and
    /// <see cref="IcalComponent.RemoveProperties"/>, or to create an <see cref="IcalContentLine"/>.
    /// </remarks>
    public static class IcalPropertyNames
    {
        /// <summary>
        /// <c>CALSCALE</c> (RFC 5545 §3.7.1): the calendar scale.
        /// </summary>
        public const string CalendarScale = "CALSCALE";

        /// <summary>
        /// <c>METHOD</c> (RFC 5545 §3.7.2): the iTIP method.
        /// </summary>
        public const string Method = "METHOD";

        /// <summary>
        /// <c>PRODID</c> (RFC 5545 §3.7.3): the identifier of the product that created the calendar.
        /// </summary>
        public const string ProductId = "PRODID";

        /// <summary>
        /// <c>VERSION</c> (RFC 5545 §3.7.4): the iCalendar version.
        /// </summary>
        public const string Version = "VERSION";

        /// <summary>
        /// <c>ATTACH</c> (RFC 5545 §3.8.1.1): an attachment.
        /// </summary>
        public const string Attachment = "ATTACH";

        /// <summary>
        /// <c>CATEGORIES</c> (RFC 5545 §3.8.1.2): categories.
        /// </summary>
        public const string Categories = "CATEGORIES";

        /// <summary>
        /// <c>CLASS</c> (RFC 5545 §3.8.1.3): the access classification.
        /// </summary>
        public const string Classification = "CLASS";

        /// <summary>
        /// <c>COMMENT</c> (RFC 5545 §3.8.1.4): a comment.
        /// </summary>
        public const string Comment = "COMMENT";

        /// <summary>
        /// <c>DESCRIPTION</c> (RFC 5545 §3.8.1.5): a description.
        /// </summary>
        public const string Description = "DESCRIPTION";

        /// <summary>
        /// <c>GEO</c> (RFC 5545 §3.8.1.6): the geographic position.
        /// </summary>
        public const string Geo = "GEO";

        /// <summary>
        /// <c>LOCATION</c> (RFC 5545 §3.8.1.7): the location.
        /// </summary>
        public const string Location = "LOCATION";

        /// <summary>
        /// <c>PERCENT-COMPLETE</c> (RFC 5545 §3.8.1.8): the percentage of a to-do that is complete.
        /// </summary>
        public const string PercentComplete = "PERCENT-COMPLETE";

        /// <summary>
        /// <c>PRIORITY</c> (RFC 5545 §3.8.1.9): the priority.
        /// </summary>
        public const string Priority = "PRIORITY";

        /// <summary>
        /// <c>RESOURCES</c> (RFC 5545 §3.8.1.10): resources.
        /// </summary>
        public const string Resources = "RESOURCES";

        /// <summary>
        /// <c>STATUS</c> (RFC 5545 §3.8.1.11): the status.
        /// </summary>
        public const string Status = "STATUS";

        /// <summary>
        /// <c>SUMMARY</c> (RFC 5545 §3.8.1.12): the summary.
        /// </summary>
        public const string Summary = "SUMMARY";

        /// <summary>
        /// <c>COMPLETED</c> (RFC 5545 §3.8.2.1): the date and time a to-do was completed.
        /// </summary>
        public const string Completed = "COMPLETED";

        /// <summary>
        /// <c>DTEND</c> (RFC 5545 §3.8.2.2): the end.
        /// </summary>
        public const string DtEnd = "DTEND";

        /// <summary>
        /// <c>DUE</c> (RFC 5545 §3.8.2.3): the due date.
        /// </summary>
        public const string Due = "DUE";

        /// <summary>
        /// <c>DTSTART</c> (RFC 5545 §3.8.2.4): the start.
        /// </summary>
        public const string DtStart = "DTSTART";

        /// <summary>
        /// <c>DURATION</c> (RFC 5545 §3.8.2.5): the duration.
        /// </summary>
        public const string Duration = "DURATION";

        /// <summary>
        /// <c>FREEBUSY</c> (RFC 5545 §3.8.2.6): free or busy time.
        /// </summary>
        public const string FreeBusy = "FREEBUSY";

        /// <summary>
        /// <c>TRANSP</c> (RFC 5545 §3.8.2.7): the time transparency.
        /// </summary>
        public const string Transparency = "TRANSP";

        /// <summary>
        /// <c>TZID</c> (RFC 5545 §3.8.3.1): the time zone identifier of a VTIMEZONE.
        /// </summary>
        public const string TimeZoneId = "TZID";

        /// <summary>
        /// <c>TZNAME</c> (RFC 5545 §3.8.3.2): the customary name of a time zone observance.
        /// </summary>
        public const string TimeZoneName = "TZNAME";

        /// <summary>
        /// <c>TZOFFSETFROM</c> (RFC 5545 §3.8.3.3): the UTC offset in use before an observance begins.
        /// </summary>
        public const string TimeZoneOffsetFrom = "TZOFFSETFROM";

        /// <summary>
        /// <c>TZOFFSETTO</c> (RFC 5545 §3.8.3.4): the UTC offset in use during an observance.
        /// </summary>
        public const string TimeZoneOffsetTo = "TZOFFSETTO";

        /// <summary>
        /// <c>TZURL</c> (RFC 5545 §3.8.3.5): the location of a published time zone definition.
        /// </summary>
        public const string TimeZoneUrl = "TZURL";

        /// <summary>
        /// <c>ATTENDEE</c> (RFC 5545 §3.8.4.1): an attendee.
        /// </summary>
        public const string Attendee = "ATTENDEE";

        /// <summary>
        /// <c>CONTACT</c> (RFC 5545 §3.8.4.2): contact information.
        /// </summary>
        public const string Contact = "CONTACT";

        /// <summary>
        /// <c>ORGANIZER</c> (RFC 5545 §3.8.4.3): the organizer.
        /// </summary>
        public const string Organizer = "ORGANIZER";

        /// <summary>
        /// <c>RECURRENCE-ID</c> (RFC 5545 §3.8.4.4): the recurrence instance a component overrides.
        /// </summary>
        public const string RecurrenceId = "RECURRENCE-ID";

        /// <summary>
        /// <c>RELATED-TO</c> (RFC 5545 §3.8.4.5): a relationship to another component.
        /// </summary>
        public const string RelatedTo = "RELATED-TO";

        /// <summary>
        /// <c>URL</c> (RFC 5545 §3.8.4.6): the location of a more dynamic rendition.
        /// </summary>
        public const string Url = "URL";

        /// <summary>
        /// <c>UID</c> (RFC 5545 §3.8.4.7): the persistent, globally unique identifier.
        /// </summary>
        public const string Uid = "UID";

        /// <summary>
        /// <c>EXDATE</c> (RFC 5545 §3.8.5.1): exception dates.
        /// </summary>
        public const string ExceptionDates = "EXDATE";

        /// <summary>
        /// <c>RDATE</c> (RFC 5545 §3.8.5.2): recurrence dates.
        /// </summary>
        public const string RecurrenceDates = "RDATE";

        /// <summary>
        /// <c>RRULE</c> (RFC 5545 §3.8.5.3): a recurrence rule.
        /// </summary>
        public const string RecurrenceRule = "RRULE";

        /// <summary>
        /// <c>ACTION</c> (RFC 5545 §3.8.6.1): the action of an alarm.
        /// </summary>
        public const string Action = "ACTION";

        /// <summary>
        /// <c>REPEAT</c> (RFC 5545 §3.8.6.2): the number of times an alarm repeats.
        /// </summary>
        public const string Repeat = "REPEAT";

        /// <summary>
        /// <c>TRIGGER</c> (RFC 5545 §3.8.6.3): when an alarm triggers.
        /// </summary>
        public const string Trigger = "TRIGGER";

        /// <summary>
        /// <c>CREATED</c> (RFC 5545 §3.8.7.1): the date and time the information was created.
        /// </summary>
        public const string Created = "CREATED";

        /// <summary>
        /// <c>DTSTAMP</c> (RFC 5545 §3.8.7.2): the date and time the iCalendar object was created or last revised.
        /// </summary>
        public const string DtStamp = "DTSTAMP";

        /// <summary>
        /// <c>LAST-MODIFIED</c> (RFC 5545 §3.8.7.3): the date and time the information was last revised.
        /// </summary>
        public const string LastModified = "LAST-MODIFIED";

        /// <summary>
        /// <c>SEQUENCE</c> (RFC 5545 §3.8.7.4): the revision sequence number.
        /// </summary>
        public const string Sequence = "SEQUENCE";

        /// <summary>
        /// <c>REQUEST-STATUS</c> (RFC 5545 §3.8.8.3): a request status.
        /// </summary>
        public const string RequestStatus = "REQUEST-STATUS";

        /// <summary>
        /// <c>NAME</c> (RFC 7986 §5.1): the name of a calendar.
        /// </summary>
        public const string Name = "NAME";

        /// <summary>
        /// <c>REFRESH-INTERVAL</c> (RFC 7986 §5.7): the suggested minimum interval between polls for changes.
        /// </summary>
        public const string RefreshInterval = "REFRESH-INTERVAL";

        /// <summary>
        /// <c>SOURCE</c> (RFC 7986 §5.8): the location from which a calendar can be refreshed.
        /// </summary>
        public const string Source = "SOURCE";

        /// <summary>
        /// <c>COLOR</c> (RFC 7986 §5.9): a color.
        /// </summary>
        public const string Color = "COLOR";

        /// <summary>
        /// <c>IMAGE</c> (RFC 7986 §5.10): an image.
        /// </summary>
        public const string Image = "IMAGE";

        /// <summary>
        /// <c>CONFERENCE</c> (RFC 7986 §5.11): a conference system.
        /// </summary>
        public const string Conference = "CONFERENCE";
    }
}
