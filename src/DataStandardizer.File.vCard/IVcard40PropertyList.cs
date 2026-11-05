using System.Collections.Generic;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The instances of one property of a vCard 4.0 card.  It is a live view of the card: adding or removing an instance changes the card.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public interface IVcard40PropertyList<TValue> : IReadOnlyList<IVcard40Property<TValue>>
    {
        /// <summary>
        /// Adds an instance after the last property of the card.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The new instance.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="value"/> can't be written, or is of a type the property doesn't allow.</exception>
        IVcard40Property<TValue> Add(TValue value);

        /// <summary>
        /// Adds an instance after the last property of the card.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <returns>The new instance.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="value"/> or <paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="value"/> can't be written, or is of a type the property doesn't allow.</exception>
        IVcard40Property<TValue> Add(TValue value, VcardPropertyParameters parameters);

        /// <summary>
        /// Removes every instance.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the instances grouped by <c>ALTID</c>, in the order each group first appears (RFC 6350 §5.4).  An instance without it is a group of its own.
        /// </summary>
        IReadOnlyList<IReadOnlyList<IVcard40Property<TValue>>> GetAlternatives();

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the first of the instances with the lowest <c>PREF</c> (RFC 6350 §5.3); or <c>null</c> if there are none.  An instance without it is least preferred.
        /// </summary>
        IVcard40Property<TValue>? GetPreferred();
#else
        /// <summary>
        /// Gets the first of the instances with the lowest <c>PREF</c> (RFC 6350 §5.3); or <c>null</c> if there are none.  An instance without it is least preferred.
        /// </summary>
        IVcard40Property<TValue> GetPreferred();
#endif

        /// <summary>
        /// Removes an instance.
        /// </summary>
        /// <param name="property">The instance, as this list gave it.</param>
        /// <returns><c>true</c> if it was in the card; otherwise <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="property"/> is <c>null</c>.</exception>
        bool Remove(IVcard40Property<TValue> property);
    }
}
