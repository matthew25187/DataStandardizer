using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A zero-copy view of a structured value such as N, ADR or ORG (RFC 6350 §3.4), split into its components.  Get one from <see cref="VcardName.TryParseView(ReadOnlySpan{char}, VcardValueFormat, out VcardStructuredValueView)"/>,
    /// <see cref="VcardAddress.TryParseView(ReadOnlySpan{char}, VcardValueFormat, out VcardStructuredValueView)"/> or
    /// <see cref="VcardOrganization.TryParseView(ReadOnlySpan{char}, VcardValueFormat, out VcardStructuredValueView)"/>.
    /// </summary>
    /// <remarks>
    /// A type that keeps a value can't hold a span, so the types that own a structured value, such as <see cref="VcardName"/>, copy its text.  This view is how to read one without
    /// allocating: getting one checks the whole value and copies nothing.  A default instance has no components.
    /// </remarks>
    public readonly ref struct VcardStructuredValueView
    {
        private readonly ReadOnlySpan<char> _value;
        private readonly VcardVersion _version;
        private readonly bool _hasLists;

        internal VcardStructuredValueView(ReadOnlySpan<char> value, int componentCount, VcardVersion version, bool hasLists)
        {
            _value = value;
            ComponentCount = componentCount;
            _version = version;
            _hasLists = hasLists;
        }

        /// <summary>
        /// Gets the number of components the value was written with.
        /// </summary>
        public int ComponentCount { get; }

        /// <summary>
        /// Gets a component.
        /// </summary>
        /// <param name="index">The 0-based position of the component, such as <see cref="VcardName.GivenNamesComponent"/>.</param>
        /// <returns>The component.  It is empty if the value was written with fewer components, as a value with no RFC 9554 components is.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
        public VcardComponentView GetComponent(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, "The index must not be negative.");

            if (index >= ComponentCount) return new VcardComponentView(default(ReadOnlySpan<char>), _hasLists);

            var rest = _value;
            for (var skipped = 0; skipped < index; skipped++)
            {
                rest = rest.Slice(VcardText.IndexOfDelimiter(rest, ';', _version) + 1);
            }

            return new VcardComponentView(rest.Slice(0, VcardText.IndexOfDelimiter(rest, ';', _version)), _hasLists);
        }

        /// <summary>
        /// Removes the escaping from an item of a component.
        /// </summary>
        /// <param name="item">The item, as the component gave it.</param>
        /// <param name="destination">Receives the text.  It must be at least as long as <paramref name="item"/>, which is always enough.</param>
        /// <returns>The number of characters written.</returns>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is shorter than <paramref name="item"/>.</exception>
        public int UnescapeItem(ReadOnlySpan<char> item, Span<char> destination)
        {
            if (destination.Length < item.Length)
                throw new ArgumentException("The destination must be at least as long as the item.", nameof(destination));

            return VcardText.Unescape(item, destination, _version);
        }
    }

    /// <summary>
    /// A zero-copy view of one component of a structured value.  Enumerate it for its items.
    /// </summary>
    public readonly ref struct VcardComponentView
    {
        private readonly bool _hasLists;

        internal VcardComponentView(ReadOnlySpan<char> rawValue, bool hasLists)
        {
            RawValue = rawValue;
            _hasLists = hasLists;
        }

        /// <summary>
        /// Gets the text of the component, with its escaping in place.
        /// </summary>
        public ReadOnlySpan<char> RawValue { get; }

        /// <summary>
        /// Gets a flag indicating if the component has no text, and so no items.
        /// </summary>
        public bool IsEmpty => RawValue.Length == 0;

        /// <summary>
        /// Gets an enumerator of the items of the component, with their escaping in place.
        /// </summary>
        public VcardListEnumerator GetEnumerator()
        {
            return new VcardListEnumerator(RawValue, !_hasLists, RawValue.Length == 0);
        }
    }
}
