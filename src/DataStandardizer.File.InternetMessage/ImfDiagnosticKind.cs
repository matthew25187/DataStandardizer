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
    }
}
