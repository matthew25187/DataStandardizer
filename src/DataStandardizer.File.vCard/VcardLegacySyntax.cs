using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The lexical features of vCard 2.1 that a content line uses.
    /// </summary>
    /// <remarks>
    /// The flags say what the line has, not whether it is allowed where it is.  In a vCard 2.1 card all of them are.  A <c>CHARSET</c> or
    /// <c>ENCODING=QUOTED-PRINTABLE</c> parameter in a later version is an unknown parameter, which <see cref="VcardContentLineReader"/> reads as any other,
    /// so the flag is how to find out that it is there.
    /// </remarks>
    [Flags]
    public enum VcardLegacySyntax
    {
        /// <summary>
        /// The line uses none of them.
        /// </summary>
        None = 0,

        /// <summary>
        /// A parameter with no name, only a value, as in <c>TEL;HOME;VOICE:</c>.
        /// </summary>
        ValuelessParameter = 1 << 0,

        /// <summary>
        /// The <c>QUOTED-PRINTABLE</c> encoding (vCard 2.1 §2.1.5).
        /// </summary>
        QuotedPrintable = 1 << 1,

        /// <summary>
        /// The <c>BASE64</c> encoding (vCard 2.1 §2.1.5), whose value is on the lines that follow and ends at an empty line.  It isn't the <c>b</c> encoding of
        /// vCard 3.0 (RFC 2426 §3.1.4).
        /// </summary>
        Base64 = 1 << 2,

        /// <summary>
        /// The <c>7BIT</c> encoding (vCard 2.1 §2.1.5), named.
        /// </summary>
        SevenBit = 1 << 3,

        /// <summary>
        /// The <c>8BIT</c> encoding (vCard 2.1 §2.1.5).
        /// </summary>
        EightBit = 1 << 4,

        /// <summary>
        /// The <c>CHARSET</c> parameter (vCard 2.1 §2.1.6).
        /// </summary>
        Charset = 1 << 5
    }
}
