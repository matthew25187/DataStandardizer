using System;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The single-bit flags in the <c>flags</c> key of the property list of an EMLX file.
    /// </summary>
    /// <remarks>
    /// Apple doesn't publish these.  They come from community documentation, such as the EMLX import code of Thunderbird, so <see cref="EmlxMetadata.Flags"/> always gives the whole integer too.  The
    /// bits that hold numbers are given by <see cref="EmlxMetadata.AttachmentCount"/> and <see cref="EmlxMetadata.PriorityLevel"/>.
    /// </remarks>
    [Flags]
    public enum EmlxMessageFlags : long
    {
        /// <summary>
        /// None of the flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// The message has been read.
        /// </summary>
        Read = 1L << 0,

        /// <summary>
        /// The message has been deleted.
        /// </summary>
        Deleted = 1L << 1,

        /// <summary>
        /// The message has been answered.
        /// </summary>
        Answered = 1L << 2,

        /// <summary>
        /// The message is encrypted.
        /// </summary>
        Encrypted = 1L << 3,

        /// <summary>
        /// The message has been flagged.
        /// </summary>
        Flagged = 1L << 4,

        /// <summary>
        /// The message is recent.
        /// </summary>
        Recent = 1L << 5,

        /// <summary>
        /// The message is a draft.
        /// </summary>
        Draft = 1L << 6,

        /// <summary>
        /// The message has been forwarded.
        /// </summary>
        Forwarded = 1L << 8,

        /// <summary>
        /// The message has been redirected.
        /// </summary>
        Redirected = 1L << 9,

        /// <summary>
        /// The message is signed.
        /// </summary>
        Signed = 1L << 23,

        /// <summary>
        /// The message is junk.
        /// </summary>
        Junk = 1L << 24,

        /// <summary>
        /// The message is not junk.
        /// </summary>
        NotJunk = 1L << 25,
    }
}
