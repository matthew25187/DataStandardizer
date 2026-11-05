namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The names of the property parameters that RFC 6350 defines.
    /// </summary>
    /// <remarks>
    /// Parameter names are case-insensitive; these are in the upper case the RFC uses.  Use them with <see cref="VcardContentLine.GetParameter"/>, or to
    /// create a <see cref="VcardContentLineParameter"/>.
    /// </remarks>
    public static class VcardParameterNames
    {
        /// <summary>
        /// <c>ALTID</c> (RFC 6350 §5.4): tags instances of a property as alternative representations of the same logical property.
        /// </summary>
        public const string AlternativeId = "ALTID";

        /// <summary>
        /// <c>CALSCALE</c> (RFC 6350 §5.8): the calendar system of a date or date-time.
        /// </summary>
        public const string CalendarScale = "CALSCALE";

        /// <summary>
        /// <c>GEO</c> (RFC 6350 §5.10): a global position specific to an address.
        /// </summary>
        public const string Geo = "GEO";

        /// <summary>
        /// <c>LABEL</c> (RFC 6350 §6.3.1): a delivery address label.
        /// </summary>
        public const string Label = "LABEL";

        /// <summary>
        /// <c>LANGUAGE</c> (RFC 6350 §5.1): the language of a value.
        /// </summary>
        public const string Language = "LANGUAGE";

        /// <summary>
        /// <c>MEDIATYPE</c> (RFC 6350 §5.7): a hint at the media type of the resource a URI identifies.
        /// </summary>
        public const string MediaType = "MEDIATYPE";

        /// <summary>
        /// <c>PID</c> (RFC 6350 §5.5): identifies a specific instance of a property.
        /// </summary>
        public const string Pid = "PID";

        /// <summary>
        /// <c>PREF</c> (RFC 6350 §5.3): the level of preference, from 1 (most preferred) to 100.
        /// </summary>
        public const string Preference = "PREF";

        /// <summary>
        /// <c>SORT-AS</c> (RFC 6350 §5.9): the strings to sort by.
        /// </summary>
        public const string SortAs = "SORT-AS";

        /// <summary>
        /// <c>TZ</c> (RFC 6350 §5.11): a time zone specific to an address.
        /// </summary>
        public const string TimeZone = "TZ";

        /// <summary>
        /// <c>TYPE</c> (RFC 6350 §5.6): class characteristics of a property.
        /// </summary>
        public const string Type = "TYPE";

        /// <summary>
        /// <c>VALUE</c> (RFC 6350 §5.2): the value type.
        /// </summary>
        public const string Value = "VALUE";
    }
}
