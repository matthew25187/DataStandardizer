using System.Text;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Options to configure the behaviour of an iCalendar reader or writer.
    /// </summary>
    /// <remarks>
    /// The default settings for all options ensure behaviour consistent with RFC 5545.  Each accommodation for common non-compliant input is a
    /// separate option, off by default.
    /// </remarks>
#if NET5_0_OR_GREATER
    public sealed record IcalOptions
#else
    public sealed class IcalOptions
#endif
    {
        /// <summary>
        /// Gets a flag indicating if a bare line feed is accepted as a line break, both to end a content line and in a fold.
        /// </summary>
        /// <remarks>
        /// RFC 5545 §3.1 requires a CRLF sequence.  When <c>false</c> (the default), a bare line feed causes an <see cref="IcalFileException"/>.
        /// A bare carriage return is never a line break.
        /// </remarks>
        public bool AcceptBareLineFeed
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets the encoding for the iCalendar data.
        /// </summary>
        /// <remarks>
        /// Applies only when the reader or writer is created from a <see cref="System.IO.Stream"/> or file path.  When <c>null</c> (the default),
        /// UTF-8 is used, which is the default charset of RFC 5545 §3.1.4.  A byte order mark in the data takes precedence.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Encoding? Encoding
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }
#else
        public Encoding Encoding { get; set; }
#endif

        /// <summary>
        /// Gets the maximum length, in characters, of a logical (unfolded) content line, not counting its line break.
        /// </summary>
        /// <remarks>
        /// RFC 5545 sets no limit, so the default is <c>null</c>, meaning no limit.  The reader keeps each logical line in memory, so setting a
        /// limit is recommended when reading untrusted data.  A longer line causes an <see cref="IcalFileException"/>.
        /// </remarks>
        public int? MaxLineLength
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }
    }
}
