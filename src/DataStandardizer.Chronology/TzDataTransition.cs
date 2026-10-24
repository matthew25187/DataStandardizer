using System;

namespace DataStandardizer.Chronology
{
    /// <summary>
    /// A change in the offset from universal time in effect in a TZ Database zone.
    /// </summary>
    public readonly struct TzDataTransition : IEquatable<TzDataTransition>
    {
        internal TzDataTransition(DateTime instantUtc, TzDataOffsetInfo before, TzDataOffsetInfo after)
        {
            if (instantUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("The instant of the transition must be a universal time.", nameof(instantUtc));

            InstantUtc = instantUtc;
            Before = before;
            After = after;
        }

        /// <summary>
        /// Gets the universal time at which the transition occurs.
        /// </summary>
        /// <value>
        /// The time, of kind <see cref="DateTimeKind.Utc"/>.
        /// </value>
        public DateTime InstantUtc { get; }

        /// <summary>
        /// Gets the offset in effect immediately before the transition.
        /// </summary>
        public TzDataOffsetInfo Before { get; }

        /// <summary>
        /// Gets the offset in effect from the transition onwards.
        /// </summary>
        public TzDataOffsetInfo After { get; }

        /// <summary>
        /// Determines whether two values are equal.
        /// </summary>
        public static bool operator ==(TzDataTransition left, TzDataTransition right) => left.Equals(right);

        /// <summary>
        /// Determines whether two values are not equal.
        /// </summary>
        public static bool operator !=(TzDataTransition left, TzDataTransition right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(TzDataTransition other)
        {
            return InstantUtc == other.InstantUtc &&
                   Before.Equals(other.Before) &&
                   After.Equals(other.After);
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is TzDataTransition other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = InstantUtc.GetHashCode();
                hashCode = (hashCode * 397) ^ Before.GetHashCode();
                hashCode = (hashCode * 397) ^ After.GetHashCode();
                return hashCode;
            }
        }
    }
}
