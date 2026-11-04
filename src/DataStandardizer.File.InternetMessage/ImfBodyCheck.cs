using System;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Checks the bytes of a body against the rules for it, however they are split: RFC 5322 §2.1.1 and §2.3, RFC 2045 §2.7 and §2.8 for what an identity encoding labels, §6.4 for the encoding
    /// of what holds it, and §6.7 and §6.8 for the lines of the encoded forms.
    /// </summary>
    internal sealed class ImfBodyCheck
    {
        /// <summary>
        /// The most octets in a line, not counting its line break (RFC 5322 §2.1.1).
        /// </summary>
        internal const int MaxLineLength = 998;

        /// <summary>
        /// The most characters in a line of an encoded body (RFC 2045 §6.7 and §6.8).
        /// </summary>
        internal const int MaxEncodedLineLength = 76;

        internal const string InvalidOctetsMessage = "The body holds an octet or a line break that its transfer encoding doesn't allow (RFC 5322 §2.3, RFC 2045 §2.7, §2.8 and §6.4).  ImfOptions.AllowInvalidBodyOctets accepts it.";
        internal const string InvalidHeaderOctetsMessage = "The header of an entity holds an octet above 127, which the 7bit label of the entity that holds it doesn't allow (RFC 2045 §2.7 and §6.4, RFC 6532 §3.7).  ImfOptions.AllowInvalidBodyOctets accepts it.";
        internal const string LongLineMessage = "The body has a line longer than its transfer encoding or RFC 5322 §2.1.1 allows (RFC 2045 §2.7, §2.8, §6.7 and §6.8).  ImfOptions.AllowLongLines accepts it.";
        internal const string LongQuotedLineMessage = "A line of the message is longer than the 998 octets of RFC 5322 §2.1.1 with the quote it has in an mbox file (RFC 4155 Appendix A).  ImfOptions.AllowLongLines accepts it.";

        private const byte CarriageReturn = 0x0D;
        private const byte LineFeed = 0x0A;
        private const byte HighestSevenBitOctet = 0x7F;
        private const byte Quote = 0x3E;
        private const int FromLength = 5;
        private const int NotMatchingFrom = -1;

        private static readonly byte[] From = { 0x46, 0x72, 0x6F, 0x6D, 0x20 };

        private readonly bool _acceptBareLineFeed;
        private readonly bool _lineFeedIsLineBreak;

        private MimeDataDomain _domain;
        private bool _hasLongLine;
        private bool _hasInvalidOctet;
        private bool _isMime;
        private int _lineLength;
        private int _maxLineLength;
        private bool _previousWasCarriageReturn;
        private int _fromMatched;
        private bool _isQuotedLine;

        /// <summary>
        /// Which lines of what is read have a quote in an mbox file that isn't in these bytes.
        /// </summary>
        internal enum QuotedLines
        {
            /// <summary>
            /// None: this isn't from an mbox file, or the quotes are in these bytes.
            /// </summary>
            None,

            /// <summary>
            /// Lines starting <c>From </c>, as in the mboxo and mboxcl variants.
            /// </summary>
            From,

            /// <summary>
            /// Lines starting with any number of <c>&gt;</c> and then <c>From </c>, as in the mboxrd variant.
            /// </summary>
            QuotedFrom,
        }

        internal ImfBodyCheck(ImfOptions options, bool lineFeedIsLineBreak)
        {
            _acceptBareLineFeed = options.AcceptBareLineFeed;
            _lineFeedIsLineBreak = lineFeedIsLineBreak;
        }

        /// <summary>
        /// Gets the transfer encoding that limits a body, given what limits the entity that holds it (RFC 2045 §6.4).
        /// </summary>
        internal static MimeDataDomain GetAllowedDomain(MimeTransferEncoding encoding, MimeDataDomain enclosing)
        {
            // An extension mechanism says nothing, so only what holds it limits it.
            var own = encoding.Domain == MimeDataDomain.Unknown ? MimeDataDomain.Binary : encoding.Domain;
            return own < enclosing ? own : enclosing;
        }

        /// <summary>
        /// Gets or sets which lines have a quote in an mbox file that isn't in the bytes.  It counts towards the 998 octets, as RFC 4155 Appendix A applies RFC 5322 §2.1.1 to each message in the file.
        /// </summary>
        internal QuotedLines Quoted { get; set; }

        /// <summary>
        /// Gets a flag indicating that the long line found is too long only with the quote it has in an mbox file.
        /// </summary>
        internal bool LongLineIsQuoted { get; private set; }

        /// <summary>
        /// Determines if a line of a header is too long in an mbox file with its quote, which the line doesn't hold.
        /// </summary>
        internal static bool IsLongQuotedLine(ReadOnlySpan<byte> line, QuotedLines quoted)
        {
            if (quoted == QuotedLines.None || line.Length < MaxLineLength) return false;

            var start = 0;
            if (quoted == QuotedLines.QuotedFrom)
            {
                while (start < line.Length && line[start] == Quote) start++;
            }

            return line.Length - start >= FromLength && line.Slice(start, FromLength).SequenceEqual(From);
        }

        /// <summary>
        /// Determines if the bytes of a field of a nested entity hold an octet that the entity holding it doesn't allow (RFC 2045 §2.7 and §6.4).
        /// </summary>
        internal static bool HasInvalidHeaderOctet(ReadOnlySpan<byte> bytes, MimeDataDomain enclosing)
        {
            if (enclosing != MimeDataDomain.SevenBit) return false;

            foreach (var octet in bytes)
            {
                if (octet > HighestSevenBitOctet) return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the longest line of a body in a transfer encoding.
        /// </summary>
        internal static int GetMaxLineLength(MimeTransferEncoding encoding)
        {
            return encoding == MimeTransferEncoding.QuotedPrintable || encoding == MimeTransferEncoding.Base64 ? MaxEncodedLineLength : MaxLineLength;
        }

        /// <summary>
        /// Starts on a body.
        /// </summary>
        /// <param name="domain">What the body may hold.  <see cref="MimeDataDomain.Binary"/> has no rules.</param>
        /// <param name="isMime"><c>false</c> if the message has no MIME fields, so that the obsolete syntax of RFC 5322 §4.1 applies, which a receiver must accept: NUL and bare CR and LF.</param>
        /// <param name="maxLineLength">The longest line.</param>
        internal void Start(MimeDataDomain domain, bool isMime, int maxLineLength)
        {
            _domain = domain;
            _isMime = isMime;
            _maxLineLength = maxLineLength;
            _lineLength = 0;
            _previousWasCarriageReturn = false;
            _hasLongLine = false;
            _hasInvalidOctet = false;
            LongLineIsQuoted = false;
            StartLine();
        }

        /// <summary>
        /// Checks more of the body.  Each problem is given once for each body.
        /// </summary>
        /// <param name="bytes">The next bytes.</param>
        /// <param name="foundLongLine"><c>true</c> if a line is too long.</param>
        /// <param name="foundInvalidOctet"><c>true</c> if an octet or line break is not allowed.</param>
        /// <param name="longLineOffset">The index in <paramref name="bytes"/> of the octet that takes a line past the limit, if one was found.</param>
        /// <param name="invalidOctetOffset">The index in <paramref name="bytes"/> at which an octet or line break was found not to be allowed, if one was.</param>
        internal void Check(ReadOnlySpan<byte> bytes, out bool foundLongLine, out bool foundInvalidOctet, out int longLineOffset, out int invalidOctetOffset)
        {
            foundLongLine = false;
            foundInvalidOctet = false;
            longLineOffset = 0;
            invalidOctetOffset = 0;
            if (_domain == MimeDataDomain.Binary) return;

            for (var index = 0; index < bytes.Length; index++)
            {
                var hadLongLine = _hasLongLine;
                var hadInvalidOctet = _hasInvalidOctet;
                CheckOctet(bytes[index]);
                if (_hasLongLine && !hadLongLine)
                {
                    foundLongLine = true;
                    longLineOffset = index;
                }

                if (_hasInvalidOctet && !hadInvalidOctet)
                {
                    foundInvalidOctet = true;
                    invalidOctetOffset = index;
                }
            }
        }

        /// <summary>
        /// Ends the body.
        /// </summary>
        /// <returns><c>true</c> if a bare carriage return, which isn't allowed, ended it, and that has not been given already.</returns>
        internal bool End()
        {
            var isBare = _previousWasCarriageReturn && _isMime && _domain != MimeDataDomain.Binary && !_hasInvalidOctet;
            _previousWasCarriageReturn = false;
            _hasInvalidOctet |= isBare;
            return isBare;
        }

        private void CheckOctet(byte octet)
        {
            if (_previousWasCarriageReturn)
            {
                _previousWasCarriageReturn = false;
                if (octet == LineFeed)
                {
                    StartLine();
                    return;
                }

                // ref. RFC 2045 §2.7: CR and LF only as a CRLF.
                _hasInvalidOctet |= _isMime;
            }

            if (octet == CarriageReturn)
            {
                _previousWasCarriageReturn = true;
                return;
            }

            if (octet == LineFeed)
            {
                _hasInvalidOctet |= _isMime && !_lineFeedIsLineBreak && !_acceptBareLineFeed;
                StartLine();
                return;
            }

            _lineLength++;
            if (Quoted != QuotedLines.None) CheckQuotedLine(octet);

            _hasLongLine |= _lineLength > _maxLineLength;
            _hasInvalidOctet |= (octet > HighestSevenBitOctet && _domain == MimeDataDomain.SevenBit) || (octet == 0 && _isMime);
        }

        private void StartLine()
        {
            _lineLength = 0;
            _fromMatched = 0;
            _isQuotedLine = false;
        }

        private void CheckQuotedLine(byte octet)
        {
            if (_fromMatched != NotMatchingFrom && !(_fromMatched == 0 && octet == Quote && Quoted == QuotedLines.QuotedFrom))
            {
                _fromMatched = octet == From[_fromMatched] ? _fromMatched + 1 : NotMatchingFrom;
                if (_fromMatched == FromLength)
                {
                    _isQuotedLine = true;
                    _fromMatched = NotMatchingFrom;
                }
            }

            if (_isQuotedLine && _lineLength >= MaxLineLength && _lineLength <= _maxLineLength && !_hasLongLine)
            {
                _hasLongLine = true;
                LongLineIsQuoted = true;
            }
        }
    }
}
