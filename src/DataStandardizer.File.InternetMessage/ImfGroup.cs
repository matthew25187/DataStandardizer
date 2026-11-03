using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A group (RFC 5322 §3.4): a display name and a list of mailboxes, which may be empty, such as <c>Undisclosed recipients:;</c>.
    /// </summary>
    public sealed class ImfGroup : ImfAddress, IEquatable<ImfGroup>
    {
        private const string InvalidFormatMessage = "The group is not in a valid format.";

        private readonly ImfPhrase _displayName;
        private readonly ImfMailbox[] _mailboxes;
        private readonly ReadOnlyCollection<ImfMailbox> _readOnlyMailboxes;

        /// <summary>
        /// Initializes a new group.
        /// </summary>
        /// <param name="displayName">The display name, as plain text.</param>
        /// <param name="mailboxes">The mailboxes, in order, which are copied.  There may be none.</param>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/> or <paramref name="mailboxes"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="displayName"/> holds a control character other than tab or an unpaired surrogate, or
        /// <paramref name="mailboxes"/> holds a <c>null</c> element.</exception>
        public ImfGroup(string displayName, IEnumerable<ImfMailbox> mailboxes)
        {
            if (displayName is null) throw new ArgumentNullException(nameof(displayName));
            if (!ImfGrammar.IsWritableText(displayName)) throw new ArgumentException("The display name holds a character that cannot be written.", nameof(displayName));

            _displayName = new ImfPhrase(displayName);
            _mailboxes = ImfGrammar.CopyElements(mailboxes, nameof(mailboxes));
            _readOnlyMailboxes = new ReadOnlyCollection<ImfMailbox>(_mailboxes);
        }

        internal ImfGroup(ImfPhrase displayName, ImfMailbox[] mailboxes)
        {
            _displayName = displayName;
            _mailboxes = mailboxes;
            _readOnlyMailboxes = new ReadOnlyCollection<ImfMailbox>(_mailboxes);
        }

        /// <inheritdoc />
        public override string DisplayName => _displayName.Text;

        /// <summary>
        /// Gets the mailboxes of the group, in order.
        /// </summary>
        public IReadOnlyList<ImfMailbox> Mailboxes => _readOnlyMailboxes;

        /// <summary>
        /// Parses a group.
        /// </summary>
        /// <param name="value">The group as UTF-8 (RFC 6532), which may be folded, such as <c>Friends: Ed &lt;c@a.test&gt;, joe@where.test;</c>.</param>
        /// <returns>The group.</returns>
        /// <exception cref="FormatException">The value is not a valid group.</exception>
        public static new ImfGroup Parse(ReadOnlySpan<byte> value)
        {
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a group.
        /// </summary>
        /// <param name="value">The group, which may be folded, such as <c>Friends: Ed &lt;c@a.test&gt;, joe@where.test;</c>.</param>
        /// <returns>The group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid group.</exception>
        public static new ImfGroup Parse(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a group.
        /// </summary>
        /// <param name="value">The group as UTF-8 (RFC 6532), which may be folded, such as <c>Friends: Ed &lt;c@a.test&gt;, joe@where.test;</c>.</param>
        /// <param name="result">The group, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid group; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfGroup result)
        {
            return ImfAddressParser.TryParseGroup(value, ImfHeaderText.DefaultOptions, out result);
        }

        /// <summary>
        /// Parses a group.
        /// </summary>
        /// <param name="value">The group, which may be folded, such as <c>Friends: Ed &lt;c@a.test&gt;, joe@where.test;</c>.</param>
        /// <param name="result">The group, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid group; otherwise <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, [MaybeNullWhen(false)] out ImfGroup result)
        {
            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, out result);
        }

        /// <summary>
        /// Parses a group.
        /// </summary>
        /// <param name="value">The group, which may be folded, such as <c>Friends: Ed &lt;c@a.test&gt;, joe@where.test;</c>.  Bytes that aren't valid UTF-8 are decoded with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> before any encoded word is read, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedEncodedWords"/>, <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <returns>The group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not a valid group.</exception>
        public static new ImfGroup Parse(ReadOnlySpan<byte> value, ImfOptions options)
        {
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a group.
        /// </summary>
        /// <param name="value">The group, which may be folded, such as <c>Friends: Ed &lt;c@a.test&gt;, joe@where.test;</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedEncodedWords"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <returns>The group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not a valid group.</exception>
        public static new ImfGroup Parse(string value, ImfOptions options)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a group.
        /// </summary>
        /// <param name="value">The group, which may be folded, such as <c>Friends: Ed &lt;c@a.test&gt;, joe@where.test;</c>.  Bytes that aren't valid UTF-8 are decoded with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> before any encoded word is read, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedEncodedWords"/>, <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <param name="result">The group, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid group; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParse(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out ImfGroup result)
        {
            ImfHeaderText.CheckOptions(options);

            return ImfAddressParser.TryParseGroup(value, options, out result);
        }

        /// <summary>
        /// Parses a group.
        /// </summary>
        /// <param name="value">The group, which may be folded, such as <c>Friends: Ed &lt;c@a.test&gt;, joe@where.test;</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedEncodedWords"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <param name="result">The group, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid group; otherwise <c>false</c>, including for <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, ImfOptions options, [MaybeNullWhen(false)] out ImfGroup result)
        {
            ImfHeaderText.CheckOptions(options);

            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, options, out result);
        }

        /// <summary>
        /// Indicates whether this group equals another.
        /// </summary>
        /// <remarks>
        /// The display name is compared case-sensitively, and the mailboxes in order, as <see cref="ImfMailbox.Equals(ImfMailbox)"/> does.
        /// </remarks>
        public bool Equals([AllowNull] ImfGroup other)
        {
            if (other is null ||
                !_displayName.ValueEquals(other._displayName) ||
                _mailboxes.Length != other._mailboxes.Length)
            {
                return false;
            }

            for (var index = 0; index < _mailboxes.Length; index++)
            {
                if (!_mailboxes[index].Equals(other._mailboxes[index])) return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals([AllowNull] ImfAddress other)
        {
            return Equals(other as ImfGroup);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = StringComparer.Ordinal.GetHashCode(_displayName.Text);
            foreach (var mailbox in _mailboxes)
            {
                hash = ImfGrammar.CombineHashCodes(hash, mailbox.GetHashCode());
            }

            return hash;
        }

        /// <summary>
        /// Formats the group in the modern form, such as <c>Friends: Ed &lt;c@a.test&gt;, joe@where.test;</c> or <c>Undisclosed recipients:;</c>.
        /// </summary>
        /// <remarks>
        /// An encoded word stays as it was read, and any other text is written as it is, which may be UTF-8.  Use <see cref="ImfAddress.ToString(ImfHeaderEncoding)"/>
        /// to choose.
        /// </remarks>
        /// <returns>The group.</returns>
        public override string ToString()
        {
            return Format(null);
        }

        internal override void AppendTo(StringBuilder output, ImfHeaderEncoding? encoding)
        {
            _displayName.AppendTo(output, encoding);
            output.Append(':');
            for (var index = 0; index < _mailboxes.Length; index++)
            {
                output.Append(index == 0 ? " " : ", ");
                _mailboxes[index].AppendTo(output, encoding);
            }

            output.Append(';');
        }
    }
}
