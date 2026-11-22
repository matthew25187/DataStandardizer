namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// The registration tree of a media type (RFC 6838 §3), shown by the prefix of its subtype.
    /// </summary>
    public enum MimeRegistrationTree
    {
        /// <summary>
        /// The standards tree, whose subtypes have no facet prefix, such as <c>text/plain</c> (RFC 6838 §3.1). A subtype that starts with <c>x-</c> has no facet prefix, so it is reported here too; RFC 6838 §3.4 takes such subtypes out of the unregistered tree, but that does not make them registrations in the standards tree.
        /// </summary>
        Standards = 0,

        /// <summary>
        /// The vendor tree, whose subtypes start with <c>vnd.</c>, such as <c>application/vnd.ms-excel</c> (RFC 6838 §3.2).
        /// </summary>
        Vendor = 1,

        /// <summary>
        /// The personal or vanity tree, whose subtypes start with <c>prs.</c> (RFC 6838 §3.3).
        /// </summary>
        Personal = 2,

        /// <summary>
        /// The unregistered tree, whose subtypes start with <c>x.</c> (RFC 6838 §3.4).
        /// </summary>
        Unregistered = 3,
    }
}
