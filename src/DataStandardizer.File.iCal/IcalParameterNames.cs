namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The names of the property parameters that RFC 5545 and RFC 7986 define.
    /// </summary>
    /// <remarks>
    /// Parameter names are case-insensitive; these are in the upper case the RFCs use.  Use them with
    /// <see cref="IcalContentLine.GetParameter"/>, or to create an <see cref="IcalContentLineParameter"/>.
    /// </remarks>
    public static class IcalParameterNames
    {
        /// <summary>
        /// <c>ALTREP</c> (RFC 5545 §3.2.1): an alternate text representation.
        /// </summary>
        public const string AlternateRepresentation = "ALTREP";

        /// <summary>
        /// <c>CN</c> (RFC 5545 §3.2.2): the common name of a calendar user.
        /// </summary>
        public const string CommonName = "CN";

        /// <summary>
        /// <c>CUTYPE</c> (RFC 5545 §3.2.3): the type of a calendar user.
        /// </summary>
        public const string CalendarUserType = "CUTYPE";

        /// <summary>
        /// <c>DELEGATED-FROM</c> (RFC 5545 §3.2.4): the calendar users that delegated participation.
        /// </summary>
        public const string DelegatedFrom = "DELEGATED-FROM";

        /// <summary>
        /// <c>DELEGATED-TO</c> (RFC 5545 §3.2.5): the calendar users participation was delegated to.
        /// </summary>
        public const string DelegatedTo = "DELEGATED-TO";

        /// <summary>
        /// <c>DIR</c> (RFC 5545 §3.2.6): a directory entry for a calendar user.
        /// </summary>
        public const string Directory = "DIR";

        /// <summary>
        /// <c>ENCODING</c> (RFC 5545 §3.2.7): the inline encoding of a value.
        /// </summary>
        public const string Encoding = "ENCODING";

        /// <summary>
        /// <c>FMTTYPE</c> (RFC 5545 §3.2.8): the media type of a value.
        /// </summary>
        public const string FormatType = "FMTTYPE";

        /// <summary>
        /// <c>FBTYPE</c> (RFC 5545 §3.2.9): whether a time is free or busy.
        /// </summary>
        public const string FreeBusyType = "FBTYPE";

        /// <summary>
        /// <c>LANGUAGE</c> (RFC 5545 §3.2.10): the language of a text value.
        /// </summary>
        public const string Language = "LANGUAGE";

        /// <summary>
        /// <c>MEMBER</c> (RFC 5545 §3.2.11): the groups a calendar user belongs to.
        /// </summary>
        public const string Member = "MEMBER";

        /// <summary>
        /// <c>PARTSTAT</c> (RFC 5545 §3.2.12): the participation status of a calendar user.
        /// </summary>
        public const string ParticipationStatus = "PARTSTAT";

        /// <summary>
        /// <c>RANGE</c> (RFC 5545 §3.2.13): the recurrence instances a recurrence identifier applies to.
        /// </summary>
        public const string Range = "RANGE";

        /// <summary>
        /// <c>RELATED</c> (RFC 5545 §3.2.14): whether a relative trigger is from the start or the end.
        /// </summary>
        public const string Related = "RELATED";

        /// <summary>
        /// <c>RELTYPE</c> (RFC 5545 §3.2.15): the type of a relationship.
        /// </summary>
        public const string RelationshipType = "RELTYPE";

        /// <summary>
        /// <c>ROLE</c> (RFC 5545 §3.2.16): the participation role of a calendar user.
        /// </summary>
        public const string Role = "ROLE";

        /// <summary>
        /// <c>RSVP</c> (RFC 5545 §3.2.17): whether a reply is expected.
        /// </summary>
        public const string Rsvp = "RSVP";

        /// <summary>
        /// <c>SENT-BY</c> (RFC 5545 §3.2.18): the calendar user acting on behalf of another.
        /// </summary>
        public const string SentBy = "SENT-BY";

        /// <summary>
        /// <c>TZID</c> (RFC 5545 §3.2.19): the time zone of a DATE-TIME value.
        /// </summary>
        public const string TimeZoneId = "TZID";

        /// <summary>
        /// <c>VALUE</c> (RFC 5545 §3.2.20): the value type of a property.
        /// </summary>
        public const string Value = "VALUE";

        /// <summary>
        /// <c>DISPLAY</c> (RFC 7986 §6.1): how an image is to be displayed.
        /// </summary>
        public const string Display = "DISPLAY";

        /// <summary>
        /// <c>EMAIL</c> (RFC 7986 §6.2): the email address of a calendar user.
        /// </summary>
        public const string Email = "EMAIL";

        /// <summary>
        /// <c>FEATURE</c> (RFC 7986 §6.3): the features of a conference system.
        /// </summary>
        public const string Feature = "FEATURE";

        /// <summary>
        /// <c>LABEL</c> (RFC 7986 §6.4): a label for a conference system.
        /// </summary>
        public const string Label = "LABEL";
    }
}
