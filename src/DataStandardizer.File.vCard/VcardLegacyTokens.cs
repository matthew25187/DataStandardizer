using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The names and values of the vCard 2.1 parameters that the content-line layer has to recognise.
    /// </summary>
    internal static class VcardLegacyTokens
    {
        internal const string CharsetName = "CHARSET";
        internal const string EncodingName = "ENCODING";
        private const string TypeName = "TYPE";
        private const string ValueName = "VALUE";

        // ref. vCard 2.1 §2.1.5 and §4: pencodingval.  "b" is RFC 2426 §3.1.4.
        internal const string Base64 = "BASE64";
        internal const string EightBit = "8BIT";
        internal const string QuotedPrintable = "QUOTED-PRINTABLE";
        internal const string SevenBit = "7BIT";
        internal const string Version30Base64 = "b";

        // ref. vCard 2.1 §4: knowntype.
        private static readonly string[] KnownTypes =
        {
            "DOM", "INTL", "POSTAL", "PARCEL", "HOME", "WORK",
            "PREF", "VOICE", "FAX", "MSG", "CELL", "PAGER",
            "BBS", "MODEM", "CAR", "ISDN", "VIDEO",
            "AOL", "APPLELINK", "ATTMAIL", "CIS", "EWORLD",
            "INTERNET", "IBMMAIL", "MCIMAIL",
            "POWERSHARE", "PRODIGY", "TLX", "X400",
            "GIF", "CGM", "WMF", "BMP", "MET", "PMB", "DIB",
            "PICT", "TIFF", "PDF", "PS", "JPEG", "QTIME",
            "MPEG", "MPEG2", "AVI",
            "WAVE", "AIFF", "PCM",
            "X509", "PGP"
        };

        // ref. vCard 2.1 §4: pvalueval.
        private static readonly string[] ValueLocations = { "INLINE", "URL", "CONTENT-ID", "CID" };

        // The character sets a bare token is taken for, a fixed list so that the answer doesn't depend on the encodings a runtime has.
        private static readonly string[] CharsetPrefixes = { "ISO-8859-", "ISO-2022-", "KOI8-", "EUC-", "WINDOWS-", "UTF-" };
        private static readonly string[] CharsetNames = { "US-ASCII", "BIG5", "GB2312", "GBK", "GB18030" };

        /// <summary>
        /// Gets the parameter a value with no name belongs to: an encoding, a value location or a character set is that parameter, and anything else is a type.
        /// </summary>
        internal static string GetValuelessParameterName(ReadOnlySpan<char> token)
        {
            if (IsEncoding(token)) return EncodingName;
            if (EqualsAny(token, ValueLocations)) return ValueName;
            if (EqualsAny(token, CharsetNames) || StartsWithAny(token, CharsetPrefixes)) return CharsetName;

            return TypeName;
        }

        /// <summary>
        /// Gets whether vCard 2.1 allows a value with no name.
        /// </summary>
        /// <remarks>
        /// ref. vCard 2.1 §4: <c>param</c> allows a <c>knowntype</c> alone.  It has no such form for an encoding, but the example in §2.6.3 is
        /// <c>SOUND;WAVE;BASE64:</c>, so an encoding is allowed too.  Nothing there allows a value location or a character set alone.
        /// </remarks>
        internal static bool IsValuelessAllowedInVersion21(ReadOnlySpan<char> token)
        {
            return EqualsAny(token, KnownTypes) || IsEncoding(token);
        }

        /// <summary>
        /// Gets the features of vCard 2.1 that a parameter uses.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="firstValue">The first value of the parameter.</param>
        internal static VcardLegacySyntax GetParameterSyntax(ReadOnlySpan<char> name, ReadOnlySpan<char> firstValue)
        {
            if (EqualsIgnoreCase(name, CharsetName)) return VcardLegacySyntax.Charset;
            if (!EqualsIgnoreCase(name, EncodingName)) return VcardLegacySyntax.None;

            if (EqualsIgnoreCase(firstValue, QuotedPrintable)) return VcardLegacySyntax.QuotedPrintable;
            if (EqualsIgnoreCase(firstValue, Base64)) return VcardLegacySyntax.Base64;
            if (EqualsIgnoreCase(firstValue, SevenBit)) return VcardLegacySyntax.SevenBit;
            if (EqualsIgnoreCase(firstValue, EightBit)) return VcardLegacySyntax.EightBit;

            return VcardLegacySyntax.None;
        }

        /// <summary>
        /// Compares with a token, ignoring the case of ASCII letters.
        /// </summary>
        internal static bool EqualsIgnoreCase(ReadOnlySpan<char> text, string token)
        {
            return text.Length == token.Length && StartsWithIgnoreCase(text, token);
        }

        private static bool IsEncoding(ReadOnlySpan<char> token)
        {
            return EqualsIgnoreCase(token, QuotedPrintable) || EqualsIgnoreCase(token, Base64) || EqualsIgnoreCase(token, SevenBit) || EqualsIgnoreCase(token, EightBit);
        }

        private static bool EqualsAny(ReadOnlySpan<char> text, string[] tokens)
        {
            foreach (var token in tokens)
            {
                if (EqualsIgnoreCase(text, token)) return true;
            }

            return false;
        }

        private static bool StartsWithAny(ReadOnlySpan<char> text, string[] prefixes)
        {
            foreach (var prefix in prefixes)
            {
                if (text.Length > prefix.Length && StartsWithIgnoreCase(text, prefix)) return true;
            }

            return false;
        }

        private static bool StartsWithIgnoreCase(ReadOnlySpan<char> text, string prefix)
        {
            for (var index = 0; index < prefix.Length; index++)
            {
                var left = text[index];
                var right = prefix[index];
                if (left == right) continue;
                if ((left | 0x20) != (right | 0x20) || (left | 0x20) < 'a' || (left | 0x20) > 'z') return false;
            }

            return true;
        }
    }
}
