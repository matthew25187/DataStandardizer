using System;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Maps the <c>TYPE</c> tokens that vCard 3.0 and vCard 2.1 use for inline binary values, such as <c>JPEG</c>, to the media types an RFC 2397 <c>data:</c> URI holds in vCard 4.0.
    /// See <see cref="MimeDataUri"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A token is mapped only where a media type stands for that format.  The other formats of vCard 2.1 §2.9 (<c>MET</c>, <c>PMB</c>, <c>DIB</c>, <c>PICT</c>, <c>AVI</c> and
    /// <c>AIFF</c>) have no media type in the IANA registry, so they have no mapping here.
    /// </para>
    /// <para>
    /// Tokens are compared case-insensitively.  So are media types, which are compared by their type and subtype alone.
    /// </para>
    /// </remarks>
    public static class VcardLegacyMediaTypes
    {
        private sealed class Entry
        {
            internal Entry(string token, string type, string subtype, bool isVersion21Token)
            {
                Token = token;
                Type = type;
                Subtype = subtype;
                IsVersion21Token = isVersion21Token;
            }

            internal string Token { get; }

            internal string Type { get; }

            internal string Subtype { get; }

            internal bool IsVersion21Token { get; }
        }

        // The first entry for a media type is the token written for it.
        private static readonly Entry[] Entries =
        {
            // ref. vCard 2.1 §2.9: knowntype.  RFC 2426 §3.1.4 asks for the IANA names, which these are for images.
            new Entry("GIF", "image", "gif", true),
            new Entry("CGM", "image", "cgm", true),
            new Entry("WMF", "image", "wmf", true),
            new Entry("BMP", "image", "bmp", true),
            new Entry("TIFF", "image", "tiff", true),
            new Entry("JPEG", "image", "jpeg", true),
            new Entry("PNG", "image", "png", false),
            new Entry("PDF", "application", "pdf", true),
            new Entry("PS", "application", "postscript", true),
            new Entry("QTIME", "video", "quicktime", true),
            new Entry("MPEG", "video", "mpeg", true),
            new Entry("MPEG2", "video", "mpeg", true),

            // ref. RFC 2361: audio/vnd.wave.  "audio/wav" is what software commonly writes, and is taken for the same format.
            new Entry("WAVE", "audio", "vnd.wave", true),
            new Entry("WAVE", "audio", "wav", true),

            // ref. RFC 2426 §3.6.6: BASIC; vCard 2.1: PCM is "the 8-bit mu-law (PCM) format specified by the MIME specification".
            new Entry("BASIC", "audio", "basic", false),
            new Entry("PCM", "audio", "basic", true),

            // ref. vCard 2.1 §2.7.2 and RFC 2426 §3.7.2: the types of a public key.
            new Entry("X509", "application", "pkix-cert", true),
            new Entry("PGP", "application", "pgp-keys", true)
        };

        /// <summary>
        /// Gets the media type a token stands for.
        /// </summary>
        /// <param name="token">The token, such as <c>JPEG</c>.</param>
        /// <param name="mediaType">The media type, if the token has one.</param>
        /// <returns><c>true</c> if the token has a media type; otherwise <c>false</c>.</returns>
        public static bool TryGetMediaType(ReadOnlySpan<char> token, out MimeMediaType mediaType)
        {
            foreach (var entry in Entries)
            {
                if (VcardValueFormatting.EqualsKeyword(token, entry.Token))
                {
                    mediaType = new MimeMediaType(entry.Type, entry.Subtype);
                    return true;
                }
            }

            mediaType = default(MimeMediaType);
            return false;
        }

        /// <summary>
        /// Gets the token a version writes for a media type.
        /// </summary>
        /// <param name="mediaType">The media type.  Its parameters are ignored.</param>
        /// <param name="version">The version.  vCard 2.1 writes only the tokens of its §2.9.</param>
        /// <param name="token">The token, if the version has one for the media type.</param>
        /// <returns><c>true</c> if the version has a token for the media type; otherwise <c>false</c>, which vCard 4.0 always gives since it uses media types.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="mediaType"/> is not initialised.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryGetToken(MimeMediaType mediaType, VcardVersion version, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? token)
#else
        public static bool TryGetToken(MimeMediaType mediaType, VcardVersion version, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out string token)
#endif
        {
            VcardValueFormatting.CheckVersion(version);

            var type = mediaType.Type;
            var subtype = mediaType.Subtype;
            if (version != VcardVersion.V4_0)
            {
                foreach (var entry in Entries)
                {
                    if ((version == VcardVersion.V3_0 || entry.IsVersion21Token) &&
                        string.Equals(type, entry.Type, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(subtype, entry.Subtype, StringComparison.OrdinalIgnoreCase))
                    {
                        token = entry.Token;
                        return true;
                    }
                }
            }

            token = null;
            return false;
        }
    }
}
