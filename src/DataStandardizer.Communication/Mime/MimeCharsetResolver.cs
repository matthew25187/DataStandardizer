using System;
using System.Collections.Generic;
using System.Text;
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Gets the <see cref="Encoding"/> for a character set, from the encodings built into the package, then the encodings the runtime
    /// provides, and then, optionally, a resolver the caller supplies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A character set is resolved by the first of these that knows its name:
    /// </para>
    /// <list type="number">
    /// <item>The built-in encodings, which give the same result on every target and runtime: <c>US-ASCII</c>, <c>ISO-8859-1</c>,
    /// <c>UTF-8</c>, <c>UTF-16</c>, <c>UTF-16BE</c>, <c>UTF-16LE</c>, <c>UTF-32</c>, <c>UTF-32BE</c> and <c>UTF-32LE</c>, and their
    /// aliases in the IANA registry, such as <c>latin1</c> and <c>csUTF8</c>.</item>
    /// <item><see cref="Encoding.GetEncoding(string)"/>, which knows the encodings of the runtime, and those of any
    /// <see cref="EncodingProvider"/> registered with <see cref="Encoding.RegisterProvider(EncodingProvider)"/>.</item>
    /// <item>The resolver passed to <see cref="MimeCharsetResolver(IMimeCharsetResolver)"/>, if any.</item>
    /// </list>
    /// <para>
    /// A character set that none of these knows is not resolved. It is up to the caller whether that is an error. The machine's default
    /// encoding, <c>Encoding.Default</c>, is never used.
    /// </para>
    /// <para>
    /// The built-in encodings don't throw: they read bytes that are not valid in the character set as U+FFFD, and write characters that
    /// the character set can't hold as <c>?</c> (<c>US-ASCII</c> and <c>ISO-8859-1</c>) or U+FFFD (the Unicode encodings). <c>UTF-16</c> and <c>UTF-32</c> are read as
    /// big-endian unless the bytes start with a byte order mark, which is removed (RFC 2781 §4.3); they are written as big-endian with no
    /// byte order mark. <c>UTF-8</c> is written with no byte order mark.
    /// </para>
    /// <para>
    /// The encodings the runtime provides depend on the runtime, not on the target framework of the package. .NET Framework has the Windows
    /// code pages, such as <c>windows-1252</c> and <c>Shift_JIS</c>. .NET Core and .NET 5 and later have only the Unicode encodings,
    /// <c>US-ASCII</c> and <c>ISO-8859-1</c>, until the application registers the code pages, once, at startup:
    /// </para>
    /// <code>
    /// Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    /// </code>
    /// <para>
    /// <c>CodePagesEncodingProvider</c> is in .NET Core 3.0 and later. On older runtimes it is in the System.Text.Encoding.CodePages package,
    /// which this package doesn't depend on. <c>UTF-7</c> is not available on .NET 5 and later, so it resolves only through a resolver the
    /// caller supplies.
    /// </para>
    /// </remarks>
    public sealed class MimeCharsetResolver : IMimeCharsetResolver
    {
        private static readonly Dictionary<string, Encoding> BuiltInEncodings = CreateBuiltInEncodings();

#if NETCOREAPP3_0_OR_GREATER
        private readonly IMimeCharsetResolver? _fallback;
#else
        private readonly IMimeCharsetResolver _fallback;
#endif

        private MimeCharsetResolver()
        {
        }

        /// <summary>
        /// Creates a resolver that resolves the character sets <see cref="Default"/> doesn't with another resolver.
        /// </summary>
        /// <param name="fallback">The resolver to use for the character sets that <see cref="Default"/> doesn't resolve.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fallback"/> is <c>null</c>.</exception>
        public MimeCharsetResolver(IMimeCharsetResolver fallback)
        {
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        /// <summary>
        /// Gets the resolver that resolves the built-in encodings and those the runtime provides.
        /// </summary>
        public static MimeCharsetResolver Default { get; } = new MimeCharsetResolver();

        /// <summary>
        /// Gets the encoding for a character set.
        /// </summary>
        /// <param name="charset">The character set.</param>
        /// <param name="encoding">The encoding, if the character set is resolved.</param>
        /// <returns><c>true</c> if the character set is resolved; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="charset"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public bool TryGetEncoding(MimeCharset charset, [NotNullWhen(true)] out Encoding? encoding)
#else
        public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
#endif
        {
            if (!charset.IsInitialized) throw new ArgumentException("The character set is not initialised.", nameof(charset));

            if (BuiltInEncodings.TryGetValue(charset.Name, out encoding)) return true;

            try
            {
                encoding = Encoding.GetEncoding(charset.Name);
                return true;
            }
            catch (ArgumentException)
            {
                // The runtime doesn't know the name.
            }
            catch (NotSupportedException)
            {
                // The runtime knows the name, but has no encoding for it.
            }

            if (_fallback != null && _fallback.TryGetEncoding(charset, out encoding)) return true;

            encoding = null;
            return false;
        }

        private static Dictionary<string, Encoding> CreateBuiltInEncodings()
        {
            // The runtime's US-ASCII and ISO-8859-1 write a best fit for some characters they can't hold, which changes the text, and
            // read invalid bytes as '?' on .NET Framework, so they are given the same fallbacks as the hand-coded ones.
#if NETSTANDARD1_3_OR_GREATER || NETCOREAPP
            var ascii = Encoding.GetEncoding("us-ascii", EncoderFallback.ReplacementFallback, new DecoderReplacementFallback("\uFFFD"));
#else
            var ascii = MimeSingleByteEncoding.Ascii;
#endif
#if NET5_0_OR_GREATER
            var latin1 = Encoding.GetEncoding("iso-8859-1", EncoderFallback.ReplacementFallback, new DecoderReplacementFallback("\uFFFD"));
#else
            var latin1 = MimeSingleByteEncoding.Latin1;
#endif
            var utf8 = new UTF8Encoding(false);
            var utf16 = MimeUnicodeSchemeEncoding.Utf16;
            var utf32 = MimeUnicodeSchemeEncoding.Utf32;

            // ref. IANA Character Sets registry: each name, its preferred MIME name and its aliases. Names that are not valid mime-charset
            // names (RFC 2978 §2.3), such as ANSI_X3.4-1968 and ISO_8859-1:1987, are left out, as a MimeCharset can't hold them.
            return new Dictionary<string, Encoding>(StringComparer.OrdinalIgnoreCase)
            {
                { "US-ASCII", ascii }, { "iso-ir-6", ascii }, { "ISO646-US", ascii }, { "us", ascii }, { "IBM367", ascii }, { "cp367", ascii },
                { "csASCII", ascii },
                { "ISO-8859-1", latin1 }, { "ISO_8859-1", latin1 }, { "iso-ir-100", latin1 }, { "latin1", latin1 }, { "l1", latin1 },
                { "IBM819", latin1 }, { "CP819", latin1 }, { "csISOLatin1", latin1 },
                { "UTF-8", utf8 }, { "csUTF8", utf8 },
                { "UTF-16", utf16 }, { "csUTF16", utf16 },
                { "UTF-16BE", utf16.BigEndian }, { "csUTF16BE", utf16.BigEndian },
                { "UTF-16LE", utf16.LittleEndian }, { "csUTF16LE", utf16.LittleEndian },
                { "UTF-32", utf32 }, { "csUTF32", utf32 },
                { "UTF-32BE", utf32.BigEndian }, { "csUTF32BE", utf32.BigEndian },
                { "UTF-32LE", utf32.LittleEndian }, { "csUTF32LE", utf32.LittleEndian },
            };
        }
    }
}
