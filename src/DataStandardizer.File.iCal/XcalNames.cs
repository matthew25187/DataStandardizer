namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The namespace and element names of xCal (RFC 6321) that are not iCalendar names.
    /// </summary>
    internal static class XcalNames
    {
        /// <summary>
        /// The namespace of xCal elements (RFC 6321 §7.1).
        /// </summary>
        internal const string Namespace = "urn:ietf:params:xml:ns:icalendar-2.0";

        /// <summary>
        /// The root element, which holds the iCalendar stream (RFC 6321 §3.2).
        /// </summary>
        internal const string ICalendar = "icalendar";

        /// <summary>
        /// The element holding the properties of a component.
        /// </summary>
        internal const string Properties = "properties";

        /// <summary>
        /// The element holding the components nested in a component.
        /// </summary>
        internal const string Components = "components";

        /// <summary>
        /// The element holding the parameters of a property (RFC 6321 §3.5).
        /// </summary>
        internal const string Parameters = "parameters";

        /// <summary>
        /// The value element for a value whose type is not known (RFC 6321 §5).
        /// </summary>
        internal const string Unknown = "unknown";

        // ref. RFC 6321§3.6.9: the parts of a PERIOD value.
        internal const string Start = "start";
        internal const string End = "end";
        internal const string Duration = "duration";

        // ref. RFC 6321§3.4.1.2: the parts of a GEO value.
        internal const string Latitude = "latitude";
        internal const string Longitude = "longitude";

        // ref. RFC 6321§3.4.1.3: the parts of a REQUEST-STATUS value.
        internal const string Code = "code";
        internal const string Description = "description";
        internal const string Data = "data";

        /// <summary>
        /// The rule part of a RECUR value whose value is a DATE or DATE-TIME (RFC 6321 §3.6.10).
        /// </summary>
        internal const string Until = "until";
    }
}
