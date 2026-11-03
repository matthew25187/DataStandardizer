using System;
using System.Diagnostics.CodeAnalysis;

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
    /// RFC 2047 encoded words in a display name are kept as written, and a quoted-string of that form stays plain text.
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
            return ImfAddressParser.TryParseAddress(value, out result);
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
    }
}
