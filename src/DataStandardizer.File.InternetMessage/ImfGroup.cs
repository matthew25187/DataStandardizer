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

            _displayName = new ImfPhrase(displayName, false);
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
            return ImfAddressParser.TryParseGroup(value, out result);
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
        /// Indicates whether this group equals another.
        /// </summary>
        /// <remarks>
        /// The display name is compared case-sensitively, and the mailboxes in order, as <see cref="ImfMailbox.Equals(ImfMailbox)"/> does.
        /// </remarks>
        public bool Equals([AllowNull] ImfGroup other)
        {
            if (other is null ||
                !string.Equals(_displayName.Text, other._displayName.Text, StringComparison.Ordinal) ||
                _displayName.HasEncodedWordAtom != other._displayName.HasEncodedWordAtom ||
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
        /// <returns>The group.</returns>
        public override string ToString()
        {
            var output = new StringBuilder();
            ImfGrammar.AppendPhrase(_displayName.Text, _displayName.HasEncodedWordAtom, output);
            output.Append(':');
            for (var index = 0; index < _mailboxes.Length; index++)
            {
                output.Append(index == 0 ? " " : ", ").Append(_mailboxes[index]);
            }

            output.Append(';');
            return output.ToString();
        }
    }
}
