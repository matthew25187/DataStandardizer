using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A multipart boundary an <see cref="ImfEntityWriter"/> is using, which can be replaced if a generated one collides with something.
    /// </summary>
    internal sealed class ImfBoundary
    {
        private const string QuotedPrintableSafeText = "=_";

        internal ImfBoundary(string value, bool isGenerated, ImfBoundary[] enclosing)
        {
            IsGenerated = isGenerated;
            Enclosing = enclosing;
            Value = value;
        }

        /// <summary>
        /// Gets the boundaries of the multipart entities around this one.
        /// </summary>
        internal ImfBoundary[] Enclosing { get; }

        internal bool IsGenerated { get; }

        /// <summary>
        /// Gets or sets a flag indicating that the boundary is that of a multipart entity that was read, whose delimiters are written as they were.
        /// </summary>
        internal bool IsPreserved { get; set; }

        /// <summary>
        /// Gets a flag indicating that quoted-printable output can't hold the boundary, because a <c>=</c> in it is always followed by two hexadecimal
        /// digits or a line break (RFC 2045 §6.7).  Base64 output can't hold any delimiter, as it has no <c>-</c>.
        /// </summary>
        internal bool IsQuotedPrintableSafe => Value.IndexOf(QuotedPrintableSafeText, StringComparison.Ordinal) >= 0;

        internal string Value { get; set; }

        /// <summary>
        /// Determines if a delimiter of one boundary would start with the other, so a reader that goes by a line's prefix could mistake it (RFC 2046 §5.1.1).
        /// </summary>
        internal bool ConflictsWith(string other)
        {
            return Value.StartsWith(other, StringComparison.Ordinal) || other.StartsWith(Value, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// A body, preamble, header or other run of bytes an <see cref="ImfEntityWriter"/> has to look at before it writes anything.  It finds the data domain
    /// and the size in quoted-printable and base64, and whether a line starts with a delimiter of an enclosing boundary.
    /// </summary>
    internal sealed class ImfBodyScan
    {
        private const int Base64LineLength = 76;
        private const byte CarriageReturn = 0x0D;
        private const int EncodedBlockSize = 4096;
        private const int LineBreakLength = 2;
        private const int QuotedPrintableLineLength = 76;
        private const int SoftLineBreakLength = 3;
        private const byte Hyphen = 0x2D;
        private const byte LineFeed = 0x0A;

        // ref. RFC 2046 §5.1.1: a boundary is at most 70 characters, after two hyphens.
        private const int MaxDelimiterLength = 72;

        private readonly bool _lineFeedIsLineBreak;
        private readonly byte[] _linePrefix = new byte[MaxDelimiterLength];

        private bool _hasBareLineBreak;
        private bool _hasEightBit;
        private bool _hasNul;
        private bool _isClassified;
        private bool _isClassifying;
        private int _lineLength;
        private int _linePrefixLength;
        private long _lineBreakBytes;
        private long _literalBytes;
        private int _maxLineLength;
        private long _otherBytes;
        private long _encodedLineLength;
        private long _softLineBreakBytes;
        private bool _previousWasCarriageReturn;
        private bool _isCheckingOutput;
        private byte[] _encoded = new byte[0];

        internal ImfBodyScan(ReadOnlyMemory<byte> memory, Stream stream, ImfBoundary[] enclosing, bool lineFeedIsLineBreak)
        {
            Memory = memory;
            Stream = stream;
            Enclosing = enclosing;
            Collisions = new List<ImfBoundary>();
            StartPosition = stream.CanSeek ? stream.Position : 0;
            _lineFeedIsLineBreak = lineFeedIsLineBreak;
        }

        /// <summary>
        /// What of a run of bytes has to be compared with the enclosing boundaries.
        /// </summary>
        internal enum CheckKind
        {
            /// <summary>
            /// The bytes themselves.
            /// </summary>
            Bytes,

            /// <summary>
            /// The output of <see cref="OutputEncoder"/>.
            /// </summary>
            Output,

            /// <summary>
            /// Nothing, as the encoding of the bytes can't hold any of the delimiters.
            /// </summary>
            None,
        }

        /// <summary>
        /// Gets or sets what has to be compared with the enclosing boundaries.
        /// </summary>
        internal CheckKind Check { get; set; }

        internal List<ImfBoundary> Collisions { get; }

        /// <summary>
        /// Gets or sets the encoder of the bytes, whose output is looked at when <see cref="Check"/> says so.
        /// </summary>
        internal ImfBodyEncoder OutputEncoder { get; set; } = NoEncoder.Instance;

        internal ImfBoundary[] Enclosing { get; }

        internal ReadOnlyMemory<byte> Memory { get; }

        internal long StartPosition { get; }

        /// <summary>
        /// Gets the stream of the bytes, or <see cref="Stream.Null"/> if they are in <see cref="Memory"/>.
        /// </summary>
        internal Stream Stream { get; }

        internal bool IsStream => !ReferenceEquals(Stream, System.IO.Stream.Null);

        /// <summary>
        /// Gets the size of the bytes in base64, with its line breaks.
        /// </summary>
        internal long Base64Length
        {
            get
            {
                var encoded = (TotalLength + 2) / 3 * 4;

                // ref. RFC 2045 §6.8: lines of 76 characters, with a CRLF between them.
                return encoded == 0 ? 0 : encoded + (encoded - 1) / Base64LineLength * LineBreakLength;
            }
        }

        /// <summary>
        /// Gets the data domain of the bytes (RFC 2045 §2.7 to §2.9), in which each line break is the writer's, and each line is at most 998 octets.
        /// </summary>
        internal bool HasLongLine => _maxLineLength > ImfHeaderFormatter.MaxLineLength;

        /// <summary>
        /// Gets a flag indicating that the bytes hold something no line of text can: a NUL, or a line break that isn't a whole one.
        /// </summary>
        internal bool HasInvalidOctets => _hasNul || _hasBareLineBreak;

        internal MimeDataDomain Domain
        {
            get
            {
                if (_hasNul || _hasBareLineBreak || _maxLineLength > ImfHeaderFormatter.MaxLineLength) return MimeDataDomain.Binary;

                return _hasEightBit ? MimeDataDomain.EightBit : MimeDataDomain.SevenBit;
            }
        }

        /// <summary>
        /// Gets a flag indicating that a CR or LF in the bytes is not part of a line break.
        /// </summary>
        internal bool HasBareLineBreak => _hasBareLineBreak;

        private long TotalLength => _literalBytes + _otherBytes + _lineBreakBytes;

        /// <summary>
        /// Gets an estimate of the size of the bytes in quoted-printable, with its soft line breaks.
        /// </summary>
        /// <param name="encodeLineBreaks"><c>true</c> if each CR and LF is encoded, so that the whole body is one line that only soft line breaks split.</param>
        internal long GetQuotedPrintableLength(bool encodeLineBreaks)
        {
            if (!encodeLineBreaks) return _literalBytes + 3 * _otherBytes + _lineBreakBytes + _softLineBreakBytes;

            var encoded = _literalBytes + 3 * _otherBytes + 3 * _lineBreakBytes;
            return encoded + GetSoftLineBreakBytes(encoded);
        }

        /// <summary>
        /// Gets the size of the soft line breaks of a line of quoted-printable (RFC 2045 §6.7 rule 5): at most 76 characters to a line, of which the last is
        /// the <c>=</c> of a soft line break, unless it is the last line.
        /// </summary>
        private static long GetSoftLineBreakBytes(long lineLength)
        {
            return lineLength <= QuotedPrintableLineLength ? 0 : (lineLength - 1) / (QuotedPrintableLineLength - 1) * SoftLineBreakLength;
        }

        /// <summary>
        /// Prepares to look at the bytes from the start.
        /// </summary>
        /// <param name="checkOutput"><c>true</c> to compare the output of <see cref="OutputEncoder"/> with the boundaries, rather than the bytes.</param>
        internal void Begin(bool checkOutput)
        {
            _isClassifying = !_isClassified;
            _linePrefixLength = 0;
            Collisions.Clear();
            _isCheckingOutput = checkOutput;
            if (checkOutput)
            {
                OutputEncoder.Reset();
                if (_encoded.Length == 0) _encoded = new byte[EncodedBlockSize];
            }

            if (IsStream && Stream.CanSeek)
            {
                Stream.Position = StartPosition;
            }
        }

        internal void Add(ReadOnlySpan<byte> bytes)
        {
            if (_isClassifying) Classify(bytes);

            if (_isCheckingOutput)
            {
                EncodeAndMatch(bytes, false);
            }
            else
            {
                MatchLines(bytes);
            }
        }

        internal void Complete()
        {
            if (_isCheckingOutput)
            {
                EncodeAndMatch(ReadOnlySpan<byte>.Empty, true);
            }

            if (_linePrefixLength >= 0) CheckLinePrefix();

            if (_isClassifying)
            {
                if (_previousWasCarriageReturn) _hasBareLineBreak = true;

                _softLineBreakBytes += GetSoftLineBreakBytes(_encodedLineLength);
                _isClassified = true;
                _isClassifying = false;
            }
        }

        private void EncodeAndMatch(ReadOnlySpan<byte> bytes, bool isFinalBlock)
        {
            while (true)
            {
                int consumed, written;
                var status = OutputEncoder.Encode(bytes, _encoded, out consumed, out written, isFinalBlock);
                MatchLines(new ReadOnlySpan<byte>(_encoded, 0, written));
                bytes = bytes.Slice(consumed);
                if (status == OperationStatus.Done) return;
            }
        }

        private void Classify(ReadOnlySpan<byte> bytes)
        {
            foreach (var octet in bytes)
            {
                if (octet == LineFeed)
                {
                    if (!_lineFeedIsLineBreak && !_previousWasCarriageReturn) _hasBareLineBreak = true;

                    _lineBreakBytes++;
                    _lineLength = 0;
                    _softLineBreakBytes += GetSoftLineBreakBytes(_encodedLineLength);
                    _encodedLineLength = 0;
                    _previousWasCarriageReturn = false;
                    continue;
                }

                if (_previousWasCarriageReturn) _hasBareLineBreak = true;

                if (octet == CarriageReturn)
                {
                    if (_lineFeedIsLineBreak) _hasBareLineBreak = true;

                    _lineBreakBytes++;
                    _previousWasCarriageReturn = true;
                    continue;
                }

                _previousWasCarriageReturn = false;
                if (octet == 0) _hasNul = true;
                if (octet >= 0x80) _hasEightBit = true;

                // ref. RFC 2045 §6.7 rules 2 and 3: printable characters other than "=", and spaces and tabs, are written as themselves.
                if ((octet >= 33 && octet <= 126 && octet != '=') || octet == ' ' || octet == '\t')
                {
                    _literalBytes++;
                    _encodedLineLength++;
                }
                else
                {
                    _otherBytes++;
                    _encodedLineLength += 3;
                }

                _lineLength++;
                if (_lineLength > _maxLineLength) _maxLineLength = _lineLength;
            }
        }

        private void MatchLines(ReadOnlySpan<byte> bytes)
        {
            if (Enclosing.Length == 0) return;

            foreach (var octet in bytes)
            {
                if (_linePrefixLength >= 0)
                {
                    if (octet == LineFeed || octet == CarriageReturn)
                    {
                        CheckLinePrefix();
                    }
                    else if (_linePrefixLength < 2 && octet != Hyphen)
                    {
                        _linePrefixLength = -1;
                    }
                    else
                    {
                        _linePrefix[_linePrefixLength++] = octet;
                        if (_linePrefixLength == MaxDelimiterLength) CheckLinePrefix();
                    }
                }

                if (octet == LineFeed) _linePrefixLength = 0;
            }
        }

        /// <summary>
        /// Compares the start of the current line with the delimiter of each enclosing boundary.  RFC 2046 §5.1.1 says a boundary must not appear as the prefix
        /// of any line, so this is stricter than <see cref="ImfEntityReader"/>, which matches a whole line.
        /// </summary>
        private void CheckLinePrefix()
        {
            foreach (var boundary in Enclosing)
            {
                if (StartsWithDelimiter(boundary.Value) && !Collisions.Contains(boundary)) Collisions.Add(boundary);
            }

            _linePrefixLength = -1;
        }

        private bool StartsWithDelimiter(string boundary)
        {
            if (_linePrefixLength < boundary.Length + 2) return false;

            for (var index = 0; index < boundary.Length; index++)
            {
                if (_linePrefix[index + 2] != boundary[index]) return false;
            }

            return _linePrefix[0] == Hyphen && _linePrefix[1] == Hyphen;
        }

        private sealed class NoEncoder : ImfBodyEncoder
        {
            internal static readonly NoEncoder Instance = new NoEncoder();

            internal override OperationStatus Encode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
            {
                throw new NotSupportedException();
            }

            internal override void Reset()
            {
            }
        }
    }
}
