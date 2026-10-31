using System.Text;
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Gets the <see cref="Encoding"/> for a character set.
    /// </summary>
    /// <remarks>
    /// Implement this to add character sets that <see cref="MimeCharsetResolver.Default"/> doesn't resolve, and pass it to
    /// <see cref="MimeCharsetResolver(IMimeCharsetResolver)"/>.
    /// </remarks>
    public interface IMimeCharsetResolver
    {
        /// <summary>
        /// Gets the encoding for a character set.
        /// </summary>
        /// <param name="charset">The character set.</param>
        /// <param name="encoding">The encoding, if the character set is resolved.</param>
        /// <returns><c>true</c> if the character set is resolved; otherwise <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        bool TryGetEncoding(MimeCharset charset, [NotNullWhen(true)] out Encoding? encoding);
#else
        bool TryGetEncoding(MimeCharset charset, out Encoding encoding);
#endif
    }
}
