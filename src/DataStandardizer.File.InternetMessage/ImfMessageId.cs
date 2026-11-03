using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A message identifier (RFC 5322 §3.6.4), as in a Message-ID, In-Reply-To or References header field, such as <c>&lt;1234@local.machine.example&gt;</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two parts hold what the identifier stands for, whatever form it was written in: <c>&lt;"a" . b@c . d&gt;</c> has the same parts as
    /// <c>&lt;a.b@c.d&gt;</c>.  Both parts are compared case-sensitively.
    /// </para>
    /// <para>
    /// <see cref="ToString"/> writes the modern form.  An identifier read from the obsolete syntax (RFC 5322 §4.5.4) may have a left part that is not a
    /// dot-atom, or a right part with white space in its brackets.  Neither has a modern form, and changing them would make it a different identifier, so those parts
    /// are written as a quoted-string or domain-literal.  Any control character in them is left out.
    /// </para>
    /// </remarks>
    public sealed class ImfMessageId : IEquatable<ImfMessageId>
    {
        private const string InvalidFormatMessage = "The message identifier is not in a valid format.";
        private const string InvalidCidUriMessage = "The cid: URI is not in a valid format.";
        private const string CidScheme = "cid:";
        private const string UriSafeCharacters = "$-_.+!*'(),";
        private const string UriReservedCharacters = ";/?:@&=";
        private const string UpperCaseHexDigits = "0123456789ABCDEF";

        /// <summary>
        /// Initializes a new message identifier.
        /// </summary>
        /// <param name="idLeft">The part before the <c>@</c>, as a dot-atom.</param>
        /// <param name="idRight">The part after the <c>@</c>, as a dot-atom or a literal with its brackets and no white space.</param>
        /// <exception cref="ArgumentNullException"><paramref name="idLeft"/> or <paramref name="idRight"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="idLeft"/> or <paramref name="idRight"/> is not valid (RFC 5322 §3.6.4).</exception>
        public ImfMessageId(string idLeft, string idRight)
        {
            if (idLeft is null) throw new ArgumentNullException(nameof(idLeft));
            if (idRight is null) throw new ArgumentNullException(nameof(idRight));
            if (!ImfGrammar.IsDotAtomText(idLeft)) throw new ArgumentException("The left part is not a dot-atom.", nameof(idLeft));
            if (!ImfGrammar.IsDotAtomText(idRight) && !ImfGrammar.IsNoFoldLiteral(idRight)) throw new ArgumentException("The right part is not a dot-atom or a literal.", nameof(idRight));

            IdLeft = idLeft;
            IdRight = idRight;
        }

        // The last parameter only tells this overload apart: the parts come from the parser, and need no checks.
        internal ImfMessageId(string idLeft, string idRight, bool isParsed)
        {
            IdLeft = idLeft;
            IdRight = idRight;
        }

        /// <summary>
        /// Gets the part before the <c>@</c>, unquoted.
        /// </summary>
        public string IdLeft { get; }

        /// <summary>
        /// Gets the part after the <c>@</c>: a dot-atom, or a literal with its brackets.
        /// </summary>
        public string IdRight { get; }

        /// <summary>
        /// Parses a message identifier.
        /// </summary>
        /// <param name="value">The identifier as UTF-8 (RFC 6532), with its angle brackets, such as <c>&lt;1234@local.machine.example&gt;</c>.</param>
        /// <returns>The message identifier.</returns>
        /// <exception cref="FormatException">The value is not a valid message identifier.</exception>
        public static ImfMessageId Parse(ReadOnlySpan<byte> value)
        {
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a message identifier.
        /// </summary>
        /// <param name="value">The identifier, with its angle brackets, such as <c>&lt;1234@local.machine.example&gt;</c>.</param>
        /// <returns>The message identifier.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid message identifier.</exception>
        public static ImfMessageId Parse(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a message identifier.
        /// </summary>
        /// <param name="value">The identifier as UTF-8 (RFC 6532), with its angle brackets, such as <c>&lt;1234@local.machine.example&gt;</c>.</param>
        /// <param name="result">The message identifier, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid message identifier; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfMessageId result)
        {
            return ImfAddressParser.TryParseMessageId(value, out result);
        }

        /// <summary>
        /// Parses a message identifier.
        /// </summary>
        /// <param name="value">The identifier, with its angle brackets, such as <c>&lt;1234@local.machine.example&gt;</c>.</param>
        /// <param name="result">The message identifier, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid message identifier; otherwise <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, [MaybeNullWhen(false)] out ImfMessageId result)
        {
            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, out result);
        }

        /// <summary>
        /// Parses the message identifiers of an In-Reply-To or References header field.
        /// </summary>
        /// <param name="value">The identifiers as UTF-8 (RFC 6532), such as <c>&lt;1234@local.machine.example&gt; &lt;3456@example.net&gt;</c>.</param>
        /// <returns>The message identifiers, in order.  There may be none, as the obsolete syntax allows (RFC 5322 §4.5.4), which also allows phrases between
        /// them.  Phrases have no meaning, so they are not kept.</returns>
        /// <exception cref="FormatException">The value is not a valid list of message identifiers.</exception>
        public static IReadOnlyList<ImfMessageId> ParseList(ReadOnlySpan<byte> value)
        {
            if (!TryParseList(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses the message identifiers of an In-Reply-To or References header field.
        /// </summary>
        /// <param name="value">The identifiers, such as <c>&lt;1234@local.machine.example&gt; &lt;3456@example.net&gt;</c>.</param>
        /// <returns>The message identifiers, in order.  There may be none, as the obsolete syntax allows (RFC 5322 §4.5.4), which also allows phrases between
        /// them.  Phrases have no meaning, so they are not kept.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid list of message identifiers.</exception>
        public static IReadOnlyList<ImfMessageId> ParseList(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParseList(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses the message identifiers of an In-Reply-To or References header field.
        /// </summary>
        /// <param name="value">The identifiers as UTF-8 (RFC 6532), such as <c>&lt;1234@local.machine.example&gt; &lt;3456@example.net&gt;</c>.</param>
        /// <param name="result">The message identifiers, in order, if the value is valid.  There may be none, as the obsolete syntax allows (RFC 5322 §4.5.4), which
        /// also allows phrases between them.  Phrases have no meaning, so they are not kept.</param>
        /// <returns><c>true</c> if the value is a valid list of message identifiers; otherwise <c>false</c>.</returns>
        public static bool TryParseList(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out IReadOnlyList<ImfMessageId> result)
        {
            result = null;
            if (!ImfAddressParser.TryParseMessageIdList(value, out var messageIds)) return false;

            result = new ReadOnlyCollection<ImfMessageId>(messageIds);
            return true;
        }

        /// <summary>
        /// Parses the message identifiers of an In-Reply-To or References header field.
        /// </summary>
        /// <param name="value">The identifiers, such as <c>&lt;1234@local.machine.example&gt; &lt;3456@example.net&gt;</c>.</param>
        /// <param name="result">The message identifiers, in order, if the value is valid.  There may be none, as the obsolete syntax allows (RFC 5322 §4.5.4), which
        /// also allows phrases between them.  Phrases have no meaning, so they are not kept.</param>
        /// <returns><c>true</c> if the value is a valid list of message identifiers; otherwise <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParseList([AllowNull, NotNullWhen(true)] string value, [MaybeNullWhen(false)] out IReadOnlyList<ImfMessageId> result)
        {
            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParseList(bytes, out result);
        }

        /// <summary>
        /// Converts a <c>cid:</c> URI (RFC 2392 §2) to the identifier in the Content-ID field of the entity that it refers to.
        /// </summary>
        /// <param name="value">The URI, such as <c>cid:foo4%25foo1@bar.net</c>.</param>
        /// <returns>The identifier, such as <c>&lt;foo4%foo1@bar.net&gt;</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParseCidUri"/> describes.</exception>
        public static ImfMessageId ParseCidUri(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParseCidUri(value, out var result)) throw new FormatException(InvalidCidUriMessage);

            return result;
        }

        /// <summary>
        /// Converts a <c>cid:</c> URI (RFC 2392 §2) to the identifier in the Content-ID field of the entity that it refers to.
        /// </summary>
        /// <remarks>
        /// As RFC 2392 §2 says, this removes the scheme, converts each <c>%</c> escape to its US-ASCII character, and puts the rest in angle brackets.  The example there shows
        /// the escape as still in the Content-ID field, which disagrees with what it says, so this does what it says.  An escape of an octet above 127 isn't valid, nor is a
        /// character that RFC 1738 §2.2 says must be escaped, such as <c>~</c> or <c>#</c>.  The scheme is compared case-insensitively.
        /// </remarks>
        /// <param name="value">The URI, such as <c>cid:foo4%25foo1@bar.net</c>.</param>
        /// <param name="result">The identifier, such as <c>&lt;foo4%foo1@bar.net&gt;</c>, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParseCidUri([AllowNull, NotNullWhen(true)] string value, [MaybeNullWhen(false)] out ImfMessageId result)
        {
            result = null;
            if (value is null || !value.StartsWith(CidScheme, StringComparison.OrdinalIgnoreCase)) return false;

            var bytes = new byte[value.Length - CidScheme.Length + 2];
            var length = 0;
            bytes[length++] = (byte)'<';
            for (var index = CidScheme.Length; index < value.Length; index++)
            {
                var c = value[index];
                if (c == '%')
                {
                    if (index + 2 >= value.Length) return false;

                    var high = GetHexValue(value[index + 1]);
                    var low = GetHexValue(value[index + 2]);
                    if (high < 0 || low < 0 || high > 7) return false;

                    bytes[length++] = (byte)(high * 16 + low);
                    index += 2;
                }
                else if (IsUriCharacter(c) || UriReservedCharacters.IndexOf(c) >= 0)
                {
                    bytes[length++] = (byte)c;
                }
                else
                {
                    return false;
                }
            }

            bytes[length++] = (byte)'>';
            return TryParse(new ReadOnlySpan<byte>(bytes, 0, length), out result);
        }

        /// <summary>
        /// Converts the identifier in a Content-ID field to the <c>cid:</c> URI that refers to the entity (RFC 2392 §2), such as <c>cid:foo4%25foo1@bar.net</c>.
        /// </summary>
        /// <remarks>
        /// Any character that a URL can't hold, or that has a special meaning in one, is written as a <c>%</c> escape (RFC 1738 §2.2).
        /// </remarks>
        /// <returns>The URI.</returns>
        /// <exception cref="InvalidOperationException">The identifier isn't US-ASCII, as only RFC 6532 allows, or holds a control character other than tab, as only the
        /// obsolete syntax allows, so it has no <c>cid:</c> URI that gives the same identifier back.</exception>
        public string ToCidUri()
        {
            // ToString leaves these out, which would make the URI refer to a different identifier.
            if (!ImfGrammar.IsWritableText(IdLeft) || !ImfGrammar.IsWritableText(IdRight))
                throw new InvalidOperationException("RFC 2392 §2 gives no form here for an identifier with a control character.");

            var text = ToString();
            var output = new StringBuilder(CidScheme, text.Length + CidScheme.Length);
            for (var index = 1; index < text.Length - 1; index++)
            {
                var c = text[index];
                if (c >= 0x80) throw new InvalidOperationException("RFC 2392 §2 has no form for an identifier that isn't US-ASCII.");

                if (c == '@' || IsUriCharacter(c))
                {
                    output.Append(c);
                }
                else
                {
                    output.Append('%').Append(UpperCaseHexDigits[c >> 4]).Append(UpperCaseHexDigits[c & 0xF]);
                }
            }

            return output.ToString();
        }

        /// <summary>
        /// Indicates whether this message identifier equals another: both parts are the same, compared case-sensitively.
        /// </summary>
        public bool Equals([AllowNull] ImfMessageId other)
        {
            return !(other is null) &&
                string.Equals(IdLeft, other.IdLeft, StringComparison.Ordinal) &&
                string.Equals(IdRight, other.IdRight, StringComparison.Ordinal);
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is ImfMessageId other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ImfGrammar.CombineHashCodes(StringComparer.Ordinal.GetHashCode(IdLeft), StringComparer.Ordinal.GetHashCode(IdRight));
        }

        /// <summary>
        /// Indicates whether two message identifiers are equal.
        /// </summary>
        public static bool operator ==([AllowNull] ImfMessageId left, [AllowNull] ImfMessageId right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two message identifiers differ.
        /// </summary>
        public static bool operator !=([AllowNull] ImfMessageId left, [AllowNull] ImfMessageId right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Formats the message identifier with its angle brackets, such as <c>&lt;1234@local.machine.example&gt;</c>.
        /// </summary>
        /// <returns>The message identifier.</returns>
        public override string ToString()
        {
            var output = new StringBuilder();
            output.Append('<');
            ImfGrammar.AppendAddrSpec(IdLeft, IdRight, output);
            output.Append('>');
            return output.ToString();
        }

        // ref. RFC 1738 §2.2: alphanumerics and the special characters "$-_.+!*'()," may be used unencoded.
        private static bool IsUriCharacter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || UriSafeCharacters.IndexOf(c) >= 0;
        }

        private static int GetHexValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;

            return -1;
        }
    }
}
