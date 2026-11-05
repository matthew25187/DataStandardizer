using System;
using System.Globalization;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A value of the PID parameter (RFC 6350 §5.5): a positive integer, or two separated by a dot such as <c>1.2</c>.  Only vCard 4.0 has this parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The second integer is the source identifier, which a CLIENTPIDMAP property gives a meaning to.  RFC 6350 §6.7.7 says it must be strictly positive, and §5.5 says the first
    /// is positive, so neither can be zero.  An integer too large for a <see cref="long"/> is rejected.  Leading zeros are accepted, and not written.
    /// </para>
    /// <para>
    /// A default instance is not initialised: <see cref="LocalId"/> and <see cref="SourceId"/> throw <see cref="InvalidOperationException"/>,
    /// <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// </remarks>
    public readonly struct VcardPid : IEquatable<VcardPid>
    {
        private const string UninitializedMessage = "The VcardPid instance is not initialised.";

        private readonly long _localId;
        private readonly long _sourceId;

        /// <summary>
        /// Creates a PID value with no source.
        /// </summary>
        /// <param name="localId">The local identifier, which must be positive.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="localId"/> is not positive.</exception>
        public VcardPid(long localId)
        {
            if (localId <= 0)
                throw new ArgumentOutOfRangeException(nameof(localId), localId, "The local identifier must be positive.");

            _localId = localId;
            _sourceId = 0;
        }

        /// <summary>
        /// Creates a PID value.
        /// </summary>
        /// <param name="localId">The local identifier, which must be positive.</param>
        /// <param name="sourceId">The source identifier, which must be positive.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="localId"/> or <paramref name="sourceId"/> is not positive.</exception>
        public VcardPid(long localId, long sourceId)
            : this(localId)
        {
            if (sourceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceId), sourceId, "The source identifier must be positive.");

            _sourceId = sourceId;
        }

        /// <summary>
        /// Gets the local identifier.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public long LocalId
        {
            get
            {
                CheckInitialized();
                return _localId;
            }
        }

        /// <summary>
        /// Gets the source identifier; or <c>null</c> if the value has none.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public long? SourceId
        {
            get
            {
                CheckInitialized();
                return _sourceId == 0 ? (long?)null : _sourceId;
            }
        }

        /// <summary>
        /// Parses a value, such as <c>1</c> or <c>1.2</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The PID value, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardPid result)
        {
            VcardValueFormatting.CheckFormat(format);

            // ref. RFC 6350 §5.5: pid-value = 1*DIGIT ["." 1*DIGIT]
            result = default(VcardPid);
            var dotIndex = value.IndexOf('.');
            long localId;
            if (!VcardValueFormatting.TryParseNumber(dotIndex < 0 ? value : value.Slice(0, dotIndex), out localId) || localId == 0) return false;

            if (dotIndex < 0)
            {
                result = new VcardPid(localId);
                return true;
            }

            long sourceId;
            if (!VcardValueFormatting.TryParseNumber(value.Slice(dotIndex + 1), out sourceId) || sourceId == 0) return false;

            result = new VcardPid(localId, sourceId);
            return true;
        }

        /// <summary>
        /// Parses a value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The PID value.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardPid Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            VcardPid result;
            if (!TryParse(value, format, out result))
                throw new FormatException("The value is not a valid PID value.");

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
            VcardValueFormatting.CheckFormat(format);

            var writer = new VcardSpanWriter(destination);
            if (_localId != 0)
            {
                writer.Append(_localId.ToString(CultureInfo.InvariantCulture).AsSpan());
                if (_sourceId != 0)
                {
                    writer.Append('.');
                    writer.Append(_sourceId.ToString(CultureInfo.InvariantCulture).AsSpan());
                }
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the value in <see cref="VcardValueFormat.Text"/> form; or gives the type name if the instance is not initialised.
        /// </summary>
        public override string ToString()
        {
            if (_localId == 0) return typeof(VcardPid).FullName ?? nameof(VcardPid);

            var localId = _localId.ToString(CultureInfo.InvariantCulture);
            return _sourceId == 0 ? localId : localId + "." + _sourceId.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Indicates whether this value equals another.
        /// </summary>
        public bool Equals(VcardPid other)
        {
            return _localId == other._localId && _sourceId == other._sourceId;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardPid other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _localId.GetHashCode() ^ (_sourceId.GetHashCode() * 397);
        }

        /// <summary>
        /// Indicates whether two values are equal.
        /// </summary>
        public static bool operator ==(VcardPid left, VcardPid right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two values differ.
        /// </summary>
        public static bool operator !=(VcardPid left, VcardPid right)
        {
            return !left.Equals(right);
        }

        private void CheckInitialized()
        {
            if (_localId == 0)
                throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
