using System;
using System.Globalization;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The structured value of the CLIENTPIDMAP property (RFC 6350 §6.7.7): a PID source identifier and the URI that gives it a global meaning.  Only vCard 4.0 has this property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type holds the URI.  To read a value without allocating, unless its URI has an escape, use <see cref="TryParseParts(ReadOnlySpan{char}, VcardValueFormat, out long, out ReadOnlySpan{char})"/>, which
    /// checks the URI's characters and scheme (RFC 3986) but leaves <see cref="Uri"/> to go further.
    /// </para>
    /// <para>
    /// A default instance is not initialised: <see cref="SourceId"/> and <see cref="Uri"/> throw <see cref="InvalidOperationException"/>,
    /// <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// <para>
    /// In jCard (RFC 7095 §3.3.1.3) the value is a JSON array with an element for each component, not a single string, so it has no <see cref="VcardValueFormat.Json"/> form of
    /// its own, and parsing or formatting in that form throws <see cref="NotSupportedException"/>.  <c>JcardReader</c> and <c>JcardWriter</c> map the array to and from the text form.
    /// Likewise in xCard (RFC 6351 §5) the value is a tree of XML elements, one for each component, so it has no <see cref="VcardValueFormat.Xml"/> form of its own either, and
    /// <c>XcardReader</c> and <c>XcardWriter</c> map the elements.
    /// </para>
    /// </remarks>
    public readonly struct VcardClientPidMap : IEquatable<VcardClientPidMap>
    {
        private const string UninitializedMessage = "The VcardClientPidMap instance is not initialised.";

        private readonly long _sourceId;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly Uri? _uri;
#else
        private readonly Uri _uri;
#endif

        /// <summary>
        /// Creates a value.
        /// </summary>
        /// <param name="sourceId">The source identifier, which RFC 6350 §6.7.7 says must be strictly positive.</param>
        /// <param name="uri">The URI, which must be absolute.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sourceId"/> is not positive.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is not absolute, or was given with characters a URI value can't hold, such as a space.</exception>
        public VcardClientPidMap(long sourceId, Uri uri)
        {
            if (sourceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceId), sourceId, "The source identifier must be positive.");
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            VcardValue.CheckUri(uri, nameof(uri));
            _sourceId = sourceId;
            _uri = uri;
        }

        /// <summary>
        /// Gets the source identifier.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public long SourceId
        {
            get
            {
                CheckInitialized();
                return _sourceId;
            }
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public Uri Uri => _uri ?? throw new InvalidOperationException(UninitializedMessage);

        /// <summary>
        /// Parses a value, such as <c>1;urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The value, if it is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardClientPidMap result)
        {
            result = default(VcardClientPidMap);
            long sourceId;
            ReadOnlySpan<char> uriText;
            if (!TryParseParts(value, format, out sourceId, out uriText)) return false;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Uri? uri;
#else
            Uri uri;
#endif
            if (!VcardValue.TryParseUri(uriText, format, out uri)) return false;

            result = new VcardClientPidMap(sourceId, uri);
            return true;
        }

        /// <summary>
        /// Parses a value without allocating, unless its URI has an escape.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="sourceId">The source identifier, if the value is valid.</param>
        /// <param name="uri">The text of the URI as it appears in the value, with any escaping, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParseParts(ReadOnlySpan<char> value, VcardValueFormat format, out long sourceId, out ReadOnlySpan<char> uri)
        {
            VcardValueFormatting.CheckStructuredFormat(format, "A CLIENTPIDMAP value");

            // ref. RFC 6350 §6.7.7: CLIENTPIDMAP-value = 1*DIGIT ";" URI
            sourceId = 0;
            uri = default(ReadOnlySpan<char>);
            var delimiterIndex = value.IndexOf(';');
            if (delimiterIndex < 0) return false;

            long parsedId;
            var uriPart = value.Slice(delimiterIndex + 1);
            if (!VcardValueFormatting.TryParseNumber(value.Slice(0, delimiterIndex), out parsedId) || parsedId == 0 || !VcardValue.IsUriValue(uriPart)) return false;

            sourceId = parsedId;
            uri = uriPart;
            return true;
        }

        /// <summary>
        /// Parses a value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardClientPidMap Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            VcardClientPidMap result;
            if (!TryParse(value, format, out result))
                throw new FormatException("The value is not a valid CLIENTPIDMAP value.");

            return result;
        }

        /// <summary>
        /// Formats the value into a span.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            VcardValueFormatting.CheckStructuredFormat(format, "A CLIENTPIDMAP value");

            var writer = new VcardSpanWriter(destination);
            if (_uri != null)
            {
                writer.Append(_sourceId.ToString(CultureInfo.InvariantCulture).AsSpan());
                writer.Append(';');
                writer.Append(VcardValue.EscapeUri(_uri.OriginalString).AsSpan());
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the value in <see cref="VcardValueFormat.Text"/> form; or gives the type name if the instance is not initialised.
        /// </summary>
        public override string ToString()
        {
            return _uri is null ? typeof(VcardClientPidMap).FullName ?? nameof(VcardClientPidMap) : _sourceId.ToString(CultureInfo.InvariantCulture) + ";" + VcardValue.EscapeUri(_uri.OriginalString);
        }

        /// <summary>
        /// Indicates whether this value equals another: the same source identifier and the same text of the URI.
        /// </summary>
        public bool Equals(VcardClientPidMap other)
        {
            return _sourceId == other._sourceId && string.Equals(_uri?.OriginalString, other._uri?.OriginalString, StringComparison.Ordinal);
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardClientPidMap other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _uri is null ? 0 : _sourceId.GetHashCode() ^ StringComparer.Ordinal.GetHashCode(_uri.OriginalString);
        }

        /// <summary>
        /// Indicates whether two values are equal.
        /// </summary>
        public static bool operator ==(VcardClientPidMap left, VcardClientPidMap right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two values differ.
        /// </summary>
        public static bool operator !=(VcardClientPidMap left, VcardClientPidMap right)
        {
            return !left.Equals(right);
        }

        private void CheckInitialized()
        {
            if (_uri is null)
                throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
