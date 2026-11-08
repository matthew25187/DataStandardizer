using System;

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// Accommodations for a Subtag Registry whose line breaks don't follow the record-jar format of RFC 5646 §3.1.1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, a registry is read by the RFC 5646 §3.1.1 grammar, which ends every line, the last one included, with the CRLF sequence. A line
    /// that ends with a line feed alone, or a last line with no line break, throws a <see cref="FormatException"/> that names the line.
    /// </para>
    /// <para>
    /// IANA publishes the registry, at <c>https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry</c>, with a line
    /// feed alone at the end of each line, so loading that file needs <see cref="AcceptBareLineFeed"/>.
    /// </para>
    /// </remarks>
    [Flags]
    public enum SubtagRegistryLoadOptions
    {
        /// <summary>
        /// Read strictly, with CRLF at the end of every line (RFC 5646 §3.1.1).
        /// </summary>
        None = 0,

        /// <summary>
        /// Accept a line feed alone as the end of a line, as well as the CRLF that RFC 5646 §3.1.1 requires. This loads the registry as IANA
        /// publishes it.
        /// </summary>
        /// <remarks>
        /// A carriage return that no line feed follows is never a line break. It is a control character, which a line may not hold
        /// (RFC 5646 §3.1.1), so it still throws a <see cref="FormatException"/>.
        /// </remarks>
        AcceptBareLineFeed = 1 << 0,

        /// <summary>
        /// Accept a last line with no line break after it. RFC 5646 §3.1.1 ends every field and every <c>%%</c> line, the last one included, with
        /// CRLF.
        /// </summary>
        AcceptMissingFinalLineBreak = 1 << 1,
    }
}
