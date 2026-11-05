using System;
using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The version of the card a line was in when it was read, as far as the line's value depends on it.
    /// </summary>
    internal enum VcardLineVersion : byte
    {
        /// <summary>The line was not read from a source, so its parameters are what the caller says they are.</summary>
        NotRead,

        /// <summary>The line is in a card that has said it is vCard 2.1.</summary>
        Version21,

        /// <summary>The line is in a card that has said it is vCard 4.0.</summary>
        Version40,

        /// <summary>The line is in a card of another version, in one that hasn't said, or outside a card.</summary>
        Other
    }

    /// <summary>
    /// Decodes a line's value by its <c>ENCODING</c> and <c>CHARSET</c> parameters, as a reader's options say.
    /// </summary>
    internal sealed class VcardValueDecoder
    {
        private static class ErrorMessage
        {
            internal const string InvalidBase64 = "The value is not valid base64.";
            internal const string InvalidForCharsetTemplate = "The value is not valid in the character set {0}.";
            internal const string InvalidQuotedPrintable = "The value is not valid quoted-printable.";
            internal const string UnknownCharsetTemplate = "The character set \"{0}\" is not known.";
            internal const string UnknownEncodingTemplate = "The encoding \"{0}\" is not known.";
            internal const string UnlabeledEightBit = "The value holds a character outside US-ASCII, but has no ENCODING=8BIT.  vCard 2.1 values are 7-bit by default.";
        }

        // Encodes the characters of a value that came from UTF-8.  A lone surrogate can only have come from a caller's text reader, and doesn't throw here.
        private static readonly Encoding DefaultSourceEncoding = new UTF8Encoding(false, false);

        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        /// <summary>
        /// Gets the decoder for a line that was not read from a source.
        /// </summary>
        internal static VcardValueDecoder Default { get; } = new VcardValueDecoder(new VcardOptions());

        private readonly bool _acceptEightBitWithoutEncoding;
        private readonly bool _acceptLegacySyntax;
        private readonly bool _allowUnknownCharsetAsLatin1;
        private readonly IMimeCharsetResolver _resolver;
        private readonly Encoding _sourceEncoding;

        internal VcardValueDecoder(VcardOptions options)
        {
            _acceptEightBitWithoutEncoding = options.AcceptEightBitWithoutEncoding;
            _acceptLegacySyntax = options.AcceptLegacySyntax;
            _allowUnknownCharsetAsLatin1 = options.AllowUnknownCharsetAsLatin1;
            _resolver = options.CharsetResolver is null ? (IMimeCharsetResolver)MimeCharsetResolver.Default : new MimeCharsetResolver(options.CharsetResolver);
            _sourceEncoding = options.Encoding ?? DefaultSourceEncoding;
        }

        /// <summary>
        /// Gets whether a backslash that doesn't start a valid TEXT escape sequence is accepted.
        /// </summary>
        internal bool AcceptsOtherTextEscapes => _acceptLegacySyntax;

        /// <summary>
        /// Gets the transfer encoding that applies to a line.  A parameter that the line's version doesn't define is an unknown parameter, which has no meaning.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="version">The version of the line's card.</param>
        /// <param name="encoding">The encoding; not initialised if its name isn't a valid token.</param>
        /// <param name="isVersion30Base64"><c>true</c> if the parameter is the <c>b</c> of RFC 2426 §4, in the strict form of RFC 2047 §4.1.</param>
        /// <returns><c>true</c> if an encoding applies.</returns>
        internal bool TryGetTransferEncoding(VcardContentLine line, VcardLineVersion version, out MimeTransferEncoding encoding, out bool isVersion30Base64)
        {
            encoding = default(MimeTransferEncoding);
            isVersion30Base64 = false;

            var parameter = line.GetParameter(VcardLegacyTokens.EncodingName);
            if (parameter is null) return false;

            var name = parameter.ValueArray[0];
            if (VcardLegacyTokens.EqualsIgnoreCase(name.AsSpan(), VcardLegacyTokens.Version30Base64))
            {
                if (version == VcardLineVersion.Version40 && !_acceptLegacySyntax) return false;

                encoding = MimeTransferEncoding.Base64;
                isVersion30Base64 = true;
                return true;
            }

            if (!AppliesLegacyParameters(version)) return false;

            MimeTransferEncoding.TryParse(name, out encoding);
            return true;
        }

        /// <summary>
        /// Gets the character set a line names, if it applies to the line's version.
        /// </summary>
        /// <returns><c>true</c> if the line names a character set; <c>false</c> if it doesn't, the name isn't valid, or the parameter doesn't apply.</returns>
        internal bool TryGetCharset(VcardContentLine line, VcardLineVersion version, out MimeCharset charset)
        {
            charset = default(MimeCharset);
            var parameter = GetCharsetParameter(line, version);
            return parameter != null && MimeCharset.TryParse(parameter.ValueArray[0], out charset);
        }

        /// <summary>
        /// Gets the octets of a line's value.
        /// </summary>
        /// <exception cref="VcardFileException">The encoding isn't known, or the value isn't valid in it.</exception>
        internal byte[] DecodeBytes(VcardContentLine line, VcardLineVersion version)
        {
            MimeTransferEncoding transferEncoding;
            bool isVersion30Base64;
            var hasEncoding = TryGetTransferEncoding(line, version, out transferEncoding, out isVersion30Base64);

            CheckKnownEncoding(line, hasEncoding, transferEncoding);
            if (!hasEncoding || IsIdentity(transferEncoding)) return _sourceEncoding.GetBytes(line.Value);

            if (transferEncoding == MimeTransferEncoding.QuotedPrintable)
            {
                // ref. vCard 2.1 §2.9: quoted-printable = <MIME RFC 1521 quoted-printable text>.  The reader has joined its soft line breaks.
                if (!MimeQuotedPrintable.TryDecode(line.Value.AsSpan(), MimeQuotedPrintableMode.Body, MimeQuotedPrintableDecoderOptions.None, out var octets))
                {
                    throw VcardContentLineErrorFactory.BuildValueException(ErrorMessage.InvalidQuotedPrintable, line);
                }

                return octets;
            }

            // ref. vCard 2.1 §2.9: base64 = <MIME RFC 1521 base64 text>, whose line breaks and indents are ignored; RFC 2426 §4: "b" is RFC 2047 §4.1, which has none.
            var mode = isVersion30Base64 ? MimeBase64Mode.Strict : MimeBase64Mode.Mime;
            if (!MimeBase64.TryDecode(line.Value.AsSpan(), mode, MimeBase64DecoderOptions.None, out var result))
            {
                throw VcardContentLineErrorFactory.BuildValueException(ErrorMessage.InvalidBase64, line);
            }

            return result;
        }

        /// <summary>
        /// Gets the text of a line's value.
        /// </summary>
        /// <exception cref="VcardFileException">The encoding or character set isn't known, or the value isn't valid in them.</exception>
        internal string DecodeText(VcardContentLine line, VcardLineVersion version, out bool usedCharsetFallback)
        {
            usedCharsetFallback = false;

            MimeTransferEncoding transferEncoding;
            bool isVersion30Base64;
            var hasEncoding = TryGetTransferEncoding(line, version, out transferEncoding, out isVersion30Base64);
            var charset = GetCharsetParameter(line, version);
            var isIdentity = !hasEncoding || IsIdentity(transferEncoding);

            // ref. vCard 2.1 §2.1.5: "The default encoding for the vCard object is 7-Bit."
            if (version == VcardLineVersion.Version21 && isIdentity && !IsEightBit(hasEncoding, transferEncoding) && !_acceptEightBitWithoutEncoding && !IsAscii(line.Value))
            {
                throw VcardContentLineErrorFactory.BuildValueException(ErrorMessage.UnlabeledEightBit, line);
            }

            // RFC 6350 §3.1 has one character set, which the source has already been decoded from.
            if (isIdentity && charset is null && version != VcardLineVersion.Version21) return line.Value;

            var bytes = DecodeBytes(line, version);
            string charsetName;
            var encoding = GetTextEncoding(line, version, charset, out charsetName, out usedCharsetFallback);
            return GetText(line, encoding, charsetName, bytes);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private Encoding GetTextEncoding(VcardContentLine line, VcardLineVersion version, VcardContentLineParameter? charset, out string charsetName, out bool usedCharsetFallback)
#else
        private Encoding GetTextEncoding(VcardContentLine line, VcardLineVersion version, VcardContentLineParameter charset, out string charsetName, out bool usedCharsetFallback)
#endif
        {
            usedCharsetFallback = false;
            if (charset is null)
            {
                // ref. vCard 2.1 §2.1.6: "The default character set is ASCII."; RFC 6350 §3.1: UTF-8.
                charsetName = version == VcardLineVersion.Version21 ? MimeCharset.UsAscii.Name : MimeCharset.Utf8.Name;
                return Resolve(version == VcardLineVersion.Version21 ? MimeCharset.UsAscii : MimeCharset.Utf8);
            }

            charsetName = charset.ValueArray[0];
            MimeCharset name;
            if (MimeCharset.TryParse(charsetName, out name) && _resolver.TryGetEncoding(name, out var encoding)) return encoding;

            if (!_allowUnknownCharsetAsLatin1)
            {
                throw VcardContentLineErrorFactory.BuildValueException(string.Format(ErrorMessage.UnknownCharsetTemplate, charsetName), line);
            }

            usedCharsetFallback = true;
            return Resolve(MimeCharset.Iso88591);
        }

        private static string GetText(VcardContentLine line, Encoding encoding, string charsetName, byte[] bytes)
        {
            // The built-in UTF-8 doesn't throw on invalid data, which would lose it.
            var isUtf8 = encoding is UTF8Encoding;
            string text;
            try
            {
                text = (isUtf8 ? StrictUtf8 : encoding).GetString(bytes, 0, bytes.Length);
            }
            catch (DecoderFallbackException exception)
            {
                throw VcardContentLineErrorFactory.BuildValueException(string.Format(ErrorMessage.InvalidForCharsetTemplate, charsetName), line, exception);
            }

            // Nor do the other built-in encodings, which read invalid data as U+FFFD.  A single-byte or legacy character set has no such character to read, but
            // UTF-16 and UTF-32 do, so for those the octets show whether it was really there.
            if (!isUtf8 && text.IndexOf('\xFFFD') >= 0 && !IsValidUnicodeScheme(encoding, bytes))
            {
                throw VcardContentLineErrorFactory.BuildValueException(string.Format(ErrorMessage.InvalidForCharsetTemplate, charsetName), line);
            }

            return text;
        }

        /// <summary>
        /// Gets whether octets are valid in an encoding that takes two or four octets for "A", as UTF-16 and UTF-32 do, written by hand since netstandard1.1 can't
        /// ask an encoding to throw.  A byte order mark says the byte order, as RFC 2781 §4.3 has it for UTF-16.
        /// </summary>
        /// <returns><c>true</c> if the encoding takes two or four octets and the octets are valid.  Any other encoding gives <c>false</c>, as it has no way to hold U+FFFD
        /// that this can check.</returns>
        private static bool IsValidUnicodeScheme(Encoding encoding, byte[] bytes)
        {
            var sample = encoding.GetBytes("A");
            if (sample.Length == 2) return IsValidUtf16(bytes, sample[0] == 0);
            if (sample.Length == 4) return IsValidUtf32(bytes, sample[0] == 0);

            return false;
        }

        private static bool IsValidUtf16(byte[] bytes, bool isBigEndian)
        {
            if (bytes.Length % 2 != 0) return false;

            var index = 0;
            if (bytes.Length >= 2 && (bytes[0] == 0xFE && bytes[1] == 0xFF || bytes[0] == 0xFF && bytes[1] == 0xFE))
            {
                isBigEndian = bytes[0] == 0xFE;
                index = 2;
            }

            while (index < bytes.Length)
            {
                var unit = isBigEndian ? (bytes[index] << 8) | bytes[index + 1] : (bytes[index + 1] << 8) | bytes[index];
                index += 2;
                if (unit < 0xD800 || unit > 0xDFFF) continue;

                // A high surrogate needs a low one straight after it, and a low one can't come first (RFC 2781 §2.2).
                if (unit > 0xDBFF || index == bytes.Length) return false;

                var next = isBigEndian ? (bytes[index] << 8) | bytes[index + 1] : (bytes[index + 1] << 8) | bytes[index];
                if (next < 0xDC00 || next > 0xDFFF) return false;

                index += 2;
            }

            return true;
        }

        private static bool IsValidUtf32(byte[] bytes, bool isBigEndian)
        {
            if (bytes.Length % 4 != 0) return false;

            var index = 0;
            if (bytes.Length >= 4)
            {
                if (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0xFE && bytes[3] == 0xFF)
                {
                    isBigEndian = true;
                    index = 4;
                }
                else if (bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0 && bytes[3] == 0)
                {
                    isBigEndian = false;
                    index = 4;
                }
            }

            for (; index < bytes.Length; index += 4)
            {
                var value = isBigEndian
                    ? ((long)bytes[index] << 24) | ((long)bytes[index + 1] << 16) | ((long)bytes[index + 2] << 8) | bytes[index + 3]
                    : ((long)bytes[index + 3] << 24) | ((long)bytes[index + 2] << 16) | ((long)bytes[index + 1] << 8) | bytes[index];

                // The Unicode Standard §3.9, D76: a scalar value is at most U+10FFFF, and not a surrogate.
                if (value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF)) return false;
            }

            return true;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardContentLineParameter? GetCharsetParameter(VcardContentLine line, VcardLineVersion version)
#else
        private VcardContentLineParameter GetCharsetParameter(VcardContentLine line, VcardLineVersion version)
#endif
        {
            return AppliesLegacyParameters(version) ? line.GetParameter(VcardLegacyTokens.CharsetName) : null;
        }

        private bool AppliesLegacyParameters(VcardLineVersion version)
        {
            return _acceptLegacySyntax || version == VcardLineVersion.Version21 || version == VcardLineVersion.NotRead;
        }

        private static void CheckKnownEncoding(VcardContentLine line, bool hasEncoding, MimeTransferEncoding encoding)
        {
            if (!hasEncoding || IsIdentity(encoding) || encoding == MimeTransferEncoding.QuotedPrintable || encoding == MimeTransferEncoding.Base64) return;

            throw VcardContentLineErrorFactory.BuildValueException(string.Format(ErrorMessage.UnknownEncodingTemplate, line.GetParameter(VcardLegacyTokens.EncodingName)?.ValueArray[0]), line);
        }

        private static bool IsAscii(string value)
        {
            foreach (var character in value)
            {
                if (character >= 0x80) return false;
            }

            return true;
        }

        private static bool IsEightBit(bool hasEncoding, MimeTransferEncoding encoding)
        {
            return hasEncoding && encoding == MimeTransferEncoding.EightBit;
        }

        // ref. vCard 2.1 §2.9: pencodingval has 7BIT and 8BIT, which leave the data as it is, and no "binary".
        private static bool IsIdentity(MimeTransferEncoding encoding)
        {
            return encoding == MimeTransferEncoding.SevenBit || encoding == MimeTransferEncoding.EightBit;
        }

        private static Encoding Resolve(MimeCharset charset)
        {
            // Each of these is built in, so it always resolves.
            MimeCharsetResolver.Default.TryGetEncoding(charset, out var encoding);
            return encoding ?? throw new InvalidOperationException();
        }
    }
}
