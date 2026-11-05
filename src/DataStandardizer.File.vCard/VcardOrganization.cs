using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The structured value of the ORG property (RFC 6350 §6.6.4): an organization name followed by zero or more levels of organizational unit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A component holds one piece of text, not a list, so in vCard 4.0 and vCard 3.0 a comma in one has to be escaped.  In vCard 2.1 a comma is ordinary text.
    /// </para>
    /// <para>
    /// This type holds copies of its text.  To read a value without allocating, use <see cref="TryParseView(ReadOnlySpan{char}, VcardValueFormat, out VcardStructuredValueView)"/>.
    /// </para>
    /// <para>
    /// A default instance is not initialised: <see cref="Name"/>, <see cref="Units"/> and <see cref="IsDefinedIn"/> throw <see cref="InvalidOperationException"/>,
    /// <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// <para>
    /// In jCard (RFC 7095 §3.3.1.3) the value is a JSON array with an element for each component, not a single string, so it has no <see cref="VcardValueFormat.Json"/> form of
    /// its own, and parsing or formatting in that form throws <see cref="NotSupportedException"/>.  <c>JcardReader</c> and <c>JcardWriter</c> map the array to and from the text form.
    /// </para>
    /// </remarks>
    public readonly struct VcardOrganization : IEquatable<VcardOrganization>
    {
        private const string UninitializedMessage = "The VcardOrganization instance is not initialised.";

        private readonly string _name;
        private readonly ReadOnlyCollection<string> _units;

        /// <summary>
        /// Creates an organization.
        /// </summary>
        /// <param name="name">The organization name.</param>
        /// <param name="units">The organizational units, the highest level first.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="units"/> or a unit is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The text holds a control character other than a tab or a line break, or a lone surrogate.</exception>
        public VcardOrganization(string name, IEnumerable<string> units)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (units is null)
                throw new ArgumentNullException(nameof(units));

            VcardText.CheckCharacters(name.AsSpan(), nameof(name));
            var copy = new List<string>(units);
            foreach (var unit in copy)
            {
                if (unit is null)
                    throw new ArgumentNullException(nameof(units), "A unit is null.");

                VcardText.CheckCharacters(unit.AsSpan(), nameof(units));
            }

            _name = name;
            _units = new ReadOnlyCollection<string>(copy);
        }

        private VcardOrganization(string name, ReadOnlyCollection<string> units)
        {
            _name = name;
            _units = units;
        }

        /// <summary>
        /// Gets the organization name.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public string Name => _name ?? throw new InvalidOperationException(UninitializedMessage);

        /// <summary>
        /// Gets the organizational units, the highest level first.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public IReadOnlyList<string> Units => _units ?? throw new InvalidOperationException(UninitializedMessage);

        /// <summary>
        /// Gets a flag indicating if a version can write this organization.  vCard 2.1 can't end a component with a backslash.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        public bool IsDefinedIn(VcardVersion version)
        {
            if (_name is null)
                throw new InvalidOperationException(UninitializedMessage);

            VcardValueFormatting.CheckVersion(version);

            if (version != VcardVersion.V2_1) return true;
            if (!VcardText.CanWriteInVersion21Component(_name)) return false;

            foreach (var unit in _units)
            {
                if (!VcardText.CanWriteInVersion21Component(unit)) return false;
            }

            return true;
        }

        /// <summary>
        /// Parses a vCard 4.0 organization, such as <c>ABC\, Inc.;North American Division;Marketing</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The organization, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardOrganization result)
        {
            return TryParse(value, format, VcardVersion.V4_0, out result);
        }

        /// <summary>
        /// Parses an organization in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="result">The organization, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardOrganization result)
        {
            result = default(VcardOrganization);
            VcardStructuredValueView view;
            if (!TryParseView(value, format, version, out view)) return false;

            var end = VcardText.IndexOfDelimiter(value, ';', version);
            var name = VcardText.Unescape(value.Slice(0, end), version);
            var units = new string[view.ComponentCount - 1];
            for (var index = 0; index < units.Length; index++)
            {
                value = value.Slice(end + 1);
                end = VcardText.IndexOfDelimiter(value, ';', version);
                units[index] = VcardText.Unescape(value.Slice(0, end), version);
            }

            result = new VcardOrganization(name, new ReadOnlyCollection<string>(units));
            return true;
        }

        /// <summary>
        /// Parses a vCard 4.0 organization without allocating, into a view of its components.
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
        /// Parses an organization in the form a version uses without allocating, into a view of its components.
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
            VcardValueFormatting.CheckStructuredFormat(format, "An ORG value");
            VcardValueFormatting.CheckVersion(version);

            // ref. RFC 6350 §6.6.4: ORG-value = component *(";" component)
            // ref. RFC 2426 §4: org-value = *(text-value ";") text-value
            // ref. vCard 2.1 §2.9: orgparts = *(strnosemi ";") strnosemi
            result = default(VcardStructuredValueView);
            int componentCount;
            if (!VcardText.TryCountComponents(value, version, false, out componentCount)) return false;

            result = new VcardStructuredValueView(value, componentCount, version, false);
            return true;
        }

        /// <summary>
        /// Parses a vCard 4.0 organization.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The organization.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardOrganization Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            return Parse(value, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Parses an organization in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The organization.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardOrganization Parse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version)
        {
            VcardOrganization result;
            if (!TryParse(value, format, version, out result))
                throw new FormatException("The value is not a valid ORG value.");

            return result;
        }

        /// <summary>
        /// Formats the organization into a span in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the organization was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            return TryFormat(destination, out charsWritten, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the organization into a span in the form a version uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns><c>true</c> if the organization was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this organization; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format, VcardVersion version)
        {
            VcardValueFormatting.CheckStructuredFormat(format, "An ORG value");
            VcardValueFormatting.CheckVersion(version);

            if (_name is null)
            {
                charsWritten = 0;
                return true;
            }

            if (!IsDefinedIn(version))
                throw VcardValueFormatting.BuildNotDefinedException("ORG", version);

            var writer = new VcardSpanWriter(destination);
            VcardText.AppendEscaped(ref writer, _name.AsSpan(), version, true);
            foreach (var unit in _units)
            {
                writer.Append(';');
                VcardText.AppendEscaped(ref writer, unit.AsSpan(), version, true);
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the organization as a string in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public string ToString(VcardValueFormat format)
        {
            return ToString(format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the organization as a string in the form a version uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this organization; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format, VcardVersion version)
        {
            var length = 0;
            if (_name != null)
            {
                length = VcardText.GetMaxEscapedLength(_name.Length);
                foreach (var unit in _units)
                {
                    length += VcardText.GetMaxEscapedLength(unit.Length) + 1;
                }
            }

            var buffer = new char[length];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format, version), charsWritten);
        }

        /// <summary>
        /// Formats the organization in <see cref="VcardValueFormat.Text"/> form, as vCard 4.0 writes it; or gives the type name if the instance is not initialised.
        /// </summary>
        public override string ToString()
        {
            return _name is null ? typeof(VcardOrganization).FullName ?? nameof(VcardOrganization) : ToString(VcardValueFormat.Text);
        }

        /// <summary>
        /// Indicates whether this organization equals another: the same name and units, compared ordinally.
        /// </summary>
        public bool Equals(VcardOrganization other)
        {
            if (!string.Equals(_name, other._name, StringComparison.Ordinal)) return false;
            if (_name is null) return true;
            if (_units.Count != other._units.Count) return false;

            for (var index = 0; index < _units.Count; index++)
            {
                if (!string.Equals(_units[index], other._units[index], StringComparison.Ordinal)) return false;
            }

            return true;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardOrganization other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (_name is null) return 0;

            var hash = StringComparer.Ordinal.GetHashCode(_name);
            foreach (var unit in _units)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(unit);
            }

            return hash;
        }

        /// <summary>
        /// Indicates whether two organizations are equal.
        /// </summary>
        public static bool operator ==(VcardOrganization left, VcardOrganization right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two organizations differ.
        /// </summary>
        public static bool operator !=(VcardOrganization left, VcardOrganization right)
        {
            return !left.Equals(right);
        }
    }
}
