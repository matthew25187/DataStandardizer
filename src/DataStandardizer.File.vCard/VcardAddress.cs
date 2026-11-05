using System;
using System.Collections.Generic;
using System.Text;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The structured value of the ADR property (RFC 6350 §6.3.1, RFC 9554 §2.1): the components of a delivery address, each a list of text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// vCard 4.0 writes seven components (RFC 6350 §6.3.1) or eighteen, with the eleven of RFC 9554 §2.1.  No other number is valid.  This type writes seven unless one of those
    /// eleven has a value.  vCard 3.0 allows one to seven components (RFC 2426 §4) and vCard 2.1 too (vCard 2.1 §2.9), and a missing one is empty.
    /// </para>
    /// <para>
    /// A comma separates the items of a component in vCard 4.0 alone.  In vCard 3.0 a component is one <c>text-value</c>, so a comma has to be escaped.  In vCard 2.1 a comma is
    /// ordinary text.
    /// </para>
    /// <para>
    /// This type holds copies of its text.  To read a value without allocating, use <see cref="TryParseView(ReadOnlySpan{char}, VcardValueFormat, out VcardStructuredValueView)"/>.
    /// </para>
    /// <para>
    /// A default instance is not initialised: its <see cref="GetComponent"/> method, its properties and <see cref="IsDefinedIn"/> throw <see cref="InvalidOperationException"/>,
    /// <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// </remarks>
    public readonly struct VcardAddress : IEquatable<VcardAddress>
    {
        /// <summary>The position of the post office box in the value.</summary>
        public const int PostOfficeBoxComponent = 0;

        /// <summary>The position of the extended address in the value.</summary>
        public const int ExtendedAddressComponent = 1;

        /// <summary>The position of the street address in the value.</summary>
        public const int StreetAddressComponent = 2;

        /// <summary>The position of the locality in the value.</summary>
        public const int LocalityComponent = 3;

        /// <summary>The position of the region in the value.</summary>
        public const int RegionComponent = 4;

        /// <summary>The position of the postal code in the value.</summary>
        public const int PostalCodeComponent = 5;

        /// <summary>The position of the country name in the value.</summary>
        public const int CountryNameComponent = 6;

        /// <summary>The position of the room in the value (RFC 9554 §2.1).</summary>
        public const int RoomComponent = 7;

        /// <summary>The position of the apartment in the value (RFC 9554 §2.1).</summary>
        public const int ApartmentComponent = 8;

        /// <summary>The position of the floor in the value (RFC 9554 §2.1).</summary>
        public const int FloorComponent = 9;

        /// <summary>The position of the street number in the value (RFC 9554 §2.1).</summary>
        public const int StreetNumberComponent = 10;

        /// <summary>The position of the street name in the value (RFC 9554 §2.1).</summary>
        public const int StreetNameComponent = 11;

        /// <summary>The position of the building in the value (RFC 9554 §2.1).</summary>
        public const int BuildingComponent = 12;

        /// <summary>The position of the block in the value (RFC 9554 §2.1).</summary>
        public const int BlockComponent = 13;

        /// <summary>The position of the subdistrict in the value (RFC 9554 §2.1).</summary>
        public const int SubdistrictComponent = 14;

        /// <summary>The position of the district in the value (RFC 9554 §2.1).</summary>
        public const int DistrictComponent = 15;

        /// <summary>The position of the landmark in the value (RFC 9554 §2.1).</summary>
        public const int LandmarkComponent = 16;

        /// <summary>The position of the direction in the value (RFC 9554 §2.1).</summary>
        public const int DirectionComponent = 17;

        private const string UninitializedMessage = "The VcardAddress instance is not initialised.";

        // ref. RFC 6350 §6.3.1: ADR-value has seven components.
        private const int Rfc6350ComponentCount = 7;

        // ref. RFC 9554 §2.1: ADR-value has eighteen components.
        private const int Rfc9554ComponentCount = 18;

        // The order of the street address made from the components of RFC 9554, which leaves it as "123 Main Street" in the example of RFC 9554 §2.1.
        private static readonly int[] StreetAddressParts =
        {
            StreetNumberComponent, StreetNameComponent, DirectionComponent, BuildingComponent, BlockComponent, FloorComponent, ApartmentComponent, RoomComponent, LandmarkComponent,
            SubdistrictComponent, DistrictComponent
        };

        private readonly VcardComponentList _components;

        /// <summary>
        /// Creates an address with the components of RFC 6350.
        /// </summary>
        /// <param name="postOfficeBox">The post office box.</param>
        /// <param name="extendedAddress">The extended address, such as an apartment or suite number.</param>
        /// <param name="streetAddress">The street address.</param>
        /// <param name="locality">The locality, such as a city.</param>
        /// <param name="region">The region, such as a state or province.</param>
        /// <param name="postalCode">The postal code.</param>
        /// <param name="countryName">The country name.</param>
        /// <exception cref="ArgumentNullException">A component or an item is <c>null</c>.  Give an empty list for a component with no items.</exception>
        /// <exception cref="ArgumentException">An item holds a control character other than a tab or a line break, or a lone surrogate.</exception>
        public VcardAddress(IEnumerable<string> postOfficeBox, IEnumerable<string> extendedAddress, IEnumerable<string> streetAddress, IEnumerable<string> locality,
            IEnumerable<string> region, IEnumerable<string> postalCode, IEnumerable<string> countryName)
        {
            var none = new string[0];
            _components = VcardComponentList.Create(postOfficeBox, extendedAddress, streetAddress, locality, region, postalCode, countryName,
                none, none, none, none, none, none, none, none, none, none, none);
        }

        /// <summary>
        /// Creates an address with the components of RFC 6350 and RFC 9554.
        /// </summary>
        /// <param name="components">The components, in the order of RFC 9554 §2.1: eighteen lists, whose positions are given by the constants of this type such as
        /// <see cref="RoomComponent"/>.  Give an empty list for a component with no items.</param>
        /// <exception cref="ArgumentNullException"><paramref name="components"/>, a component or an item is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">There are not eighteen components, or an item holds a control character other than a tab or a line break, or a lone surrogate.</exception>
        public VcardAddress(IReadOnlyList<IEnumerable<string>> components)
        {
            if (components is null)
                throw new ArgumentNullException(nameof(components));
            if (components.Count != Rfc9554ComponentCount)
                throw new ArgumentException("An address has eighteen components.", nameof(components));

            var copy = new IEnumerable<string>[Rfc9554ComponentCount];
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = components[index];
            }

            _components = VcardComponentList.Create(copy);
        }

        private VcardAddress(VcardComponentList components)
        {
            _components = components;
        }

        /// <summary>
        /// Gets the items of a component.
        /// </summary>
        /// <param name="index">The position of the component, given by the constants of this type such as <see cref="LocalityComponent"/>.</param>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not the position of a component.</exception>
        public IReadOnlyList<string> GetComponent(int index)
        {
            var components = GetComponents();
            if (index < 0 || index >= Rfc9554ComponentCount)
                throw new ArgumentOutOfRangeException(nameof(index), index, "An address has eighteen components.");

            return components[index];
        }

        /// <summary>
        /// Gets the post office box.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> PostOfficeBox => GetComponents()[PostOfficeBoxComponent];

        /// <summary>
        /// Gets the extended address.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> ExtendedAddress => GetComponents()[ExtendedAddressComponent];

        /// <summary>
        /// Gets the street address.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> StreetAddress => GetComponents()[StreetAddressComponent];

        /// <summary>
        /// Gets the locality.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Locality => GetComponents()[LocalityComponent];

        /// <summary>
        /// Gets the region.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Region => GetComponents()[RegionComponent];

        /// <summary>
        /// Gets the postal code.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> PostalCode => GetComponents()[PostalCodeComponent];

        /// <summary>
        /// Gets the country name.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> CountryName => GetComponents()[CountryNameComponent];

        /// <summary>
        /// Gets the room, suite number or identifier (RFC 9554 §2.1).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Room => GetComponents()[RoomComponent];

        /// <summary>
        /// Gets the apartment number, extension designation or box number (RFC 9554 §2.1).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Apartment => GetComponents()[ApartmentComponent];

        /// <summary>
        /// Gets the building floor or level (RFC 9554 §2.1).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Floor => GetComponents()[FloorComponent];

        /// <summary>
        /// Gets the street number (RFC 9554 §2.1).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> StreetNumber => GetComponents()[StreetNumberComponent];

        /// <summary>
        /// Gets the street name (RFC 9554 §2.1).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> StreetName => GetComponents()[StreetNameComponent];

        /// <summary>
        /// Gets the building, tower or condominium (RFC 9554 §2.1).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Building => GetComponents()[BuildingComponent];

        /// <summary>
        /// Gets the block name or number (RFC 9554 §2.1).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Block => GetComponents()[BlockComponent];

        /// <summary>
        /// Gets the subdistrict (RFC 9554 §2.1).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Subdistrict => GetComponents()[SubdistrictComponent];

        /// <summary>
        /// Gets the district (RFC 9554 §2.1).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> District => GetComponents()[DistrictComponent];

        /// <summary>
        /// Gets the landmark or other publicly known prominent feature (RFC 9554 §2.1).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Landmark => GetComponents()[LandmarkComponent];

        /// <summary>
        /// Gets the cardinal direction or quadrant (RFC 9554 §2.1).
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Direction => GetComponents()[DirectionComponent];

        /// <summary>
        /// Gets a flag indicating if the address has a value in one of the components of RFC 9554.  ref. RFC 9554 §2.1: if so, a reader "SHOULD ignore the street component", which is then
        /// there for backwards compatibility.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public bool HasDetailedComponents => !GetComponents().AreEmptyFrom(Rfc6350ComponentCount);

        /// <summary>
        /// Gets the components, for use within the package; or <c>null</c> if the instance is not initialised.
        /// </summary>
        internal VcardComponentList Components => _components;

        /// <summary>
        /// Gets this address with its street address made from the components of RFC 9554, if it has none, so that a consumer that doesn't know RFC 9554 can read them.  ref. RFC 9554 §2.1:
        /// "Implementations SHOULD write a combined value of these components in the street address component for backwards compatibility".
        /// </summary>
        /// <remarks>
        /// <para>
        /// The RFC doesn't say how to combine them, and what reads best depends on the country.  This joins them with a space: the street number, street name, direction, building, block,
        /// floor, apartment, room, landmark, subdistrict and district.  To say otherwise, give the address a street address, which this leaves as it is.
        /// </para>
        /// <para>
        /// <see cref="VcardCard.Addresses"/> applies this when it adds an address.  To write an address without it, add a <see cref="VcardContentLine"/> to <see cref="VcardCard.Properties"/>.
        /// </para>
        /// </remarks>
        /// <returns>The address.</returns>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public VcardAddress WithBackwardCompatibleComponents()
        {
            var components = GetComponents();
            if (components[StreetAddressComponent].Count != 0) return this;

            var street = new StringBuilder();
            foreach (var part in StreetAddressParts)
            {
                foreach (var item in components[part])
                {
                    if (item.Length == 0) continue;

                    if (street.Length != 0)
                    {
                        street.Append(' ');
                    }

                    street.Append(item);
                }
            }

            return street.Length == 0 ? this : new VcardAddress(components.WithItems(StreetAddressComponent, new[] { street.ToString() }));
        }

        /// <summary>
        /// Gets a flag indicating if a version can write this address.  vCard 3.0 and vCard 2.1 have no RFC 9554 components and one item in a component, and vCard 2.1 can't end one
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
                    return components.AreEmptyFrom(Rfc6350ComponentCount) && components.HasSingleItems;
                case VcardVersion.V2_1:
                    return components.AreEmptyFrom(Rfc6350ComponentCount) && components.HasSingleItems && components.CanWriteInVersion21;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Parses a vCard 4.0 address, such as <c>;;123 Main Street;Any Town;CA;91921-1234;U.S.A.</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The address, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardAddress result)
        {
            return TryParse(value, format, VcardVersion.V4_0, out result);
        }

        /// <summary>
        /// Parses an address in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="result">The address, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardAddress result)
        {
            result = default(VcardAddress);
            VcardStructuredValueView view;
            if (!TryParseView(value, format, version, out view)) return false;

            result = new VcardAddress(VcardComponentList.Parse(value, version, version == VcardVersion.V4_0, Rfc9554ComponentCount));
            return true;
        }

        /// <summary>
        /// Parses a vCard 4.0 address without allocating, into a view of its components.
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
        /// Parses an address in the form a version uses without allocating, into a view of its components.
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

            // ref. RFC 2426 §4: adr-value = 0*6(text-value ";") text-value
            // ref. vCard 2.1 §2.9: addressparts = 0*6(strnosemi ";") strnosemi
            result = default(VcardStructuredValueView);
            var hasLists = version == VcardVersion.V4_0;
            int componentCount;
            if (!VcardText.TryCountComponents(value, version, hasLists, out componentCount)) return false;

            var isValidCount = hasLists
                ? componentCount == Rfc6350ComponentCount || componentCount == Rfc9554ComponentCount
                : componentCount <= Rfc6350ComponentCount;
            if (!isValidCount) return false;

            result = new VcardStructuredValueView(value, componentCount, version, hasLists);
            return true;
        }

        /// <summary>
        /// Parses a vCard 4.0 address.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The address.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardAddress Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            return Parse(value, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Parses an address in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The address.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardAddress Parse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version)
        {
            VcardAddress result;
            if (!TryParse(value, format, version, out result))
                throw new FormatException("The value is not a valid ADR value.");

            return result;
        }

        /// <summary>
        /// Formats the address into a span in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the address was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            return TryFormat(destination, out charsWritten, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the address into a span in the form a version uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns><c>true</c> if the address was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this address; see <see cref="IsDefinedIn"/>.</exception>
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
                throw VcardValueFormatting.BuildNotDefinedException("ADR", version);

            var writer = new VcardSpanWriter(destination);
            _components.Append(ref writer, version, _components.AreEmptyFrom(Rfc6350ComponentCount) ? Rfc6350ComponentCount : Rfc9554ComponentCount);
            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the address as a string in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public string ToString(VcardValueFormat format)
        {
            return ToString(format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the address as a string in the form a version uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this address; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format, VcardVersion version)
        {
            var buffer = new char[_components is null ? 0 : _components.MaxLength];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format, version), charsWritten);
        }

        /// <summary>
        /// Formats the address in <see cref="VcardValueFormat.Text"/> form, as vCard 4.0 writes it; or gives the type name if the instance is not initialised.
        /// </summary>
        public override string ToString()
        {
            return _components is null ? typeof(VcardAddress).FullName ?? nameof(VcardAddress) : ToString(VcardValueFormat.Text);
        }

        /// <summary>
        /// Indicates whether this address equals another: the same items in the same components, compared ordinally.
        /// </summary>
        public bool Equals(VcardAddress other)
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
            return obj is VcardAddress other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _components is null ? 0 : _components.GetListHashCode();
        }

        /// <summary>
        /// Indicates whether two addresses are equal.
        /// </summary>
        public static bool operator ==(VcardAddress left, VcardAddress right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two addresses differ.
        /// </summary>
        public static bool operator !=(VcardAddress left, VcardAddress right)
        {
            return !left.Equals(right);
        }

        private VcardComponentList GetComponents()
        {
            return _components ?? throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
