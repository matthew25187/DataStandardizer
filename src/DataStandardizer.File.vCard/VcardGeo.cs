using System;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The value of the GEO property: a URI in vCard 4.0 (RFC 6350 §6.5.2), and a latitude and longitude in vCard 3.0 (RFC 2426 §3.4.2) and vCard 2.1 (vCard 2.1 §2.4.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is held in the form vCard 4.0 uses, as a URI.  RFC 6350 §6.5.2 says the <c>geo</c> scheme of RFC 5870 is particularly well suited but other schemes may be used, so
    /// a URI of another scheme is valid and <see cref="Latitude"/> and <see cref="Longitude"/> are <c>null</c> for it.  They are <c>null</c> too for a <c>geo</c> URI whose
    /// <c>crs</c> parameter names a system other than <c>wgs84</c>, since RFC 5870 §3.4.2 gives them no meaning there.
    /// </para>
    /// <para>
    /// vCard 3.0 separates the two numbers with a semicolon (<c>37.386013;-122.082932</c>), and vCard 2.1 with a comma (<c>37.24,-17.87</c>).  The digits are kept as they were
    /// written when one form is turned into another.  Those versions can write only a plain latitude and longitude; see <see cref="IsDefinedIn"/>.
    /// </para>
    /// <para>
    /// Parsing allocates, since the URI is held as a string.  A default instance is not initialised: its properties and <see cref="IsDefinedIn"/> throw
    /// <see cref="InvalidOperationException"/>, <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// </remarks>
    public readonly struct VcardGeo : IEquatable<VcardGeo>
    {
        private const string UninitializedMessage = "The VcardGeo instance is not initialised.";
        private const string GeoScheme = "GEO";
        private const string GeoPrefix = "geo:";
        private const string CrsParameter = "CRS";
        private const string UncertaintyParameter = "U";
        private const string Wgs84 = "WGS84";

        // ref. RFC 5870 §3.4.2: "Latitudes range from -90 to 90 and longitudes range from -180 to 180."
        private const double MaxLatitude = 90;
        private const double MaxLongitude = 180;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly string? _uri;
#else
        private readonly string _uri;
#endif
        private readonly double? _latitude;
        private readonly double? _longitude;
        private readonly double? _altitude;
        private readonly double? _uncertainty;
        private readonly bool _isPlain;

        /// <summary>
        /// Creates a value from a latitude and longitude in WGS 84.
        /// </summary>
        /// <param name="latitude">The latitude, from -90 to 90.</param>
        /// <param name="longitude">The longitude, from -180 to 180.</param>
        /// <exception cref="ArgumentOutOfRangeException">The latitude or longitude is out of range, or is NaN.</exception>
        public VcardGeo(double latitude, double longitude)
        {
            if (!(latitude >= -MaxLatitude && latitude <= MaxLatitude))
                throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be in the range -90 - 90.");
            if (!(longitude >= -MaxLongitude && longitude <= MaxLongitude))
                throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be in the range -180 - 180.");

            _uri = GeoPrefix + VcardValue.FormatFloatText(latitude) + "," + VcardValue.FormatFloatText(longitude);
            _latitude = latitude;
            _longitude = longitude;
            _altitude = null;
            _uncertainty = null;
            _isPlain = true;
        }

        private VcardGeo(string uri, double? latitude, double? longitude, double? altitude, double? uncertainty, bool isPlain)
        {
            _uri = uri;
            _latitude = latitude;
            _longitude = longitude;
            _altitude = altitude;
            _uncertainty = uncertainty;
            _isPlain = isPlain;
        }

        /// <summary>
        /// Gets the text of the URI, the form vCard 4.0 writes.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public string UriText => _uri ?? throw new InvalidOperationException(UninitializedMessage);

        /// <summary>
        /// Gets the latitude; or <c>null</c> if the URI doesn't give one in WGS 84.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public double? Latitude
        {
            get
            {
                CheckInitialized();
                return _latitude;
            }
        }

        /// <summary>
        /// Gets the longitude; or <c>null</c> if the URI doesn't give one in WGS 84.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public double? Longitude
        {
            get
            {
                CheckInitialized();
                return _longitude;
            }
        }

        /// <summary>
        /// Gets the altitude in metres (RFC 5870 §3.4.2); or <c>null</c> if the URI doesn't give one in WGS 84.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public double? Altitude
        {
            get
            {
                CheckInitialized();
                return _altitude;
            }
        }

        /// <summary>
        /// Gets the uncertainty in metres, the <c>u</c> parameter of RFC 5870 §3.4.3; or <c>null</c> if the URI has none.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public double? Uncertainty
        {
            get
            {
                CheckInitialized();
                return _uncertainty;
            }
        }

        /// <summary>
        /// Gets a flag indicating if a version can write this value.  vCard 4.0 can write them all, and vCard 3.0 and vCard 2.1 only a <c>geo</c> URI of a latitude and a longitude
        /// with nothing else.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        public bool IsDefinedIn(VcardVersion version)
        {
            CheckInitialized();
            VcardValueFormatting.CheckVersion(version);

            return version == VcardVersion.V4_0 || _isPlain;
        }

        /// <summary>
        /// Parses a vCard 4.0 value as it appears in a content line, such as <c>geo:37.386013\,-122.082932</c>, with its comma escaped as RFC 6350 §3.4 and its erratum 3846 say.  To read
        /// a <see cref="Uri"/>, such as one from <see cref="VcardCard.GeoLocations"/>, use <see cref="TryParse(Uri, out VcardGeo)"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The value, if it is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardGeo result)
        {
            return TryParse(value, format, VcardVersion.V4_0, out result);
        }

        /// <summary>
        /// Parses a value in the form a version uses.
        /// </summary>
        /// <param name="value">The value: a URI in vCard 4.0, with its comma escaped, <c>37.386013;-122.082932</c> in vCard 3.0 and <c>37.24,-17.87</c> in vCard 2.1.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="result">The value, if it is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, out VcardGeo result)
        {
            VcardValueFormatting.CheckFormat(format);
            VcardValueFormatting.CheckVersion(version);

            return version == VcardVersion.V4_0 ? TryParseUri(value, format, out result) : TryParseLegacy(value, version, out result);
        }

        /// <summary>
        /// Reads a URI, such as one from <see cref="VcardCard.GeoLocations"/>.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="result">The value, if the URI is valid.</param>
        /// <returns><c>true</c> if the URI is valid: absolute, and if it is of the <c>geo</c> scheme, a <c>geo</c> URI of RFC 5870; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
        public static bool TryParse(Uri uri, out VcardGeo result)
        {
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            result = default(VcardGeo);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Uri? parsed;
#else
            Uri parsed;
#endif
            return uri.IsAbsoluteUri && VcardValue.TryParseUriText(uri.OriginalString.AsSpan(), out parsed) && TryCreateFromUri(parsed, out result);
        }

        /// <summary>
        /// Parses a vCard 4.0 value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardGeo Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            return Parse(value, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Parses a value in the form a version uses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardGeo Parse(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version)
        {
            VcardGeo result;
            if (!TryParse(value, format, version, out result))
                throw new FormatException("The value is not a valid GEO value.");

            return result;
        }

        /// <summary>
        /// Formats the value into a span in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            return TryFormat(destination, out charsWritten, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the value into a span in the form a version uses.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this value; see <see cref="IsDefinedIn"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format, VcardVersion version)
        {
            VcardValueFormatting.CheckFormat(format);
            VcardValueFormatting.CheckVersion(version);

            if (_uri is null)
            {
                charsWritten = 0;
                return true;
            }

            if (!IsDefinedIn(version))
                throw VcardValueFormatting.BuildNotDefinedException("GEO", version);

            var writer = new VcardSpanWriter(destination);
            if (version == VcardVersion.V4_0)
            {
                writer.Append(VcardValue.EscapeUri(_uri).AsSpan());
            }
            else
            {
                var coordinates = _uri.AsSpan(GeoPrefix.Length);
                var commaIndex = coordinates.IndexOf(',');
                writer.Append(coordinates.Slice(0, commaIndex));
                writer.Append(version == VcardVersion.V3_0 ? ';' : ',');
                writer.Append(coordinates.Slice(commaIndex + 1));
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the value as a string in the form vCard 4.0 uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public string ToString(VcardValueFormat format)
        {
            return ToString(format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats the value as a string in the form a version uses.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="version"/> has no form for this value; see <see cref="IsDefinedIn"/>.</exception>
        public string ToString(VcardValueFormat format, VcardVersion version)
        {
            // A vCard 4.0 value is the longest form, with a backslash before each comma.
            var buffer = new char[_uri is null ? 0 : VcardValue.EscapeUri(_uri).Length];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format, version), charsWritten);
        }

        /// <summary>
        /// Formats the value in <see cref="VcardValueFormat.Text"/> form, as vCard 4.0 writes it; or gives the type name if the instance is not initialised.
        /// </summary>
        public override string ToString()
        {
            return _uri is null ? typeof(VcardGeo).FullName ?? nameof(VcardGeo) : VcardValue.EscapeUri(_uri);
        }

        /// <summary>
        /// Indicates whether this value equals another: the same text of the URI, compared ordinally.
        /// </summary>
        public bool Equals(VcardGeo other)
        {
            return string.Equals(_uri, other._uri, StringComparison.Ordinal);
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardGeo other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _uri is null ? 0 : StringComparer.Ordinal.GetHashCode(_uri);
        }

        /// <summary>
        /// Indicates whether two values are equal.
        /// </summary>
        public static bool operator ==(VcardGeo left, VcardGeo right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two values differ.
        /// </summary>
        public static bool operator !=(VcardGeo left, VcardGeo right)
        {
            return !left.Equals(right);
        }

        private static bool TryParseUri(ReadOnlySpan<char> value, VcardValueFormat format, out VcardGeo result)
        {
            result = default(VcardGeo);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Uri? uri;
#else
            Uri uri;
#endif
            return VcardValue.TryParseUri(value, format, out uri) && TryCreateFromUri(uri, out result);
        }

        private static bool TryCreateFromUri(Uri uri, out VcardGeo result)
        {
            // ref. RFC 6350 §6.5.2: "other schemes MAY be used".  A value that says it is a geo URI has to be one.
            var text = uri.OriginalString.AsSpan();
            var colonIndex = text.IndexOf(':');
            if (!VcardValueFormatting.EqualsKeyword(text.Slice(0, colonIndex), GeoScheme))
            {
                result = new VcardGeo(uri.OriginalString, null, null, null, null, false);
                return true;
            }

            return TryParseGeoPath(text.Slice(colonIndex + 1), uri.OriginalString, out result);
        }

        private static bool TryParseGeoPath(ReadOnlySpan<char> path, string uri, out VcardGeo result)
        {
            // ref. RFC 5870 §3.3: geo-path = coordinates p; coordinates = coord-a "," coord-b [ "," coord-c ]; p = [ crsp ] [ uncp ] *parameter
            result = default(VcardGeo);
            var parametersIndex = path.IndexOf(';');
            var coordinates = parametersIndex < 0 ? path : path.Slice(0, parametersIndex);

            var firstComma = coordinates.IndexOf(',');
            if (firstComma < 0) return false;

            var rest = coordinates.Slice(firstComma + 1);
            var secondComma = rest.IndexOf(',');
            var hasAltitude = secondComma >= 0;
            double first, second, third = 0;
            if (!VcardValue.TryParseFloatCore(coordinates.Slice(0, firstComma), false, out first) ||
                !VcardValue.TryParseFloatCore(hasAltitude ? rest.Slice(0, secondComma) : rest, false, out second) ||
                (hasAltitude && !VcardValue.TryParseFloatCore(rest.Slice(secondComma + 1), false, out third)))
            {
                return false;
            }

            var isWgs84 = true;
            double? uncertainty = null;
            if (parametersIndex >= 0 && !TryParseParameters(path.Slice(parametersIndex + 1), out isWgs84, out uncertainty)) return false;

            if (!isWgs84)
            {
                result = new VcardGeo(uri, null, null, null, uncertainty, false);
                return true;
            }

            if (Math.Abs(first) > MaxLatitude || Math.Abs(second) > MaxLongitude) return false;

            result = new VcardGeo(uri, first, second, hasAltitude ? third : (double?)null, uncertainty, !hasAltitude && parametersIndex < 0);
            return true;
        }

        private static bool TryParseParameters(ReadOnlySpan<char> parameters, out bool isWgs84, out double? uncertainty)
        {
            isWgs84 = true;
            uncertainty = null;
            var position = 0;
            var hasCrs = false;
            while (true)
            {
                var end = parameters.IndexOf(';');
                var parameter = end < 0 ? parameters : parameters.Slice(0, end);
                var equalsIndex = parameter.IndexOf('=');
                var name = equalsIndex < 0 ? parameter : parameter.Slice(0, equalsIndex);
                var value = equalsIndex < 0 ? default(ReadOnlySpan<char>) : parameter.Slice(equalsIndex + 1);

                // ref. RFC 5870 §3.3: parameter = ";" pname [ "=" pvalue ]; pname = labeltext; labeltext = 1*( alphanum / "-" ); pvalue = 1*paramchar
                if (!IsLabelText(name) || (equalsIndex >= 0 && !IsParameterValue(value))) return false;

                if (VcardValueFormatting.EqualsKeyword(name, CrsParameter))
                {
                    // ref. RFC 5870 §3.3: "Both 'crs' and 'u' parameters MUST NOT appear more than once each", and p = [ crsp ] [ uncp ] *parameter.
                    if (position != 0 || !IsLabelText(value)) return false;

                    hasCrs = true;
                    isWgs84 = VcardValueFormatting.EqualsKeyword(value, Wgs84);
                }
                else if (VcardValueFormatting.EqualsKeyword(name, UncertaintyParameter))
                {
                    double parsed;
                    if (position != (hasCrs ? 1 : 0) || !VcardValue.TryParseFloatCore(value, false, out parsed) || value[0] == '-') return false;

                    uncertainty = parsed;
                }

                position++;
                if (end < 0) return true;

                parameters = parameters.Slice(end + 1);
            }
        }

        private static bool TryParseLegacy(ReadOnlySpan<char> value, VcardVersion version, out VcardGeo result)
        {
            // ref. RFC 2426 §4: value = float-value ";" float-value
            // ref. vCard 2.1 §2.4.6: "GEO:37.24,-17.87"
            result = default(VcardGeo);
            var delimiterIndex = value.IndexOf(version == VcardVersion.V3_0 ? ';' : ',');
            if (delimiterIndex < 0) return false;

            var latitudeText = value.Slice(0, delimiterIndex);
            var longitudeText = value.Slice(delimiterIndex + 1);
            double latitude, longitude;
            if (!VcardValue.TryParseFloatCore(latitudeText, true, out latitude) || !VcardValue.TryParseFloatCore(longitudeText, true, out longitude)) return false;
            if (Math.Abs(latitude) > MaxLatitude || Math.Abs(longitude) > MaxLongitude) return false;

            // ref. RFC 5870 §3.3: num = [ "-" ] pnum, so a geo URI has no plus sign.
            var uri = GeoPrefix + WithoutPlusSign(latitudeText) + "," + WithoutPlusSign(longitudeText);
            result = new VcardGeo(uri, latitude, longitude, null, null, true);
            return true;
        }

        private static string WithoutPlusSign(ReadOnlySpan<char> number)
        {
            return (number[0] == '+' ? number.Slice(1) : number).ToString();
        }

        private static bool IsLabelText(ReadOnlySpan<char> text)
        {
            if (text.Length == 0) return false;

            foreach (var character in text)
            {
                var isAlphanumeric = VcardValueFormatting.IsDigit(character) || (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
                if (!isAlphanumeric && character != '-') return false;
            }

            return true;
        }

        // ref. RFC 5870 §3.3: paramchar = p-unreserved / unreserved / pct-encoded.  The percent-encodings have been checked as part of the URI.
        private static bool IsParameterValue(ReadOnlySpan<char> text)
        {
            if (text.Length == 0) return false;

            foreach (var character in text)
            {
                var isAlphanumeric = VcardValueFormatting.IsDigit(character) || (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
                if (!isAlphanumeric && "[]:&+$-_.!~*'()%".IndexOf(character) < 0) return false;
            }

            return true;
        }

        private void CheckInitialized()
        {
            if (_uri is null)
                throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
