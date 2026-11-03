using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// An address (RFC 5322 §3.4): an <see cref="ImfMailbox"/> or an <see cref="ImfGroup"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The white space, comments and obsolete syntax of the input (RFC 5322 §4) are not kept.  <see cref="object.ToString"/> writes the modern form.
    /// </para>
    /// <para>
    /// <see cref="DisplayName"/> holds the text of any RFC 2047 encoded words in a display name (RFC 2047 §5(3)), and a quoted-string of that form stays plain text.
    /// A word that can't be decoded stays as written (RFC 2047 §6.2), as <see cref="ImfOptions.AllowMalformedEncodedWords"/> describes.  Encoded words are never read
    /// in a local part or domain.  Decoded text can hold anything that the words encode, including control characters and line breaks, so check it before using it
    /// where those matter.
    /// </para>
    /// </remarks>
    public abstract class ImfAddress : IEquatable<ImfAddress>
    {
        private const string InvalidFormatMessage = "The address is not in a valid format.";

        private protected ImfAddress()
        {
        }

        /// <summary>
        /// Gets the display name, or an empty string if a mailbox has none.
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Parses an address.
        /// </summary>
        /// <param name="value">The address as UTF-8 (RFC 6532), which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <returns>The address.</returns>
        /// <exception cref="FormatException">The value is not a valid address.</exception>
        public static ImfAddress Parse(ReadOnlySpan<byte> value)
        {
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses an address.
        /// </summary>
        /// <param name="value">The address, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <returns>The address.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid address.</exception>
        public static ImfAddress Parse(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses an address.
        /// </summary>
        /// <param name="value">The address as UTF-8 (RFC 6532), which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <param name="result">The address, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid address; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfAddress result)
        {
            return ImfAddressParser.TryParseAddress(value, ImfHeaderText.DefaultOptions, out result);
        }

        /// <summary>
        /// Parses an address.
        /// </summary>
        /// <param name="value">The address, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <param name="result">The address, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid address; otherwise <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, [MaybeNullWhen(false)] out ImfAddress result)
        {
            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, out result);
        }

        /// <summary>
        /// Parses an address.
        /// </summary>
        /// <param name="value">The address, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.  Bytes that aren't valid UTF-8 are decoded with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> before any encoded word is read, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedEncodedWords"/>, <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <returns>The address.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not a valid address.</exception>
        public static ImfAddress Parse(ReadOnlySpan<byte> value, ImfOptions options)
        {
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses an address.
        /// </summary>
        /// <param name="value">The address, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedEncodedWords"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <returns>The address.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not a valid address.</exception>
        public static ImfAddress Parse(string value, ImfOptions options)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses an address.
        /// </summary>
        /// <param name="value">The address, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.  Bytes that aren't valid UTF-8 are decoded with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> before any encoded word is read, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedEncodedWords"/>, <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <param name="result">The address, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid address; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParse(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out ImfAddress result)
        {
            ImfHeaderText.CheckOptions(options);

            return ImfAddressParser.TryParseAddress(value, options, out result);
        }

        /// <summary>
        /// Parses an address.
        /// </summary>
        /// <param name="value">The address, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedEncodedWords"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <param name="result">The address, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid address; otherwise <c>false</c>, including for <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, ImfOptions options, [MaybeNullWhen(false)] out ImfAddress result)
        {
            ImfHeaderText.CheckOptions(options);

            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, options, out result);
        }

        /// <summary>
        /// Formats the address in the modern form, for a writer with the given header encoding.
        /// </summary>
        /// <remarks>
        /// <para>
        /// With <see cref="ImfHeaderEncoding.EncodedWords"/>, a display name that isn't US-ASCII is written as RFC 2047 encoded words, in UTF-8, whatever form it was read in.
        /// With <see cref="ImfHeaderEncoding.Utf8"/>, it is written as UTF-8 (RFC 6532), and an encoded word that was read is written as its text.
        /// </para>
        /// <para>
        /// A local part or domain can't hold an encoded word (RFC 2047 §5), so one that isn't US-ASCII stays as it is, and a writer with
        /// <see cref="ImfHeaderEncoding.EncodedWords"/> refuses it.  A control character in a display name has no form that can be written here, so it is left out.
        /// </para>
        /// </remarks>
        /// <param name="encoding">How to write text that is not US-ASCII.</param>
        /// <returns>The address, unfolded.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="encoding"/> is not an <see cref="ImfHeaderEncoding"/> member.</exception>
        public string ToString(ImfHeaderEncoding encoding)
        {
            ImfHeaderText.CheckEncoding(encoding);

            return Format(encoding);
        }

        /// <summary>
        /// Indicates whether this address equals another.
        /// </summary>
        /// <remarks>
        /// A mailbox never equals a group.  See <see cref="ImfMailbox.Equals(ImfMailbox)"/> and <see cref="ImfGroup.Equals(ImfGroup)"/> for how each compares.
        /// </remarks>
        public abstract bool Equals([AllowNull] ImfAddress other);

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public sealed override bool Equals(object? obj)
#else
        public sealed override bool Equals(object obj)
#endif
        {
            return obj is ImfAddress other && Equals(other);
        }

        /// <inheritdoc />
        public abstract override int GetHashCode();

        /// <summary>
        /// Indicates whether two addresses are equal.
        /// </summary>
        public static bool operator ==([AllowNull] ImfAddress left, [AllowNull] ImfAddress right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two addresses differ.
        /// </summary>
        public static bool operator !=([AllowNull] ImfAddress left, [AllowNull] ImfAddress right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Writes the address in the modern form.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="encoding">How to write text that is not US-ASCII; or <c>null</c> to write as the address holds it, with each encoded word as it was read and any
        /// other text as UTF-8.</param>
        internal abstract void AppendTo(StringBuilder output, ImfHeaderEncoding? encoding);

        /// <summary>
        /// Formats the address in the modern form.
        /// </summary>
        private protected string Format(ImfHeaderEncoding? encoding)
        {
            var output = new StringBuilder();
            AppendTo(output, encoding);
            return output.ToString();
        }
    }
}
