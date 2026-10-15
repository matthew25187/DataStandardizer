using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A change in the UTC offset of a time zone: the onset of an observance (RFC 5545 §3.6.5).
    /// </summary>
    public readonly struct IcalTimeZoneTransition : IEquatable<IcalTimeZoneTransition>
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly string? _name;
#else
        private readonly string _name;
#endif

        /// <summary>
        /// Creates a transition.
        /// </summary>
        /// <param name="instantUtc">The instant of the transition, in UTC.  A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as
        /// UTC.</param>
        /// <param name="offsetFrom">The UTC offset in use before the transition.</param>
        /// <param name="offsetTo">The UTC offset in use from the transition onwards.</param>
        /// <param name="kind">Whether standard time or daylight saving time is in use from the transition onwards.</param>
        /// <param name="name">The customary name of the time zone from the transition onwards, such as <c>EST</c>; or <c>null</c> if there is
        /// none.</param>
        /// <exception cref="ArgumentException"><paramref name="instantUtc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not an <see cref="IcalTimeZoneObservanceKind"/>
        /// member.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public IcalTimeZoneTransition(DateTime instantUtc, IcalUtcOffset offsetFrom, IcalUtcOffset offsetTo, IcalTimeZoneObservanceKind kind,
            string? name)
#else
        public IcalTimeZoneTransition(DateTime instantUtc, IcalUtcOffset offsetFrom, IcalUtcOffset offsetTo, IcalTimeZoneObservanceKind kind,
            string name)
#endif
        {
            if (kind != IcalTimeZoneObservanceKind.Standard && kind != IcalTimeZoneObservanceKind.Daylight)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "The observance kind is not valid.");

            InstantUtc = IcalTimeZoneRules.ToUtcKind(instantUtc, nameof(instantUtc));
            OffsetFrom = offsetFrom;
            OffsetTo = offsetTo;
            Kind = kind;
            _name = name;
        }

        /// <summary>
        /// Gets the instant of the transition, of kind <see cref="DateTimeKind.Utc"/>.
        /// </summary>
        public DateTime InstantUtc { get; }

        /// <summary>
        /// Gets the UTC offset in use before the transition.
        /// </summary>
        public IcalUtcOffset OffsetFrom { get; }

        /// <summary>
        /// Gets the UTC offset in use from the transition onwards.
        /// </summary>
        public IcalUtcOffset OffsetTo { get; }

        /// <summary>
        /// Gets whether standard time or daylight saving time is in use from the transition onwards.
        /// </summary>
        public IcalTimeZoneObservanceKind Kind { get; }

        /// <summary>
        /// Gets the customary name of the time zone from the transition onwards, such as <c>EST</c>; or <c>null</c> if there is none.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Name => _name;
#else
        public string Name => _name;
#endif

        /// <summary>
        /// Indicates whether this transition equals another: the same instant, offsets, kind and name, compared ordinally.
        /// </summary>
        public bool Equals(IcalTimeZoneTransition other)
        {
            return InstantUtc == other.InstantUtc && OffsetFrom == other.OffsetFrom && OffsetTo == other.OffsetTo && Kind == other.Kind &&
                   string.Equals(_name, other._name, StringComparison.Ordinal);
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalTimeZoneTransition other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = InstantUtc.GetHashCode();
            hashCode = hashCode * 31 + OffsetFrom.GetHashCode();
            hashCode = hashCode * 31 + OffsetTo.GetHashCode();
            hashCode = hashCode * 31 + (int)Kind;
            return _name is null ? hashCode : hashCode * 31 + StringComparer.Ordinal.GetHashCode(_name);
        }

        /// <summary>
        /// Indicates whether two transitions are equal.
        /// </summary>
        public static bool operator ==(IcalTimeZoneTransition left, IcalTimeZoneTransition right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two transitions differ.
        /// </summary>
        public static bool operator !=(IcalTimeZoneTransition left, IcalTimeZoneTransition right)
        {
            return !left.Equals(right);
        }
    }
}
