using System;
#if !NET10_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
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
#if !NET10_0_OR_GREATER
        private const ulong HighBits = 0x8080808080808080UL;
        private const ulong LowBits = 0x0101010101010101UL;
#endif

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
        /// Determines if the bytes of a field of a nested entity hold an octet that the entity holding it doesn't allow (RFC 2045 §2.7 and §6.4).
        /// </summary>
        internal static bool HasInvalidHeaderOctet(ReadOnlySpan<byte> bytes, MimeDataDomain enclosing)
        {
            return enclosing == MimeDataDomain.SevenBit && IndexOfNonPlain(bytes, MimeDataDomain.SevenBit, false) >= 0;
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
        /// <param name="longLineOffset">The index in <paramref name="bytes"/> of the octet that takes a line past the limit, if one was found.</param>
        /// <param name="invalidOctetOffset">The index in <paramref name="bytes"/> at which an octet or line break was found not to be allowed, if one was.</param>
        internal void Check(ReadOnlySpan<byte> bytes, out bool foundLongLine, out bool foundInvalidOctet, out int longLineOffset, out int invalidOctetOffset)
        {
            foundLongLine = false;
            foundInvalidOctet = false;
            longLineOffset = 0;
            invalidOctetOffset = 0;
            if (_domain == MimeDataDomain.Binary) return;

            var index = 0;
            while (true)
            {
                index += SkipPlainLines(bytes.Slice(index));
                if (index == bytes.Length) return;

                // Only the checks of single octets decide what is wrong, so a body gets the same answer however the plain lines around the problem are passed over.
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

                index++;
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

        /// <summary>
        /// Passes over the lines at the start of some bytes that have no problem, and stops at the first octet that <see cref="CheckOctet"/> has to look at.
        /// </summary>
        /// <returns>The number of bytes passed over.</returns>
        private int SkipPlainLines(ReadOnlySpan<byte> bytes)
        {
            if (_previousWasCarriageReturn || _hasInvalidOctet || _hasLongLine) return 0;

            var position = 0;
            while (position < bytes.Length)
            {
                // The search ends just past the limit, as a line longer than that stops here, so that a long line isn't searched again for each of its octets.
                var room = _maxLineLength - _lineLength;
                var rest = bytes.Slice(position);
                var window = rest.Length > room ? rest.Slice(0, room + 1) : rest;
                var lineEnd = window.IndexOfAny(CarriageReturn, LineFeed);
                var segment = lineEnd >= 0 ? window.Slice(0, lineEnd) : window.Length > room ? window.Slice(0, room) : window;

                var nonPlain = IndexOfNonPlain(segment, _domain, _isMime);
                if (nonPlain >= 0)
                {
                    _lineLength += nonPlain;
                    return position + nonPlain;
                }

                _lineLength += segment.Length;
                position += segment.Length;
                if (lineEnd < 0) return position;

                // ref. RFC 2045 §2.7: CR and LF only as a CRLF.  A CR with nothing after it yet may be half of one, which CheckOctet remembers.
                var lineBreakLength = bytes[position] == LineFeed ? (_lineFeedIsLineBreak ? 1 : 0) : (position + 1 < bytes.Length && bytes[position + 1] == LineFeed ? 2 : 0);
                if (lineBreakLength == 0) return position;

                position += lineBreakLength;
                _lineLength = 0;
            }

            return position;
        }

        /// <summary>
        /// Finds the first octet, in bytes with no line break, that a body with some rules doesn't allow: one above 127 under <see cref="MimeDataDomain.SevenBit"/>, and a NUL in a body with MIME
        /// fields.
        /// </summary>
        /// <returns>The index of the octet, or -1 if there is none.</returns>
        private static int IndexOfNonPlain(ReadOnlySpan<byte> bytes, MimeDataDomain domain, bool isMime)
        {
            if (domain != MimeDataDomain.SevenBit) return isMime ? bytes.IndexOf((byte)0) : -1;

#if NET10_0_OR_GREATER
            return bytes.IndexOfAnyExceptInRange(isMime ? (byte)1 : (byte)0, HighestSevenBitOctet);
#else
            // Eight octets at a time.  The subtraction sets a high bit only for a NUL, or just above one, so a word that shows nothing has nothing in it, and the octets of one that shows
            // something are looked at one by one.
            var index = 0;
            while (bytes.Length - index >= sizeof(ulong))
            {
                var word = MemoryMarshal.Read<ulong>(bytes.Slice(index));
                if (((isMime ? word | (word - LowBits) : word) & HighBits) != 0) break;

                index += sizeof(ulong);
            }

            for (; index < bytes.Length; index++)
            {
                var octet = bytes[index];
                if (octet > HighestSevenBitOctet || (octet == 0 && isMime)) return index;
            }

            return -1;
#endif
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
