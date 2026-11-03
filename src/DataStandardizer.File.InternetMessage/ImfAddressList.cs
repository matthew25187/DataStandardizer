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
            result = null;
            if (!ImfAddressParser.TryParseAddressList(value, out var addresses)) return false;

            result = new ImfAddressList(addresses);
            return true;
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
        /// <returns>The list.</returns>
        public override string ToString()
        {
            var output = new StringBuilder();
            for (var index = 0; index < _addresses.Length; index++)
            {
                if (index > 0) output.Append(", ");

                output.Append(_addresses[index]);
            }

            return output.ToString();
        }
    }
}
