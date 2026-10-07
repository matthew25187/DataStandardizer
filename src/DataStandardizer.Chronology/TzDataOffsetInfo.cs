using System;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Chronology
{
    /// <summary>
    /// The offset from universal time in effect in a TZ Database zone over a period, together with the abbreviation by which it is known.
    /// </summary>
    public readonly struct TzDataOffsetInfo : IEquatable<TzDataOffsetInfo>
    {
#if NETCOREAPP3_0_OR_GREATER
        private readonly string? _abbreviation;
#else
        [CanBeNull]
        private readonly string _abbreviation;
#endif

        internal TzDataOffsetInfo(
            TimeSpan standardOffset,
            TimeSpan daylightSavings,
            bool isDaylightSavingTime,
#if NETSTANDARD
            [NotNull]
#endif
            string abbreviation,
            DateTime? validFromUtc,
            DateTime? validUntilUtc)
        {
            if (abbreviation is null)
                throw new ArgumentNullException(nameof(abbreviation));

            if (validFromUtc.HasValue && validFromUtc.Value.Kind != DateTimeKind.Utc)
                throw new ArgumentException("The start of the period must be a universal time.", nameof(validFromUtc));

            if (validUntilUtc.HasValue && validUntilUtc.Value.Kind != DateTimeKind.Utc)
                throw new ArgumentException("The end of the period must be a universal time.", nameof(validUntilUtc));

            if (validFromUtc.HasValue && validUntilUtc.HasValue && validUntilUtc.Value <= validFromUtc.Value)
                throw new ArgumentOutOfRangeException(nameof(validUntilUtc), validUntilUtc, "The end of the period must follow its start.");

            StandardOffset = standardOffset;
            DaylightSavings = daylightSavings;
            IsDaylightSavingTime = isDaylightSavingTime;
            _abbreviation = abbreviation;
            ValidFromUtc = validFromUtc;
            ValidUntilUtc = validUntilUtc;
        }

        /// <summary>
        /// Gets the total offset from universal time, being the sum of <see cref="StandardOffset"/> and <see cref="DaylightSavings"/>.
        /// </summary>
        public TimeSpan UtcOffset => StandardOffset + DaylightSavings;

        /// <summary>
        /// Gets the offset of standard time from universal time.
        /// </summary>
        public TimeSpan StandardOffset { get; }

        /// <summary>
        /// Gets the amount of time added to the standard offset.
        /// </summary>
        /// <remarks>
        /// The amount may be negative, as for Europe/Dublin in winter.
        /// </remarks>
        public TimeSpan DaylightSavings { get; }

        /// <summary>
        /// Gets a value indicating whether daylight saving time is in effect.
        /// </summary>
        public bool IsDaylightSavingTime { get; }

        /// <summary>
        /// Gets the abbreviation for the offset, for example <c>EST</c>, <c>BST</c> or <c>+0530</c>.
        /// </summary>
#if NETSTANDARD
        [NotNull]
#endif
        public string Abbreviation => _abbreviation ?? string.Empty;

        /// <summary>
        /// Gets the universal time from which the offset is in effect.
        /// </summary>
        /// <value>
        /// The time, of kind <see cref="DateTimeKind.Utc"/>, or <see langword="null"/> where the offset has been in effect since the beginning of time.
        /// </value>
        public DateTime? ValidFromUtc { get; }

        /// <summary>
        /// Gets the universal time at which the offset ceases to be in effect.
        /// </summary>
        /// <value>
        /// The time, of kind <see cref="DateTimeKind.Utc"/>, or <see langword="null"/> where the offset remains in effect indefinitely.
        /// </value>
        public DateTime? ValidUntilUtc { get; }

        /// <summary>
        /// Determines whether two values are equal.
        /// </summary>
        public static bool operator ==(TzDataOffsetInfo left, TzDataOffsetInfo right) => left.Equals(right);

        /// <summary>
        /// Determines whether two values are not equal.
        /// </summary>
        public static bool operator !=(TzDataOffsetInfo left, TzDataOffsetInfo right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(TzDataOffsetInfo other)
        {
            return StandardOffset == other.StandardOffset &&
                   DaylightSavings == other.DaylightSavings &&
                   IsDaylightSavingTime == other.IsDaylightSavingTime &&
                   string.Equals(Abbreviation, other.Abbreviation, StringComparison.Ordinal) &&
                   ValidFromUtc == other.ValidFromUtc &&
                   ValidUntilUtc == other.ValidUntilUtc;
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is TzDataOffsetInfo other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StandardOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ DaylightSavings.GetHashCode();
                hashCode = (hashCode * 397) ^ IsDaylightSavingTime.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Abbreviation);
                hashCode = (hashCode * 397) ^ ValidFromUtc.GetHashCode();
                hashCode = (hashCode * 397) ^ ValidUntilUtc.GetHashCode();
                return hashCode;
            }
        }
    }
}
