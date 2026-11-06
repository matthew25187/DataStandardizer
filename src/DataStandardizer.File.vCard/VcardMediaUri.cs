using System;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The value of a <c>PHOTO</c>, <c>LOGO</c> or <c>SOUND</c> in vCard 4.0 (RFC 6350 §6.2.4, §6.6.3 and §6.7.5): a URI, often an RFC 2397 <c>data:</c> URI that holds the media inline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The URI is held as text, so it can be of any length on every runtime.  <see cref="Uri"/> on .NET Framework can't hold more than 65519 characters, which the <c>data:</c> URI of a photo
    /// easily passes, so <see cref="Uri"/> gives a <see cref="System.Uri"/> only for text no longer than that, on every runtime.  <see cref="TryGetData"/> gives inline data of any length.
    /// </para>
    /// <para>
    /// Two values are equal if their text is the same, in an ordinal comparison.
    /// </para>
    /// </remarks>
    public sealed class VcardMediaUri : IEquatable<VcardMediaUri>
    {
        private const string DataScheme = "data:";

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardMediaUri(string text, Uri? uri)
#else
        private VcardMediaUri(string text, Uri uri)
#endif
        {
            Text = text;
            Uri = uri;
        }

        /// <summary>
        /// Gets the text of the URI, without the escaping of a content line.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the URI; or <c>null</c> if <see cref="Text"/> has more than 65519 characters, which <see cref="System.Uri"/> on .NET Framework can't hold.  It is <c>null</c> for such text on
        /// every runtime.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Uri? Uri { get; }
#else
        public Uri Uri { get; }
#endif

        /// <summary>
        /// Gets a flag indicating if the URI is of the <c>data:</c> scheme (RFC 2397 §2).  <see cref="TryGetData"/> says whether it is a valid one.
        /// </summary>
        public bool IsData => Text.StartsWith(DataScheme, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a value from a URI.
        /// </summary>
        /// <param name="uri">The URI, which must be absolute.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is not absolute, was given with characters a URI value can't hold, such as a space, or has more than 65519
        /// characters, which <see cref="System.Uri"/> on .NET Framework can't hold.  Use <see cref="FromData"/> for inline data of any length.</exception>
        public static VcardMediaUri FromUri(Uri uri)
        {
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            VcardValue.CheckUri(uri, nameof(uri));
            return new VcardMediaUri(uri.OriginalString, uri);
        }

        /// <summary>
        /// Creates a value from inline data, of any length.
        /// </summary>
        /// <param name="data">The <c>data:</c> URI.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentException"><paramref name="data"/> is not initialised, or has characters a URI value can't hold, such as a space.</exception>
        public static VcardMediaUri FromData(MimeDataUri data)
        {
            var text = data.ToString();
            if (text.Length == 0)
                throw new ArgumentException("The data URI is not initialised.", nameof(data));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            VcardMediaUri? result;
#else
            VcardMediaUri result;
#endif
            if (!TryFromText(text, out result))
                throw new ArgumentException("The data URI has characters that a URI value can't hold.", nameof(data));

            return result;
        }

        /// <summary>
        /// Gets the inline data.
        /// </summary>
        /// <param name="data">The data, if the URI is a valid <c>data:</c> URI.</param>
        /// <returns><c>true</c> if the URI is a valid <c>data:</c> URI (RFC 2397 §3); otherwise <c>false</c>.</returns>
        public bool TryGetData(out MimeDataUri data)
        {
            return MimeDataUri.TryParse(Text.AsSpan(), out data);
        }

        /// <summary>
        /// Indicates whether this value equals another: whether their <see cref="Text"/> is the same, in an ordinal comparison.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <returns><c>true</c> if they are equal; otherwise <c>false</c>.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public bool Equals(VcardMediaUri? other)
#else
        public bool Equals(VcardMediaUri other)
#endif
        {
            return other != null && string.Equals(Text, other.Text, StringComparison.Ordinal);
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return Equals(obj as VcardMediaUri);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Text);
        }

        /// <summary>
        /// Gets the text of the URI.
        /// </summary>
        /// <returns><see cref="Text"/>.</returns>
        public override string ToString()
        {
            return Text;
        }

        /// <summary>
        /// Creates a value from a URI that a content line gave.
        /// </summary>
        internal static VcardMediaUri FromParsedUri(Uri uri)
        {
            return new VcardMediaUri(uri.OriginalString, uri);
        }

        /// <summary>
        /// Creates a value from the text of a URI with no escaping of a content line.  Text longer than <see cref="VcardValue.MaxUriLengthOnEveryRuntime"/> is checked without a
        /// <see cref="System.Uri"/>, as <see cref="VcardValue.IsValidUriText"/> does.
        /// </summary>
        /// <returns><c>false</c> if the text isn't that of a URI value.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static bool TryFromText(string text, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out VcardMediaUri? result)
#else
        internal static bool TryFromText(string text, out VcardMediaUri result)
#endif
        {
            result = null;
            if (text.Length > VcardValue.MaxUriLengthOnEveryRuntime)
            {
                if (!VcardValue.IsUriText(text.AsSpan())) return false;

                result = new VcardMediaUri(text, null);
                return true;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Uri? uri;
#else
            Uri uri;
#endif
            if (!VcardValue.TryParseUriText(text.AsSpan(), out uri)) return false;

            result = new VcardMediaUri(text, uri);
            return true;
        }
    }
}
