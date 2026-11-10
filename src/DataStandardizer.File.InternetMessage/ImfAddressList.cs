using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// An ordered, read-only list of addresses (RFC 5322 §3.4), with at least one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A group is parsed wherever an address may appear, as RFC 6854 allows in From and Sender.  Which forms a given field allows is not checked here.
    /// </para>
    /// <para>
    /// The empty elements of the obsolete form (RFC 5322 §4.4), such as in <c>a@b.test, , c@d.test</c>, are not kept.
    /// </para>
    /// </remarks>
    public sealed class ImfAddressList : IReadOnlyList<ImfAddress>, IEquatable<ImfAddressList>
    {
        private const string InvalidFormatMessage = "The address list is not in a valid format.";

        private readonly ImfAddress[] _addresses;
        private readonly ReadOnlyCollection<ImfAddress> _readOnlyAddresses;

        /// <summary>
        /// Initializes a new list.
        /// </summary>
        /// <param name="addresses">The addresses, in order, which are copied.</param>
        /// <exception cref="ArgumentNullException"><paramref name="addresses"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="addresses"/> is empty, as RFC 5322 §3.4 needs at least one address, or holds a <c>null</c> element.</exception>
        public ImfAddressList(IEnumerable<ImfAddress> addresses)
            : this(ImfGrammar.CopyElements(addresses, nameof(addresses)))
        {
            if (_addresses.Length == 0) throw new ArgumentException("The list has no addresses.", nameof(addresses));
        }

        private ImfAddressList(ImfAddress[] addresses)
        {
            _addresses = addresses;
            _readOnlyAddresses = new ReadOnlyCollection<ImfAddress>(addresses);
        }

        /// <inheritdoc />
        public int Count => _addresses.Length;

        /// <inheritdoc />
        public ImfAddress this[int index] => _readOnlyAddresses[index];

        /// <summary>
        /// Parses a list of addresses.
        /// </summary>
        /// <param name="value">The list as UTF-8 (RFC 6532), which may be folded, such as <c>Mary Smith &lt;mary@x.test&gt;, jdoe@example.org</c>.</param>
        /// <returns>The list.</returns>
        /// <exception cref="FormatException">The value is not a valid list of addresses.</exception>
        public static ImfAddressList Parse(ReadOnlySpan<byte> value)
        {
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a list of addresses.
        /// </summary>
        /// <param name="value">The list, which may be folded, such as <c>Mary Smith &lt;mary@x.test&gt;, jdoe@example.org</c>.</param>
        /// <returns>The list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid list of addresses.</exception>
        public static ImfAddressList Parse(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a list of addresses.
        /// </summary>
        /// <param name="value">The list as UTF-8 (RFC 6532), which may be folded, such as <c>Mary Smith &lt;mary@x.test&gt;, jdoe@example.org</c>.</param>
        /// <param name="result">The list, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid list of addresses; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfAddressList result)
        {
            return TryParseCore(value, ImfHeaderText.DefaultOptions, out result);
        }

        /// <summary>
        /// Parses a list of addresses.
        /// </summary>
        /// <param name="value">The list, which may be folded, such as <c>Mary Smith &lt;mary@x.test&gt;, jdoe@example.org</c>.</param>
        /// <param name="result">The list, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid list of addresses; otherwise <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, [MaybeNullWhen(false)] out ImfAddressList result)
        {
            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, out result);
        }

        /// <summary>
        /// Parses an address list.
        /// </summary>
        /// <param name="value">The address list, which may be folded, such as <c>Mary Smith &lt;mary@x.test&gt;, jdoe@example.org</c>.  Bytes that aren't valid UTF-8 are decoded with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> before any encoded word is read, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedEncodedWords"/>, <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <returns>The address list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not a valid address list.</exception>
        public static ImfAddressList Parse(ReadOnlySpan<byte> value, ImfOptions options)
        {
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses an address list.
        /// </summary>
        /// <param name="value">The address list, which may be folded, such as <c>Mary Smith &lt;mary@x.test&gt;, jdoe@example.org</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedEncodedWords"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <returns>The address list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not a valid address list.</exception>
        public static ImfAddressList Parse(string value, ImfOptions options)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses an address list.
        /// </summary>
        /// <param name="value">The address list, which may be folded, such as <c>Mary Smith &lt;mary@x.test&gt;, jdoe@example.org</c>.  Bytes that aren't valid UTF-8 are decoded with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> before any encoded word is read, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedEncodedWords"/>, <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <param name="result">The address list, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid address list; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParse(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out ImfAddressList result)
        {
            ImfHeaderText.CheckOptions(options);

            return TryParseCore(value, options, out result);
        }

        /// <summary>
        /// Parses an address list.
        /// </summary>
        /// <param name="value">The address list, which may be folded, such as <c>Mary Smith &lt;mary@x.test&gt;, jdoe@example.org</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedEncodedWords"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <param name="result">The address list, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid address list; otherwise <c>false</c>, including for <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, ImfOptions options, [MaybeNullWhen(false)] out ImfAddressList result)
        {
            ImfHeaderText.CheckOptions(options);

            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, options, out result);
        }

        /// <inheritdoc />
        public IEnumerator<ImfAddress> GetEnumerator()
        {
            return _readOnlyAddresses.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Indicates whether this list equals another: it has equal addresses in the same order.
        /// </summary>
        public bool Equals([AllowNull] ImfAddressList other)
        {
            if (other is null || _addresses.Length != other._addresses.Length) return false;

            for (var index = 0; index < _addresses.Length; index++)
            {
                if (!_addresses[index].Equals(other._addresses[index])) return false;
            }

            return true;
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is ImfAddressList other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = 0;
            foreach (var address in _addresses)
            {
                hash = ImfGrammar.CombineHashCodes(hash, address.GetHashCode());
            }

            return hash;
        }

        /// <summary>
        /// Indicates whether two lists are equal.
        /// </summary>
        public static bool operator ==([AllowNull] ImfAddressList left, [AllowNull] ImfAddressList right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two lists differ.
        /// </summary>
        public static bool operator !=([AllowNull] ImfAddressList left, [AllowNull] ImfAddressList right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Formats the list in the modern form, such as <c>Mary Smith &lt;mary@x.test&gt;, jdoe@example.org</c>.
        /// </summary>
        /// <remarks>
        /// An encoded word stays as it was read, and any other text is written as it is, which may be UTF-8.  A word right next to a period is the exception: the
        /// phrase is written as new encoded words, as only the obsolete form could keep it.  Use <see cref="ToString(ImfHeaderEncoding)"/> to choose.
        /// </remarks>
        /// <returns>The list.</returns>
        public override string ToString()
        {
            return Format(null);
        }

        /// <summary>
        /// Formats the list in the modern form, for a writer with the given header encoding.
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
        /// <returns>The list, unfolded.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="encoding"/> is not an <see cref="ImfHeaderEncoding"/> member.</exception>
        public string ToString(ImfHeaderEncoding encoding)
        {
            ImfHeaderText.CheckEncoding(encoding);

            return Format(encoding);
        }

        private static bool TryParseCore(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out ImfAddressList result)
        {
            result = null;
            if (!ImfAddressParser.TryParseAddressList(value, options, out var addresses)) return false;

            result = new ImfAddressList(addresses);
            return true;
        }

        private string Format(ImfHeaderEncoding? encoding)
        {
            var output = new StringBuilder();
            for (var index = 0; index < _addresses.Length; index++)
            {
                if (index > 0) output.Append(", ");

                _addresses[index].AppendTo(output, encoding);
            }

            return output.ToString();
        }
    }
}
