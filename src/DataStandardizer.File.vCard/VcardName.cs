using System;
using System.Collections.Generic;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The structured value of the N property (RFC 6350 §6.2.2, RFC 9554 §2.2): the components of a name, each a list of text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// vCard 4.0 writes five components (RFC 6350 §6.2.2) or seven, with the secondary surname and generation of RFC 9554 §2.2.  No other number is valid.  This type writes five
    /// unless one of those two has a value.  vCard 3.0 allows one to five components (RFC 2426 §4) and vCard 2.1 too (vCard 2.1 §2.9), and a missing one is empty.
    /// </para>
    /// <para>
    /// A comma separates the items of a component in vCard 4.0 and vCard 3.0.  In vCard 2.1 a comma is ordinary text, so a component has one item.
    /// </para>
    /// <para>
    /// This type holds copies of its text.  To read a value without allocating, use <see cref="TryParseView(ReadOnlySpan{char}, VcardValueFormat, out VcardStructuredValueView)"/>.
    /// </para>
    /// <para>
    /// A default instance is not initialised: its component properties and <see cref="IsDefinedIn"/> throw <see cref="InvalidOperationException"/>,
    /// <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// </remarks>
    public readonly struct VcardName : IEquatable<VcardName>
    {
        /// <summary>
        /// The position of the family names in the value.
        /// </summary>
        public const int FamilyNamesComponent = 0;

        /// <summary>
        /// The position of the given names in the value.
        /// </summary>
        public const int GivenNamesComponent = 1;

        /// <summary>
        /// The position of the additional names in the value.
        /// </summary>
        public const int AdditionalNamesComponent = 2;

        /// <summary>
        /// The position of the honorific prefixes in the value.
        /// </summary>
        public const int HonorificPrefixesComponent = 3;

        /// <summary>
        /// The position of the honorific suffixes in the value.
        /// </summary>
        public const int HonorificSuffixesComponent = 4;

        /// <summary>
        /// The position of the secondary surnames in the value (RFC 9554 §2.2).
        /// </summary>
        public const int SecondarySurnamesComponent = 5;

        /// <summary>
        /// The position of the generation in the value (RFC 9554 §2.2).
        /// </summary>
        public const int GenerationComponent = 6;

        private const string UninitializedMessage = "The VcardName instance is not initialised.";

        // ref. RFC 6350 §6.2.2: N-value = list-component 4(";" list-component)
        private const int Rfc6350ComponentCount = 5;

        // ref. RFC 9554 §2.2: N-value = list-component 6(";" list-component)
        private const int Rfc9554ComponentCount = 7;

        private readonly VcardComponentList _components;

        /// <summary>
        /// Creates a name with the components of RFC 6350.
        /// </summary>
        /// <param name="familyNames">The family names.</param>
        /// <param name="givenNames">The given names.</param>
        /// <param name="additionalNames">The additional names.</param>
        /// <param name="honorificPrefixes">The honorific prefixes.</param>
        /// <param name="honorificSuffixes">The honorific suffixes.</param>
        /// <exception cref="ArgumentNullException">A component or an item is <c>null</c>.  Give an empty list for a component with no items.</exception>
        /// <exception cref="ArgumentException">An item holds a control character other than a tab or a line break, or a lone surrogate.</exception>
        public VcardName(IEnumerable<string> familyNames, IEnumerable<string> givenNames, IEnumerable<string> additionalNames, IEnumerable<string> honorificPrefixes,
            IEnumerable<string> honorificSuffixes)
            : this(familyNames, givenNames, additionalNames, honorificPrefixes, honorificSuffixes, new string[0], new string[0])
        {
        }

        /// <summary>
        /// Creates a name with the components of RFC 6350 and RFC 9554.
        /// </summary>
        /// <param name="familyNames">The family names.</param>
        /// <param name="givenNames">The given names.</param>
        /// <param name="additionalNames">The additional names.</param>
        /// <param name="honorificPrefixes">The honorific prefixes.</param>
        /// <param name="honorificSuffixes">The honorific suffixes.</param>
        /// <param name="secondarySurnames">The secondary surnames.</param>
        /// <param name="generations">The generation, such as <c>Jr.</c>.</param>
        /// <exception cref="ArgumentNullException">A component or an item is <c>null</c>.  Give an empty list for a component with no items.</exception>
        /// <exception cref="ArgumentException">An item holds a control character other than a tab or a line break, or a lone surrogate.</exception>
        public VcardName(IEnumerable<string> familyNames, IEnumerable<string> givenNames, IEnumerable<string> additionalNames, IEnumerable<string> honorificPrefixes,
            IEnumerable<string> honorificSuffixes, IEnumerable<string> secondarySurnames, IEnumerable<string> generations)
        {
            _components = VcardComponentList.Create(familyNames, givenNames, additionalNames, honorificPrefixes, honorificSuffixes, secondarySurnames, generations);
        }

        private VcardName(VcardComponentList components)
        {
            _components = components;
        }

        /// <summary>
        /// Gets the family names.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> FamilyNames => GetComponents()[FamilyNamesComponent];

        /// <summary>
        /// Gets the given names.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> GivenNames => GetComponents()[GivenNamesComponent];

        /// <summary>
        /// Gets the additional names.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> AdditionalNames => GetComponents()[AdditionalNamesComponent];

        /// <summary>
        /// Gets the honorific prefixes.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> HonorificPrefixes => GetComponents()[HonorificPrefixesComponent];

        /// <summary>
        /// Gets the honorific suffixes.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> HonorificSuffixes => GetComponents()[HonorificSuffixesComponent];

        /// <summary>
        /// Gets the secondary surnames (RFC 9554 §2.2).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> SecondarySurnames => GetComponents()[SecondarySurnamesComponent];

        /// <summary>
        /// Gets the generation (RFC 9554 §2.2).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Generations => GetComponents()[GenerationComponent];

        /// <summary>
        /// Gets the family names without any that are also a secondary surname.  ref. RFC 9554 §2.2: a reader "SHOULD ignore any value in the backwards-compatible component if an
        /// equal value is set in the new component".
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> FamilyNamesExcludingSecondarySurnames => GetComponents().GetItemsNotIn(FamilyNamesComponent, SecondarySurnamesComponent);

        /// <summary>
        /// Gets the honorific suffixes without any that are also a generation.  ref. RFC 9554 §2.2: 'a "Jr." that occurs in both honorific suffixes and generation should only be handled
        /// as a generational marker.'
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> HonorificSuffixesExcludingGenerations => GetComponents().GetItemsNotIn(HonorificSuffixesComponent, GenerationComponent);

        /// <summary>
        /// Gets the components, for use within the package; or <c>null</c> if the instance is not initialised.
        /// </summary>
        internal VcardComponentList Components => _components;

        /// <summary>
        /// Gets this name with its secondary surnames also in the family names, and its generations also in the honorific suffixes, each after what is there unless it is there already.
        /// A consumer that doesn't know RFC 9554 can then read them.  ref. RFC 9554 §2.2: "implementations SHOULD add such values to both the newly defined components and their
        /// backwards-compatible counterpart".
        /// </summary>
        /// <remarks>
        /// <see cref="VcardCard.SetName(VcardName)"/> applies this.  To write a name without it, add a <see cref="VcardContentLine"/> to <see cref="VcardCard.Properties"/>.
        /// </remarks>
        /// <returns>The name.</returns>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public VcardName WithBackwardCompatibleComponents()
        {
            return new VcardName(GetComponents().WithItemsOf(FamilyNamesComponent, SecondarySurnamesComponent).WithItemsOf(HonorificSuffixesComponent, GenerationComponent));
        }

        /// <summary>
        /// Gets this name with nothing in the components of RFC 9554, for a version that has none of them.
        /// </summary>
        internal VcardName WithoutRfc9554Components()
        {
            return new VcardName(GetComponents().WithoutItemsFrom(Rfc6350ComponentCount));
        }

        /// <summary>
        /// Gets a flag indicating if a version can write this name.  vCard 3.0 and vCard 2.1 have no RFC 9554 components.  vCard 2.1 has one item in a component, and can't end one
        /// with a backslash.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        public bool IsDefinedIn(VcardVersion version)
        {
            var components = GetComponents();
            VcardValueFormatting.CheckVersion(version);

            switch (version)
            {
                case VcardVersion.V3_0:
                    return components.AreEmptyFrom(Rfc6350ComponentCount);
                case VcardVersion.V2_1:
                    return components.AreEmptyFrom(Rfc6350ComponentCount) && components.HasSingleItems && components.CanWriteInVersion21;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Parses a vCard 4.0 name, such as <c>Public;John;Quinlan;Mr.;Esq.</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The name, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardName result)
        {
            return TryParse(value, format, VcardVersion.V4_0, out result);
        }

        /// <summary>
        /// Parses a name in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="result">The name, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardName result)
        {
            result = default(VcardName);
            VcardStructuredValueView view;
            if (!TryParseView(value, format, version, out view)) return false;

            result = new VcardName(VcardComponentList.Parse(value, version, version != VcardVersion.V2_1, Rfc9554ComponentCount));
            return true;
        }

        /// <summary>
        /// Parses a vCard 4.0 name without allocating, into a view of its components.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The view, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParseView(ReadOnlySpan<char> value, VcardValueFormat format, out VcardStructuredValueView result)
        {
            return TryParseView(value, format, VcardVersion.V4_0, out result);
        }

        /// <summary>
        /// Parses a name in the form a version uses without allocating, into a view of its components.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="result">The view, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParseView(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardStructuredValueView result)
        {
            VcardValueFormatting.CheckFormat(format);
            VcardValueFormatting.CheckVersion(version);

            // ref. RFC 2426 §4: n-value = 0*4(text-value *("," text-value) ";") text-value *("," text-value)
            // ref. vCard 2.1 §2.9: nameparts = 0*4(strnosemi ";") strnosemi
            result = default(VcardStructuredValueView);
            int componentCount;
            if (!VcardText.TryCountComponents(value, version, true, out componentCount)) return false;

            var isValidCount = version == VcardVersion.V4_0
                ? componentCount == Rfc6350ComponentCount || componentCount == Rfc9554ComponentCount
                : componentCount <= Rfc6350ComponentCount;
            if (!isValidCount) return false;

            result = new VcardStructuredValueView(value, componentCount, version, version != VcardVersion.V2_1);
            return true;
        }

        /// <summary>
        /// Parses a vCard 4.0 name.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The name.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardName Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            return Parse(value, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Parses a name in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The name.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardName Parse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version)
        {
            VcardName result;
            if (!TryParse(value, format, version, out result))
                throw new FormatException("The value is not a valid N value.");

            return result;
        }

        /// <summary>
        /// Formats the name into a span in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the name was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            return TryFormat(destination, out charsWritten, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the name into a span in the form a version uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns><c>true</c> if the name was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this name; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format, VcardVersion version)
        {
            VcardValueFormatting.CheckFormat(format);
            VcardValueFormatting.CheckVersion(version);

            if (_components is null)
            {
                charsWritten = 0;
                return true;
            }

            if (!IsDefinedIn(version))
                throw VcardValueFormatting.BuildNotDefinedException("N", version);

            var writer = new VcardSpanWriter(destination);
            _components.Append(ref writer, version, _components.AreEmptyFrom(Rfc6350ComponentCount) ? Rfc6350ComponentCount : Rfc9554ComponentCount);
            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the name as a string in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public string ToString(VcardValueFormat format)
        {
            return ToString(format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the name as a string in the form a version uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this name; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format, VcardVersion version)
        {
            var buffer = new char[_components is null ? 0 : _components.MaxLength];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format, version), charsWritten);
        }

        /// <summary>
        /// Formats the name in <see cref="VcardValueFormat.Text"/> form, as vCard 4.0 writes it; or gives the type name if the instance is not initialised.
        /// </summary>
        public override string ToString()
        {
            return _components is null ? typeof(VcardName).FullName ?? nameof(VcardName) : ToString(VcardValueFormat.Text);
        }

        /// <summary>
        /// Indicates whether this name equals another: the same items in the same components, compared ordinally.
        /// </summary>
        public bool Equals(VcardName other)
        {
            if (_components is null || other._components is null) return _components is null && other._components is null;

            return _components.EqualsList(other._components);
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardName other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _components is null ? 0 : _components.GetListHashCode();
        }

        /// <summary>
        /// Indicates whether two names are equal.
        /// </summary>
        public static bool operator ==(VcardName left, VcardName right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two names differ.
        /// </summary>
        public static bool operator !=(VcardName left, VcardName right)
        {
            return !left.Equals(right);
        }

        private VcardComponentList GetComponents()
        {
            return _components ?? throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
