using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A mailbox (RFC 5322 §3.4): an optional display name, and an address made of a local part and a domain.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The local part and domain hold what the address stands for, whatever form it was written in.  <c>"jdoe"@example.org</c> and
    /// <c>jdoe@example . org</c> have the same local part and domain as <c>jdoe@example.org</c>, and the source route of the obsolete form
    /// <c>&lt;@node.test:mary@example.net&gt;</c> is dropped.
    /// </para>
    /// <para>
    /// <see cref="ToString"/> writes the modern form.  The obsolete syntax alone can put a control character in a quoted-string or domain-literal, or a
    /// bracket or backslash in a domain-literal (RFC 5322 §4.1 and §4.4).  Such a character is parsed, but has no modern form, so it is left out when written.
    /// </para>
    /// </remarks>
    public sealed class ImfMailbox : ImfAddress, IEquatable<ImfMailbox>
    {
        private const string InvalidFormatMessage = "The mailbox is not in a valid format.";

        private readonly ImfPhrase _displayName;

        /// <summary>
        /// Initializes a new mailbox with no display name.
        /// </summary>
        /// <param name="localPart">The local part, unquoted.</param>
        /// <param name="domain">The domain, as a dot-atom or a domain-literal with its brackets.  As when parsed, a domain-literal loses the white space just inside its brackets, and other white space in it becomes one space.</param>
        /// <exception cref="ArgumentNullException"><paramref name="localPart"/> or <paramref name="domain"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="localPart"/> holds a control character other than tab or an unpaired surrogate, or
        /// <paramref name="domain"/> is not a valid domain (RFC 5322 §3.4.1).</exception>
        public ImfMailbox(string localPart, string domain)
            : this(string.Empty, localPart, domain)
        {
        }

        /// <summary>
        /// Initializes a new mailbox.
        /// </summary>
        /// <param name="displayName">The display name, as plain text, or an empty string for none.</param>
        /// <param name="localPart">The local part, unquoted.</param>
        /// <param name="domain">The domain, as a dot-atom or a domain-literal with its brackets.  As when parsed, a domain-literal loses the white space just inside its brackets, and other white space in it becomes one space.</param>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/>, <paramref name="localPart"/> or <paramref name="domain"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="displayName"/> or <paramref name="localPart"/> holds a control character other than tab or an
        /// unpaired surrogate, or <paramref name="domain"/> is not a valid domain (RFC 5322 §3.4.1).</exception>
        public ImfMailbox(string displayName, string localPart, string domain)
        {
            if (displayName is null) throw new ArgumentNullException(nameof(displayName));
            if (localPart is null) throw new ArgumentNullException(nameof(localPart));
            if (domain is null) throw new ArgumentNullException(nameof(domain));
            if (!ImfGrammar.IsWritableText(displayName)) throw new ArgumentException("The display name holds a character that cannot be written.", nameof(displayName));
            if (!ImfGrammar.IsWritableText(localPart)) throw new ArgumentException("The local part holds a character that cannot be written.", nameof(localPart));
            if (!ImfGrammar.IsDomain(domain)) throw new ArgumentException("The domain is not valid.", nameof(domain));

            _displayName = new ImfPhrase(displayName);
            LocalPart = localPart;
            Domain = ImfGrammar.NormalizeDomain(domain);
        }

        internal ImfMailbox(ImfPhrase displayName, string localPart, string domain)
        {
            _displayName = displayName;
            LocalPart = localPart;
            Domain = domain;
        }

        /// <inheritdoc />
        public override string DisplayName => _displayName.Text;

        /// <summary>
        /// Gets the local part, unquoted.
        /// </summary>
        public string LocalPart { get; }

        /// <summary>
        /// Gets the domain: a dot-atom, or a domain-literal with its brackets.
        /// </summary>
        public string Domain { get; }

        /// <summary>
        /// Gets the addr-spec in the modern form, with no display name or angle brackets, such as <c>mary@example.net</c>.
        /// </summary>
        public string Address
        {
            get
            {
                var output = new StringBuilder();
                ImfGrammar.AppendAddrSpec(LocalPart, Domain, output);
                return output.ToString();
            }
        }

        /// <summary>
        /// Parses a mailbox.
        /// </summary>
        /// <param name="value">The mailbox as UTF-8 (RFC 6532), which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <returns>The mailbox.</returns>
        /// <exception cref="FormatException">The value is not a valid mailbox.</exception>
        public static new ImfMailbox Parse(ReadOnlySpan<byte> value)
        {
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a mailbox.
        /// </summary>
        /// <param name="value">The mailbox, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <returns>The mailbox.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid mailbox.</exception>
        public static new ImfMailbox Parse(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a mailbox.
        /// </summary>
        /// <param name="value">The mailbox as UTF-8 (RFC 6532), which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <param name="result">The mailbox, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid mailbox; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfMailbox result)
        {
            return ImfAddressParser.TryParseMailbox(value, ImfHeaderText.DefaultOptions, out result);
        }

        /// <summary>
        /// Parses a mailbox.
        /// </summary>
        /// <param name="value">The mailbox, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <param name="result">The mailbox, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid mailbox; otherwise <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, [MaybeNullWhen(false)] out ImfMailbox result)
        {
            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, out result);
        }

        /// <summary>
        /// Parses a mailbox.
        /// </summary>
        /// <param name="value">The mailbox, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.  Bytes that aren't valid UTF-8 are decoded with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> before any encoded word is read, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedEncodedWords"/>, <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <returns>The mailbox.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not a valid mailbox.</exception>
        public static new ImfMailbox Parse(ReadOnlySpan<byte> value, ImfOptions options)
        {
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a mailbox.
        /// </summary>
        /// <param name="value">The mailbox, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedEncodedWords"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <returns>The mailbox.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not a valid mailbox.</exception>
        public static new ImfMailbox Parse(string value, ImfOptions options)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a mailbox.
        /// </summary>
        /// <param name="value">The mailbox, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.  Bytes that aren't valid UTF-8 are decoded with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> before any encoded word is read, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedEncodedWords"/>, <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <param name="result">The mailbox, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid mailbox; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParse(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out ImfMailbox result)
        {
            ImfHeaderText.CheckOptions(options);

            return ImfAddressParser.TryParseMailbox(value, options, out result);
        }

        /// <summary>
        /// Parses a mailbox.
        /// </summary>
        /// <param name="value">The mailbox, which may be folded, such as <c>Mary Smith &lt;mary@example.net&gt;</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedEncodedWords"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <param name="result">The mailbox, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid mailbox; otherwise <c>false</c>, including for <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, ImfOptions options, [MaybeNullWhen(false)] out ImfMailbox result)
        {
            ImfHeaderText.CheckOptions(options);

            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, options, out result);
        }

        /// <summary>
        /// Indicates whether this mailbox has the same address as another, whatever their display names.
        /// </summary>
        /// <remarks>
        /// The local part is compared case-sensitively and the domain case-insensitively.
        /// </remarks>
        /// <param name="other">The other mailbox.</param>
        /// <returns><c>true</c> if the addresses are the same; <c>false</c> if not, or if <paramref name="other"/> is <c>null</c>.</returns>
        public bool HasSameAddress([AllowNull, NotNullWhen(true)] ImfMailbox other)
        {
            return !(other is null) &&
                string.Equals(LocalPart, other.LocalPart, StringComparison.Ordinal) &&
                string.Equals(Domain, other.Domain, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Indicates whether this mailbox equals another.
        /// </summary>
        /// <remarks>
        /// The display name and local part are compared case-sensitively and the domain case-insensitively.  Display names are compared as text, so
        /// <c>=?UTF-8?Q?a?=</c> and <c>a</c> are equal.  Use <see cref="HasSameAddress"/> to ignore the display name.
        /// </remarks>
        public bool Equals([AllowNull] ImfMailbox other)
        {
            return HasSameAddress(other) &&
                _displayName.ValueEquals(other._displayName);
        }

        /// <inheritdoc />
        public override bool Equals([AllowNull] ImfAddress other)
        {
            return Equals(other as ImfMailbox);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = ImfGrammar.CombineHashCodes(StringComparer.Ordinal.GetHashCode(LocalPart), StringComparer.OrdinalIgnoreCase.GetHashCode(Domain));
            return ImfGrammar.CombineHashCodes(hash, StringComparer.Ordinal.GetHashCode(_displayName.Text));
        }

        /// <summary>
        /// Formats the mailbox in the modern form, such as <c>Mary Smith &lt;mary@example.net&gt;</c> or <c>mary@example.net</c>.
        /// </summary>
        /// <remarks>
        /// An encoded word stays as it was read, and any other text is written as it is, which may be UTF-8.  Use <see cref="ImfAddress.ToString(ImfHeaderEncoding)"/>
        /// to choose.
        /// </remarks>
        /// <returns>The mailbox.</returns>
        public override string ToString()
        {
            return Format(null);
        }

        internal override void AppendTo(StringBuilder output, ImfHeaderEncoding? encoding)
        {
            if (_displayName.Text.Length == 0)
            {
                ImfGrammar.AppendAddrSpec(LocalPart, Domain, output);
            }
            else
            {
                _displayName.AppendTo(output, encoding);
                output.Append(" <");
                ImfGrammar.AppendAddrSpec(LocalPart, Domain, output);
                output.Append('>');
            }
        }
    }
}
