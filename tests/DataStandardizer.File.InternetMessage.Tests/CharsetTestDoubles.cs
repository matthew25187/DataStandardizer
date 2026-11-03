using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// A character set resolver that gives one encoding for every character set.
    /// </summary>
    internal sealed class FixedCharsetResolver : IMimeCharsetResolver
    {
        private readonly Encoding _encoding;

        public FixedCharsetResolver(Encoding encoding)
        {
            _encoding = encoding;
        }

#if NETCOREAPP3_0_OR_GREATER
        public bool TryGetEncoding(MimeCharset charset, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Encoding encoding)
#else
        public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
#endif
        {
            encoding = _encoding;
            return true;
        }
    }
}
