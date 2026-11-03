namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The kinds of problem that an <see cref="ImfDiagnostic"/> reports.
    /// </summary>
    public enum ImfDiagnosticKind
    {
        /// <summary>
        /// A body has a Content-Transfer-Encoding that is an extension mechanism (RFC 2045 §6.3), so it could not be decoded.
        /// </summary>
        UnknownTransferEncoding = 0,

        /// <summary>
        /// The header of a message has a MIME field and no MIME-Version field, which RFC 2045 §4 requires.
        /// </summary>
        MissingMimeVersion = 1,

        /// <summary>
        /// The header has no field that RFC 5322 requires: Date or From (§3.6), Sender when From has more than one mailbox (§3.6.2, RFC 6854 §2.1), or in a block of resent
        /// fields, Resent-Date, Resent-From, or Resent-Sender when Resent-From has more than one mailbox (§3.6.6).
        /// </summary>
        MissingRequiredField = 2,

        /// <summary>
        /// The header has more than one of a field that the table in RFC 5322 §3.6 allows at most once.
        /// </summary>
        CardinalityViolation = 3,

        /// <summary>
        /// The body of a field that RFC 5322 §3.6 defines does not follow its grammar.
        /// </summary>
        InvalidFieldValue = 4,
    }
}
