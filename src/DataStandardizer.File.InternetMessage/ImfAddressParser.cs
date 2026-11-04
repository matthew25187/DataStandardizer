using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Parses the grammar of RFC 5322 §3.4 and §3.6.4, with the obsolete forms of §4, over the tokens of an <see cref="ImfTokenizer"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Which fields allow which forms is not decided here: a list of addresses takes groups wherever one appears (RFC 6854).
    /// </para>
    /// <para>
    /// Each <c>TryParse</c> method has an <c>IsValid</c> method, for a conformance check, that accepts and refuses the same values and keeps nothing.  They go through the one grammar, whose methods
    /// take a flag saying whether to build what they read.  An encoded word is the one thing they don't pass over, as decoding it calls <see cref="ImfOptions.CharsetResolver"/>
    /// (see <see cref="ImfPhrase.TrySkip"/>).
    /// </para>
    /// </remarks>
    internal static class ImfAddressParser
    {
        // What the methods of the grammar give for what they were asked not to build.
        private static readonly ImfMailbox NoMailbox = new ImfMailbox(new ImfPhrase(string.Empty), string.Empty, string.Empty);
        private static readonly ImfMailbox[] NoMailboxes = new ImfMailbox[0];
        private static readonly ImfMessageId NoMessageId = new ImfMessageId(string.Empty, string.Empty, true);

        internal static bool TryParseAddress(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out ImfAddress result)
        {
            result = null;
            if (!ImfHeaderText.TryGetUtf8(value, options, out var utf8)) return false;

            var tokenizer = new ImfTokenizer(utf8);
            if (TryReadAddress(ref tokenizer, options, true, true, out var address, out _) && IsAtEnd(ref tokenizer))
            {
                result = address;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a value is what <see cref="TryParseAddress"/> accepts, and counts its mailboxes, including all those in a group.
        /// </summary>
        internal static bool IsValidAddress(ReadOnlySpan<byte> value, ImfOptions options, out int mailboxCount)
        {
            mailboxCount = 0;
            if (!ImfHeaderText.TryGetUtf8(value, options, out var utf8)) return false;

            var tokenizer = new ImfTokenizer(utf8);
            return TryReadAddress(ref tokenizer, options, true, false, out _, out mailboxCount) && IsAtEnd(ref tokenizer);
        }

        internal static bool TryParseMailbox(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out ImfMailbox result)
        {
            result = null;
            if (!TryParseAddress(value, options, out var address)) return false;

            result = address as ImfMailbox;
            return !(result is null);
        }

        internal static bool TryParseGroup(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out ImfGroup result)
        {
            result = null;
            if (!TryParseAddress(value, options, out var address)) return false;

            result = address as ImfGroup;
            return !(result is null);
        }

        internal static bool TryParseAddressList(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out ImfAddress[] result)
        {
            result = null;
            if (!ImfHeaderText.TryGetUtf8(value, options, out var utf8)) return false;

            var addresses = new List<ImfAddress>();
            if (!TryReadAddressList(utf8, options, addresses, out _)) return false;

            result = addresses.ToArray();
            return true;
        }

        /// <summary>
        /// Determines if a value is what <see cref="TryParseAddressList"/> accepts, and counts its mailboxes, including all those in groups.
        /// </summary>
        internal static bool IsValidAddressList(ReadOnlySpan<byte> value, ImfOptions options, out int mailboxCount)
        {
            mailboxCount = 0;
            return ImfHeaderText.TryGetUtf8(value, options, out var utf8) && TryReadAddressList(utf8, options, null, out mailboxCount);
        }

        // ref. RFC 5322 §3.4: address-list = (address *("," address)) / obs-addr-list
        // ref. RFC 5322 §4.4: obs-addr-list = *([CFWS] ",") address *("," [address / CFWS])
        private static bool TryReadAddressList(ReadOnlySpan<byte> utf8, ImfOptions options, [AllowNull] List<ImfAddress> addresses, out int mailboxCount)
        {
            mailboxCount = 0;
            var addressCount = 0;
            var tokenizer = new ImfTokenizer(utf8);
            while (true)
            {
                if (!tokenizer.TryPeek(out var token)) return false;

                if (token.Kind == ImfTokenKind.Comma)
                {
                    tokenizer.TryRead(out token);
                    continue;
                }

                if (token.Kind == ImfTokenKind.End) break;

                if (!TryReadAddress(ref tokenizer, options, true, !(addresses is null), out var address, out var mailboxes) || !TryReadListSeparator(ref tokenizer, ImfTokenKind.End)) return false;

                addresses?.Add(address);
                addressCount++;
                mailboxCount += mailboxes;
            }

            return addressCount > 0;
        }

        internal static bool TryParsePhraseList(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out ImfPhrase[] result)
        {
            result = null;
            if (!ImfHeaderText.TryGetUtf8(value, options, out var utf8)) return false;

            var phrases = new List<ImfPhrase>();
            if (!TryReadPhraseList(utf8, options, phrases)) return false;

            result = phrases.ToArray();
            return true;
        }

        /// <summary>
        /// Determines if a value is what <see cref="TryParsePhraseList"/> accepts.
        /// </summary>
        internal static bool IsValidPhraseList(ReadOnlySpan<byte> value, ImfOptions options)
        {
            return ImfHeaderText.TryGetUtf8(value, options, out var utf8) && TryReadPhraseList(utf8, options, null);
        }

        // ref. RFC 5322 §3.6.5: keywords = "Keywords:" phrase *("," phrase) CRLF
        // ref. RFC 5322 §4.1: obs-phrase-list = [phrase / CFWS] *("," [phrase / CFWS])
        private static bool TryReadPhraseList(ReadOnlySpan<byte> utf8, ImfOptions options, [AllowNull] List<ImfPhrase> phrases)
        {
            var tokenizer = new ImfTokenizer(utf8);
            while (true)
            {
                if (!tokenizer.TryPeek(out var token)) return false;

                if (token.Kind == ImfTokenKind.Comma)
                {
                    tokenizer.TryRead(out token);
                    continue;
                }

                if (token.Kind == ImfTokenKind.End) return true;

                if (!TryReadPhrase(ref tokenizer, options, !(phrases is null), out var phrase) || !TryReadListSeparator(ref tokenizer, ImfTokenKind.End)) return false;

                phrases?.Add(phrase);
            }
        }

        internal static bool TryParseMessageId(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfMessageId result)
        {
            result = null;
            if (!ImfTokenizer.IsWellFormed(value)) return false;

            var tokenizer = new ImfTokenizer(value);
            if (TryReadMessageId(ref tokenizer, true, out var messageId) && IsAtEnd(ref tokenizer))
            {
                result = messageId;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a value is what <see cref="TryParseMessageId"/> accepts.
        /// </summary>
        internal static bool IsValidMessageId(ReadOnlySpan<byte> value)
        {
            if (!ImfTokenizer.IsWellFormed(value)) return false;

            var tokenizer = new ImfTokenizer(value);
            return TryReadMessageId(ref tokenizer, false, out _) && IsAtEnd(ref tokenizer);
        }

        internal static bool TryParseMessageIdList(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfMessageId[] result)
        {
            result = null;
            if (!ImfTokenizer.IsWellFormed(value)) return false;

            var messageIds = new List<ImfMessageId>();
            if (!TryReadMessageIdList(value, messageIds)) return false;

            result = messageIds.ToArray();
            return true;
        }

        /// <summary>
        /// Determines if a value is what <see cref="TryParseMessageIdList"/> accepts.
        /// </summary>
        internal static bool IsValidMessageIdList(ReadOnlySpan<byte> value)
        {
            return ImfTokenizer.IsWellFormed(value) && TryReadMessageIdList(value, null);
        }

        // ref. RFC 5322 §3.6.4: in-reply-to = "In-Reply-To:" 1*msg-id CRLF; references = "References:" 1*msg-id CRLF
        // ref. RFC 5322 §4.5.4: obs-in-reply-to = "In-Reply-To" *WSP ":" *(phrase / msg-id) CRLF; obs-references likewise.  The phrases have no meaning.
        private static bool TryReadMessageIdList(ReadOnlySpan<byte> utf8, [AllowNull] List<ImfMessageId> messageIds)
        {
            var build = !(messageIds is null);
            var tokenizer = new ImfTokenizer(utf8);
            while (true)
            {
                if (!tokenizer.TryPeek(out var token)) return false;

                if (token.Kind == ImfTokenKind.End) return true;

                if (token.Kind == ImfTokenKind.LessThan)
                {
                    if (!TryReadMessageId(ref tokenizer, build, out var messageId)) return false;

                    messageIds?.Add(messageId);
                }
                else if (!TryReadPhrase(ref tokenizer, ImfHeaderText.DefaultOptions, build, out _))
                {
                    return false;
                }
            }
        }

        // ref. RFC 5322 §3.6.3: bcc = "Bcc:" [address-list / CFWS] CRLF
        // ref. RFC 5322 §4.5.3: obs-bcc = "Bcc" *WSP ":" (address-list / (*([CFWS] ",") [CFWS])) CRLF
        internal static bool IsEmptyAddressList(ReadOnlySpan<byte> value, ImfOptions options)
        {
            if (!ImfHeaderText.TryGetUtf8(value, options, out var utf8)) return false;

            var tokenizer = new ImfTokenizer(utf8);
            while (tokenizer.TryRead(out var token))
            {
                if (token.Kind == ImfTokenKind.End) return true;
                if (token.Kind != ImfTokenKind.Comma) return false;
            }

            return false;
        }

        // The result is null for the empty path.
        internal static bool TryParsePath(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNull] out ImfMailbox result)
        {
            result = null;
            return ImfHeaderText.TryGetUtf8(value, options, out var utf8) && TryReadPath(utf8, true, out result);
        }

        /// <summary>
        /// Determines if a value is what <see cref="TryParsePath"/> accepts.
        /// </summary>
        internal static bool IsValidPath(ReadOnlySpan<byte> value, ImfOptions options)
        {
            return ImfHeaderText.TryGetUtf8(value, options, out var utf8) && TryReadPath(utf8, false, out _);
        }

        // ref. RFC 5322 §3.6.7: path = angle-addr / ([CFWS] "<" [CFWS] ">" [CFWS])
        private static bool TryReadPath(ReadOnlySpan<byte> utf8, bool build, [MaybeNull] out ImfMailbox result)
        {
            result = null;
            var tokenizer = new ImfTokenizer(utf8);
            var scan = tokenizer;
            if (scan.TryRead(out var open) && open.Kind == ImfTokenKind.LessThan && scan.TryRead(out var close) && close.Kind == ImfTokenKind.GreaterThan)
            {
                return IsAtEnd(ref scan);
            }

            if (!TryReadAngleAddr(ref tokenizer, build, out var localPart, out var domain) || !IsAtEnd(ref tokenizer)) return false;

            if (build) result = new ImfMailbox(new ImfPhrase(string.Empty), localPart, domain);

            return true;
        }

        // The date is left unparsed, for the caller to parse under its options.
        internal static bool TryParseReceived(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out string[] tokens, out bool hasDate, out ReadOnlySpan<byte> date)
        {
            tokens = null;
            hasDate = false;
            date = default;
            if (!ImfHeaderText.TryGetUtf8(value, options, out var utf8)) return false;

            var list = new List<string>();
            if (!TryReadReceived(utf8, list, out hasDate, out date)) return false;

            tokens = list.ToArray();
            return true;
        }

        /// <summary>
        /// Determines if a value is what <see cref="TryParseReceived"/> accepts.
        /// </summary>
        internal static bool IsValidReceived(ReadOnlySpan<byte> value, ImfOptions options, out bool hasDate, out ReadOnlySpan<byte> date)
        {
            hasDate = false;
            date = default;
            return ImfHeaderText.TryGetUtf8(value, options, out var utf8) && TryReadReceived(utf8, null, out hasDate, out date);
        }

        // ref. RFC 5322 §3.6.7: received = "Received:" *received-token ";" date-time CRLF
        // ref. RFC 5322 §4.5.7: obs-received = "Received" *WSP ":" *received-token CRLF
        private static bool TryReadReceived(ReadOnlySpan<byte> utf8, [AllowNull] List<string> tokens, out bool hasDate, out ReadOnlySpan<byte> date)
        {
            hasDate = false;
            date = default;
            var tokenizer = new ImfTokenizer(utf8);
            while (true)
            {
                if (!tokenizer.TryPeek(out var token)) return false;

                if (token.Kind == ImfTokenKind.End) return true;

                if (token.Kind == ImfTokenKind.Semicolon)
                {
                    hasDate = true;
                    date = utf8.Slice(token.Start + token.Length);
                    return true;
                }

                if (!TryReadReceivedToken(ref tokenizer, !(tokens is null), out var text)) return false;

                tokens?.Add(text);
            }
        }

        // ref. RFC 5322 §3.6.7: received-token = word / angle-addr / addr-spec / domain
        private static bool TryReadReceivedToken(ref ImfTokenizer tokenizer, bool build, [MaybeNullWhen(false)] out string text)
        {
            text = null;
            if (!tokenizer.TryPeek(out var token)) return false;

            if (token.Kind == ImfTokenKind.DomainLiteral)
            {
                tokenizer.TryRead(out token);
                text = build ? tokenizer.GetText(token) : string.Empty;
                return true;
            }

            if (token.Kind == ImfTokenKind.LessThan)
            {
                if (!TryReadAngleAddr(ref tokenizer, build, out var angleLocalPart, out var angleDomain)) return false;

                text = string.Empty;
                if (build)
                {
                    var output = new StringBuilder();
                    output.Append('<');
                    ImfGrammar.AppendAddrSpec(angleLocalPart, angleDomain, output);
                    output.Append('>');
                    text = output.ToString();
                }

                return true;
            }

            // A word and a dot-atom start alike, and only what follows them tells a domain from the local part of an addr-spec.
            var scan = tokenizer;
            if (!TryReadLocalPart(ref tokenizer, build, out var words) || !tokenizer.TryPeek(out var next)) return false;

            if (next.Kind != ImfTokenKind.At)
            {
                // Without an "@", only a local part can hold a quoted-string next to a period, so this must be one word, or a domain.
                if (token.Kind == ImfTokenKind.QuotedString
                    ? !scan.TryRead(out _) || !scan.TryPeek(out next) || next.Kind == ImfTokenKind.Period
                    : !TryReadDomain(ref scan, false, out _))
                {
                    return false;
                }

                text = words;
                return true;
            }

            tokenizer.TryRead(out token);
            if (!TryReadDomain(ref tokenizer, build, out var domain)) return false;

            text = build ? new StringBuilder().Append(words).Append('@').Append(domain).ToString() : string.Empty;
            return true;
        }

        // ref. RFC 5322 §3.4: address = mailbox / group; mailbox = name-addr / addr-spec; name-addr = [display-name] angle-addr
        private static bool TryReadAddress(ref ImfTokenizer tokenizer, ImfOptions options, bool allowGroup, bool build, [MaybeNullWhen(false)] out ImfAddress address, out int mailboxCount)
        {
            address = null;
            mailboxCount = 1;

            // Only what follows the leading words tells a name from a local part, and each is read once more, so this stays linear.
            var scan = tokenizer;
            if (!TrySkipWords(ref scan, out var next)) return false;

            ImfPhrase displayName;
            switch (next.Kind)
            {
                case ImfTokenKind.At:
                    if (!TryReadAddrSpec(ref tokenizer, build, out var localPart, out var domain)) return false;

                    address = build ? new ImfMailbox(new ImfPhrase(string.Empty), localPart, domain) : NoMailbox;
                    return true;

                case ImfTokenKind.LessThan:
                    displayName = new ImfPhrase(string.Empty);
                    if (tokenizer.TryPeek(out var first) && first.Kind != ImfTokenKind.LessThan && !TryReadPhrase(ref tokenizer, options, build, out displayName)) return false;

                    if (!TryReadAngleAddr(ref tokenizer, build, out var angleLocalPart, out var angleDomain)) return false;

                    address = build ? new ImfMailbox(displayName, angleLocalPart, angleDomain) : NoMailbox;
                    return true;

                case ImfTokenKind.Colon:
                    if (!allowGroup || !TryReadPhrase(ref tokenizer, options, build, out displayName)) return false;

                    if (!TryReadGroupList(ref tokenizer, options, build, out var mailboxes, out mailboxCount)) return false;

                    address = build ? (ImfAddress)new ImfGroup(displayName, mailboxes) : NoMailbox;
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryReadPhrase(ref ImfTokenizer tokenizer, ImfOptions options, bool build, out ImfPhrase phrase)
        {
            phrase = default;
            return build ? ImfPhrase.TryRead(ref tokenizer, options, out phrase) : ImfPhrase.TrySkip(ref tokenizer, options);
        }

        private static bool TrySkipWords(ref ImfTokenizer tokenizer, out ImfToken next)
        {
            while (true)
            {
                if (!tokenizer.TryRead(out next)) return false;
                if (!next.IsWord && next.Kind != ImfTokenKind.Period) return true;
            }
        }

        // ref. RFC 5322 §3.4: group = display-name ":" [group-list] ";" [CFWS]; group-list = mailbox-list / CFWS / obs-group-list
        // ref. RFC 5322 §4.4: obs-mbox-list = *([CFWS] ",") mailbox *("," [mailbox / CFWS]); obs-group-list = 1*([CFWS] ",") [CFWS]
        private static bool TryReadGroupList(ref ImfTokenizer tokenizer, ImfOptions options, bool build, [MaybeNullWhen(false)] out ImfMailbox[] mailboxes, out int mailboxCount)
        {
            mailboxes = null;
            mailboxCount = 0;
            if (!tokenizer.TryRead(out var colon) || colon.Kind != ImfTokenKind.Colon) return false;

            var list = build ? new List<ImfMailbox>() : null;
            while (true)
            {
                if (!tokenizer.TryPeek(out var token)) return false;

                if (token.Kind == ImfTokenKind.Comma)
                {
                    tokenizer.TryRead(out token);
                    continue;
                }

                if (token.Kind == ImfTokenKind.Semicolon)
                {
                    tokenizer.TryRead(out token);
                    mailboxes = list?.ToArray() ?? NoMailboxes;
                    return true;
                }

                if (!TryReadAddress(ref tokenizer, options, false, build, out var address, out _) || !TryReadListSeparator(ref tokenizer, ImfTokenKind.Semicolon)) return false;

                list?.Add((ImfMailbox)address);
                mailboxCount++;
            }
        }

        // Checks that an element of a list is followed by a comma, or by what ends the list, and leaves it to be read.
        private static bool TryReadListSeparator(ref ImfTokenizer tokenizer, ImfTokenKind end)
        {
            return tokenizer.TryPeek(out var token) && (token.Kind == ImfTokenKind.Comma || token.Kind == end);
        }

        // ref. RFC 5322 §3.4: angle-addr = [CFWS] "<" addr-spec ">" [CFWS] / obs-angle-addr
        // ref. RFC 5322 §4.4: obs-angle-addr = [CFWS] "<" obs-route addr-spec ">" [CFWS]
        private static bool TryReadAngleAddr(ref ImfTokenizer tokenizer, bool build, [MaybeNullWhen(false)] out string localPart, [MaybeNullWhen(false)] out string domain)
        {
            localPart = null;
            domain = null;
            if (!tokenizer.TryRead(out var open) || open.Kind != ImfTokenKind.LessThan) return false;
            if (!TrySkipObsoleteRoute(ref tokenizer)) return false;
            if (!TryReadAddrSpec(ref tokenizer, build, out localPart, out domain)) return false;

            return tokenizer.TryRead(out var close) && close.Kind == ImfTokenKind.GreaterThan;
        }

        // ref. RFC 5322 §4.4: obs-route = obs-domain-list ":"; obs-domain-list = *(CFWS / ",") "@" domain *("," [CFWS] ["@" domain])
        // A source route is dropped: §4.4 says to ignore it.
        private static bool TrySkipObsoleteRoute(ref ImfTokenizer tokenizer)
        {
            if (!tokenizer.TryPeek(out var token)) return false;
            if (token.Kind != ImfTokenKind.At && token.Kind != ImfTokenKind.Comma) return true;

            while (tokenizer.TryPeek(out token) && token.Kind == ImfTokenKind.Comma)
            {
                tokenizer.TryRead(out token);
            }

            if (!TrySkipRouteDomain(ref tokenizer)) return false;

            while (true)
            {
                if (!tokenizer.TryRead(out token)) return false;

                if (token.Kind == ImfTokenKind.Colon) return true;
                if (token.Kind != ImfTokenKind.Comma) return false;

                if (!tokenizer.TryPeek(out token)) return false;

                if (token.Kind == ImfTokenKind.At && !TrySkipRouteDomain(ref tokenizer)) return false;
            }
        }

        private static bool TrySkipRouteDomain(ref ImfTokenizer tokenizer)
        {
            return tokenizer.TryRead(out var at) && at.Kind == ImfTokenKind.At && TryReadDomain(ref tokenizer, false, out _);
        }

        // ref. RFC 5322 §3.4.1: addr-spec = local-part "@" domain
        private static bool TryReadAddrSpec(ref ImfTokenizer tokenizer, bool build, [MaybeNullWhen(false)] out string localPart, [MaybeNullWhen(false)] out string domain)
        {
            domain = null;
            if (!TryReadLocalPart(ref tokenizer, build, out localPart)) return false;

            return tokenizer.TryRead(out var at) && at.Kind == ImfTokenKind.At && TryReadDomain(ref tokenizer, build, out domain);
        }

        // ref. RFC 5322 §3.4.1: local-part = dot-atom / quoted-string / obs-local-part; §4.4: obs-local-part = word *("." word)
        private static bool TryReadLocalPart(ref ImfTokenizer tokenizer, bool build, [MaybeNullWhen(false)] out string localPart)
        {
            localPart = null;
            var text = build ? new StringBuilder() : null;
            while (true)
            {
                if (!tokenizer.TryRead(out var word) || !word.IsWord) return false;

                text?.Append(tokenizer.GetText(word));

                if (!tokenizer.TryPeek(out var token)) return false;

                if (token.Kind != ImfTokenKind.Period) break;

                tokenizer.TryRead(out token);
                text?.Append('.');
            }

            localPart = text?.ToString() ?? string.Empty;
            return true;
        }

        // ref. RFC 5322 §3.4.1: domain = dot-atom / domain-literal / obs-domain; §4.4: obs-domain = atom *("." atom)
        private static bool TryReadDomain(ref ImfTokenizer tokenizer, bool build, [MaybeNullWhen(false)] out string domain)
        {
            domain = null;
            if (!tokenizer.TryRead(out var token)) return false;

            if (token.Kind == ImfTokenKind.DomainLiteral)
            {
                domain = build ? tokenizer.GetText(token) : string.Empty;
                return true;
            }

            var text = build ? new StringBuilder() : null;
            while (true)
            {
                if (token.Kind != ImfTokenKind.Atom) return false;

                text?.Append(tokenizer.GetText(token));

                if (!tokenizer.TryPeek(out token)) return false;

                if (token.Kind != ImfTokenKind.Period) break;

                tokenizer.TryRead(out token);
                if (!tokenizer.TryRead(out token)) return false;

                text?.Append('.');
            }

            domain = text?.ToString() ?? string.Empty;
            return true;
        }

        // ref. RFC 5322 §3.6.4: msg-id = [CFWS] "<" id-left "@" id-right ">" [CFWS]
        // ref. RFC 5322 §4.5.4: obs-id-left = local-part; obs-id-right = domain
        private static bool TryReadMessageId(ref ImfTokenizer tokenizer, bool build, [MaybeNullWhen(false)] out ImfMessageId messageId)
        {
            messageId = null;
            if (!tokenizer.TryRead(out var open) || open.Kind != ImfTokenKind.LessThan) return false;
            if (!TryReadAddrSpec(ref tokenizer, build, out var idLeft, out var idRight)) return false;
            if (!tokenizer.TryRead(out var close) || close.Kind != ImfTokenKind.GreaterThan) return false;

            messageId = build ? new ImfMessageId(idLeft, idRight, true) : NoMessageId;
            return true;
        }

        private static bool IsAtEnd(ref ImfTokenizer tokenizer)
        {
            return tokenizer.TryRead(out var token) && token.Kind == ImfTokenKind.End;
        }
    }
}
