namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The namespace and element names of xCard (RFC 6351) that are not vCard names.
    /// </summary>
    internal static class XcardNames
    {
        /// <summary>
        /// The namespace of xCard elements (RFC 6351 §8.1).
        /// </summary>
        internal const string Namespace = "urn:ietf:params:xml:ns:vcard-4.0";

        /// <summary>
        /// The root element, which holds the cards (RFC 6351 §5).
        /// </summary>
        internal const string VCards = "vcards";

        /// <summary>
        /// The element of a card (RFC 6351 §5).
        /// </summary>
        internal const string VCard = "vcard";

        /// <summary>
        /// The element holding the properties of a group (RFC 6351 §5).
        /// </summary>
        internal const string Group = "group";

        /// <summary>
        /// The attribute of a group element that holds the group's name (RFC 6351 §5).
        /// </summary>
        internal const string GroupName = "name";

        /// <summary>
        /// The element holding the parameters of a property (RFC 6351 §5).
        /// </summary>
        internal const string Parameters = "parameters";

        /// <summary>
        /// The value element for a value whose type is not known (RFC 6351 §6).
        /// </summary>
        internal const string Unknown = "unknown";

        /// <summary>
        /// The value element of TEXT, which also holds each component of ORG (RFC 6351 Appendix A).
        /// </summary>
        internal const string Text = "text";

        /// <summary>
        /// The value element of URI, which also holds the URI of CLIENTPIDMAP (RFC 6351 Appendix A).
        /// </summary>
        internal const string Uri = "uri";

        // ref. RFC 6351 Appendix A: the value elements a DATE-AND-OR-TIME is written in.
        internal const string Date = "date";
        internal const string DateTime = "date-time";
        internal const string Time = "time";

        // ref. RFC 6351 Appendix A §6.2.7: the parts of a GENDER value.
        internal const string Sex = "sex";
        internal const string Identity = "identity";

        // ref. RFC 6351 Appendix A §6.7.7: the source identifier of a CLIENTPIDMAP value.
        internal const string SourceId = "sourceid";

        /// <summary>
        /// The elements of the components of N, by position (RFC 6351 Appendix A §6.2.2).  RFC 9554 adds the last two, and defines no xCard form for them, so their names are those
        /// agreed for this package, after the name component kinds of RFC 9553.
        /// </summary>
        internal static readonly string[] NameComponents = { "surname", "given", "additional", "prefix", "suffix", "surname2", "generation" };

        /// <summary>
        /// The elements of the components of ADR, by position (RFC 6351 Appendix A §6.3.1).  RFC 9554 adds the last eleven, and defines no xCard form for them, so their names are
        /// those agreed for this package, after its <c>ADR-component-</c> rules.
        /// </summary>
        internal static readonly string[] AddressComponents =
        {
            "pobox", "ext", "street", "locality", "region", "code", "country", "room", "apartment", "floor", "streetnumber", "streetname", "building", "block", "subdistrict",
            "district", "landmark", "direction"
        };
    }
}
