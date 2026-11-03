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
    /// Which fields allow which forms is not decided here: a list of addresses takes groups wherever one appears (RFC 6854).
    /// </remarks>
    internal static class ImfAddressParser
    {
        internal static bool TryParseAddress(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfAddress result)
        {
            result = null;
            if (!ImfTokenizer.IsWellFormed(value)) return false;

            var tokenizer = new ImfTokenizer(value);
            if (TryReadAddress(ref tokenizer, true, out var address) && IsAtEnd(ref tokenizer))
            {
                result = address;
                return true;
            }

            return false;
        }

        internal static bool TryParseMailbox(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfMailbox result)
        {
            result = null;
            if (!TryParseAddress(value, out var address)) return false;

            result = address as ImfMailbox;
            return !(result is null);
        }

        internal static bool TryParseGroup(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfGroup result)
        {
            result = null;
            if (!TryParseAddress(value, out var address)) return false;

            result = address as ImfGroup;
            return !(result is null);
        }

        // ref. RFC 5322 §3.4: address-list = (address *("," address)) / obs-addr-list
        // ref. RFC 5322 §4.4: obs-addr-list = *([CFWS] ",") address *("," [address / CFWS])
        internal static bool TryParseAddressList(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfAddress[] result)
        {
            result = null;
            if (!ImfTokenizer.IsWellFormed(value)) return false;

            var tokenizer = new ImfTokenizer(value);
            var addresses = new List<ImfAddress>();
            while (true)
            {
                if (!tokenizer.TryPeek(out var token)) return false;

                if (token.Kind == ImfTokenKind.Comma)
                {
                    tokenizer.TryRead(out token);
                    continue;
                }

                if (token.Kind == ImfTokenKind.End) break;

                if (!TryReadAddress(ref tokenizer, true, out var address) || !TryReadListSeparator(ref tokenizer, ImfTokenKind.End)) return false;

                addresses.Add(address);
            }

            if (addresses.Count == 0) return false;

            result = addresses.ToArray();
            return true;
        }

        // ref. RFC 5322 §3.6.5: keywords = "Keywords:" phrase *("," phrase) CRLF
        // ref. RFC 5322 §4.1: obs-phrase-list = [phrase / CFWS] *("," [phrase / CFWS])
        internal static bool TryParsePhraseList(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfPhrase[] result)
        {
            result = null;
            if (!ImfTokenizer.IsWellFormed(value)) return false;

            var tokenizer = new ImfTokenizer(value);
            var phrases = new List<ImfPhrase>();
            while (true)
            {
                if (!tokenizer.TryPeek(out var token)) return false;

                if (token.Kind == ImfTokenKind.Comma)
                {
                    tokenizer.TryRead(out token);
                    continue;
                }

                if (token.Kind == ImfTokenKind.End) break;

                if (!ImfPhrase.TryRead(ref tokenizer, out var phrase) || !TryReadListSeparator(ref tokenizer, ImfTokenKind.End)) return false;

                phrases.Add(phrase);
            }

            result = phrases.ToArray();
            return true;
        }

        internal static bool TryParseMessageId(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfMessageId result)
        {
            result = null;
            if (!ImfTokenizer.IsWellFormed(value)) return false;

            var tokenizer = new ImfTokenizer(value);
            if (TryReadMessageId(ref tokenizer, out var messageId) && IsAtEnd(ref tokenizer))
            {
                result = messageId;
                return true;
            }

            return false;
        }

        // ref. RFC 5322 §3.6.4: in-reply-to = "In-Reply-To:" 1*msg-id CRLF; references = "References:" 1*msg-id CRLF
        // ref. RFC 5322 §4.5.4: obs-in-reply-to = "In-Reply-To" *WSP ":" *(phrase / msg-id) CRLF; obs-references likewise.  The phrases have no meaning.
        internal static bool TryParseMessageIdList(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfMessageId[] result)
        {
            result = null;
            if (!ImfTokenizer.IsWellFormed(value)) return false;

            var tokenizer = new ImfTokenizer(value);
            var messageIds = new List<ImfMessageId>();
            while (true)
            {
                if (!tokenizer.TryPeek(out var token)) return false;

                if (token.Kind == ImfTokenKind.End) break;

                if (token.Kind == ImfTokenKind.LessThan)
                {
                    if (!TryReadMessageId(ref tokenizer, out var messageId)) return false;

                    messageIds.Add(messageId);
                }
                else if (!ImfPhrase.TryRead(ref tokenizer, out _))
                {
                    return false;
                }
            }

            result = messageIds.ToArray();
            return true;
        }

        // ref. RFC 5322 §3.4: address = mailbox / group; mailbox = name-addr / addr-spec; name-addr = [display-name] angle-addr
        private static bool TryReadAddress(ref ImfTokenizer tokenizer, bool allowGroup, [MaybeNullWhen(false)] out ImfAddress address)
        {
            address = null;

            // Only what follows the leading words tells a name from a local part, and each is read once more, so this stays linear.
            var scan = tokenizer;
            if (!TrySkipWords(ref scan, out var next)) return false;

            ImfPhrase displayName;
            switch (next.Kind)
            {
                case ImfTokenKind.At:
                    if (!TryReadAddrSpec(ref tokenizer, out var localPart, out var domain)) return false;

                    address = new ImfMailbox(new ImfPhrase(string.Empty, false), localPart, domain);
                    return true;

                case ImfTokenKind.LessThan:
                    displayName = new ImfPhrase(string.Empty, false);
                    if (tokenizer.TryPeek(out var first) && first.Kind != ImfTokenKind.LessThan && !ImfPhrase.TryRead(ref tokenizer, out displayName)) return false;

                    if (!TryReadAngleAddr(ref tokenizer, out var angleLocalPart, out var angleDomain)) return false;

                    address = new ImfMailbox(displayName, angleLocalPart, angleDomain);
                    return true;

                case ImfTokenKind.Colon:
                    if (!allowGroup || !ImfPhrase.TryRead(ref tokenizer, out displayName)) return false;

                    if (!TryReadGroupList(ref tokenizer, out var mailboxes)) return false;

                    address = new ImfGroup(displayName, mailboxes);
                    return true;

                default:
                    return false;
            }
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
        private static bool TryReadGroupList(ref ImfTokenizer tokenizer, [MaybeNullWhen(false)] out ImfMailbox[] mailboxes)
        {
            mailboxes = null;
            if (!tokenizer.TryRead(out var colon) || colon.Kind != ImfTokenKind.Colon) return false;

            var list = new List<ImfMailbox>();
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
                    mailboxes = list.ToArray();
                    return true;
                }

                if (!TryReadAddress(ref tokenizer, false, out var address) || !TryReadListSeparator(ref tokenizer, ImfTokenKind.Semicolon)) return false;

                list.Add((ImfMailbox)address);
            }
        }

        // Checks that an element of a list is followed by a comma, or by what ends the list, and leaves it to be read.
        private static bool TryReadListSeparator(ref ImfTokenizer tokenizer, ImfTokenKind end)
        {
            return tokenizer.TryPeek(out var token) && (token.Kind == ImfTokenKind.Comma || token.Kind == end);
        }

        // ref. RFC 5322 §3.4: angle-addr = [CFWS] "<" addr-spec ">" [CFWS] / obs-angle-addr
        // ref. RFC 5322 §4.4: obs-angle-addr = [CFWS] "<" obs-route addr-spec ">" [CFWS]
        private static bool TryReadAngleAddr(ref ImfTokenizer tokenizer, [MaybeNullWhen(false)] out string localPart, [MaybeNullWhen(false)] out string domain)
        {
            localPart = null;
            domain = null;
            if (!tokenizer.TryRead(out var open) || open.Kind != ImfTokenKind.LessThan) return false;
            if (!TrySkipObsoleteRoute(ref tokenizer)) return false;
            if (!TryReadAddrSpec(ref tokenizer, out localPart, out domain)) return false;

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
            return tokenizer.TryRead(out var at) && at.Kind == ImfTokenKind.At && TryReadDomain(ref tokenizer, out _);
        }

        // ref. RFC 5322 §3.4.1: addr-spec = local-part "@" domain
        private static bool TryReadAddrSpec(ref ImfTokenizer tokenizer, [MaybeNullWhen(false)] out string localPart, [MaybeNullWhen(false)] out string domain)
        {
            domain = null;
            if (!TryReadLocalPart(ref tokenizer, out localPart)) return false;

            return tokenizer.TryRead(out var at) && at.Kind == ImfTokenKind.At && TryReadDomain(ref tokenizer, out domain);
        }

        // ref. RFC 5322 §3.4.1: local-part = dot-atom / quoted-string / obs-local-part; §4.4: obs-local-part = word *("." word)
        private static bool TryReadLocalPart(ref ImfTokenizer tokenizer, [MaybeNullWhen(false)] out string localPart)
        {
            localPart = null;
            var text = new StringBuilder();
            while (true)
            {
                if (!tokenizer.TryRead(out var word) || !word.IsWord) return false;

                text.Append(tokenizer.GetText(word));

                if (!tokenizer.TryPeek(out var token)) return false;

                if (token.Kind != ImfTokenKind.Period) break;

                tokenizer.TryRead(out token);
                text.Append('.');
            }

            localPart = text.ToString();
            return true;
        }

        // ref. RFC 5322 §3.4.1: domain = dot-atom / domain-literal / obs-domain; §4.4: obs-domain = atom *("." atom)
        private static bool TryReadDomain(ref ImfTokenizer tokenizer, [MaybeNullWhen(false)] out string domain)
        {
            domain = null;
            if (!tokenizer.TryRead(out var token)) return false;

            if (token.Kind == ImfTokenKind.DomainLiteral)
            {
                domain = tokenizer.GetText(token);
                return true;
            }

            var text = new StringBuilder();
            while (true)
            {
                if (token.Kind != ImfTokenKind.Atom) return false;

                text.Append(tokenizer.GetText(token));

                if (!tokenizer.TryPeek(out token)) return false;

                if (token.Kind != ImfTokenKind.Period) break;

                tokenizer.TryRead(out token);
                if (!tokenizer.TryRead(out token)) return false;

                text.Append('.');
            }

            domain = text.ToString();
            return true;
        }

        // ref. RFC 5322 §3.6.4: msg-id = [CFWS] "<" id-left "@" id-right ">" [CFWS]
        // ref. RFC 5322 §4.5.4: obs-id-left = local-part; obs-id-right = domain
        private static bool TryReadMessageId(ref ImfTokenizer tokenizer, [MaybeNullWhen(false)] out ImfMessageId messageId)
        {
            messageId = null;
            if (!tokenizer.TryRead(out var open) || open.Kind != ImfTokenKind.LessThan) return false;
            if (!TryReadAddrSpec(ref tokenizer, out var idLeft, out var idRight)) return false;
            if (!tokenizer.TryRead(out var close) || close.Kind != ImfTokenKind.GreaterThan) return false;

            messageId = new ImfMessageId(idLeft, idRight, true);
            return true;
        }

        private static bool IsAtEnd(ref ImfTokenizer tokenizer)
        {
            return tokenizer.TryRead(out var token) && token.Kind == ImfTokenKind.End;
        }
    }
}
