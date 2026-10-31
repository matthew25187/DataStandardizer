namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// The registration tree of a media type (RFC 6838 §3), shown by the prefix of its subtype.
    /// </summary>
    public enum MimeRegistrationTree
    {
        /// <summary>
        /// The standards tree, whose subtypes have no prefix, such as <c>text/plain</c> (RFC 6838 §3.1).
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
        /// The unregistered tree, whose subtypes start with <c>x.</c>, or with <c>x-</c> for types made before RFC 6838 (RFC 6838 §3.4).
        /// </summary>
        Unregistered = 3,
    }
}
