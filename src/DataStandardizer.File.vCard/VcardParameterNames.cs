namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The names of the property parameters of vCard 4.0: those RFC 6350 defines, and those of the extensions RFC 6715, RFC 8605 and RFC 9554.
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
        /// <c>AUTHOR</c> (RFC 9554 §4.1): the author of a value, as a URI.
        /// </summary>
        public const string Author = "AUTHOR";

        /// <summary>
        /// <c>AUTHOR-NAME</c> (RFC 9554 §4.2): the name of the author of a value.
        /// </summary>
        public const string AuthorName = "AUTHOR-NAME";

        /// <summary>
        /// <c>CALSCALE</c> (RFC 6350 §5.8): the calendar system of a date or date-time.
        /// </summary>
        public const string CalendarScale = "CALSCALE";

        /// <summary>
        /// <c>CC</c> (RFC 8605 §3.1): the ISO 3166 country code of the country name of an address.
        /// </summary>
        public const string CountryCode = "CC";

        /// <summary>
        /// <c>CREATED</c> (RFC 9554 §4.3): when a property was created in a card.
        /// </summary>
        public const string Created = "CREATED";

        /// <summary>
        /// <c>DERIVED</c> (RFC 9554 §4.4): that a value is derived from other properties of the card.
        /// </summary>
        public const string Derived = "DERIVED";

        /// <summary>
        /// <c>GEO</c> (RFC 6350 §5.10): a global position specific to an address.
        /// </summary>
        public const string Geo = "GEO";

        /// <summary>
        /// <c>INDEX</c> (RFC 6715 §3.1): the position of a value among the instances of a property.
        /// </summary>
        public const string Index = "INDEX";

        /// <summary>
        /// <c>LABEL</c> (RFC 6350 §6.3.1, RFC 9554 §4.5): a delivery address label.
        /// </summary>
        public const string Label = "LABEL";

        /// <summary>
        /// <c>LANGUAGE</c> (RFC 6350 §5.1): the language of a value.
        /// </summary>
        public const string Language = "LANGUAGE";

        /// <summary>
        /// <c>LEVEL</c> (RFC 6715 §3.2): the level of an expertise, hobby or interest.
        /// </summary>
        public const string Level = "LEVEL";

        /// <summary>
        /// <c>MEDIATYPE</c> (RFC 6350 §5.7): a hint at the media type of the resource a URI identifies.
        /// </summary>
        public const string MediaType = "MEDIATYPE";

        /// <summary>
        /// <c>PHONETIC</c> (RFC 9554 §4.6): that a value is the phonetic representation of another property of the same name, and in which system.
        /// </summary>
        public const string Phonetic = "PHONETIC";

        /// <summary>
        /// <c>PID</c> (RFC 6350 §5.5): identifies a specific instance of a property.
        /// </summary>
        public const string Pid = "PID";

        /// <summary>
        /// <c>PREF</c> (RFC 6350 §5.3): the level of preference, from 1 (most preferred) to 100.
        /// </summary>
        public const string Preference = "PREF";

        /// <summary>
        /// <c>PROP-ID</c> (RFC 9554 §4.7): identifies a property among those of the same name.
        /// </summary>
        public const string PropertyId = "PROP-ID";

        /// <summary>
        /// <c>SCRIPT</c> (RFC 9554 §4.8): the script a value is written in.
        /// </summary>
        public const string Script = "SCRIPT";

        /// <summary>
        /// <c>SERVICE-TYPE</c> (RFC 9554 §4.9): the online service of a messaging or social media profile.
        /// </summary>
        public const string ServiceType = "SERVICE-TYPE";

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
        /// <c>USERNAME</c> (RFC 9554 §4.10): the user of a messaging or social media service.
        /// </summary>
        public const string Username = "USERNAME";

        /// <summary>
        /// <c>VALUE</c> (RFC 6350 §5.2): the value type.
        /// </summary>
        public const string Value = "VALUE";
    }
}
