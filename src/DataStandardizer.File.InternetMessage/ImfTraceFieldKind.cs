namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The kinds of trace field (RFC 5322 §3.6.7).
    /// </summary>
    public enum ImfTraceFieldKind
    {
        /// <summary>
        /// A Return-Path field.
        /// </summary>
        ReturnPath = 0,

        /// <summary>
        /// A Received field.
        /// </summary>
        Received = 1,
    }
}
