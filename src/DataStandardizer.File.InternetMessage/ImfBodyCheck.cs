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
        internal const string LongLineMessage = "The body has a line longer than its transfer encoding or RFC 5322 §2.1.1 allows (RFC 2045 §2.7, §2.8, §6.7 and §6.8).  ImfOptions.AllowLongLines accepts it.";

        private const byte CarriageReturn = 0x0D;
        private const byte LineFeed = 0x0A;
        private const byte HighestSevenBitOctet = 0x7F;

        private readonly bool _acceptBareLineFeed;
        private readonly bool _lineFeedIsLineBreak;

        private MimeDataDomain _domain;
        private bool _hasLongLine;
        private bool _hasInvalidOctet;
        private bool _isMime;
        private int _lineLength;
        private int _maxLineLength;
        private bool _previousWasCarriageReturn;

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
        }

        /// <summary>
        /// Checks more of the body.  Each problem is given once for each body.
        /// </summary>
        /// <param name="bytes">The next bytes.</param>
        /// <param name="foundLongLine"><c>true</c> if a line is too long.</param>
        /// <param name="foundInvalidOctet"><c>true</c> if an octet or line break is not allowed.</param>
        internal void Check(ReadOnlySpan<byte> bytes, out bool foundLongLine, out bool foundInvalidOctet)
        {
            var hadLongLine = _hasLongLine;
            var hadInvalidOctet = _hasInvalidOctet;
            if (_domain != MimeDataDomain.Binary)
            {
                foreach (var octet in bytes)
                {
                    CheckOctet(octet);
                }
            }

            foundLongLine = _hasLongLine && !hadLongLine;
            foundInvalidOctet = _hasInvalidOctet && !hadInvalidOctet;
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
                    _lineLength = 0;
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
                _lineLength = 0;
                return;
            }

            _lineLength++;
            _hasLongLine |= _lineLength > _maxLineLength;
            _hasInvalidOctet |= (octet > HighestSevenBitOctet && _domain == MimeDataDomain.SevenBit) || (octet == 0 && _isMime);
        }
    }
}
