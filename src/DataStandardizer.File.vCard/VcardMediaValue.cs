using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The value of a <c>PHOTO</c>, <c>LOGO</c>, <c>SOUND</c> or <c>KEY</c> in the shape of vCard 3.0 or 2.1: inline data with a <c>TYPE</c> that says its format, a URI, or for a key, and a
    /// sound in vCard 2.1 (§2.6.3), text.
    /// </summary>
    /// <remarks>
    /// The canonical form of inline data is an RFC 2397 <c>data:</c> URI, which <see cref="VcardCard.Photos"/> gives.
    /// </remarks>
    public sealed class VcardMediaValue
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly byte[]? _data;

        private VcardMediaValue(byte[]? data, Uri? uri, string? text, string? typeToken)
#else
        private readonly byte[] _data;

        private VcardMediaValue(byte[] data, Uri uri, string text, string typeToken)
#endif
        {
            _data = data;
            Uri = uri;
            Text = text;
            TypeToken = typeToken;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the URI; or <c>null</c> if the value is something else.
        /// </summary>
        public Uri? Uri { get; }

        /// <summary>
        /// Gets the text of a key; or <c>null</c> if the value is something else.
        /// </summary>
        public string? Text { get; }

        /// <summary>
        /// Gets the <c>TYPE</c> that says the format, such as <c>JPEG</c>; or <c>null</c> if there is none.
        /// </summary>
        public string? TypeToken { get; }

        /// <summary>
        /// Creates inline data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="typeToken">The <c>TYPE</c> that says the format, such as <c>JPEG</c>; or <c>null</c> for none.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <c>null</c>.</exception>
        public static VcardMediaValue FromData(byte[] data, string? typeToken = null)
        {
            return new VcardMediaValue(CheckData(data), null, null, typeToken);
        }

        /// <summary>
        /// Creates a URI.
        /// </summary>
        /// <param name="uri">The URI, which must be absolute.</param>
        /// <param name="typeToken">The <c>TYPE</c> that says the format, such as <c>JPEG</c>; or <c>null</c> for none.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="uri"/> can't be written.</exception>
        public static VcardMediaValue FromUri(Uri uri, string? typeToken = null)
        {
            return new VcardMediaValue(null, CheckUri(uri), null, typeToken);
        }

        /// <summary>
        /// Creates the text of a key.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeToken">The <c>TYPE</c> that says the format, such as <c>PGP</c>; or <c>null</c> for none.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public static VcardMediaValue FromText(string text, string? typeToken = null)
        {
            return new VcardMediaValue(null, null, CheckText(text), typeToken);
        }

        /// <summary>
        /// Gets a copy of the inline data; or <c>null</c> if the value is something else.
        /// </summary>
        public byte[]? GetData()
        {
            return (byte[]?)_data?.Clone();
        }
#else
        /// <summary>
        /// Gets the URI; or <c>null</c> if the value is something else.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Gets the text of a key; or <c>null</c> if the value is something else.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the <c>TYPE</c> that says the format, such as <c>JPEG</c>; or <c>null</c> if there is none.
        /// </summary>
        public string TypeToken { get; }

        /// <summary>
        /// Creates inline data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="typeToken">The <c>TYPE</c> that says the format, such as <c>JPEG</c>; or <c>null</c> for none.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <c>null</c>.</exception>
        public static VcardMediaValue FromData(byte[] data, string typeToken = null)
        {
            return new VcardMediaValue(CheckData(data), null, null, typeToken);
        }

        /// <summary>
        /// Creates a URI.
        /// </summary>
        /// <param name="uri">The URI, which must be absolute.</param>
        /// <param name="typeToken">The <c>TYPE</c> that says the format, such as <c>JPEG</c>; or <c>null</c> for none.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="uri"/> can't be written.</exception>
        public static VcardMediaValue FromUri(Uri uri, string typeToken = null)
        {
            return new VcardMediaValue(null, CheckUri(uri), null, typeToken);
        }

        /// <summary>
        /// Creates the text of a key.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeToken">The <c>TYPE</c> that says the format, such as <c>PGP</c>; or <c>null</c> for none.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public static VcardMediaValue FromText(string text, string typeToken = null)
        {
            return new VcardMediaValue(null, null, CheckText(text), typeToken);
        }

        /// <summary>
        /// Gets a copy of the inline data; or <c>null</c> if the value is something else.
        /// </summary>
        public byte[] GetData()
        {
            return (byte[])_data?.Clone();
        }
#endif

        private static byte[] CheckData(byte[] data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            return (byte[])data.Clone();
        }

        private static Uri CheckUri(Uri uri)
        {
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            VcardValue.CheckUri(uri, nameof(uri));
            return uri;
        }

        private static string CheckText(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            return text;
        }
    }
}
