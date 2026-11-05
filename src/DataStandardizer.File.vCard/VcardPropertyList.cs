using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The instances of one property of a card whose values are valid, in the order they appear.  It is a live view of <see cref="VcardCard.Properties"/>: adding or removing an
    /// instance changes the card, and changing the card changes what this gives.
    /// </summary>
    /// <remarks>
    /// An instance whose value isn't valid, which <see cref="VcardOptions.AllowInvalidValues"/> lets a reader accept, is not here, but it is still in
    /// <see cref="VcardCard.Properties"/>.  Each member reads the properties of the card, so it takes time in proportion to their number.
    /// </remarks>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public sealed class VcardPropertyList<TValue> : IReadOnlyList<VcardProperty<TValue>>
    {
        private readonly VcardCard _card;
        private readonly VcardPropertyCodec<TValue> _codec;

        internal VcardPropertyList(VcardCard card, VcardPropertyCodec<TValue> codec)
        {
            _card = card;
            _codec = codec;
            Version40 = new Version40View(this);
        }

        /// <summary>
        /// Gets the number of instances.
        /// </summary>
        public int Count => GetInstances().Count;

        /// <summary>
        /// Gets the view of the list in the shape of vCard 4.0.
        /// </summary>
        internal IVcard40PropertyList<TValue> Version40 { get; }

        /// <summary>
        /// Gets an instance.
        /// </summary>
        /// <param name="index">The 0-based index.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
        public VcardProperty<TValue> this[int index] => GetInstances()[index];

        /// <summary>
        /// Adds an instance after the last property of the card.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The new instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> can't be written, or is of a type the property doesn't allow.</exception>
        public VcardProperty<TValue> Add(TValue value)
        {
            return AddLine(value, _codec.CreateLine(value, null));
        }

        /// <summary>
        /// Adds an instance after the last property of the card.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <returns>The new instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> can't be written, or is of a type the property doesn't allow.</exception>
        public VcardProperty<TValue> Add(TValue value, VcardPropertyParameters parameters)
        {
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));

            return AddLine(value, _codec.CreateLine(value, parameters));
        }

        /// <summary>
        /// Removes every instance, including any whose value isn't valid.
        /// </summary>
        public void Clear()
        {
            _card.RemoveProperties(_codec.Name);
        }

        /// <summary>
        /// Gets the instances grouped by <c>ALTID</c>, in the order each group first appears (RFC 6350 §5.4).  An instance without it is a group of its own.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<VcardProperty<TValue>>> GetAlternatives()
        {
            var groups = VcardPropertyParameterReader.GroupByAlternatives(GetInstances(), instance => instance.Line);
            var result = new List<IReadOnlyList<VcardProperty<TValue>>>(groups.Count);
            foreach (var group in groups)
            {
                result.Add(new ReadOnlyCollection<VcardProperty<TValue>>(group));
            }

            return new ReadOnlyCollection<IReadOnlyList<VcardProperty<TValue>>>(result);
        }

        /// <summary>
        /// Gets an enumerator of the instances there are when it is called.
        /// </summary>
        public IEnumerator<VcardProperty<TValue>> GetEnumerator()
        {
            return GetInstances().GetEnumerator();
        }

        /// <summary>
        /// Gets the first of the instances with the lowest <c>PREF</c> (RFC 6350 §5.3); or <c>null</c> if there are none.  An instance without it is least preferred.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public VcardProperty<TValue>? GetPreferred()
#else
        public VcardProperty<TValue> GetPreferred()
#endif
        {
            var instances = GetInstances();
            var index = VcardPropertyParameterReader.IndexOfPreferred(instances, instance => instance.Line);
            return index < 0 ? null : instances[index];
        }

        /// <summary>
        /// Removes an instance.
        /// </summary>
        /// <param name="property">The instance.</param>
        /// <returns><c>true</c> if it was in the card; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="property"/> is <c>null</c>.</exception>
        public bool Remove(VcardProperty<TValue> property)
        {
            if (property is null)
                throw new ArgumentNullException(nameof(property));

            return _card.Properties.Remove(property.Line);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private VcardProperty<TValue> AddLine(TValue value, VcardContentLine line)
        {
            _card.Properties.Add(line);
            return new VcardProperty<TValue>(line, value);
        }

        private List<VcardProperty<TValue>> GetInstances()
        {
            var result = new List<VcardProperty<TValue>>();
            foreach (var line in _card.Properties)
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                VcardProperty<TValue>? property;
#else
                VcardProperty<TValue> property;
#endif
                if (string.Equals(line.Name, _codec.Name, StringComparison.OrdinalIgnoreCase) && _codec.TryCreateProperty(line, out property))
                {
                    result.Add(property);
                }
            }

            return result;
        }

        /// <summary>
        /// The list, in the shape of vCard 4.0.  It is a separate object, so that a caller of the list doesn't have two kinds of element to choose from when it enumerates.
        /// </summary>
        private sealed class Version40View : IVcard40PropertyList<TValue>
        {
            private readonly VcardPropertyList<TValue> _list;

            internal Version40View(VcardPropertyList<TValue> list)
            {
                _list = list;
            }

            public int Count => _list.Count;

            public IVcard40Property<TValue> this[int index] => _list[index];

            public IVcard40Property<TValue> Add(TValue value)
            {
                return _list.Add(value);
            }

            public IVcard40Property<TValue> Add(TValue value, VcardPropertyParameters parameters)
            {
                return _list.Add(value, parameters);
            }

            public void Clear()
            {
                _list.Clear();
            }

            public IReadOnlyList<IReadOnlyList<IVcard40Property<TValue>>> GetAlternatives()
            {
                // A read-only list of a class is covariant, so the lists don't have to be copied.
                return _list.GetAlternatives();
            }

            public IEnumerator<IVcard40Property<TValue>> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            public IVcard40Property<TValue>? GetPreferred()
#else
            public IVcard40Property<TValue> GetPreferred()
#endif
            {
                return _list.GetPreferred();
            }

            public bool Remove(IVcard40Property<TValue> property)
            {
                if (property is null)
                    throw new ArgumentNullException(nameof(property));

                var instance = property as VcardProperty<TValue>;
                return instance != null && _list.Remove(instance);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
